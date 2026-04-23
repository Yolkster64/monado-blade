# API Reference - Monado Blade v2.4.0

**Complete API reference for all public interfaces and services**

*Read time: 25-30 minutes | Skill level: Intermediate*

## Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [Provider Adapter API](#provider-adapter-api)
3. [Smart Router API](#smart-router-api)
4. [Health Monitor API](#health-monitor-api)
5. [Failover Controller API](#failover-controller-api)
6. [Provider Registry API](#provider-registry-api)
7. [Error Types](#error-types)
8. [Response Models](#response-models)

---

## Core Interfaces

### IServiceComponent

Base interface for all service components.

```csharp
public interface IServiceComponent : IAsyncDisposable
{
    /// <summary>Unique component identifier.</summary>
    string ComponentId { get; }
    
    /// <summary>Component type/category.</summary>
    string ComponentType { get; }
    
    /// <summary>Initializes the component asynchronously.</summary>
    /// <param name="context">Service context</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> InitializeAsync(
        IServiceContext context, 
        CancellationToken ct = default);
    
    /// <summary>Gets current health status.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Health status</returns>
    Task<HealthStatus> GetHealthAsync(CancellationToken ct = default);
    
    /// <summary>Gracefully shuts down the component.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
```

**Example Usage:**

```csharp
var component = new OpenAIProvider(context);
var initResult = await component.InitializeAsync(context);
if (initResult.IsFailure)
{
    Console.WriteLine($"Failed to initialize: {initResult.Message}");
    return;
}

var health = await component.GetHealthAsync();
Console.WriteLine($"Component health: {health.State}");

await component.ShutdownAsync();
```

### IServiceContext

Provides context for service operations.

```csharp
public interface IServiceContext
{
    /// <summary>Correlation ID for distributed tracing.</summary>
    string CorrelationId { get; }
    
    /// <summary>User/tenant context.</summary>
    IPrincipal? Principal { get; }
    
    /// <summary>Configuration provider.</summary>
    IConfigurationProvider Configuration { get; }
    
    /// <summary>Logging provider.</summary>
    ILoggingProvider Logger { get; }
    
    /// <summary>Metrics collector.</summary>
    IMetricsCollector Metrics { get; }
    
    /// <summary>Cache provider.</summary>
    ICacheProvider Cache { get; }
    
    /// <summary>Dependency injection container.</summary>
    IServiceProvider ServiceProvider { get; }
}
```

**Example Usage:**

```csharp
// In any service
var apiKey = context.Configuration.Get<string>("API_KEY");
context.Logger.Information("Processing request");
context.Metrics.IncrementCounter("requests_processed");

var cached = await context.Cache.GetAsync<CachedData>("key");
```

---

## Provider Adapter API

### IProviderAdapter

Main interface for all providers.

```csharp
public interface IProviderAdapter : IServiceComponent
{
    /// <summary>Provider name (e.g., "OpenAI", "Groq").</summary>
    string ProviderName { get; }
    
    /// <summary>Provider version (e.g., "gpt-4-turbo").</summary>
    string ProviderVersion { get; }
    
    /// <summary>Provider capabilities.</summary>
    ProviderCapabilities Capabilities { get; }
    
    /// <summary>Cost per 1K tokens (normalized).</summary>
    decimal CostPer1kTokens { get; }
    
    /// <summary>Average latency in milliseconds.</summary>
    int AverageLatencyMs { get; }
    
    /// <summary>Executes inference request.</summary>
    /// <param name="request">Inference request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Inference response or error</returns>
    Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request,
        CancellationToken ct = default);
    
    /// <summary>Gets provider availability status.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Provider status</returns>
    Task<ProviderStatus> GetStatusAsync(CancellationToken ct = default);
}
```

### ProviderCapabilities

```csharp
public class ProviderCapabilities
{
    /// <summary>Supports streaming responses.</summary>
    public bool SupportsStreaming { get; init; }
    
    /// <summary>Supports vision/image inputs.</summary>
    public bool SupportsVision { get; init; }
    
    /// <summary>Supports tool/function calling.</summary>
    public bool SupportsTools { get; init; }
    
    /// <summary>Maximum context window size.</summary>
    public int MaxContextLength { get; init; }
    
    /// <summary>Maximum output length.</summary>
    public int MaxOutputLength { get; init; }
    
    /// <summary>Cost per input token.</summary>
    public decimal CostPerInputToken { get; init; }
    
    /// <summary>Cost per output token.</summary>
    public decimal CostPerOutputToken { get; init; }
}
```

### InferenceRequest

```csharp
public class InferenceRequest
{
    /// <summary>Model identifier.</summary>
    public string Model { get; init; }
    
    /// <summary>Messages in conversation.</summary>
    public List<Message> Messages { get; init; }
    
    /// <summary>Maximum tokens to generate.</summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>Temperature for randomness (0-2).</summary>
    public float Temperature { get; init; } = 0.7f;
    
    /// <summary>Top-p probability mass (0-1).</summary>
    public float? TopP { get; init; }
    
    /// <summary>Frequency penalty (-2 to 2).</summary>
    public float? FrequencyPenalty { get; init; }
    
    /// <summary>Presence penalty (-2 to 2).</summary>
    public float? PresencePenalty { get; init; }
    
    /// <summary>Tools/functions available.</summary>
    public List<Tool>? Tools { get; init; }
    
    /// <summary>Enable response streaming.</summary>
    public bool Stream { get; init; }
}

public class Message
{
    public string Role { get; init; }  // "system", "user", "assistant"
    public string Content { get; init; }
}

public class Tool
{
    public string Name { get; init; }
    public string Description { get; init; }
    public Dictionary<string, object> Parameters { get; init; }
}
```

### InferenceResponse

```csharp
public class InferenceResponse
{
    /// <summary>Generated content.</summary>
    public string Content { get; init; }
    
    /// <summary>Input tokens used.</summary>
    public int InputTokens { get; init; }
    
    /// <summary>Output tokens generated.</summary>
    public int OutputTokens { get; init; }
    
    /// <summary>Finish reason (stop, length, tool_use).</summary>
    public string FinishReason { get; init; }
    
    /// <summary>Metrics about the response.</summary>
    public ProviderMetrics Metrics { get; init; }
}

public class ProviderMetrics
{
    /// <summary>Response latency in milliseconds.</summary>
    public int LatencyMs { get; init; }
    
    /// <summary>Estimated cost of this request.</summary>
    public decimal EstimatedCost { get; init; }
    
    /// <summary>When request was made.</summary>
    public DateTime RequestTime { get; init; }
    
    /// <summary>Provider ID that handled request.</summary>
    public string ProviderId { get; init; }
}
```

### ProviderStatus

```csharp
public enum ProviderStatus
{
    /// <summary>Provider is healthy and responsive.</summary>
    Healthy = 0,
    
    /// <summary>Provider is working but degraded (slow/partial outage).</summary>
    Degraded = 1,
    
    /// <summary>Provider is not accessible.</summary>
    Unhealthy = 2
}
```

**Example Usage:**

```csharp
var request = new InferenceRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new()
    {
        new() { Role = "user", Content = "What is 2+2?" }
    },
    MaxTokens = 100,
    Temperature = 0.7f
};

var result = await provider.InferenceAsync(request);

if (result.IsSuccess)
{
    var response = result.Value;
    Console.WriteLine($"Response: {response.Content}");
    Console.WriteLine($"Cost: ${response.Metrics.EstimatedCost:F4}");
    Console.WriteLine($"Latency: {response.Metrics.LatencyMs}ms");
}
else
{
    Console.WriteLine($"Error: {result.Error.Message}");
}
```

---

## Smart Router API

### ISmartRouter

Routes requests to optimal providers.

```csharp
public interface ISmartRouter : IServiceComponent
{
    /// <summary>Executes inference with automatic provider selection.</summary>
    /// <param name="request">Inference request</param>
    /// <param name="strategy">Routing strategy to use</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Inference response</returns>
    Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request,
        IRoutingStrategy strategy,
        CancellationToken ct = default);
    
    /// <summary>Gets statistics about routing decisions.</summary>
    /// <returns>Routing statistics</returns>
    Task<RoutingStats> GetStatsAsync();
    
    /// <summary>Registers a new provider.</summary>
    /// <param name="provider">Provider to register</param>
    Task RegisterProviderAsync(IProviderAdapter provider);
    
    /// <summary>Unregisters a provider.</summary>
    /// <param name="providerName">Name of provider to unregister</param>
    Task UnregisterProviderAsync(string providerName);
}
```

### IRoutingStrategy

```csharp
public interface IRoutingStrategy
{
    /// <summary>Strategy name.</summary>
    string Name { get; }
    
    /// <summary>Priority weight for this strategy.</summary>
    int Priority { get; }
    
    /// <summary>Calculates score for a provider.</summary>
    /// <param name="provider">Provider to score</param>
    /// <param name="context">Routing context</param>
    /// <returns>Score (higher is better)</returns>
    decimal CalculateScore(ProviderCandidate provider, RoutingContext context);
}
```

### Built-in Strategies

```csharp
// Cost optimization - picks cheapest provider
var strategy = new CostOptimizationStrategy();

// Latency optimization - picks fastest provider
var strategy = new LatencyOptimizationStrategy();

// Quality optimization - picks best model
var strategy = new QualityOptimizationStrategy();

// Balanced - mix of all factors
var strategy = new BalancedStrategy();

// Adaptive - changes based on request metadata
var strategy = new AdaptiveStrategy();
```

**Example Usage:**

```csharp
var router = serviceProvider.GetRequiredService<ISmartRouter>();
var strategy = new LatencyOptimizationStrategy();

var request = new InferenceRequest { /* ... */ };
var result = await router.InferenceAsync(request, strategy);

var stats = await router.GetStatsAsync();
Console.WriteLine($"Total requests: {stats.TotalRequests}");
Console.WriteLine($"Average latency: {stats.AverageLatency}ms");
```

---

## Health Monitor API

### IHealthMonitor

Monitors health of all providers.

```csharp
public interface IHealthMonitor : IServiceComponent
{
    /// <summary>Gets health of a specific provider.</summary>
    /// <param name="providerName">Name of provider</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Health status</returns>
    Task<HealthStatus> GetProviderHealthAsync(
        string providerName,
        CancellationToken ct = default);
    
    /// <summary>Gets health of all providers.</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Map of provider health</returns>
    Task<Dictionary<string, HealthStatus>> GetAllHealthAsync(
        CancellationToken ct = default);
    
    /// <summary>Subscribes to health change events.</summary>
    /// <param name="providerName">Provider to monitor</param>
    /// <param name="handler">Event handler</param>
    /// <returns>Disposable subscription</returns>
    IDisposable OnHealthChanged(
        string providerName,
        Func<HealthStatusChangeEvent, Task> handler);
    
    /// <summary>Gets health history for a provider.</summary>
    /// <param name="providerName">Provider name</param>
    /// <param name="minutes">History window in minutes</param>
    /// <returns>Health history</returns>
    Task<List<HealthHistoryEntry>> GetHealthHistoryAsync(
        string providerName,
        int minutes = 60);
}
```

### HealthStatus

```csharp
public class HealthStatus
{
    /// <summary>Component ID.</summary>
    public string ComponentId { get; init; }
    
    /// <summary>Current health state.</summary>
    public HealthState State { get; init; }
    
    /// <summary>Detailed reason if unhealthy.</summary>
    public string Reason { get; init; }
    
    /// <summary>When status was last updated.</summary>
    public DateTime LastChecked { get; init; }
    
    /// <summary>Exception that caused unhealthy state (if any).</summary>
    public Exception Exception { get; init; }
    
    public static HealthStatus Healthy(string componentId) =>
        new() { ComponentId = componentId, State = HealthState.Healthy };
    
    public static HealthStatus Unhealthy(
        string componentId,
        string reason,
        Exception ex = null) =>
        new()
        {
            ComponentId = componentId,
            State = HealthState.Unhealthy,
            Reason = reason,
            Exception = ex
        };
}

public enum HealthState
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2
}
```

**Example Usage:**

```csharp
var monitor = serviceProvider.GetRequiredService<IHealthMonitor>();

// Get specific provider health
var health = await monitor.GetProviderHealthAsync("OpenAI");
Console.WriteLine($"Status: {health.State}");

// Get all health
var allHealth = await monitor.GetAllHealthAsync();
foreach (var (provider, status) in allHealth)
{
    Console.WriteLine($"{provider}: {status.State}");
}

// Subscribe to changes
monitor.OnHealthChanged("Groq", async evt =>
{
    Console.WriteLine($"Health changed: {evt.OldState} → {evt.NewState}");
});

// Get history
var history = await monitor.GetHealthHistoryAsync("OpenAI", 60);
```

---

## Failover Controller API

### IFailoverController

Handles provider failover automatically.

```csharp
public interface IFailoverController : IServiceComponent
{
    /// <summary>Gets current failover status.</summary>
    Task<FailoverStatus> GetStatusAsync();
    
    /// <summary>Manually trigger failover to backup provider.</summary>
    /// <param name="fromProvider">Current provider</param>
    /// <param name="reason">Reason for failover</param>
    /// <param name="ct">Cancellation token</param>
    Task<Result> TriggerFailoverAsync(
        string fromProvider,
        string reason,
        CancellationToken ct = default);
    
    /// <summary>Gets failover events.</summary>
    /// <param name="hours">History window</param>
    /// <returns>Failover events</returns>
    Task<List<FailoverEvent>> GetFailoverHistoryAsync(int hours = 24);
    
    /// <summary>Subscribes to failover events.</summary>
    /// <param name="handler">Event handler</param>
    /// <returns>Disposable subscription</returns>
    IDisposable OnFailover(Func<FailoverEvent, Task> handler);
}
```

### Failover Configuration

```csharp
public class FailoverConfiguration
{
    /// <summary>Enable automatic failover.</summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>Failures before triggering failover.</summary>
    public int FailureThreshold { get; init; } = 5;
    
    /// <summary>Time window for counting failures (ms).</summary>
    public int FailureWindow { get; init; } = 60000;
    
    /// <summary>Time before retrying failed provider (ms).</summary>
    public int RecoveryTimeout { get; init; } = 300000;
    
    /// <summary>Prioritized list of fallback providers.</summary>
    public List<string> FallbackProviders { get; init; }
}
```

**Example Usage:**

```csharp
var failover = serviceProvider
    .GetRequiredService<IFailoverController>();

// Get current status
var status = await failover.GetStatusAsync();
Console.WriteLine($"Active provider: {status.ActiveProvider}");

// Manually failover
var result = await failover.TriggerFailoverAsync(
    "OpenAI",
    "Rate limit exceeded");

// Monitor failover events
failover.OnFailover(async evt =>
{
    Console.WriteLine(
        $"Failover: {evt.FailedProvider} → {evt.BackupProvider}");
});
```

---

## Provider Registry API

### IProviderRegistry

Manages provider registration and discovery.

```csharp
public interface IProviderRegistry : IServiceComponent
{
    /// <summary>Registers a provider.</summary>
    /// <param name="provider">Provider to register</param>
    /// <param name="ct">Cancellation token</param>
    Task<Result> RegisterAsync(
        IProviderAdapter provider,
        CancellationToken ct = default);
    
    /// <summary>Unregisters a provider.</summary>
    /// <param name="providerName">Provider name</param>
    /// <param name="ct">Cancellation token</param>
    Task<Result> UnregisterAsync(
        string providerName,
        CancellationToken ct = default);
    
    /// <summary>Gets a specific provider.</summary>
    /// <param name="providerName">Provider name</param>
    /// <returns>Provider or null</returns>
    IProviderAdapter GetProvider(string providerName);
    
    /// <summary>Gets all registered providers.</summary>
    /// <returns>List of providers</returns>
    IEnumerable<IProviderAdapter> GetAllProviders();
    
    /// <summary>Finds providers by capability.</summary>
    /// <param name="capability">Required capability</param>
    /// <returns>Matching providers</returns>
    IEnumerable<IProviderAdapter> FindByCapability(
        string capability);
}
```

**Example Usage:**

```csharp
var registry = serviceProvider
    .GetRequiredService<IProviderRegistry>();

// Register a new provider
var provider = new CustomProvider(context);
await registry.RegisterAsync(provider);

// Get specific provider
var openai = registry.GetProvider("OpenAI");

// Find providers with specific capabilities
var visionProviders = registry.FindByCapability("vision");
```

---

## Error Types

### Result<T>

All operations return Result<T> for type-safe error handling:

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(ErrorCode Code, string Message, Exception Exception) : Result<T>;
    
    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;
    
    public T Value => this is Success s ? s.Value : throw new InvalidOperationException();
    public ErrorCode Code => this is Failure f ? f.Code : throw new InvalidOperationException();
}
```

### ErrorCode

Pre-defined error codes for type-safe error handling:

```csharp
public enum ErrorCode
{
    // Configuration errors (1000-1999)
    ConfigurationError = 1000,
    InvalidConfiguration = 1001,
    MissingConfiguration = 1002,
    
    // Provider errors (2000-2999)
    ProviderError = 2000,
    ProviderUnavailable = 2001,
    ProviderUnauthorized = 2002,
    ProviderRateLimited = 2003,
    ProviderQuotaExceeded = 2004,
    
    // Validation errors (3000-3999)
    ValidationError = 3000,
    InvalidInput = 3001,
    MissingInput = 3002,
    
    // Operation errors (4000-4999)
    OperationFailed = 4000,
    OperationTimeout = 4001,
    OperationCanceled = 4002,
    
    // Routing errors (5000-5999)
    RoutingError = 5000,
    NoProvidersAvailable = 5001,
    AllProvidersFailed = 5002,
    
    // System errors (6000-6999)
    SystemError = 6000,
    OutOfMemory = 6001,
    InternalError = 6002
}
```

### Exception Handling

```csharp
// Instead of try-catch
try
{
    var result = await provider.InferenceAsync(request);
}
catch (Exception ex)
{
    // Generic exception handling
}

// Use Result<T> pattern
var result = await provider.InferenceAsync(request);

if (result.IsFailure)
{
    var failure = result as Result<T>.Failure;
    switch (failure.Code)
    {
        case ErrorCode.ProviderRateLimited:
            // Handle rate limiting
            await Task.Delay(1000);
            break;
        
        case ErrorCode.ProviderUnauthorized:
            // Handle auth failure
            Console.WriteLine("Invalid credentials");
            break;
        
        default:
            // Generic error handling
            Console.WriteLine($"Error: {failure.Message}");
            break;
    }
}
```

---

## Response Models

### Common Response Structures

```csharp
// Success response
public class SuccessResponse<T>
{
    public bool Success { get; init; } = true;
    public T Data { get; init; }
    public string CorrelationId { get; init; }
}

// Error response
public class ErrorResponse
{
    public bool Success { get; init; } = false;
    public string Code { get; init; }
    public string Message { get; init; }
    public Dictionary<string, string> Details { get; init; }
    public string CorrelationId { get; init; }
}

// Inference response
public class InferenceApiResponse
{
    public string Id { get; init; }
    public InferenceResponse Result { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}

// Batch response
public class BatchResponse
{
    public List<InferenceApiResponse> Results { get; init; }
    public BatchStatistics Statistics { get; init; }
}

public class BatchStatistics
{
    public int Total { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public long TotalLatencyMs { get; init; }
    public decimal TotalCost { get; init; }
}
```

---

## Complete Example: Using Multiple APIs

```csharp
// Setup
var router = serviceProvider.GetRequiredService<ISmartRouter>();
var monitor = serviceProvider.GetRequiredService<IHealthMonitor>();
var failover = serviceProvider.GetRequiredService<IFailoverController>();
var registry = serviceProvider.GetRequiredService<IProviderRegistry>();

// Check health before proceeding
var allHealth = await monitor.GetAllHealthAsync();
var healthyProviders = allHealth
    .Where(x => x.Value.State == HealthState.Healthy)
    .Count();

Console.WriteLine($"Healthy providers: {healthyProviders}");

// Create request
var request = new InferenceRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new() { new() { Role = "user", Content = "Hello" } },
    MaxTokens = 100
};

// Execute with adaptive routing
var strategy = new AdaptiveStrategy();
var result = await router.InferenceAsync(request, strategy);

if (result.IsSuccess)
{
    var response = result.Value;
    Console.WriteLine($"Response: {response.Content}");
    Console.WriteLine($"Cost: ${response.Metrics.EstimatedCost:F4}");
}
else
{
    Console.WriteLine($"Error: {result.Code}");
}

// Subscribe to health changes
monitor.OnHealthChanged("OpenAI", async evt =>
{
    Console.WriteLine($"OpenAI health: {evt.NewState}");
    if (evt.NewState == HealthState.Unhealthy)
    {
        await failover.TriggerFailoverAsync("OpenAI", "Health check failed");
    }
});
```

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
