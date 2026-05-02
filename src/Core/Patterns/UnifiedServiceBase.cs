// ============================================================================
// MONADO BLADE OPTIMIZATION - UNIFIED SERVICE FRAMEWORK
// Hour 3-4: Consolidates all service patterns into single reusable base
// Expected consolidation: 150+ lines across multiple service files
// ============================================================================

namespace MonadoBlade.Core.Patterns;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Unified service base class - eliminates boilerplate across ALL services.
/// CONSOLIDATION: Replaces ServiceComponentBase duplication, adds lifecycle management.
/// Benefits: Automatic health checks, metrics collection, graceful shutdown.
/// </summary>
public abstract class UnifiedServiceBase : IDisposable, IAsyncDisposable
{
    protected readonly string ServiceId;
    protected readonly AsyncConcurrencyPool ConcurrencyPool;
    protected readonly AsyncOperationMetrics Metrics;
    
    private volatile bool _isInitialized;
    private volatile bool _isDisposed;
    private readonly SemaphoreSlim _initializationLock;
    private readonly List<Func<Task>> _shutdownHooks;

    protected UnifiedServiceBase(string serviceId, int maxConcurrency = 5)
    {
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        ConcurrencyPool = new AsyncConcurrencyPool(maxConcurrency);
        Metrics = new AsyncOperationMetrics();
        _initializationLock = new SemaphoreSlim(1, 1);
        _shutdownHooks = new();
    }

    /// <summary>
    /// Idempotent initialization with double-checked locking and metrics.
    /// CONSOLIDATION: Replaces OnInitializeAsync pattern in all services.
    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return true;
        if (_isDisposed) throw new ObjectDisposedException($"{ServiceId} has been disposed");

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return true;

            var sw = Stopwatch.StartNew();
            
            try
            {
                await OnInitializeAsync(cancellationToken);
                _isInitialized = true;
                sw.Stop();
                
                Metrics.Record($"{ServiceId}.Initialize", sw.Elapsed);
                Console.WriteLine($"[{ServiceId}] Initialized in {sw.ElapsedMilliseconds}ms");
                
                return true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Metrics.Record($"{ServiceId}.InitializeFailure", sw.Elapsed);
                Console.WriteLine($"[{ServiceId}] Initialization failed: {ex.Message}");
                throw;
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>Override to implement service-specific initialization logic.</summary>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) 
        => Task.CompletedTask;

    /// <summary>
    /// Execute operation with automatic metrics collection and resilience.
    /// CONSOLIDATION: Unifies operation execution across all service implementations.
    /// </summary>
    protected async Task<AsyncOperationResult<T>> ExecuteAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? timeout = null)
    {
        using (var lease = await ConcurrencyPool.AcquireAsync($"{ServiceId}.{operationName}"))
        {
            var resilientOp = new ResilientAsyncOperation<T>(
                operation,
                $"{ServiceId}.{operationName}",
                maxRetries,
                timeout: timeout);
            
            var result = await resilientOp.ExecuteAsync();
            Metrics.Record($"{ServiceId}.{operationName}", result.Duration);
            
            return result;
        }
    }

    /// <summary>Register cleanup logic to execute on shutdown.</summary>
    protected void RegisterShutdownHook(Func<Task> hook)
    {
        _shutdownHooks.Add(hook ?? throw new ArgumentNullException(nameof(hook)));
    }

    /// <summary>Graceful shutdown with all cleanup hooks.</summary>
    public async Task ShutdownAsync()
    {
        if (_isDisposed) return;

        var sw = Stopwatch.StartNew();
        
        try
        {
            foreach (var hook in _shutdownHooks)
            {
                try
                {
                    await hook();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{ServiceId}] Shutdown hook failed: {ex.Message}");
                }
            }

            await OnShutdownAsync();
            sw.Stop();
            
            Metrics.Record($"{ServiceId}.Shutdown", sw.Elapsed);
            Console.WriteLine($"[{ServiceId}] Shutdown completed in {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"[{ServiceId}] Shutdown error: {ex.Message}");
            throw;
        }
    }

    /// <summary>Override for service-specific shutdown logic.</summary>
    protected virtual Task OnShutdownAsync() => Task.CompletedTask;

    /// <summary>Health check endpoint - returns service status.</summary>
    public virtual async Task<ServiceHealth> GetHealthAsync()
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var health = await OnHealthCheckAsync();
            sw.Stop();
            
            return new ServiceHealth
            {
                ServiceId = ServiceId,
                IsHealthy = health,
                LastCheckTime = DateTime.UtcNow,
                CheckDuration = sw.Elapsed,
                ActiveConcurrency = ConcurrencyPool.ActiveCount,
                WaitingOperations = ConcurrencyPool.WaitingCount
            };
        }
        catch
        {
            return new ServiceHealth
            {
                ServiceId = ServiceId,
                IsHealthy = false,
                LastCheckTime = DateTime.UtcNow,
                CheckDuration = sw.Elapsed
            };
        }
    }

    /// <summary>Override for custom health check logic.</summary>
    protected virtual Task<bool> OnHealthCheckAsync() => Task.FromResult(true);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
        _initializationLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        await ShutdownAsync();
        _initializationLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Print comprehensive metrics report.</summary>
    public void PrintMetrics()
    {
        Console.WriteLine($"\n=== SERVICE METRICS: {ServiceId} ===");
        Metrics.PrintReport();
    }
}

/// <summary>Health status information for service monitoring.</summary>
public sealed class ServiceHealth
{
    public string ServiceId { get; init; }
    public bool IsHealthy { get; init; }
    public DateTime LastCheckTime { get; init; }
    public TimeSpan CheckDuration { get; init; }
    public int ActiveConcurrency { get; init; }
    public int WaitingOperations { get; init; }
}

/// <summary>
/// Simplified generic resilient operation - provides better type safety.
/// </summary>
public sealed class ResilientAsyncOperation<T>
{
    private readonly Func<CancellationToken, Task<T>> _operation;
    private readonly string _operationName;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;
    private readonly TimeSpan _timeout;
    private int _circuitBreakerOpenCount;
    private DateTime _circuitBreakerResetTime;
    private const int CircuitBreakerThreshold = 5;

    public ResilientAsyncOperation(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        TimeSpan? timeout = null)
    {
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        _operationName = operationName;
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        _backoffMultiplier = backoffMultiplier;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public async Task<AsyncOperationResult<T>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_circuitBreakerOpenCount >= CircuitBreakerThreshold)
        {
            if (DateTime.UtcNow - _circuitBreakerResetTime < TimeSpan.FromSeconds(10))
            {
                return AsyncOperationResult<T>.Failure(
                    new InvalidOperationException("Circuit breaker open"),
                    TimeSpan.Zero,
                    _maxRetries,
                    _operationName);
            }
            _circuitBreakerOpenCount = 0;
        }

        var sw = Stopwatch.StartNew();
        TimeSpan delay = _initialDelay;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(_timeout);
                    var result = await _operation(cts.Token).ConfigureAwait(false);
                    _circuitBreakerOpenCount = 0;
                    sw.Stop();
                    return AsyncOperationResult<T>.Success(result, sw.Elapsed, attempt, _operationName);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                sw.Stop();
                _circuitBreakerOpenCount++;
                return AsyncOperationResult<T>.Failure(ex, sw.Elapsed, attempt, _operationName);
            }
            catch (Exception ex)
            {
                _circuitBreakerOpenCount++;
                if (attempt < _maxRetries)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _backoffMultiplier);
                }
                else
                {
                    sw.Stop();
                    _circuitBreakerResetTime = DateTime.UtcNow;
                    return AsyncOperationResult<T>.Failure(ex, sw.Elapsed, attempt, _operationName);
                }
            }
        }

        sw.Stop();
        return AsyncOperationResult<T>.Failure(
            new InvalidOperationException("Operation exhausted retries"),
            sw.Elapsed,
            _maxRetries,
            _operationName);
    }
}

/// <summary>
/// Unified logging interface - consolidates all logging implementations.
/// CONSOLIDATION: Replaces ILoggingProvider duplication.
/// </summary>
public interface IUnifiedLogger
{
    void Debug(string message, Exception ex = null);
    void Information(string message);
    void Warning(string message, Exception ex = null);
    void Error(string message, Exception ex = null);
    void Critical(string message, Exception ex = null);
}

/// <summary>Simple console-based unified logger implementation.</summary>
public sealed class ConsoleUnifiedLogger : IUnifiedLogger
{
    private readonly string _source;

    public ConsoleUnifiedLogger(string source = "MonadoBlade")
    {
        _source = source;
    }

    public void Debug(string message, Exception ex = null) 
        => Log("DEBUG", message, ex);

    public void Information(string message) 
        => Log("INFO", message, null);

    public void Warning(string message, Exception ex = null) 
        => Log("WARN", message, ex);

    public void Error(string message, Exception ex = null) 
        => Log("ERROR", message, ex);

    public void Critical(string message, Exception ex = null) 
        => Log("CRIT", message, ex);

    private void Log(string level, string message, Exception ex)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [{_source}] {level,5} | {message}");
        
        if (ex != null)
        {
            Console.WriteLine($"         Exception: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"         Inner: {ex.InnerException.Message}");
            }
        }
    }
}
