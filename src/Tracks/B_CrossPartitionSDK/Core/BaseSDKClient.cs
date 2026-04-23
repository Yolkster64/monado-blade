namespace MonadoBlade.SDKs.Core;

using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;
using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// Universal SDK Configuration - used by all 50+ SDKs
/// Eliminates boilerplate across language bindings.
/// </summary>
public class SDKConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.monado-blade.io";
    public string Region { get; set; } = "us-east-1";
    
    // Authentication
    public bool UseMutualTLS { get; set; } = true;
    public string? TLSCertPath { get; set; }
    public string? TLSKeyPath { get; set; }
    
    // Networking
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConnectionPoolSize { get; set; } = 100;
    public bool EnableCompression { get; set; } = true;
    
    // Retry Policy
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
    
    // Circuit Breaker
    public bool EnableCircuitBreaker { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 10;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromSeconds(60);
    
    // Logging & Metrics
    public bool EnableDebugLogging { get; set; } = false;
    public bool EnableMetrics { get; set; } = true;
    public string? MetricsEndpoint { get; set; }
    
    public static SDKConfiguration FromEnvironment(string prefix = "MONADO_")
    {
        var config = new SDKConfiguration
        {
            ApiKey = Environment.GetEnvironmentVariable($"{prefix}API_KEY") ?? string.Empty,
            ApiSecret = Environment.GetEnvironmentVariable($"{prefix}API_SECRET") ?? string.Empty,
            Endpoint = Environment.GetEnvironmentVariable($"{prefix}ENDPOINT") ?? "https://api.monado-blade.io",
            Region = Environment.GetEnvironmentVariable($"{prefix}REGION") ?? "us-east-1",
            UseMutualTLS = bool.TryParse(Environment.GetEnvironmentVariable($"{prefix}USE_MTLS"), out var mtls) && mtls,
        };
        return config;
    }
}

/// <summary>
/// Universal SDK Error - used by all 50+ SDKs
/// Provides typed error handling with codes and messages.
/// </summary>
public class SDKException : Exception
{
    public string ErrorCode { get; }
    public string? RequestId { get; }
    public Dictionary<string, object?> Context { get; }

    public SDKException(
        string errorCode,
        string message,
        string? requestId = null,
        Exception? innerException = null,
        Dictionary<string, object?>? context = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        RequestId = requestId;
        Context = context ?? new();
    }

    public override string ToString() =>
        $"[{ErrorCode}] {Message}" + 
        (RequestId != null ? $" (Request: {RequestId})" : "");
}

/// <summary>
/// Circuit breaker pattern - prevents cascading failures across SDKs.
/// Used by all 50+ SDKs to fail fast.
/// </summary>
public class CircuitBreaker
{
    private enum State { Closed, Open, HalfOpen }
    
    private State _state = State.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _openedTime = DateTime.MinValue;
    private readonly object _lock = new();

    public int FailureThreshold { get; }
    public TimeSpan OpenTimeout { get; }
    public string Name { get; }

    public CircuitBreaker(
        string name,
        int failureThreshold = 10,
        TimeSpan? openTimeout = null)
    {
        Name = name;
        FailureThreshold = failureThreshold;
        OpenTimeout = openTimeout ?? TimeSpan.FromSeconds(60);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_state == State.Open)
            {
                if (DateTime.UtcNow - _openedTime > OpenTimeout)
                {
                    _state = State.HalfOpen;
                    _failureCount = 0;
                }
                else
                {
                    throw new SDKException(
                        "CIRCUIT_BREAKER_OPEN",
                        $"Circuit breaker '{Name}' is open");
                }
            }
        }

        try
        {
            var result = await operation(ct);
            
            lock (_lock)
            {
                _failureCount = 0;
                if (_state == State.HalfOpen)
                    _state = State.Closed;
            }
            
            return result;
        }
        catch
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= FailureThreshold)
                {
                    _state = State.Open;
                    _openedTime = DateTime.UtcNow;
                }
            }
            throw;
        }
    }

    public CircuitBreakerState GetState()
    {
        lock (_lock)
        {
            return new CircuitBreakerState(
                _state.ToString(),
                _failureCount,
                _lastFailureTime,
                _state == State.Open ? _openedTime.Add(OpenTimeout) : null);
        }
    }

    public record CircuitBreakerState(
        string State,
        int FailureCount,
        DateTime LastFailure,
        DateTime? ResetTime);
}

/// <summary>
/// Retry policy with exponential backoff - standard across all SDKs.
/// </summary>
public class RetryPolicy
{
    public int MaxRetries { get; }
    public TimeSpan InitialDelay { get; }
    public double BackoffMultiplier { get; }
    public TimeSpan MaxDelay { get; }
    public Func<Exception, int, bool>? ShouldRetry { get; set; }

    public RetryPolicy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        TimeSpan? maxDelay = null)
    {
        MaxRetries = maxRetries;
        InitialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        BackoffMultiplier = backoffMultiplier;
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(10);
        ShouldRetry = DefaultShouldRetry;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt >= MaxRetries || !(ShouldRetry?.Invoke(ex, attempt) ?? false))
                    throw;

                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, ct);
            }
        }

        throw lastException!;
    }

    private TimeSpan CalculateDelay(int attemptNumber)
    {
        var delay = InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber);
        var jitter = new Random().NextDouble() * 0.1 * delay; // 10% jitter
        var total = TimeSpan.FromMilliseconds(Math.Min(delay + jitter, MaxDelay.TotalMilliseconds));
        return total;
    }

    private static bool DefaultShouldRetry(Exception ex, int attempt)
    {
        // Retry on transient errors
        if (ex is HttpRequestException hre && hre.StatusCode.HasValue)
        {
            return (int)hre.StatusCode! >= 500 ||
                   (int)hre.StatusCode! == 429; // Too many requests
        }
        return ex is TimeoutException or OperationCanceledException;
    }
}

/// <summary>
/// Universal base SDK client - all 50+ SDKs inherit from this.
/// Provides common functionality: auth, retry, circuit breaker, metrics, logging.
/// </summary>
public abstract class BaseSDKClient : ServiceComponentBase
{
    protected SDKConfiguration _config;
    protected CircuitBreaker _circuitBreaker;
    protected RetryPolicy _retryPolicy;
    protected HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, object> _requestCorrelationIds = new();

    protected BaseSDKClient(
        IServiceContext context,
        string componentId,
        SDKConfiguration? config = null)
        : base(context, componentId)
    {
        _config = config ?? SDKConfiguration.FromEnvironment();
        _circuitBreaker = new CircuitBreaker(
            componentId,
            _config.CircuitBreakerThreshold,
            _config.CircuitBreakerTimeout);
        _retryPolicy = new RetryPolicy(
            _config.MaxRetries,
            _config.InitialRetryDelay,
            2.0,
            _config.MaxRetryDelay);

        _httpClient = new HttpClient();
        ConfigureHttpClient();
        
        ComponentType = "SDKClient";
    }

    protected virtual void ConfigureHttpClient()
    {
        _httpClient.Timeout = _config.RequestTimeout;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", GetUserAgent());
        _httpClient.DefaultRequestHeaders.Add("X-SDK-Version", GetSDKVersion());
    }

    protected async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken ct = default)
    {
        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.Information($"{operationName} started (RequestId: {requestId})");

            var result = await _circuitBreaker.ExecuteAsync(
                token => _retryPolicy.ExecuteAsync(operation, token),
                ct);

            stopwatch.Stop();
            _logger.Information(
                $"{operationName} completed in {stopwatch.ElapsedMilliseconds}ms (RequestId: {requestId})");

            RecordMetric($"sdk_{GetSDKName()}_success", operationName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(
                $"{operationName} failed: {ex.Message} (RequestId: {requestId})",
                ex);

            RecordMetric($"sdk_{GetSDKName()}_error", operationName, stopwatch.ElapsedMilliseconds);
            
            throw new SDKException(
                "SDK_OPERATION_FAILED",
                $"Failed to {operationName}: {ex.Message}",
                requestId,
                ex);
        }
    }

    protected void RecordMetric(
        string metricName,
        string operation,
        long elapsedMs)
    {
        if (!_config.EnableMetrics)
            return;

        _metrics.RecordDuration(
            metricName,
            TimeSpan.FromMilliseconds(elapsedMs),
            ("operation", operation), 
            ("sdk", GetSDKName()));
    }

    protected virtual string GetSDKName() => ComponentId;
    protected virtual string GetSDKVersion() => "1.0.0";
    protected virtual string GetUserAgent() => $"MonadoSDK/{GetSDKVersion()}";

    public CircuitBreakerState GetCircuitBreakerStatus() =>
        _circuitBreaker.GetState();

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        var cbState = _circuitBreaker.GetState();
        
        return cbState.State == "Open"
            ? HealthStatus.Degraded(ComponentId, $"Circuit breaker open ({cbState.FailureCount} failures)")
            : HealthStatus.Healthy(ComponentId);
    }

    public override async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        await base.DisposeAsync();
    }
}

/// <summary>
/// Generic REST SDK client - template for REST-based SDKs (Python, Node, etc.)
/// </summary>
public class RestSDKClient : BaseSDKClient
{
    public RestSDKClient(
        IServiceContext context,
        string componentId,
        SDKConfiguration? config = null)
        : base(context, componentId, config)
    {
    }

    protected async Task<T> GetAsync<T>(
        string path,
        Dictionary<string, string>? queryParams = null,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async token =>
            {
                var uri = BuildUri(path, queryParams);
                var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, token);
                return await DeserializeAsync<T>(response, token);
            },
            $"GET {path}",
            ct);
    }

    protected async Task<T> PostAsync<T>(
        string path,
        object? body = null,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async token =>
            {
                var uri = BuildUri(path);
                var content = body != null
                    ? new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(body),
                        System.Text.Encoding.UTF8,
                        "application/json")
                    : null;

                var response = await _httpClient.PostAsync(uri, content, token);
                return await DeserializeAsync<T>(response, token);
            },
            $"POST {path}",
            ct);
    }

    private string BuildUri(string path, Dictionary<string, string>? queryParams = null)
    {
        var builder = new UriBuilder($"{_config.Endpoint}{path}");
        
        if (queryParams != null)
        {
            var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            builder.Query = query;
        }

        return builder.ToString();
    }

    private async Task<T> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new SDKException(
                $"HTTP_{response.StatusCode}",
                $"HTTP {response.StatusCode}: {content}");
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(content) 
            ?? throw new SDKException("DESERIALIZATION_FAILED", "Failed to deserialize response");
    }
}

/// <summary>
/// Mock SDK client for testing - all SDKs include mock implementation.
/// </summary>
public class MockSDKClient : BaseSDKClient
{
    private readonly Dictionary<string, object> _mockData = new();
    private readonly Func<Random>? _randomDelay;

    public MockSDKClient(
        IServiceContext context,
        string componentId,
        Func<Random>? randomDelay = null)
        : base(context, componentId)
    {
        _randomDelay = randomDelay;
    }

    public void SetMockData(string key, object value) => _mockData[key] = value;

    public async Task<T> GetMockAsync<T>(
        string key,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(
            async token =>
            {
                if (_randomDelay != null)
                {
                    var delay = _randomDelay().Next(10, 100);
                    await Task.Delay(delay, token);
                }

                if (!_mockData.TryGetValue(key, out var data))
                    throw new SDKException("NOT_FOUND", $"Mock data '{key}' not found");

                return (T?)System.Text.Json.JsonSerializer.Deserialize(
                    System.Text.Json.JsonSerializer.Serialize(data),
                    typeof(T))
                    ?? throw new SDKException("DESERIALIZATION_FAILED", "Failed to deserialize mock data");
            },
            $"MOCK_GET {key}",
            ct);
    }
}
