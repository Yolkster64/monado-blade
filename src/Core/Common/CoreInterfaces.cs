namespace MonadoBlade.Core.Common;

/// <summary>
/// Core interface ALL services implement. Every system component extends this.
/// Provides unified lifecycle, context, and metadata management.
/// </summary>
public interface IServiceComponent : IAsyncDisposable
{
    /// <summary>Gets the unique component identifier.</summary>
    string ComponentId { get; }
    
    /// <summary>Gets the component type/category.</summary>
    string ComponentType { get; }
    
    /// <summary>Initializes component asynchronously with configuration.</summary>
    Task<Result> InitializeAsync(IServiceContext context, CancellationToken ct = default);
    
    /// <summary>Performs health check on the component.</summary>
    Task<HealthStatus> GetHealthAsync(CancellationToken ct = default);
    
    /// <summary>Gracefully shuts down the component.</summary>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}

/// <summary>Provides context for all service operations.</summary>
public interface IServiceContext
{
    /// <summary>Gets correlation ID for distributed tracing.</summary>
    string CorrelationId { get; }
    
    /// <summary>Gets user/tenant context.</summary>
    IPrincipal? Principal { get; }
    
    /// <summary>Gets configuration provider.</summary>
    IConfigurationProvider Configuration { get; }
    
    /// <summary>Gets logging provider.</summary>
    ILoggingProvider Logger { get; }
    
    /// <summary>Gets metrics collector.</summary>
    IMetricsCollector Metrics { get; }
    
    /// <summary>Gets cache provider.</summary>
    ICacheProvider Cache { get; }
    
    /// <summary>Gets dependency resolver.</summary>
    IServiceProvider ServiceProvider { get; }
}

/// <summary>Unified configuration provider across all systems.</summary>
public interface IConfigurationProvider
{
    /// <summary>Gets a configuration value by key.</summary>
    Result<T> Get<T>(string key);
    
    /// <summary>Gets a configuration value with default.</summary>
    T Get<T>(string key, T defaultValue);
    
    /// <summary>Sets a configuration value.</summary>
    Result Set<T>(string key, T value, bool persistent = false);
    
    /// <summary>Validates configuration schema.</summary>
    Result Validate();
    
    /// <summary>Watches for configuration changes.</summary>
    IDisposable Watch(string key, Action<object?> onChanged);
}

/// <summary>Unified logging interface for all components.</summary>
public interface ILoggingProvider
{
    void Trace(string message, params object?[] args);
    void Debug(string message, params object?[] args);
    void Information(string message, params object?[] args);
    void Warning(string message, params object?[] args);
    void Error(string message, Exception? ex = null, params object?[] args);
    void Fatal(string message, Exception? ex = null, params object?[] args);
    
    /// <summary>Logs operation with automatic timing and metrics.</summary>
    Task<Result<T>> LogOperationAsync<T>(
        string operationName,
        Func<Task<Result<T>>> operation,
        LogLevel level = LogLevel.Information);
}

/// <summary>Unified metrics collection across all systems.</summary>
public interface IMetricsCollector
{
    void IncrementCounter(string metricName, long value = 1, params (string, string)[] tags);
    void SetGauge(string metricName, double value, params (string, string)[] tags);
    void RecordHistogram(string metricName, long value, params (string, string)[] tags);
    
    /// <summary>Records operation duration.</summary>
    void RecordDuration(string operationName, TimeSpan duration, params (string, string)[] tags);
}

/// <summary>Unified cache interface supporting multiple backends.</summary>
public interface ICacheProvider : IAsyncDisposable
{
    /// <summary>Gets a cached value.</summary>
    Task<Result<T?>> GetAsync<T>(string key, CancellationToken ct = default);
    
    /// <summary>Sets a cached value.</summary>
    Task<Result> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    
    /// <summary>Removes a cached value.</summary>
    Task<Result> RemoveAsync(string key, CancellationToken ct = default);
    
    /// <summary>Gets or computes a cached value.</summary>
    Task<Result<T>> GetOrComputeAsync<T>(
        string key,
        Func<CancellationToken, Task<Result<T>>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
    
    /// <summary>Invalidates cache by pattern.</summary>
    Task<Result> InvalidateByPatternAsync(string pattern, CancellationToken ct = default);
}

/// <summary>Health status for component monitoring.</summary>
public record HealthStatus(
    string ComponentId,
    HealthState State,
    string? Message = null,
    Dictionary<string, object?>? Details = null)
{
    public static HealthStatus Healthy(string componentId, string? message = null) =>
        new(componentId, HealthState.Healthy, message);
    
    public static HealthStatus Degraded(string componentId, string message, Dictionary<string, object?>? details = null) =>
        new(componentId, HealthState.Degraded, message, details);
    
    public static HealthStatus Unhealthy(string componentId, string message, Exception? ex = null) =>
        new(componentId, HealthState.Unhealthy, message, ex?.Message != null ? new() { { "Exception", ex.Message } } : null);
}

public enum HealthState { Healthy, Degraded, Unhealthy }
public enum LogLevel { Trace, Debug, Information, Warning, Error, Fatal }
