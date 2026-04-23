namespace MonadoBlade.Tracks.AIHub;

using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;

/// <summary>
/// Example implementation of AIHub Track showing how to use the architecture.
/// All Track A providers implement IAIProvider interface.
/// </summary>
public abstract class BaseAIProviderService : ServiceComponentBase, IAIProvider
{
    public string ProviderName => GetType().Name;
    public abstract AIProviderCapabilities Capabilities { get; }

    protected BaseAIProviderService(IServiceContext context, string componentId)
        : base(context, componentId)
    {
        ComponentType = "AIProvider";
    }

    public bool CanHandle(AIInferenceRequest request) =>
        Capabilities.SupportedModels.Contains(request.Model);

    /// <summary>
    /// Inference with automatic retries, caching, metrics, and fallback.
    /// Shows how to compose multiple patterns.
    /// </summary>
    public async Task<Result<AIInferenceResult>> InferenceAsync(
        AIInferenceRequest request,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        // Validate request
        if (!CanHandle(request))
            return ErrorCode.AIProviderError.ToFailure<AIInferenceResult>(
                $"Provider {ProviderName} does not support model {request.Model}");

        if (string.IsNullOrEmpty(request.Prompt))
            return ErrorCode.ValidationFailed.ToFailure<AIInferenceResult>("Prompt cannot be empty");

        // Try cache first
        var cacheKey = $"ai:inference:{ProviderName}:{request.Model}:{request.Prompt.GetHashCode()}";
        var result = await CachingPattern.GetOrComputeAsync(
            cacheKey,
            ct => ExecuteInferenceWithRetryAsync(request, ct),
            _context.Cache,
            TimeSpan.FromHours(24),
            ct);

        return result;
    }

    private async Task<Result<AIInferenceResult>> ExecuteInferenceWithRetryAsync(
        AIInferenceRequest request,
        CancellationToken ct)
    {
        return await AsyncOperationPattern.ExecuteWithRetryAsync(
            $"AIInference_{ProviderName}",
            ct => OnInferenceAsync(request, ct),
            _logger,
            _metrics,
            new RetryPolicyConfig(
                MaxRetries: 3,
                InitialDelay: TimeSpan.FromMilliseconds(100),
                IsRetryable: code => code switch
                {
                    ErrorCode.AIProviderRateLimited or
                    ErrorCode.Timeout or
                    ErrorCode.CommunicationError => true,
                    _ => false
                }),
            ct);
    }

    /// <summary>Override to implement provider-specific inference.</summary>
    protected abstract Task<Result<AIInferenceResult>> OnInferenceAsync(
        AIInferenceRequest request,
        CancellationToken ct);

    protected override Task<HealthStatus> OnGetHealthAsync(CancellationToken ct) =>
        Task.FromResult(HealthStatus.Healthy(ComponentId, "Ready for inference"));
}

/// <summary>Example OpenAI provider implementation.</summary>
public class OpenAIProvider : BaseAIProviderService
{
    private string? _apiKey;

    public override AIProviderCapabilities Capabilities { get; } = new(
        SupportedModels: new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" },
        MaxInputTokens: 128000,
        MaxOutputTokens: 4096,
        SupportsStreaming: true,
        SupportsFunctionCalling: true,
        SupportsVision: true);

    public OpenAIProvider(IServiceContext context)
        : base(context, "OpenAI") { }

    protected override async Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        // Get API key from secure configuration
        var keyResult = context.Configuration.GetRequired<string>("AIProviders:OpenAI:ApiKey");
        if (keyResult is Result<string>.Failure failure)
            return failure;

        var key = (keyResult as Result<string>.Success)!.Value;
        if (string.IsNullOrEmpty(key))
            return ErrorCode.ConfigurationError.ToFailure("OpenAI API key not configured");

        _apiKey = key;
        _logger.Information("OpenAI provider initialized");
        return Result.Success();
    }

    protected override async Task<Result<AIInferenceResult>> OnInferenceAsync(
        AIInferenceRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return ErrorCode.AIProviderError.ToFailure<AIInferenceResult>("API key not set");

        // Simulated API call (real implementation would use HttpClient)
        await Task.Delay(100, ct);

        return new AIInferenceResult(
            Content: $"Response from OpenAI model {request.Model}",
            Usage: new(100, 50, 150),
            Metadata: new() { { "provider", "openai" } }).ToSuccess();
    }
}

/// <summary>AI Hub aggregator that routes to best provider based on model/strategy.</summary>
public class AIHubService : ServiceComponentBase
{
    private readonly Dictionary<string, IAIProvider> _providers = new();
    private readonly IEventBus _eventBus;

    public AIHubService(IServiceContext context, IEventBus eventBus)
        : base(context, "AIHub")
    {
        ComponentType = "AIHubService";
        _eventBus = eventBus;
    }

    /// <summary>Registers AI provider (automatic de-duplication).</summary>
    public async Task<Result> RegisterProviderAsync(IAIProvider provider, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var initResult = await provider.InitializeAsync(_context, ct);
        if (initResult is Result.Failure failure)
            return failure;

        lock (_providers)
        {
            _providers[provider.ProviderName] = provider;
        }

        _logger.Information($"Registered AI provider: {provider.ProviderName}");
        await _eventBus.PublishAsync(new ProviderRegisteredEvent(provider.ProviderName));

        return Result.Success();
    }

    /// <summary>
    /// Unified inference interface - routes to best provider automatically.
    /// Shows cross-track resilience pattern.
    /// </summary>
    public async Task<Result<AIInferenceResult>> InferenceAsync(
        AIInferenceRequest request,
        AIHubStrategy strategy = AIHubStrategy.BestAvailable,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        // Get suitable providers
        IAIProvider[] suitableProviders;
        lock (_providers)
        {
            suitableProviders = _providers.Values
                .Where(p => p.CanHandle(request))
                .ToArray();
        }

        if (suitableProviders.Length == 0)
            return ErrorCode.AIProviderError.ToFailure<AIInferenceResult>(
                $"No provider available for model: {request.Model}");

        // Try providers based on strategy
        return strategy switch
        {
            AIHubStrategy.BestAvailable => await TryProvidersSequentiallyAsync(
                suitableProviders, request, ct),
            AIHubStrategy.Fastest => await TryProvidersConcurrentlyAsync(
                suitableProviders, request, ct),
            _ => ErrorCode.InvalidArgument.ToFailure<AIInferenceResult>("Unknown strategy")
        };
    }

    private async Task<Result<AIInferenceResult>> TryProvidersSequentiallyAsync(
        IAIProvider[] providers,
        AIInferenceRequest request,
        CancellationToken ct)
    {
        foreach (var provider in providers)
        {
            var result = await provider.InferenceAsync(request, ct);
            if (result is Result<AIInferenceResult>.Success)
                return result;

            _logger.Warning($"Provider {provider.ProviderName} failed, trying next...");
        }

        return ErrorCode.AIProviderUnavailable.ToFailure<AIInferenceResult>("All providers failed");
    }

    private async Task<Result<AIInferenceResult>> TryProvidersConcurrentlyAsync(
        IAIProvider[] providers,
        AIInferenceRequest request,
        CancellationToken ct)
    {
        var tasks = providers.Select(p => p.InferenceAsync(request, ct)).ToArray();
        var results = await Task.WhenAll(tasks);

        var successResult = results.FirstOrDefault(r => r is Result<AIInferenceResult>.Success);
        if (successResult != null)
            return (Result<AIInferenceResult>)successResult;

        return ErrorCode.AIProviderUnavailable.ToFailure<AIInferenceResult>("All providers failed");
    }

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        IAIProvider[] providers;
        lock (_providers)
        {
            providers = _providers.Values.ToArray();
        }

        if (providers.Length == 0)
            return HealthStatus.Degraded(ComponentId, "No providers registered");

        var healthResults = await Task.WhenAll(
            providers.Select(p => p.GetHealthAsync(ct)));

        var unhealthyCount = healthResults.Count(h => h.State == HealthState.Unhealthy);
        if (unhealthyCount > 0)
            return HealthStatus.Degraded(ComponentId, $"{unhealthyCount} providers unhealthy",
                new() { { "healthy_providers", providers.Length - unhealthyCount } });

        return HealthStatus.Healthy(ComponentId, $"{providers.Length} providers ready");
    }
}

public record ProviderRegisteredEvent(string ProviderName);
public enum AIHubStrategy { BestAvailable, Fastest }
