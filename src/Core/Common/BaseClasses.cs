namespace MonadoBlade.Core.Common;

using System.Collections.Concurrent;

/// <summary>
/// Base class for all service components. Implements lifecycle, health, and context management.
/// Eliminates boilerplate across all 4 tracks.
/// </summary>
public abstract class ServiceComponentBase : IServiceComponent
{
    protected readonly IServiceContext _context;
    protected readonly ILoggingProvider _logger;
    protected readonly IMetricsCollector _metrics;
    private volatile bool _isInitialized;
    private volatile bool _isShutdown;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public string ComponentId { get; }
    public string ComponentType { get; protected set; } = "Generic";

    protected ServiceComponentBase(IServiceContext context, string componentId)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ComponentId = componentId ?? throw new ArgumentNullException(nameof(componentId));
        _logger = context.Logger;
        _metrics = context.Metrics;
    }

    /// <summary>Initializes component with double-checked locking pattern.</summary>
    public async Task<Result> InitializeAsync(IServiceContext context, CancellationToken ct = default)
    {
        if (_isInitialized) return Result.Success();
        if (_isShutdown) return ErrorCode.InvalidOperation.ToFailure("Component already shut down");

        await _initializationLock.WaitAsync(ct);
        try
        {
            if (_isInitialized) return Result.Success();
            
            var startTime = DateTime.UtcNow;
            _logger.Information($"Initializing component: {ComponentId}");
            
            var result = await OnInitializeAsync(context, ct);
            if (result is Result.Failure failure)
            {
                _logger.Error($"Initialization failed for {ComponentId}: {failure.Message}");
                return failure;
            }

            _isInitialized = true;
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDuration($"component_initialization_{ComponentId}", duration);
            _logger.Information($"Component initialized successfully: {ComponentId} ({duration.TotalMilliseconds}ms)");
            
            return Result.Success();
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>Override to implement component-specific initialization.</summary>
    protected virtual Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct) =>
        Task.FromResult(Result.Success());

    /// <summary>Performs health check with automatic timing.</summary>
    public async Task<HealthStatus> GetHealthAsync(CancellationToken ct = default)
    {
        if (!_isInitialized)
            return HealthStatus.Unhealthy(ComponentId, "Component not initialized");

        if (_isShutdown)
            return HealthStatus.Unhealthy(ComponentId, "Component is shut down");

        var startTime = DateTime.UtcNow;
        try
        {
            var health = await OnGetHealthAsync(ct);
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDuration($"health_check_{ComponentId}", duration, ("state", health.State.ToString()));
            return health;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Health check failed for {ComponentId}", ex);
            return HealthStatus.Unhealthy(ComponentId, "Health check error", ex);
        }
    }

    /// <summary>Override to implement component-specific health checks.</summary>
    protected virtual Task<HealthStatus> OnGetHealthAsync(CancellationToken ct) =>
        Task.FromResult(HealthStatus.Healthy(ComponentId));

    /// <summary>Gracefully shuts down component with timeout protection.</summary>
    public async Task<Result> ShutdownAsync(CancellationToken ct = default)
    {
        if (_isShutdown) return Result.Success();
        _isShutdown = true;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30-second shutdown timeout

            _logger.Information($"Shutting down component: {ComponentId}");
            var result = await OnShutdownAsync(cts.Token);
            
            _logger.Information($"Component shut down successfully: {ComponentId}");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.Warning($"Shutdown timeout for component: {ComponentId}");
            return ErrorCode.Timeout.ToFailure("Shutdown timeout");
        }
        catch (Exception ex)
        {
            _logger.Error($"Shutdown error for component: {ComponentId}", ex);
            return ErrorCode.Unknown.ToFailure(ex.Message, ex);
        }
    }

    /// <summary>Override to implement component-specific shutdown.</summary>
    protected virtual Task<Result> OnShutdownAsync(CancellationToken ct) =>
        Task.FromResult(Result.Success());

    /// <summary>Disposes component asynchronously.</summary>
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
        _initializationLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"Component {ComponentId} not initialized");
    }

    protected void ThrowIfShutdown()
    {
        if (_isShutdown)
            throw new InvalidOperationException($"Component {ComponentId} is shut down");
    }
}

/// <summary>Base class for request/response processors with validation and logging.</summary>
public abstract class ProcessorBase<TRequest, TResponse> : ServiceComponentBase
{
    protected ProcessorBase(IServiceContext context, string componentId) 
        : base(context, componentId) { }

    /// <summary>Processes a request with automatic validation, timing, and metrics.</summary>
    public async Task<Result<TResponse>> ProcessAsync(TRequest request, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();
        ThrowIfShutdown();

        // Validate input
        var validationResult = ValidateRequest(request);
        if (validationResult is Result.Failure failure)
            return failure.ToFailure<TResponse>();

        var startTime = DateTime.UtcNow;
        try
        {
            _logger.Information($"Processing {ComponentId}: {typeof(TRequest).Name}");
            var result = await OnProcessAsync(request, ct);
            
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDuration($"process_{ComponentId}", duration, ("success", "true"));
            
            return result;
        }
        catch (OperationCanceledException ex)
        {
            _metrics.IncrementCounter($"process_{ComponentId}_cancelled");
            return ErrorCode.Timeout.ToFailure<TResponse>("Operation cancelled", ex);
        }
        catch (Exception ex)
        {
            _logger.Error($"Processing error in {ComponentId}", ex);
            _metrics.IncrementCounter($"process_{ComponentId}_error");
            return ErrorCode.Unknown.ToFailure<TResponse>(ex.Message, ex);
        }
    }

    /// <summary>Override to implement request-specific processing.</summary>
    protected abstract Task<Result<TResponse>> OnProcessAsync(TRequest request, CancellationToken ct);

    /// <summary>Override to implement request validation.</summary>
    protected virtual Result ValidateRequest(TRequest request) => Result.Success();
}

/// <summary>Base class for managing resources with pooling and disposal.</summary>
public abstract class ResourcePoolBase<T> : ServiceComponentBase where T : class, IAsyncDisposable
{
    protected readonly ConcurrentBag<T> _available = new();
    protected readonly ConcurrentBag<T> _inUse = new();
    protected readonly int _maxPoolSize;

    protected ResourcePoolBase(IServiceContext context, string componentId, int maxPoolSize = 100)
        : base(context, componentId)
    {
        _maxPoolSize = maxPoolSize;
    }

    /// <summary>Acquires resource from pool, creating if necessary.</summary>
    public async Task<Result<T>> AcquireAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (_available.TryTake(out var resource))
        {
            _inUse.Add(resource);
            return resource.ToSuccess();
        }

        if (_inUse.Count < _maxPoolSize)
        {
            try
            {
                var newResource = await CreateResourceAsync(ct);
                _inUse.Add(newResource);
                _metrics.SetGauge($"pool_{ComponentId}_size", _inUse.Count);
                return newResource.ToSuccess();
            }
            catch (Exception ex)
            {
                return ErrorCode.ResourceUnavailable.ToFailure<T>("Failed to create resource", ex);
            }
        }

        return ErrorCode.ResourceExhausted.ToFailure<T>("Pool exhausted");
    }

    /// <summary>Releases resource back to pool.</summary>
    public async Task<Result> ReleaseAsync(T resource)
    {
        if (resource == null) return Result.Success();

        if (_inUse.TryTake(resource))
        {
            _available.Add(resource);
            _metrics.SetGauge($"pool_{ComponentId}_available", _available.Count);
            return Result.Success();
        }

        await resource.DisposeAsync();
        return Result.Success();
    }

    /// <summary>Override to create new resources.</summary>
    protected abstract Task<T> CreateResourceAsync(CancellationToken ct);

    protected override async Task<Result> OnShutdownAsync(CancellationToken ct)
    {
        while (_available.TryTake(out var resource))
            await resource.DisposeAsync();

        while (_inUse.TryTake(out var resource))
            await resource.DisposeAsync();

        return Result.Success();
    }
}
