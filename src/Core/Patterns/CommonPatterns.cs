namespace MonadoBlade.Core.Patterns;

/// <summary>
/// Async operation pattern with automatic timing, retries, and metrics.
/// Used across all tracks for consistency.
/// </summary>
public static class AsyncOperationPattern
{
    /// <summary>Executes operation with automatic retry and metrics.</summary>
    public static async Task<Result<T>> ExecuteWithRetryAsync<T>(
        string operationName,
        Func<CancellationToken, Task<Result<T>>> operation,
        ILoggingProvider? logger = null,
        IMetricsCollector? metrics = null,
        RetryPolicyConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new RetryPolicyConfig();
        var startTime = DateTime.UtcNow;
        int attempt = 0;

        while (attempt <= config.MaxRetries)
        {
            try
            {
                logger?.Debug($"Executing {operationName} (attempt {attempt + 1}/{config.MaxRetries + 1})");
                var result = await operation(ct);

                if (result is Result<T>.Success)
                {
                    var duration = DateTime.UtcNow - startTime;
                    metrics?.RecordDuration($"op_{operationName}", duration, ("attempt", attempt.ToString()));
                    logger?.Information($"{operationName} succeeded ({duration.TotalMilliseconds}ms)");
                    return result;
                }

                if (result is Result<T>.Failure failure)
                {
                    var isRetryable = config.IsRetryable?.Invoke(failure.ErrorCode) ?? IsDefaultRetryable(failure.ErrorCode);
                    if (!isRetryable || attempt >= config.MaxRetries)
                    {
                        metrics?.IncrementCounter($"op_{operationName}_failed", tags: ("error_code", failure.ErrorCode.ToString()));
                        return failure;
                    }

                    logger?.Warning($"{operationName} failed with {failure.ErrorCode}, retrying...");
                }
            }
            catch (OperationCanceledException)
            {
                metrics?.IncrementCounter($"op_{operationName}_cancelled");
                return ErrorCode.Timeout.ToFailure<T>("Operation cancelled");
            }
            catch (Exception ex)
            {
                if (attempt >= config.MaxRetries)
                {
                    logger?.Error($"{operationName} failed after {attempt + 1} attempts", ex);
                    metrics?.IncrementCounter($"op_{operationName}_error");
                    return ErrorCode.Unknown.ToFailure<T>(ex.Message, ex);
                }
                logger?.Warning($"{operationName} error on attempt {attempt + 1}", ex);
            }

            if (attempt < config.MaxRetries)
            {
                var delay = CalculateBackoff(attempt, config);
                await Task.Delay(delay, ct);
            }

            attempt++;
        }

        return ErrorCode.Unknown.ToFailure<T>("Operation failed after all retries");
    }

    private static bool IsDefaultRetryable(ErrorCode code) => code switch
    {
        ErrorCode.Timeout or
        ErrorCode.CommunicationError or
        ErrorCode.ResourceUnavailable or
        ErrorCode.AIProviderRateLimited => true,
        _ => false
    };

    private static TimeSpan CalculateBackoff(int attempt, RetryPolicyConfig config)
    {
        var initial = config.InitialDelay ?? TimeSpan.FromMilliseconds(100);
        var delay = TimeSpan.FromMilliseconds(initial.TotalMilliseconds * Math.Pow(config.BackoffMultiplier, attempt));
        var max = config.MaxDelay ?? TimeSpan.FromSeconds(30);
        return delay > max ? max : delay;
    }
}

/// <summary>
/// Caching pattern with automatic invalidation and composition.
/// </summary>
public static class CachingPattern
{
    /// <summary>Gets or computes value with automatic caching and expiration.</summary>
    public static async Task<Result<T>> GetOrComputeAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<Result<T>>> factory,
        ICacheProvider cache,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        // Try cache first
        var cached = await cache.GetAsync<T>(cacheKey, ct);
        if (cached is Result<T>.Success success)
            return success.Value.ToSuccess();

        // Compute if not cached
        var result = await factory(ct);
        if (result is Result<T>.Success computed)
        {
            await cache.SetAsync(cacheKey, computed.Value, expiration, ct);
        }

        return result;
    }

    /// <summary>Invalidates related cache entries by pattern.</summary>
    public static async Task<Result> InvalidatePatternAsync(
        string pattern,
        ICacheProvider cache,
        CancellationToken ct = default) =>
        await cache.InvalidateByPatternAsync(pattern, ct);
}

/// <summary>
/// Configuration pattern with type-safe access and validation.
/// </summary>
public static class ConfigurationPattern
{
    public static Result<T> GetRequired<T>(this IConfigurationProvider config, string key)
    {
        var result = config.Get<T>(key);
        return result is Result<T>.Success success ? success : 
            ErrorCode.ConfigurationError.ToFailure<T>($"Required configuration key not found: {key}");
    }

    public static T GetRequiredWithDefault<T>(this IConfigurationProvider config, string key, T defaultValue) =>
        config.Get(key, defaultValue);
}

/// <summary>
/// Security pattern with input validation and sanitization.
/// </summary>
public static class SecurityPattern
{
    /// <summary>Validates and sanitizes string input.</summary>
    public static Result<string> ValidateStringInput(
        string input,
        int minLength = 1,
        int maxLength = int.MaxValue,
        string? allowedCharacters = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ErrorCode.ValidationFailed.ToFailure<string>("Input cannot be empty");

        if (input.Length < minLength || input.Length > maxLength)
            return ErrorCode.OutOfRange.ToFailure<string>($"Input length must be between {minLength} and {maxLength}");

        if (!string.IsNullOrEmpty(allowedCharacters) && !input.All(c => allowedCharacters.Contains(c)))
            return ErrorCode.InvalidFormat.ToFailure<string>("Input contains invalid characters");

        return input.ToSuccess();
    }

    /// <summary>Validates object reference equality for security-sensitive operations.</summary>
    public static Result ValidateReferenceEquality<T>(T expected, T actual) where T : class
    {
        if (!ReferenceEquals(expected, actual))
            return ErrorCode.ValidationFailed.ToFailure("Reference equality check failed");
        return Result.Success();
    }

    /// <summary>Validates no injection attack patterns.</summary>
    public static Result<string> SanitizeForDatabase(string input)
    {
        if (input.Contains("'") || input.Contains("\"") || input.Contains(";") || input.Contains("--"))
            return ErrorCode.ValidationFailed.ToFailure<string>("Input contains potentially dangerous characters");
        return input.ToSuccess();
    }
}

/// <summary>
/// Atomicity pattern for operations requiring all-or-nothing semantics.
/// </summary>
public class AtomicOperation
{
    private readonly List<Func<Task>> _rollbackActions = new();
    private readonly ILoggingProvider _logger;
    private bool _committed;

    public AtomicOperation(ILoggingProvider logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Registers an action with automatic rollback.</summary>
    public async Task<Result<T>> ExecuteAsync<T>(
        string stepName,
        Func<Task<Result<T>>> action,
        Func<Task>? rollback = null)
    {
        if (_committed)
            return ErrorCode.InvalidOperation.ToFailure<T>("Operation already committed");

        try
        {
            _logger.Information($"Executing atomic step: {stepName}");
            var result = await action();

            if (result is Result<T>.Success)
            {
                if (rollback != null)
                    _rollbackActions.Add(rollback);
                return result;
            }

            await RollbackAsync();
            return (Result<T>)result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Atomic step failed: {stepName}", ex);
            await RollbackAsync();
            return ErrorCode.Unknown.ToFailure<T>(ex.Message, ex);
        }
    }

    /// <summary>Commits all actions atomically.</summary>
    public async Task<Result> CommitAsync()
    {
        _committed = true;
        _logger.Information("Atomic operation committed");
        return Result.Success();
    }

    /// <summary>Rolls back all registered actions in reverse order.</summary>
    private async Task RollbackAsync()
    {
        _logger.Warning("Rolling back atomic operation");
        for (int i = _rollbackActions.Count - 1; i >= 0; i--)
        {
            try
            {
                await _rollbackActions[i]();
            }
            catch (Exception ex)
            {
                _logger.Error("Rollback step failed", ex);
            }
        }
    }
}

/// <summary>
/// Resource management pattern with automatic cleanup.
/// </summary>
public class ResourceScope : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> _resources = new();
    private bool _disposed;

    /// <summary>Acquires resource and registers for automatic cleanup.</summary>
    public async Task<T> AcquireAsync<T>(Func<Task<T>> factory) where T : IAsyncDisposable
    {
        var resource = await factory();
        _resources.Add(resource);
        return resource;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        for (int i = _resources.Count - 1; i >= 0; i--)
        {
            try
            {
                await _resources[i].DisposeAsync();
            }
            catch
            {
                // Suppress disposal errors to ensure all resources are cleaned up
            }
        }

        GC.SuppressFinalize(this);
    }
}
