namespace MonadoBlade.SDKs.CSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Hermes C# SDK - NuGet package with full DI support
/// Feature parity with C# services, async/await throughout
/// </summary>
public class HermesClientConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.monado-blade.io";
    public string Region { get; set; } = "us-east-1";
    
    public bool UseMutualTLS { get; set; } = true;
    public string? TLSCertPath { get; set; }
    public string? TLSKeyPath { get; set; }
    
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxPoolSize { get; set; } = 100;
    public bool EnableCompression { get; set; } = true;
    
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
    
    public bool EnableCircuitBreaker { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 10;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromSeconds(60);
    
    public bool EnableMetrics { get; set; } = true;
    public string? MetricsEndpoint { get; set; }

    public static HermesClientConfiguration FromEnvironment(string prefix = "MONADO_")
    {
        return new HermesClientConfiguration
        {
            ApiKey = Environment.GetEnvironmentVariable($"{prefix}API_KEY") ?? string.Empty,
            ApiSecret = Environment.GetEnvironmentVariable($"{prefix}API_SECRET") ?? string.Empty,
            Endpoint = Environment.GetEnvironmentVariable($"{prefix}ENDPOINT") ?? "https://api.monado-blade.io",
            Region = Environment.GetEnvironmentVariable($"{prefix}REGION") ?? "us-east-1",
        };
    }
}

public class HermesException : Exception
{
    public string ErrorCode { get; }
    public string? RequestId { get; }
    public Dictionary<string, object?> Context { get; }

    public HermesException(
        string errorCode,
        string message,
        string? requestId = null,
        Dictionary<string, object?>? context = null)
        : base(message)
    {
        ErrorCode = errorCode;
        RequestId = requestId;
        Context = context ?? new();
    }

    public override string ToString() =>
        $"[{ErrorCode}] {Message}" + (RequestId != null ? $" (Request: {RequestId})" : "");
}

/// <summary>Unified LLM request/response models - shared across SDKs</summary>
public class LLMCompleteRequest
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "claude";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-3-opus";

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;
}

public class LLMCompleteResponse
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public Dictionary<string, int> Usage { get; set; } = new();

    [JsonPropertyName("stop_reason")]
    public string StopReason { get; set; } = "end_turn";
}

// Provider stubs for Azure CLI, Foundry, ccodex
public class AzureCLIProvider : BaseAIProviderService
{
    public override AIProviderCapabilities Capabilities { get; } = new(
        SupportedModels: new[] { "azure-openai", "azure-resource-manager" },
        MaxInputTokens: 64000,
        MaxOutputTokens: 2048,
        SupportsStreaming: true,
        SupportsFunctionCalling: true,
        SupportsVision: false);

    public AzureCLIProvider(IServiceContext context) : base(context, "AzureCLI") { }

    protected override Task<Result<AIInferenceResult>> OnInferenceAsync(AIInferenceRequest request, CancellationToken ct)
    {
        // TODO: Implement Azure CLI integration logic
        return Task.FromResult(new AIInferenceResult(Content: "Azure CLI response stub").ToSuccess());
    }
}

public class FoundryProvider : BaseAIProviderService
{
    public override AIProviderCapabilities Capabilities { get; } = new(
        SupportedModels: new[] { "foundry-smart-contract" },
        MaxInputTokens: 32000,
        MaxOutputTokens: 1024,
        SupportsStreaming: false,
        SupportsFunctionCalling: true,
        SupportsVision: false);

    public FoundryProvider(IServiceContext context) : base(context, "Foundry") { }

    protected override Task<Result<AIInferenceResult>> OnInferenceAsync(AIInferenceRequest request, CancellationToken ct)
    {
        // TODO: Implement Foundry integration logic
        return Task.FromResult(new AIInferenceResult(Content: "Foundry response stub").ToSuccess());
    }
}

public class CCodexProvider : BaseAIProviderService
{
    public override AIProviderCapabilities Capabilities { get; } = new(
        SupportedModels: new[] { "ccodex-search", "ccodex-docs" },
        MaxInputTokens: 16000,
        MaxOutputTokens: 1024,
        SupportsStreaming: false,
        SupportsFunctionCalling: false,
        SupportsVision: false);

    public CCodexProvider(IServiceContext context) : base(context, "CCodex") { }

    protected override Task<Result<AIInferenceResult>> OnInferenceAsync(AIInferenceRequest request, CancellationToken ct)
    {
        // TODO: Implement ccodex integration logic
        return Task.FromResult(new AIInferenceResult(Content: "ccodex response stub").ToSuccess());
    }
}

/// <summary>Task execution models</summary>
public class DistributedTask
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public Dictionary<string, object?> Input { get; set; } = new();

    [JsonPropertyName("resources")]
    public Dictionary<string, int> Resources { get; set; } = new()
    {
        { "cpu", 1 },
        { "memory", 512 }
    };

    [JsonPropertyName("timeout")]
    public int TimeoutSeconds { get; set; } = 3600;
}

public class TaskExecutionResult
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "running";

    [JsonPropertyName("result")]
    public Dictionary<string, object?> Result { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Main Hermes C# SDK Client
/// Full async/await support, DI-ready, completely typed
/// </summary>
public class HermesClient : IAsyncDisposable
{
    private readonly HermesClientConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly CircuitBreakerService _circuitBreaker;
    private readonly RetryPolicyService _retryPolicy;
    private readonly IServiceProvider? _serviceProvider;

    public LLMService LLM { get; }
    public TaskService Tasks { get; }
    public AIHubService AIHub { get; }

    public HermesClient(
        HermesClientConfiguration? config = null,
        HttpClient? httpClient = null,
        IServiceProvider? serviceProvider = null)
    {
        _config = config ?? HermesClientConfiguration.FromEnvironment();
        _serviceProvider = serviceProvider;
        _httpClient = httpClient ?? CreateDefaultHttpClient();
        
        _circuitBreaker = new CircuitBreakerService(
            "hermes-client",
            _config.CircuitBreakerThreshold,
            _config.CircuitBreakerTimeout);
        
        _retryPolicy = new RetryPolicyService(
            _config.MaxRetries,
            _config.InitialRetryDelay,
            _config.RetryBackoffMultiplier,
            _config.MaxRetryDelay);

        LLM = new LLMService(this);
        Tasks = new TaskService(this);
        AIHub = new AIHubService(this);
    }

    private HttpClient CreateDefaultHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        return new HttpClient(handler)
        {
            Timeout = _config.RequestTimeout,
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bearer {_config.ApiKey}" },
                { "User-Agent", "HermesSDK/1.0.0" },
                { "X-SDK-Version", "1.0.0" }
            }
        };
    }

    public async Task<T> RequestAsync<T>(
        string method,
        string path,
        object? body = null,
        CancellationToken ct = default) where T : class
    {
        var requestId = Guid.NewGuid().ToString();

        async Task<T> Execute()
        {
            var url = $"{_config.Endpoint}{path}";
            var request = new HttpRequestMessage(new HttpMethod(method), url)
            {
                Headers = { { "X-Request-Id", requestId } }
            };

            if (body != null)
                request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsAsync<T>(ct);
            return content ?? throw new HermesException(
                "DESERIALIZATION_FAILED",
                "Failed to deserialize response",
                requestId);
        }

        try
        {
            return await _circuitBreaker.ExecuteAsync(
                () => _retryPolicy.ExecuteAsync(Execute, ct),
                ct);
        }
        catch (HermesException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new HermesException(
                "SDK_REQUEST_FAILED",
                $"Request failed: {ex.Message}",
                requestId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>LLM Service - call LLMs via multiple providers</summary>
public class LLMService
{
    private readonly HermesClient _client;

    public LLMService(HermesClient client) => _client = client;

    public async Task<LLMCompleteResponse> CompleteAsync(
        LLMCompleteRequest request,
        CancellationToken ct = default)
    {
        return await _client.RequestAsync<LLMCompleteResponse>(
            "POST",
            "/v1/llm/complete",
            request,
            ct);
    }

    public async Task<LLMCompleteResponse> CompleteAsync(
        string prompt,
        string provider = "claude",
        string model = "claude-3-opus",
        double temperature = 0.7,
        int maxTokens = 1024,
        CancellationToken ct = default)
    {
        var request = new LLMCompleteRequest
        {
            Provider = provider,
            Model = model,
            Prompt = prompt,
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        return await CompleteAsync(request, ct);
    }

    public IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string provider = "claude",
        string model = "claude-3-opus",
        CancellationToken ct = default)
    {
        return StreamInternalAsync(prompt, provider, model, ct);
    }

    private async IAsyncEnumerable<string> StreamInternalAsync(
        string prompt,
        string provider,
        string model,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_client._config.Endpoint}/v1/llm/stream")
        {
            Content = JsonContent.Create(new
            {
                prompt,
                provider,
                model
            })
        };

        using var response = await _client._httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (!string.IsNullOrEmpty(line))
                yield return line;
        }
    }
}

/// <summary>Task Service - execute distributed tasks</summary>
public class TaskService
{
    private readonly HermesClient _client;

    public TaskService(HermesClient client) => _client = client;

    public async Task<TaskExecutionResult> ExecuteAsync(
        DistributedTask task,
        CancellationToken ct = default)
    {
        return await _client.RequestAsync<TaskExecutionResult>(
            "POST",
            "/v1/tasks/execute",
            task,
            ct);
    }

    public async Task<TaskExecutionResult> ExecuteAsync(
        string taskName,
        Dictionary<string, object?>? input = null,
        Dictionary<string, int>? resources = null,
        int timeoutSeconds = 3600,
        CancellationToken ct = default)
    {
        var task = new DistributedTask
        {
            Name = taskName,
            Input = input ?? new(),
            Resources = resources ?? new() { { "cpu", 1 }, { "memory", 512 } },
            TimeoutSeconds = timeoutSeconds
        };

        return await ExecuteAsync(task, ct);
    }

    public async Task<TaskExecutionResult> GetStatusAsync(
        string taskId,
        CancellationToken ct = default)
    {
        return await _client.RequestAsync<TaskExecutionResult>(
            "GET",
            $"/v1/tasks/{taskId}/status",
            null,
            ct);
    }
}

/// <summary>AI Hub Service - model training & optimization</summary>
public class AIHubService
{
    private readonly HermesClient _client;

    public AIHubService(HermesClient client) => _client = client;

    public async Task<Dictionary<string, object?>> TrainModelAsync(
        string datasetId,
        string modelType,
        Dictionary<string, object?>? hyperparameters = null,
        CancellationToken ct = default)
    {
        return await _client.RequestAsync<Dictionary<string, object?>>(
            "POST",
            "/v1/ai-hub/train",
            new
            {
                dataset_id = datasetId,
                model_type = modelType,
                hyperparameters = hyperparameters ?? new()
            },
            ct);
    }

    public async Task<Dictionary<string, object?>> OptimizeAsync(
        string modelId,
        string target = "latency",
        CancellationToken ct = default)
    {
        return await _client.RequestAsync<Dictionary<string, object?>>(
            "POST",
            "/v1/ai-hub/optimize",
            new { model_id = modelId, target },
            ct);
    }
}

/// <summary>Circuit Breaker Service - prevents cascading failures</summary>
public class CircuitBreakerService
{
    private enum State { Closed, Open, HalfOpen }

    private State _state = State.Closed;
    private int _failureCount = 0;
    private DateTime _openedTime = DateTime.MinValue;

    public string Name { get; }
    public int FailureThreshold { get; }
    public TimeSpan OpenTimeout { get; }

    public CircuitBreakerService(
        string name,
        int failureThreshold = 10,
        TimeSpan? openTimeout = null)
    {
        Name = name;
        FailureThreshold = failureThreshold;
        OpenTimeout = openTimeout ?? TimeSpan.FromSeconds(60);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
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
                throw new HermesException(
                    "CIRCUIT_BREAKER_OPEN",
                    $"Circuit breaker '{Name}' is open");
            }
        }

        try
        {
            var result = await operation();
            _failureCount = 0;
            if (_state == State.HalfOpen)
                _state = State.Closed;
            return result;
        }
        catch
        {
            _failureCount++;
            if (_failureCount >= FailureThreshold)
            {
                _state = State.Open;
                _openedTime = DateTime.UtcNow;
            }
            throw;
        }
    }
}

/// <summary>Retry Policy Service - exponential backoff with jitter</summary>
public class RetryPolicyService
{
    public int MaxRetries { get; }
    public TimeSpan InitialDelay { get; }
    public double BackoffMultiplier { get; }
    public TimeSpan MaxDelay { get; }

    public RetryPolicyService(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        TimeSpan? maxDelay = null)
    {
        MaxRetries = maxRetries;
        InitialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        BackoffMultiplier = backoffMultiplier;
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(10);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt >= MaxRetries || !ShouldRetry(ex))
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
        var jitter = Random.Shared.NextDouble() * 0.1 * delay;
        var total = Math.Min(delay + jitter, MaxDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(total);
    }

    private static bool ShouldRetry(Exception ex)
    {
        if (ex is HttpRequestException hre && hre.StatusCode.HasValue)
        {
            var code = (int)hre.StatusCode;
            return code >= 500 || code == 429;
        }
        return ex is TimeoutException or OperationCanceledException or HermesException;
    }
}

/// <summary>Extension methods for DI integration</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHermesClient(
        this IServiceCollection services,
        HermesClientConfiguration? config = null)
    {
        services.AddHttpClient<HermesClient>()
            .ConfigureHttpClient(client =>
            {
                var c = config ?? HermesClientConfiguration.FromEnvironment();
                client.Timeout = c.RequestTimeout;
                client.BaseAddress = new Uri(c.Endpoint);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {c.ApiKey}");
            });

        services.AddSingleton(config ?? HermesClientConfiguration.FromEnvironment());
        return services;
    }
}
