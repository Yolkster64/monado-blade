namespace MonadoBlade.Tracks.APIGateway;

using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;
using System.Collections.Concurrent;

/// <summary>
/// External API Gateway - only available on Server profile.
/// Features: Rate limiting, authentication, monitoring, filtering.
/// </summary>
public interface IExternalAPIGateway : IServiceComponent
{
    /// <summary>Is the gateway currently accepting external requests.</summary>
    bool IsEnabled { get; }
    
    /// <summary>Enables the API gateway for external access.</summary>
    Task<Result> EnableAsync(CancellationToken ct = default);
    
    /// <summary>Disables the API gateway for external access.</summary>
    Task<Result> DisableAsync(CancellationToken ct = default);
    
    /// <summary>Processes an incoming API request.</summary>
    Task<Result<APIResponse>> ProcessRequestAsync(
        APIRequest request,
        CancellationToken ct = default);
    
    /// <summary>Gets gateway metrics and statistics.</summary>
    Task<Result<GatewayMetrics>> GetMetricsAsync(CancellationToken ct = default);
    
    /// <summary>Gets recent request log entries.</summary>
    Task<Result<APIRequestLog[]>> GetRequestLogsAsync(
        int? limit = null,
        CancellationToken ct = default);
}

/// <summary>
/// API request model.
/// </summary>
public record APIRequest(
    string Method,
    string Path,
    Dictionary<string, string>? Headers = null,
    string? Body = null,
    string? ApiKey = null,
    string? ClientIp = null);

/// <summary>
/// API response model.
/// </summary>
public record APIResponse(
    int StatusCode,
    Dictionary<string, string>? Headers = null,
    string? Body = null,
    string? Error = null);

/// <summary>
/// Gateway metrics.
/// </summary>
public record GatewayMetrics(
    long TotalRequests,
    long SuccessfulRequests,
    long FailedRequests,
    long RateLimitedRequests,
    double RequestsPerSecond,
    double AverageResponseTimeMs,
    int UniqueClientsToday,
    DateTime LastRequestAt);

/// <summary>
/// API request log entry.
/// </summary>
public record APIRequestLog(
    DateTime Timestamp,
    string Method,
    string Path,
    string? ClientIp,
    int ResponseCode,
    long ResponseTimeMs,
    string? Error);

/// <summary>
/// External API Gateway implementation.
/// </summary>
public class ExternalAPIGateway : ServiceComponentBase, IExternalAPIGateway
{
    private bool _isEnabled;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _rateLimits = new();
    private readonly ConcurrentBag<APIRequestLog> _requestLogs = new();
    private readonly HashSet<string> _allowedEndpoints;
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _rateLimitedRequests;
    private DateTime _lastRequestAt = DateTime.UtcNow;
    private readonly object _metricsLock = new();

    public bool IsEnabled => _isEnabled;

    public ExternalAPIGateway(IServiceContext context, string componentId)
        : base(context, componentId)
    {
        ComponentType = "ExternalAPIGateway";
        _allowedEndpoints = new HashSet<string>
        {
            "/api/tasks",
            "/api/status",
            "/api/metrics",
            "/api/health",
            "/api/webhook"
        };
    }

    protected override async Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        _logger.Information("Initializing External API Gateway");
        _logger.Information("Gateway: Rate limiting enabled (per-client)");
        _logger.Information("Gateway: Authentication required (API keys)");
        _logger.Information("Gateway: Endpoint filtering enabled");
        _logger.Information("Gateway: All requests logged and monitored");
        _logger.Information("Gateway: Initially DISABLED (requires explicit enable)");
        
        _isEnabled = false;
        return Result.Success();
    }

    public async Task<Result> EnableAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        _isEnabled = true;
        _logger.Warning("External API Gateway ENABLED");
        _metrics.IncrementCounter("gateway_enabled");
        return Result.Success();
    }

    public async Task<Result> DisableAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        _isEnabled = false;
        _logger.Warning("External API Gateway DISABLED");
        _metrics.IncrementCounter("gateway_disabled");
        return Result.Success();
    }

    public async Task<Result<APIResponse>> ProcessRequestAsync(
        APIRequest request,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var clientIp = request.ClientIp ?? "unknown";
        var startTime = DateTime.UtcNow;

        try
        {
            // Check if gateway is enabled
            if (!_isEnabled)
                return CreateResponse(403, "API Gateway disabled", null, 5);

            // Check rate limiting
            if (!CheckRateLimit(clientIp))
            {
                LogRequest(request, 429, (DateTime.UtcNow - startTime).TotalMilliseconds, "Rate limited");
                Interlocked.Increment(ref _rateLimitedRequests);
                return CreateResponse(429, "Too many requests", null, 3);
            }

            // Validate authentication
            var authResult = ValidateAuthentication(request);
            if (authResult is Result.Failure failure)
            {
                LogRequest(request, 401, (DateTime.UtcNow - startTime).TotalMilliseconds, "Unauthorized");
                Interlocked.Increment(ref _failedRequests);
                return new Result<APIResponse>.Failure(failure.ErrorCode, failure.Message, failure.InnerException);
            }

            // Check endpoint whitelist
            if (!IsEndpointAllowed(request.Path))
            {
                LogRequest(request, 403, (DateTime.UtcNow - startTime).TotalMilliseconds, "Forbidden endpoint");
                Interlocked.Increment(ref _failedRequests);
                return CreateResponse(403, "Endpoint not allowed", null, 2);
            }

            // Process request
            var response = await ProcessAllowedRequestAsync(request, ct);
            
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            LogRequest(request, response.StatusCode, responseTime, response.Error);

            if (response.StatusCode >= 200 && response.StatusCode < 300)
                Interlocked.Increment(ref _successfulRequests);
            else
                Interlocked.Increment(ref _failedRequests);

            Interlocked.Increment(ref _totalRequests);
            _lastRequestAt = DateTime.UtcNow;

            _metrics.IncrementCounter("gateway_request", tags: ("method", request.Method), ("endpoint", request.Path));

            return response.ToSuccess();
        }
        catch (Exception ex)
        {
            _logger.Error($"Gateway request error: {request.Path}", ex);
            LogRequest(request, 500, (DateTime.UtcNow - startTime).TotalMilliseconds, ex.Message);
            Interlocked.Increment(ref _failedRequests);
            return CreateResponse(500, "Internal server error", null, 10);
        }
    }

    public async Task<Result<GatewayMetrics>> GetMetricsAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        lock (_metricsLock)
        {
            var rpsCounter = _totalRequests > 0 ? _totalRequests / (DateTime.UtcNow - _lastRequestAt).TotalSeconds : 0;
            
            var avgResponseTime = _totalRequests > 0
                ? _requestLogs.Take((int)_totalRequests).Average(r => r.ResponseTimeMs)
                : 0;

            var uniqueClients = _requestLogs
                .Where(r => r.Timestamp > DateTime.UtcNow.AddHours(-24))
                .Select(r => r.ClientIp)
                .Distinct()
                .Count();

            var metrics = new GatewayMetrics(
                TotalRequests: _totalRequests,
                SuccessfulRequests: _successfulRequests,
                FailedRequests: _failedRequests,
                RateLimitedRequests: _rateLimitedRequests,
                RequestsPerSecond: rpsCounter,
                AverageResponseTimeMs: avgResponseTime,
                UniqueClientsToday: uniqueClients,
                LastRequestAt: _lastRequestAt);

            return metrics.ToSuccess();
        }
    }

    public async Task<Result<APIRequestLog[]>> GetRequestLogsAsync(int? limit = null, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var logs = _requestLogs
            .OrderByDescending(r => r.Timestamp)
            .Take(limit ?? 100)
            .ToArray();

        return logs.ToSuccess();
    }

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        lock (_metricsLock)
        {
            var errorRate = _totalRequests > 0 ? (_failedRequests * 100.0) / _totalRequests : 0;

            var details = new Dictionary<string, object?>
            {
                { "enabled", _isEnabled },
                { "totalRequests", _totalRequests },
                { "errorRate", errorRate },
                { "requestsPerSecond", _totalRequests / Math.Max(1, (DateTime.UtcNow - _lastRequestAt).TotalSeconds) }
            };

            if (errorRate > 50)
                return HealthStatus.Unhealthy(ComponentId, $"High error rate: {errorRate:F1}%", details);

            if (errorRate > 20)
                return HealthStatus.Degraded(ComponentId, $"Elevated error rate: {errorRate:F1}%", details);

            return HealthStatus.Healthy(ComponentId, 
                $"Gateway {'enabled' if (_isEnabled) else 'disabled'} ({_totalRequests} requests)", 
                details);
        }
    }

    private bool CheckRateLimit(string clientIp)
    {
        const int requestsPerMinute = 60;
        const int requestsPerHour = 1000;

        var now = DateTime.UtcNow;
        var bucket = _rateLimits.GetOrAdd(clientIp, _ => new RateLimitBucket());

        lock (bucket)
        {
            bucket.Trim(now);

            if (bucket.MinuteRequests.Count >= requestsPerMinute)
                return false;

            if (bucket.HourRequests.Count >= requestsPerHour)
                return false;

            bucket.MinuteRequests.Enqueue(now);
            bucket.HourRequests.Enqueue(now);

            return true;
        }
    }

    private Result ValidateAuthentication(APIRequest request)
    {
        // In production, validate against database/vault
        if (string.IsNullOrEmpty(request.ApiKey))
            return ErrorCode.AuthenticationFailed.ToFailure("API key required");

        // Simulate valid API key
        if (request.ApiKey.StartsWith("key_"))
            return Result.Success();

        return ErrorCode.AuthenticationFailed.ToFailure("Invalid API key");
    }

    private bool IsEndpointAllowed(string path)
    {
        return _allowedEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<APIResponse> ProcessAllowedRequestAsync(APIRequest request, CancellationToken ct)
    {
        // Simulate endpoint processing
        await Task.Delay(10, ct);

        return request.Path.ToLowerInvariant() switch
        {
            var p when p.StartsWith("/api/tasks") => 
                new APIResponse(200, Body: "{\"status\":\"success\",\"tasks\":[]}"),
            var p when p.StartsWith("/api/status") => 
                new APIResponse(200, Body: "{\"status\":\"healthy\"}"),
            var p when p.StartsWith("/api/metrics") => 
                new APIResponse(200, Body: "{\"requests\":1000,\"errors\":5}"),
            var p when p.StartsWith("/api/health") => 
                new APIResponse(200, Body: "{\"health\":\"ok\"}"),
            var p when p.StartsWith("/api/webhook") && request.Method == "POST" => 
                new APIResponse(202, Body: "{\"accepted\":true}"),
            _ => new APIResponse(404, Error: "Not found")
        };
    }

    private void LogRequest(APIRequest request, int statusCode, double responseTimeMs, string? error)
    {
        var log = new APIRequestLog(
            Timestamp: DateTime.UtcNow,
            Method: request.Method,
            Path: request.Path,
            ClientIp: request.ClientIp,
            ResponseCode: statusCode,
            ResponseTimeMs: (long)responseTimeMs,
            Error: error);

        _requestLogs.Add(log);

        if (_requestLogs.Count > 10000)
        {
            var overflow = _requestLogs.Count - 10000;
            for (int i = 0; i < overflow; i++)
                _requestLogs.TryTake(out _);
        }
    }

    private APIResponse CreateResponse(int statusCode, string message, string? body, int responseTimeMs)
    {
        return new APIResponse(
            StatusCode: statusCode,
            Body: body ?? $"{{\"message\":\"{message}\"}}",
            Error: statusCode >= 400 ? message : null);
    }
}

/// <summary>
/// Rate limit tracking bucket for per-client limits.
/// </summary>
internal class RateLimitBucket
{
    public Queue<DateTime> MinuteRequests { get; } = new();
    public Queue<DateTime> HourRequests { get; } = new();

    public void Trim(DateTime now)
    {
        var oneMinuteAgo = now.AddMinutes(-1);
        var oneHourAgo = now.AddHours(-1);

        while (MinuteRequests.Count > 0 && MinuteRequests.Peek() < oneMinuteAgo)
            MinuteRequests.Dequeue();

        while (HourRequests.Count > 0 && HourRequests.Peek() < oneHourAgo)
            HourRequests.Dequeue();
    }
}
