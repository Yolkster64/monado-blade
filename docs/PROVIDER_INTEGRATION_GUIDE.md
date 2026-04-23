# Provider Integration Guide - Monado Blade v2.4.0

**Complete guide to integrating new providers and understanding the provider architecture**

*Read time: 20-30 minutes | Skill level: Intermediate*

## Table of Contents

1. [Overview](#overview)
2. [Provider Architecture](#provider-architecture)
3. [The ProviderAdapter Interface](#the-provideradapter-interface)
4. [5-Minute Quick Integration](#5-minute-quick-integration)
5. [Step-by-Step Integration Example](#step-by-step-integration-example)
6. [Worked Examples](#worked-examples)
7. [Advanced Patterns](#advanced-patterns)
8. [Troubleshooting](#troubleshooting)
9. [Performance Tuning](#performance-tuning)

---

## Overview

### What is a Provider?

A Provider in Monado Blade is an abstraction for any external AI/ML service or model:
- **Cloud APIs** (OpenAI, Anthropic, Groq, Replicate)
- **Local Models** (LM Studio, Ollama, Local LLaMA)
- **Proprietary Services** (Azure OpenAI, AWS Bedrock)
- **Custom Models** (Your own inference servers)

### Why the Provider Pattern?

The provider pattern enables:
- ✅ **Vendor Agnosticism** - Swap providers without code changes
- ✅ **Load Balancing** - Distribute requests across providers
- ✅ **Cost Optimization** - Use cheapest provider per request
- ✅ **Latency Optimization** - Use fastest provider per request
- ✅ **Fallback Handling** - Automatic failover to backup providers
- ✅ **Monitoring** - Track metrics per provider
- ✅ **Caching** - Cache responses by provider capability

---

## Provider Architecture

### System Architecture

```
┌─────────────────────────────────────┐
│     Your Application                │
├─────────────────────────────────────┤
│         SmartRouter                 │
│   (Chooses which provider to use)   │
├────────────┬────────────┬───────────┤
│            │            │           │
▼            ▼            ▼           ▼
┌────┐  ┌────┐  ┌────┐  ┌────┐
│OP │  │AnthropicGrqProv│ │Azure │
│Prov│  │Provider         │ │Prov  │
└────┘  └────┘  └────┘  └────┘

       (Each implements IProviderAdapter)

        │            │            │           │
        └────────────┼────────────┘           │
                     │                         │
         ┌───────────▼───────────┐            │
         │   HealthMonitor       │            │
         │   (Tracks health)     │            │
         └───────────────────────┘            │
                                              │
         ┌────────────────────────────────────▼─────┐
         │    FailoverController                    │
         │    (Handles provider failures)           │
         └────────────────────────────────────────────┘
```

### Provider Lifecycle

```
┌──────────┐
│ Create   │ - New provider instance
└────┬─────┘
     │
     ▼
┌──────────────┐
│ Initialize  │ - Load config, auth, connect
└────┬─────────┘
     │
     ▼
┌──────────────┐
│ Ready       │ - Accept requests
└────┬─────────┘
     │
     ├─────────────────────┐
     │                     │
     ▼                     ▼
┌──────────────┐   ┌──────────────┐
│ Inference   │   │ Health Check │
└────┬─────────┘   └──────┬───────┘
     │                     │
     ▼                     ▼
┌──────────────┐   ┌──────────────┐
│ Monitoring  │───▶│ Metrics      │
└─────────────┘    └──────────────┘
     │
     ├─────────────────────┐
     │ (repeat until)      │
     └─────────────────────┘
                     │
                     ▼
          ┌──────────────────┐
          │ Shutdown         │
          │ (Clean up)       │
          └──────────────────┘
```

---

## The ProviderAdapter Interface

### Core Interface Definition

```csharp
public interface IProviderAdapter : IServiceComponent
{
    /// <summary>Gets provider name (e.g., "OpenAI", "Anthropic").</summary>
    string ProviderName { get; }
    
    /// <summary>Gets provider version (e.g., "gpt-4-turbo").</summary>
    string ProviderVersion { get; }
    
    /// <summary>Gets capabilities this provider supports.</summary>
    ProviderCapabilities Capabilities { get; }
    
    /// <summary>Gets current cost per 1K tokens.</summary>
    decimal CostPer1kTokens { get; }
    
    /// <summary>Gets average latency (ms) for this provider.</summary>
    int AverageLatencyMs { get; }
    
    /// <summary>Executes inference request.</summary>
    Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request,
        CancellationToken ct = default);
    
    /// <summary>Gets provider availability status.</summary>
    Task<ProviderStatus> GetStatusAsync(CancellationToken ct = default);
}

public class ProviderCapabilities
{
    public bool SupportsStreaming { get; init; }
    public bool SupportsVision { get; init; }
    public bool SupportsTools { get; init; }
    public int MaxContextLength { get; init; }
    public int MaxOutputLength { get; init; }
    public decimal CostPerInputToken { get; init; }
    public decimal CostPerOutputToken { get; init; }
}

public class InferenceRequest
{
    public string Model { get; init; }
    public List<Message> Messages { get; init; }
    public int? MaxTokens { get; init; }
    public float Temperature { get; init; }
    public List<Tool>? Tools { get; init; }
    public bool Stream { get; init; }
}

public class InferenceResponse
{
    public string Content { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public ProviderMetrics Metrics { get; init; }
}

public class ProviderMetrics
{
    public int LatencyMs { get; init; }
    public decimal EstimatedCost { get; init; }
    public DateTime RequestTime { get; init; }
}
```

### Key Properties Explained

| Property | Purpose | Example |
|----------|---------|---------|
| `ProviderName` | Unique identifier for provider | `"OpenAI"`, `"Anthropic"` |
| `Capabilities` | What features this provider supports | Vision, streaming, tools |
| `CostPer1kTokens` | For cost optimization routing | $0.03, $0.01 |
| `AverageLatencyMs` | For latency optimization routing | 250ms, 500ms |

---

## 5-Minute Quick Integration

### The Minimum Viable Provider

```csharp
using MonadoBlade.Core.Common;

namespace MonadoBlade.Providers;

/// <summary>Minimal provider implementation - extend for production.</summary>
public class MinimalProvider : ServiceComponentBase, IProviderAdapter
{
    private readonly HttpClient _client;
    private string _apiKey;
    
    public string ProviderName => "MinimalProvider";
    public string ProviderVersion => "1.0.0";
    
    public ProviderCapabilities Capabilities => new()
    {
        SupportsStreaming = false,
        SupportsVision = false,
        SupportsTools = false,
        MaxContextLength = 4096,
        MaxOutputLength = 2048,
        CostPerInputToken = 0.0001m,
        CostPerOutputToken = 0.0002m
    };
    
    public decimal CostPer1kTokens => 0.15m;
    public int AverageLatencyMs => 500;
    
    public MinimalProvider(IServiceContext context) 
        : base(context, "minimal-provider")
    {
        _client = new HttpClient();
    }
    
    protected override async Task<Result> OnInitializeAsync(
        IServiceContext context, 
        CancellationToken ct)
    {
        _apiKey = context.Configuration.Get<string>("PROVIDER_API_KEY");
        if (string.IsNullOrEmpty(_apiKey))
            return ErrorCode.ConfigurationError.ToFailure("API key not configured");
        
        _client.DefaultRequestHeaders.Authorization = 
            new("Bearer", _apiKey);
        
        return Result.Success();
    }
    
    public async Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request, 
        CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Call your provider's API here
            var response = await CallProviderApiAsync(request, ct);
            
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return Result.Success(new InferenceResponse
            {
                Content = response.Content,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Metrics = new ProviderMetrics
                {
                    LatencyMs = latency,
                    EstimatedCost = CalculateCost(
                        response.InputTokens, 
                        response.OutputTokens),
                    RequestTime = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Inference failed in {ProviderName}", ex);
            return ErrorCode.ProviderError.ToFailure(ex.Message, ex);
        }
    }
    
    public async Task<ProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.GetAsync("/health", ct);
            return response.IsSuccessStatusCode 
                ? ProviderStatus.Healthy 
                : ProviderStatus.Degraded;
        }
        catch
        {
            return ProviderStatus.Unhealthy;
        }
    }
    
    protected override Task<HealthStatus> OnGetHealthAsync(CancellationToken ct) =>
        Task.FromResult(HealthStatus.Healthy(ComponentId));
    
    private async Task<ProviderApiResponse> CallProviderApiAsync(
        InferenceRequest request, 
        CancellationToken ct)
    {
        // TODO: Implement actual API call
        throw new NotImplementedException();
    }
    
    private decimal CalculateCost(int inputTokens, int outputTokens) =>
        (inputTokens * Capabilities.CostPerInputToken) +
        (outputTokens * Capabilities.CostPerOutputToken);
}
```

**That's it!** This provider:
- ✅ Inherits from `ServiceComponentBase` (lifecycle management)
- ✅ Implements `IProviderAdapter` (compatibility)
- ✅ Has initialization logic
- ✅ Handles health checks
- ✅ Performs inference
- ✅ Tracks metrics
- ✅ Manages API keys securely

---

## Step-by-Step Integration Example

### Step 1: Create the Provider Class

```csharp
public class CustomProvider : ServiceComponentBase, IProviderAdapter
{
    private readonly HttpClient _httpClient;
    private ILoggingProvider _logger;
    
    public string ProviderName => "CustomProvider";
    public string ProviderVersion => "2.0.0";
    
    public CustomProvider(IServiceContext context) 
        : base(context, "custom-provider")
    {
        _httpClient = new HttpClient();
        _logger = context.Logger;
    }
}
```

### Step 2: Define Capabilities

```csharp
public ProviderCapabilities Capabilities => new()
{
    SupportsStreaming = true,
    SupportsVision = true,
    SupportsTools = true,
    MaxContextLength = 128000,
    MaxOutputLength = 4096,
    CostPerInputToken = 0.00003m,  // $0.03 per 1M input tokens
    CostPerOutputToken = 0.00006m  // $0.06 per 1M output tokens
};
```

### Step 3: Implement Initialization

```csharp
protected override async Task<Result> OnInitializeAsync(
    IServiceContext context, 
    CancellationToken ct)
{
    _logger.Information($"Initializing {ProviderName}...");
    
    // Load configuration
    var apiKeyResult = context.Configuration.Get<string>("CUSTOM_API_KEY");
    var apiUrlResult = context.Configuration.Get<string>("CUSTOM_API_URL");
    
    if (apiKeyResult is null || apiUrlResult is null)
        return ErrorCode.ConfigurationError
            .ToFailure("Missing required configuration");
    
    // Setup authentication
    _httpClient.DefaultRequestHeaders.Add(
        "Authorization", 
        $"Bearer {apiKeyResult}");
    
    _httpClient.BaseAddress = new Uri(apiUrlResult);
    
    // Test connection
    var testResult = await TestConnectionAsync(ct);
    if (testResult.IsFailure)
        return testResult;
    
    _logger.Information($"{ProviderName} initialized successfully");
    return Result.Success();
}
```

### Step 4: Implement Inference

```csharp
public async Task<Result<InferenceResponse>> InferenceAsync(
    InferenceRequest request, 
    CancellationToken ct)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        // Validate request
        if (request.MaxTokens > Capabilities.MaxOutputLength)
            return ErrorCode.ValidationError
                .ToFailure("Max tokens exceeds provider limit");
        
        // Transform request to provider format
        var providerRequest = TransformRequest(request);
        
        // Call provider API
        var apiResponse = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            providerRequest,
            ct);
        
        if (!apiResponse.IsSuccessStatusCode)
        {
            var errorContent = await apiResponse.Content.ReadAsStringAsync(ct);
            return ErrorCode.ProviderError
                .ToFailure($"Provider returned {apiResponse.StatusCode}: {errorContent}");
        }
        
        // Parse response
        var providerResponse = await apiResponse.Content
            .ReadAsAsync<ProviderApiResponse>(ct);
        
        // Calculate metrics
        var latency = (int)stopwatch.ElapsedMilliseconds;
        var cost = CalculateCost(
            providerResponse.Usage.PromptTokens,
            providerResponse.Usage.CompletionTokens);
        
        // Log metrics
        _logger.Information(
            $"Inference completed: {latency}ms, {providerResponse.Usage.TotalTokens} tokens, ${cost:F4}");
        
        return Result.Success(new InferenceResponse
        {
            Content = providerResponse.Choices[0].Message.Content,
            InputTokens = providerResponse.Usage.PromptTokens,
            OutputTokens = providerResponse.Usage.CompletionTokens,
            Metrics = new ProviderMetrics
            {
                LatencyMs = latency,
                EstimatedCost = cost,
                RequestTime = DateTime.UtcNow
            }
        });
    }
    catch (TaskCanceledException ex)
    {
        _logger.Warning($"Request timeout in {ProviderName}: {ex.Message}");
        return ErrorCode.TimeoutError.ToFailure("Request timeout", ex);
    }
    catch (Exception ex)
    {
        _logger.Error($"Inference failed in {ProviderName}", ex);
        return ErrorCode.ProviderError.ToFailure(ex.Message, ex);
    }
}
```

### Step 5: Implement Health Checks

```csharp
public async Task<ProviderStatus> GetStatusAsync(CancellationToken ct)
{
    try
    {
        var response = await _httpClient.GetAsync("/health", ct);
        return response.IsSuccessStatusCode 
            ? ProviderStatus.Healthy 
            : ProviderStatus.Degraded;
    }
    catch (TaskCanceledException)
    {
        return ProviderStatus.Unhealthy;
    }
    catch (Exception ex)
    {
        _logger.Warning($"Health check failed: {ex.Message}");
        return ProviderStatus.Unhealthy;
    }
}
```

### Step 6: Register the Provider

```csharp
// In your DI container setup
services.AddSingleton<IProviderAdapter>(sp =>
{
    var context = sp.GetRequiredService<IServiceContext>();
    var provider = new CustomProvider(context);
    provider.InitializeAsync(context).GetAwaiter().GetResult();
    return provider;
});

// Or use the ProviderRegistry
var registry = sp.GetRequiredService<IProviderRegistry>();
var provider = new CustomProvider(context);
await provider.InitializeAsync(context);
await registry.RegisterAsync(provider);
```

---

## Worked Examples

### Example 1: Groq API Provider (Fast Inference)

Groq offers extremely fast inference (sub-100ms). Here's a complete implementation:

```csharp
public class GroqProvider : ServiceComponentBase, IProviderAdapter
{
    private readonly HttpClient _client;
    private string _apiKey;
    private string _baseUrl = "https://api.groq.com/openai/v1";
    
    public string ProviderName => "Groq";
    public string ProviderVersion => "1.0.0";
    
    public ProviderCapabilities Capabilities => new()
    {
        SupportsStreaming = true,
        SupportsVision = false,  // As of 2024
        SupportsTools = false,
        MaxContextLength = 32768,
        MaxOutputLength = 4096,
        CostPerInputToken = 0.0000005m,  // Very cheap
        CostPerOutputToken = 0.0000015m
    };
    
    public decimal CostPer1kTokens => 0.0005m;
    public int AverageLatencyMs => 80;  // Sub-100ms inference
    
    public GroqProvider(IServiceContext context) 
        : base(context, "groq-provider")
    {
        _client = new HttpClient();
    }
    
    protected override async Task<Result> OnInitializeAsync(
        IServiceContext context, 
        CancellationToken ct)
    {
        _apiKey = context.Configuration.Get<string>("GROQ_API_KEY");
        if (string.IsNullOrEmpty(_apiKey))
            return ErrorCode.ConfigurationError
                .ToFailure("GROQ_API_KEY not configured");
        
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        
        // Test connection
        try
        {
            var response = await _client.GetAsync(
                $"{_baseUrl}/models",
                ct);
            
            if (!response.IsSuccessStatusCode)
                return ErrorCode.ProviderError
                    .ToFailure("Failed to connect to Groq");
        }
        catch (Exception ex)
        {
            return ErrorCode.ProviderError
                .ToFailure("Connection test failed", ex);
        }
        
        return Result.Success();
    }
    
    public async Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request, 
        CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var groqRequest = new
            {
                model = "mixtral-8x7b-32768",  // Recommended for speed
                messages = request.Messages.Select(m => new
                {
                    role = m.Role.ToLower(),
                    content = m.Content
                }),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 1024,
                stream = false
            };
            
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/chat/completions")
            {
                Content = JsonContent.Create(groqRequest)
            };
            
            var response = await _client.SendAsync(httpRequest, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return ErrorCode.ProviderError
                    .ToFailure($"Groq error: {response.StatusCode} - {error}");
            }
            
            var result = await response.Content
                .ReadAsAsync<GroqResponse>(ct);
            
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var cost = CalculateCost(
                result.Usage.PromptTokens,
                result.Usage.CompletionTokens);
            
            return Result.Success(new InferenceResponse
            {
                Content = result.Choices[0].Message.Content,
                InputTokens = result.Usage.PromptTokens,
                OutputTokens = result.Usage.CompletionTokens,
                Metrics = new ProviderMetrics
                {
                    LatencyMs = latency,
                    EstimatedCost = cost,
                    RequestTime = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error("Groq inference failed", ex);
            return ErrorCode.ProviderError.ToFailure(ex.Message, ex);
        }
    }
    
    public async Task<ProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.GetAsync(
                $"{_baseUrl}/models",
                ct);
            return response.IsSuccessStatusCode 
                ? ProviderStatus.Healthy 
                : ProviderStatus.Degraded;
        }
        catch
        {
            return ProviderStatus.Unhealthy;
        }
    }
    
    protected override Task<HealthStatus> OnGetHealthAsync(CancellationToken ct) =>
        Task.FromResult(HealthStatus.Healthy(ComponentId));
    
    private decimal CalculateCost(int inputTokens, int outputTokens) =>
        (inputTokens * Capabilities.CostPerInputToken) +
        (outputTokens * Capabilities.CostPerOutputToken);
    
    private class GroqResponse
    {
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
    }
    
    private class Choice
    {
        public Message Message { get; set; }
    }
    
    private class Message
    {
        public string Content { get; set; }
    }
    
    private class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
```

**Usage:**
```csharp
// Register Groq provider
services.AddSingleton<IProviderAdapter, GroqProvider>();

// The SmartRouter will automatically use Groq for latency-sensitive tasks
```

### Example 2: LM Studio Local Model Provider

For local inference without API calls:

```csharp
public class LMStudioProvider : ServiceComponentBase, IProviderAdapter
{
    private readonly HttpClient _client;
    private string _serverUrl;
    
    public string ProviderName => "LMStudio";
    public string ProviderVersion => "1.0.0";
    
    public ProviderCapabilities Capabilities => new()
    {
        SupportsStreaming = true,
        SupportsVision = false,
        SupportsTools = false,
        MaxContextLength = 4096,
        MaxOutputLength = 2048,
        CostPerInputToken = 0,  // Local - free
        CostPerOutputToken = 0
    };
    
    public decimal CostPer1kTokens => 0;  // Free!
    public int AverageLatencyMs => 3000;  // GPU-dependent
    
    public LMStudioProvider(IServiceContext context) 
        : base(context, "lmstudio-provider")
    {
        _client = new HttpClient();
    }
    
    protected override async Task<Result> OnInitializeAsync(
        IServiceContext context, 
        CancellationToken ct)
    {
        _serverUrl = context.Configuration.Get<string>(
            "LMSTUDIO_URL",
            "http://localhost:1234");
        
        // Verify LM Studio is running
        try
        {
            var response = await _client.GetAsync(
                $"{_serverUrl}/api/config",
                ct);
            
            if (!response.IsSuccessStatusCode)
                return ErrorCode.ProviderError
                    .ToFailure("LM Studio server not accessible");
        }
        catch (Exception ex)
        {
            return ErrorCode.ProviderError
                .ToFailure("Cannot connect to LM Studio", ex);
        }
        
        return Result.Success();
    }
    
    public async Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request, 
        CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var lmRequest = new
            {
                messages = request.Messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens ?? 1024
            };
            
            var httpResponse = await _client.PostAsJsonAsync(
                $"{_serverUrl}/api/chat/completions",
                lmRequest,
                ct);
            
            if (!httpResponse.IsSuccessStatusCode)
                return ErrorCode.ProviderError
                    .ToFailure("LM Studio request failed");
            
            var response = await httpResponse.Content
                .ReadAsAsync<LMStudioResponse>(ct);
            
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return Result.Success(new InferenceResponse
            {
                Content = response.Choices[0].Message.Content,
                InputTokens = response.Usage.PromptTokens,
                OutputTokens = response.Usage.CompletionTokens,
                Metrics = new ProviderMetrics
                {
                    LatencyMs = latency,
                    EstimatedCost = 0,  // Free!
                    RequestTime = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error("LM Studio inference failed", ex);
            return ErrorCode.ProviderError.ToFailure(ex.Message, ex);
        }
    }
    
    public async Task<ProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.GetAsync(
                $"{_serverUrl}/api/config",
                ct);
            return response.IsSuccessStatusCode 
                ? ProviderStatus.Healthy 
                : ProviderStatus.Unhealthy;
        }
        catch
        {
            return ProviderStatus.Unhealthy;
        }
    }
    
    protected override Task<HealthStatus> OnGetHealthAsync(CancellationToken ct) =>
        Task.FromResult(HealthStatus.Healthy(ComponentId));
    
    private class LMStudioResponse
    {
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
    }
    
    private class Choice
    {
        public Message Message { get; set; }
    }
    
    private class Message
    {
        public string Content { get; set; }
    }
    
    private class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
```

### Example 3: Replicate Hosted Models Provider

For on-demand model scaling:

```csharp
public class ReplicateProvider : ServiceComponentBase, IProviderAdapter
{
    private readonly HttpClient _client;
    private string _apiKey;
    
    public string ProviderName => "Replicate";
    public string ProviderVersion => "1.0.0";
    
    public ProviderCapabilities Capabilities => new()
    {
        SupportsStreaming = false,
        SupportsVision = true,
        SupportsTools = false,
        MaxContextLength = 4096,
        MaxOutputLength = 1024,
        CostPerInputToken = 0.00005m,
        CostPerOutputToken = 0.00015m
    };
    
    public decimal CostPer1kTokens => 0.0005m;
    public int AverageLatencyMs => 2000;
    
    public ReplicateProvider(IServiceContext context) 
        : base(context, "replicate-provider")
    {
        _client = new HttpClient();
    }
    
    protected override async Task<Result> OnInitializeAsync(
        IServiceContext context, 
        CancellationToken ct)
    {
        _apiKey = context.Configuration.Get<string>("REPLICATE_API_KEY");
        if (string.IsNullOrEmpty(_apiKey))
            return ErrorCode.ConfigurationError
                .ToFailure("REPLICATE_API_KEY not configured");
        
        _client.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {_apiKey}");
        
        return Result.Success();
    }
    
    public async Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request, 
        CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Start prediction
            var predictionRequest = new
            {
                version = "abc123",  // Model version
                input = new
                {
                    prompt = request.Messages.Last().Content,
                    max_tokens = request.MaxTokens ?? 1024
                }
            };
            
            var response = await _client.PostAsJsonAsync(
                "https://api.replicate.com/v1/predictions",
                predictionRequest,
                ct);
            
            var prediction = await response.Content
                .ReadAsAsync<ReplicatePrediction>(ct);
            
            // Poll for completion
            var completedPrediction = await PollForCompletion(
                prediction.Id,
                ct);
            
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return Result.Success(new InferenceResponse
            {
                Content = string.Join("", completedPrediction.Output),
                InputTokens = EstimateTokens(request.Messages.Last().Content),
                OutputTokens = EstimateTokens(
                    string.Join("", completedPrediction.Output)),
                Metrics = new ProviderMetrics
                {
                    LatencyMs = latency,
                    EstimatedCost = 0.001m,  // Approximate
                    RequestTime = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error("Replicate inference failed", ex);
            return ErrorCode.ProviderError.ToFailure(ex.Message, ex);
        }
    }
    
    public async Task<ProviderStatus> GetStatusAsync(CancellationToken ct)
    {
        try
        {
            var response = await _client.GetAsync(
                "https://api.replicate.com/v1/predictions",
                ct);
            return response.IsSuccessStatusCode 
                ? ProviderStatus.Healthy 
                : ProviderStatus.Degraded;
        }
        catch
        {
            return ProviderStatus.Unhealthy;
        }
    }
    
    protected override Task<HealthStatus> OnGetHealthAsync(CancellationToken ct) =>
        Task.FromResult(HealthStatus.Healthy(ComponentId));
    
    private async Task<ReplicatePrediction> PollForCompletion(
        string predictionId,
        CancellationToken ct,
        int maxAttempts = 60)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(1000, ct);
            
            var response = await _client.GetAsync(
                $"https://api.replicate.com/v1/predictions/{predictionId}",
                ct);
            
            var prediction = await response.Content
                .ReadAsAsync<ReplicatePrediction>(ct);
            
            if (prediction.Status == "succeeded" || prediction.Status == "failed")
                return prediction;
        }
        
        throw new TimeoutException("Prediction polling timed out");
    }
    
    private int EstimateTokens(string text) =>
        (int)Math.Ceiling(text.Length / 4.0);
    
    private class ReplicatePrediction
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public List<string> Output { get; set; }
    }
}
```

---

## Advanced Patterns

### Caching Responses

```csharp
public async Task<Result<InferenceResponse>> InferenceAsync(
    InferenceRequest request, 
    CancellationToken ct)
{
    var cacheKey = $"inference:{ProviderName}:{ComputeHash(request)}";
    
    // Check cache first
    var cached = await _context.Cache.GetAsync<InferenceResponse>(
        cacheKey,
        ct);
    
    if (cached is not null)
    {
        _logger.Information($"Cache hit for {cacheKey}");
        return Result.Success(cached);
    }
    
    // Cache miss - call provider
    var result = await CallProviderAsync(request, ct);
    
    if (result.IsSuccess && request.MaxTokens < 500)
    {
        // Cache only small responses
        await _context.Cache.SetAsync(
            cacheKey,
            result.Value,
            TimeSpan.FromHours(1),
            ct);
    }
    
    return result;
}
```

### Circuit Breaker Pattern

```csharp
private int _failureCount = 0;
private CircuitBreakerState _state = CircuitBreakerState.Closed;
private DateTime _lastFailureTime = DateTime.MinValue;

public async Task<Result<InferenceResponse>> InferenceAsync(
    InferenceRequest request, 
    CancellationToken ct)
{
    if (_state == CircuitBreakerState.Open)
    {
        var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
        if (timeSinceLastFailure.TotalSeconds >= 60)
        {
            _state = CircuitBreakerState.HalfOpen;
            _logger.Information($"{ProviderName} circuit breaker: half-open");
        }
        else
        {
            return ErrorCode.ProviderError
                .ToFailure("Circuit breaker open - provider unavailable");
        }
    }
    
    try
    {
        var result = await CallProviderAsync(request, ct);
        
        if (_state == CircuitBreakerState.HalfOpen && result.IsSuccess)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _logger.Information($"{ProviderName} circuit breaker: closed");
        }
        
        return result;
    }
    catch (Exception ex)
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
        
        if (_failureCount >= 5)
        {
            _state = CircuitBreakerState.Open;
            _logger.Error($"{ProviderName} circuit breaker: opened");
        }
        
        return ErrorCode.ProviderError.ToFailure(ex.Message, ex);
    }
}
```

---

## Troubleshooting

### Issue: "API Key Not Configured"

**Solution:**
```csharp
// Check environment variables
var apiKey = Environment.GetEnvironmentVariable("PROVIDER_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: Set PROVIDER_API_KEY environment variable");
    return;
}

// Or use configuration
var result = context.Configuration.Get<string>("PROVIDER_API_KEY");
if (!result)
    throw new Exception("API key configuration failed");
```

### Issue: "Provider Returns 401 Unauthorized"

**Solution:**
```csharp
// Check header format
// Correct:
headers.Add("Authorization", $"Bearer {token}");

// Verify token is still valid
var tokenExpiry = DateTime.Parse(context.Configuration.Get<string>("TOKEN_EXPIRY"));
if (DateTime.UtcNow > tokenExpiry)
{
    _logger.Warning("Token expired - refreshing...");
    await RefreshTokenAsync();
}
```

### Issue: "Timeouts on Inference Requests"

**Solution:**
```csharp
// Set reasonable timeouts
_client.Timeout = TimeSpan.FromSeconds(60);

// Use CancellationToken
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var response = await _client.PostAsync(..., cts.Token);
}
catch (TaskCanceledException)
{
    _logger.Warning("Request timeout");
    return ErrorCode.TimeoutError.ToFailure("Provider request timed out");
}
```

### Issue: "Cost Calculation Mismatch"

**Solution:**
```csharp
// Always calculate based on actual tokens from provider response
var actualInputTokens = response.Usage.PromptTokens;
var actualOutputTokens = response.Usage.CompletionTokens;

var cost = (actualInputTokens * Capabilities.CostPerInputToken) +
           (actualOutputTokens * Capabilities.CostPerOutputToken);

// Don't estimate - use actual values
```

---

## Performance Tuning

### Latency Optimization

| Strategy | Latency Reduction |
|----------|-------------------|
| Connection pooling | 20-30% |
| Request batching | 30-50% |
| Response caching | 90%+ |
| Parallel requests | 40-60% |

```csharp
// Use connection pooling
private static readonly HttpClient _sharedClient = new()
{
    Timeout = TimeSpan.FromSeconds(30)
};

// Batch requests when possible
public async Task<List<InferenceResponse>> BatchInferenceAsync(
    List<InferenceRequest> requests,
    CancellationToken ct)
{
    var tasks = requests
        .Select(r => InferenceAsync(r, ct))
        .ToList();
    
    var results = await Task.WhenAll(tasks);
    return results
        .Where(r => r.IsSuccess)
        .Select(r => r.Value)
        .ToList();
}
```

### Throughput Optimization

```csharp
// Use SemaphoreSlim for concurrent request limiting
private readonly SemaphoreSlim _concurrencyLimit = new(10);

public async Task<Result<InferenceResponse>> InferenceAsync(
    InferenceRequest request, 
    CancellationToken ct)
{
    await _concurrencyLimit.WaitAsync(ct);
    try
    {
        return await CallProviderAsync(request, ct);
    }
    finally
    {
        _concurrencyLimit.Release();
    }
}
```

### Memory Optimization

```csharp
// Don't accumulate responses in memory
public async IAsyncEnumerable<InferenceResponse> StreamInferenceAsync(
    IAsyncEnumerable<InferenceRequest> requests,
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var request in requests.WithCancellation(ct))
    {
        var response = await InferenceAsync(request, ct);
        if (response.IsSuccess)
            yield return response.Value;
    }
}
```

---

## Next Steps

1. **Choose your provider** - Pick a provider you want to integrate
2. **Create the class** - Use one of the examples as a template
3. **Implement methods** - Focus on initialization and inference first
4. **Test** - Use the SmartRouter to test your provider
5. **Monitor** - Check metrics in the monitoring dashboard
6. **Optimize** - Tune for your specific use case

**Ready to integrate? Pick a provider and get started!**

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
