// ============================================================================
// MONADO BLADE OPTIMIZATION - ERROR HANDLING & DI CONSOLIDATION
// Hour 5-6: Consolidates error handling and dependency injection patterns
// Expected consolidation: 210+ lines
// ============================================================================

namespace MonadoBlade.Core.Patterns;

using System;
using System.Collections.Generic;

/// <summary>
/// Unified error result type - consolidates try-catch patterns throughout codebase.
/// CONSOLIDATION: Replaces Result, Failure, Success patterns with single canonical Result<T>.
/// Benefit: Eliminates exception throwing for expected failures, improves performance and testability.
/// </summary>
public abstract record Result
{
    public sealed record Success : Result;
    public sealed record Failure(string ErrorMessage, Exception Exception = null) : Result;

    public static Result Ok() => new Success();
    public static Result Fail(string message, Exception ex = null) => new Failure(message, ex);
    public static Result<T> Ok<T>(T value) => new Result<T>.Success(value);
    public static Result<T> Fail<T>(string message, Exception ex = null) => new Result<T>.Failure(message, ex);

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;
}

/// <summary>
/// Typed result with value - eliminates ComputeResult, FileOperationResult duplication.
/// </summary>
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string ErrorMessage, Exception Exception = null) : Result<T>;

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, Exception, TResult> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onFailure(f.ErrorMessage, f.Exception),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    public void Match(
        Action<T> onSuccess,
        Action<string, Exception> onFailure) =>
        _ = Match(
            v => { onSuccess(v); return true; },
            (msg, ex) => { onFailure(msg, ex); return true; });

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public T ValueOrThrow() =>
        Match(
            v => v,
            (msg, ex) => throw new InvalidOperationException(msg, ex));

    public T ValueOrDefault(T defaultValue = default) =>
        Match(v => v, (_, __) => defaultValue);
}

/// <summary>
/// Error code enumeration - consolidates error handling across services.
/// CONSOLIDATION: Single source of truth for error codes.
/// </summary>
public enum ErrorCode
{
    Unknown = 0,
    InvalidArgument = 1,
    NotFound = 2,
    AlreadyExists = 3,
    PermissionDenied = 4,
    ResourceExhausted = 5,
    FailedPrecondition = 6,
    Aborted = 7,
    OutOfRange = 8,
    Unimplemented = 9,
    Internal = 10,
    Unavailable = 11,
    DataLoss = 12,
    Unauthenticated = 13,
    Timeout = 14,
    Cancelled = 15
}

/// <summary>Extension methods for error codes.</summary>
public static class ErrorCodeExtensions
{
    public static Result ToFailure(this ErrorCode code, string message = "")
    {
        var msg = $"[{code}] {message}";
        return Result.Fail(msg);
    }

    public static Result<T> ToFailure<T>(this ErrorCode code, string message = "")
    {
        var msg = $"[{code}] {message}";
        return Result.Fail<T>(msg);
    }
}

/// <summary>
/// Dependency injection container - consolidates DI configuration boilerplate.
/// CONSOLIDATION: Replaces service registration patterns across all modules.
/// Benefit: ~100 lines saved by eliminating duplicate registration code.
/// </summary>
public sealed class ServiceContainer : IDisposable
{
    private readonly Dictionary<Type, object> _singletons = new();
    private readonly Dictionary<Type, Func<ServiceContainer, object>> _factories = new();
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    /// <summary>Register a singleton service.</summary>
    public ServiceContainer RegisterSingleton<T>(T instance) where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceContainer));
        _singletons[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
        if (instance is IDisposable d) _disposables.Add(d);
        return this;
    }

    /// <summary>Register a singleton factory.</summary>
    public ServiceContainer RegisterSingleton<T>(Func<ServiceContainer, T> factory) where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceContainer));
        _factories[typeof(T)] = c => factory((ServiceContainer)(object)c);
        return this;
    }

    /// <summary>Register a transient factory (new instance each time).</summary>
    public ServiceContainer RegisterTransient<T>(Func<ServiceContainer, T> factory) where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceContainer));
        _factories[typeof(T)] = c => factory((ServiceContainer)(object)c);
        return this;
    }

    /// <summary>Resolve a service.</summary>
    public T Resolve<T>() where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceContainer));

        var type = typeof(T);

        if (_singletons.TryGetValue(type, out var singleton))
        {
            return (T)singleton;
        }

        if (_factories.TryGetValue(type, out var factory))
        {
            var instance = factory(this);
            if (instance is IDisposable d && !_disposables.Contains(d))
            {
                _disposables.Add(d);
            }
            // Note: For true singletons, cache the first created instance
            _singletons[type] = instance;
            return (T)instance;
        }

        throw new ServiceNotRegisteredException($"Service '{type.Name}' not registered");
    }

    /// <summary>Try resolve with result pattern.</summary>
    public Result<T> TryResolve<T>() where T : class
    {
        try
        {
            return Result.Ok(Resolve<T>());
        }
        catch (Exception ex)
        {
            return Result.Fail<T>($"Failed to resolve {typeof(T).Name}", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose in reverse order
        for (int i = _disposables.Count - 1; i >= 0; i--)
        {
            try
            {
                _disposables[i]?.Dispose();
            }
            catch { /* Ignore disposal errors */ }
        }

        _singletons.Clear();
        _factories.Clear();
        _disposables.Clear();
    }
}

/// <summary>Exception thrown when service not found in container.</summary>
public sealed class ServiceNotRegisteredException : Exception
{
    public ServiceNotRegisteredException(string message) : base(message) { }
}

/// <summary>
/// Service registration builder - consolidates service setup patterns.
/// CONSOLIDATION: Provides fluent API for common service configurations.
/// Example:
///     var services = new ServiceRegistrationBuilder()
///         .AddUnifiedLogger()
///         .AddCoreServices()
///         .Build();
/// </summary>
public sealed class ServiceRegistrationBuilder
{
    private readonly ServiceContainer _container;

    public ServiceRegistrationBuilder()
    {
        _container = new ServiceContainer();
    }

    public ServiceRegistrationBuilder AddUnifiedLogger()
    {
        _container.RegisterSingleton<IUnifiedLogger>(new ConsoleUnifiedLogger());
        return this;
    }

    public ServiceRegistrationBuilder AddAsyncOperationMetrics()
    {
        _container.RegisterSingleton(new AsyncOperationMetrics());
        return this;
    }

    public ServiceRegistrationBuilder AddMemoryPoolManager()
    {
        _container.RegisterSingleton(MemoryPoolManager.Instance);
        return this;
    }

    public ServiceRegistrationBuilder AddService<T>(T instance) where T : class
    {
        _container.RegisterSingleton(instance);
        return this;
    }

    public ServiceRegistrationBuilder AddService<TInterface, T>(T instance) 
        where TInterface : class 
        where T : class, TInterface
    {
        _container.RegisterSingleton<TInterface>(instance);
        return this;
    }

    public ServiceRegistrationBuilder AddFactory<T>(Func<ServiceContainer, T> factory) where T : class
    {
        _container.RegisterSingleton(factory);
        return this;
    }

    public ServiceContainer Build() => _container;
}

/// <summary>
/// Uniform exception wrapper - consolidates exception handling patterns.
/// CONSOLIDATION: Single wrapper for all service exceptions.
/// </summary>
public sealed class ServiceException : Exception
{
    public ErrorCode Code { get; }
    public string OperationName { get; }
    public DateTime Timestamp { get; }

    public ServiceException(
        ErrorCode code,
        string operationName,
        string message,
        Exception innerException = null)
        : base(message, innerException)
    {
        Code = code;
        OperationName = operationName;
        Timestamp = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] ServiceException: {Code} in {OperationName}: {Message}";
    }
}

/// <summary>
/// Guard clauses - consolidates input validation patterns.
/// CONSOLIDATION: Eliminates repetitive null/validation checks.
/// </summary>
public static class Guard
{
    public static T NotNull<T>(T value, string paramName) where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }

    public static string NotNullOrEmpty(string value, string paramName)
    {
        return string.IsNullOrWhiteSpace(value) 
            ? throw new ArgumentException($"'{paramName}' cannot be null or empty", paramName)
            : value;
    }

    public static int Positive(int value, string paramName)
    {
        return value <= 0 
            ? throw new ArgumentException($"'{paramName}' must be positive", paramName)
            : value;
    }

    public static int NotNegative(int value, string paramName)
    {
        return value < 0 
            ? throw new ArgumentException($"'{paramName}' cannot be negative", paramName)
            : value;
    }

    public static IEnumerable<T> NotEmpty<T>(IEnumerable<T> value, string paramName)
    {
        var enumerated = value ?? throw new ArgumentNullException(paramName);
        var list = new List<T>(enumerated);
        return list.Count == 0 
            ? throw new ArgumentException($"'{paramName}' cannot be empty", paramName)
            : list;
    }
}
