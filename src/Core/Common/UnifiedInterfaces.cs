namespace MonadoBlade.Core.Common;

/// <summary>
/// Unified AI provider interface. ALL AI systems implement this.
/// Used by Track A (AIHub), Track B (SDKs), Track C (Orchestration), Track D (UI).
/// </summary>
public interface IAIProvider : IServiceComponent
{
    string ProviderName { get; }
    AIProviderCapabilities Capabilities { get; }
    
    /// <summary>Performs inference with automatic fallback and caching.</summary>
    Task<Result<AIInferenceResult>> InferenceAsync(
        AIInferenceRequest request,
        CancellationToken ct = default);
    
    /// <summary>Validates if provider can handle the request.</summary>
    bool CanHandle(AIInferenceRequest request);
}

public record AIInferenceRequest(
    string Model,
    string Prompt,
    AIInferenceOptions? Options = null,
    Dictionary<string, object?>? Metadata = null);

public record AIInferenceResult(
    string Content,
    AIUsageMetrics Usage,
    Dictionary<string, object?>? Metadata = null);

public record AIUsageMetrics(
    int InputTokens,
    int OutputTokens,
    int TotalTokens);

public record AIInferenceOptions(
    float Temperature = 0.7f,
    int MaxTokens = 2000,
    float TopP = 0.9f,
    int TopK = 40,
    string[]? StopSequences = null);

public record AIProviderCapabilities(
    string[] SupportedModels,
    int MaxInputTokens,
    int MaxOutputTokens,
    bool SupportsStreaming,
    bool SupportsFunctionCalling,
    bool SupportsVision);

/// <summary>
/// Unified SDK provider interface. Track B organizes 50+ SDKs through this.
/// </summary>
public interface ISDKProvider : IServiceComponent
{
    string SDKName { get; }
    string TargetPlatform { get; }
    Version SDKVersion { get; }
    
    /// <summary>Executes SDK-specific operation with automatic retry and fallback.</summary>
    Task<Result<object?>> ExecuteAsync(
        string operation,
        Dictionary<string, object?> parameters,
        CancellationToken ct = default);
    
    /// <summary>Lists available operations for this SDK.</summary>
    Task<Result<string[]>> GetAvailableOperationsAsync(CancellationToken ct = default);
}

/// <summary>
/// Unified VM management interface. Track C uses this for orchestration.
/// </summary>
public interface IVirtualMachineManager : IServiceComponent
{
    /// <summary>Gets VM state without modification.</summary>
    Task<Result<VMState>> GetStateAsync(string vmId, CancellationToken ct = default);
    
    /// <summary>Creates VM atomically (all-or-nothing).</summary>
    Task<Result<string>> CreateAsync(VMConfiguration config, CancellationToken ct = default);
    
    /// <summary>Starts VM with health verification.</summary>
    Task<Result> StartAsync(string vmId, CancellationToken ct = default);
    
    /// <summary>Stops VM gracefully with timeout.</summary>
    Task<Result> StopAsync(string vmId, TimeSpan timeout = default, CancellationToken ct = default);
    
    /// <summary>Deletes VM with cleanup.</summary>
    Task<Result> DeleteAsync(string vmId, CancellationToken ct = default);
}

public record VMState(
    string VMId,
    VMStatus Status,
    DateTime LastStateChange,
    Dictionary<string, object?>? Metadata = null);

public record VMConfiguration(
    string Name,
    int CpuCount,
    long MemoryBytes,
    long StorageBytes,
    string[] NetworkIds,
    Dictionary<string, string>? Tags = null);

public enum VMStatus { Stopped, Starting, Running, Stopping, Paused, Error }

/// <summary>
/// Unified load balancing interface for Track C.
/// </summary>
public interface ILoadBalancer : IServiceComponent
{
    /// <summary>Selects best VM for request distribution.</summary>
    Task<Result<string>> SelectVMAsync(
        IEnumerable<string> availableVMs,
        LoadBalancingStrategy strategy = LoadBalancingStrategy.LeastConnections,
        CancellationToken ct = default);
    
    /// <summary>Records request completion for metrics.</summary>
    Task RecordRequestAsync(string vmId, RequestMetrics metrics);
}

public record RequestMetrics(
    long DurationMs,
    bool Success,
    int? ErrorCode = null);

public enum LoadBalancingStrategy { RoundRobin, LeastConnections, Random, IPHash, WeightedRoundRobin }

/// <summary>
/// Unified UI component interface for Track D.
/// </summary>
public interface IUIComponent : IServiceComponent
{
    UIComponentType ComponentType { get; }
    
    /// <summary>Renders component with given state.</summary>
    Task<Result<string>> RenderAsync(object? state = null, CancellationToken ct = default);
    
    /// <summary>Handles user input event.</summary>
    Task<Result<UIEventResponse>> HandleInputAsync(UIEvent input, CancellationToken ct = default);
}

public record UIEvent(
    string EventType,
    string ComponentId,
    Dictionary<string, object?>? Payload = null);

public record UIEventResponse(
    bool Handled,
    UIUpdateDelta? Updates = null,
    object? NewState = null);

public record UIUpdateDelta(
    string[] UpdatedPaths,
    Dictionary<string, object?> Changes);

public enum UIComponentType { Button, TextBox, Grid, Chart, Dashboard, CustomControl }

/// <summary>
/// Unified validator interface used across all tracks.
/// </summary>
public interface IValidator<T>
{
    /// <summary>Validates object and returns detailed results.</summary>
    Task<Result<ValidationResult>> ValidateAsync(T obj, CancellationToken ct = default);
}

public record ValidationResult(
    bool IsValid,
    ValidationFailure[]? Failures = null);

public record ValidationFailure(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue = null);

/// <summary>
/// Unified event/message bus for inter-track communication.
/// </summary>
public interface IEventBus : IServiceComponent
{
    /// <summary>Publishes event to all subscribers.</summary>
    Task<Result> PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class;
    
    /// <summary>Subscribes to event type.</summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task<Result>> handler)
        where TEvent : class;
}

/// <summary>
/// Unified retry policy for resilience across all systems.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>Executes operation with automatic retry.</summary>
    Task<Result<T>> ExecuteAsync<T>(
        string operationName,
        Func<int, CancellationToken, Task<Result<T>>> operation,
        CancellationToken ct = default);
}

public record RetryPolicyConfig(
    int MaxRetries = 3,
    TimeSpan? InitialDelay = null,
    TimeSpan? MaxDelay = null,
    double BackoffMultiplier = 2.0,
    Func<ErrorCode, bool>? IsRetryable = null);
