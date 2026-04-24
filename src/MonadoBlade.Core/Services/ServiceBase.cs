namespace MonadoBlade.Core.Services;

using System.Collections.Concurrent;

/// <summary>
/// Base class for service implementations providing common functionality.
/// </summary>
public abstract class ServiceBase : IService
{
    protected readonly ILogger _logger;
    protected readonly ConcurrentDictionary<string, object> _cache;

    protected ServiceBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new ConcurrentDictionary<string, object>();
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    protected void LogInfo(string message, params object?[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    protected void LogWarning(string message, params object?[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    protected void LogError(Exception ex, string message, params object?[] args)
    {
        _logger.LogError(ex, message, args);
    }

    /// <summary>
    /// Gets a cached value or computes it if not cached.
    /// </summary>
    protected async Task<T?> GetCachedOrComputeAsync<T>(
        string cacheKey,
        Func<Task<T?>> computeAsync,
        TimeSpan? cacheDuration = null) where T : class
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
            return (T?)cached;

        var value = await computeAsync();
        if (value != null)
            _cache.AddOrUpdate(cacheKey, value, (_, _) => value);

        return value;
    }

    /// <summary>
    /// Invalidates a cache entry.
    /// </summary>
    protected void InvalidateCache(string cacheKey)
    {
        _cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Invalidates all cache entries matching a pattern.
    /// </summary>
    protected void InvalidateCachePattern(string pattern)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
            _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    protected void ClearCache()
    {
        _cache.Clear();
    }
}

/// <summary>
/// Logger interface for dependency injection.
/// </summary>
public interface ILogger
{
    void LogInformation(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogError(Exception ex, string message, params object?[] args);
}
