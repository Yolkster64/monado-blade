namespace MonadoBlade.Core.Patterns;

/// <summary>
/// Universal Result pattern for all operations. Eliminates exceptions for expected failures.
/// All tracks use this consistently for operation outcomes.
/// </summary>
public abstract record Result
{
    public sealed record Success(object? Value = null) : Result;
    public sealed record Failure(ErrorCode ErrorCode, string Message, Exception? InnerException = null) : Result;
    
    public T Match<T>(Func<object?, T> onSuccess, Func<ErrorCode, string, Exception?, T> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onFailure(f.ErrorCode, f.Message, f.InnerException),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    
    public async Task<T> MatchAsync<T>(
        Func<object?, Task<T>> onSuccess,
        Func<ErrorCode, string, Exception?, Task<T>> onFailure) =>
        this switch
        {
            Success s => await onSuccess(s.Value),
            Failure f => await onFailure(f.ErrorCode, f.Message, f.InnerException),
            _ => throw new InvalidOperationException("Unknown result type")
        };
}

/// <summary>Generic Result&lt;T&gt; for typed operations.</summary>
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(ErrorCode ErrorCode, string Message, Exception? InnerException = null) : Result<T>;
    
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ErrorCode, string, Exception?, TResult> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onFailure(f.ErrorCode, f.Message, f.InnerException),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    
    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<ErrorCode, string, Exception?, Task<TResult>> onFailure) =>
        this switch
        {
            Success s => await onSuccess(s.Value),
            Failure f => await onFailure(f.ErrorCode, f.Message, f.InnerException),
            _ => throw new InvalidOperationException("Unknown result type")
        };
}

/// <summary>Helper methods for Result pattern.</summary>
public static class ResultExtensions
{
    public static Result<T> ToSuccess<T>(this T value) => new Result<T>.Success(value);
    
    public static Result<T> ToFailure<T>(this ErrorCode code, string message, Exception? ex = null) =>
        new Result<T>.Failure(code, message, ex);
    
    public static Result ToSuccess(this object? value = null) => new Result.Success(value);
    
    public static Result ToFailure(this ErrorCode code, string message, Exception? ex = null) =>
        new Result.Failure(code, message, ex);
}
