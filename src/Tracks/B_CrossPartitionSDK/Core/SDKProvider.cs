namespace MonadoBlade.Tracks.SDK;

using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;

/// <summary>
/// Base SDK provider eliminating duplicate code across 50+ SDKs.
/// Each platform (AWS, Azure, GCP, etc.) gets one provider.
/// </summary>
public abstract class BaseSDKProvider : ServiceComponentBase, ISDKProvider
{
    public string SDKName { get; protected set; } = "";
    public abstract string TargetPlatform { get; }
    public abstract Version SDKVersion { get; }

    protected Dictionary<string, Func<Dictionary<string, object?>, CancellationToken, Task<Result<object?>>>> _operations = new();

    protected BaseSDKProvider(IServiceContext context, string componentId)
        : base(context, componentId)
    {
        ComponentType = "SDKProvider";
    }

    /// <summary>
    /// Universal SDK execution with retry, metrics, and validation.
    /// Eliminates boilerplate from 50+ SDK wrappers.
    /// </summary>
    public async Task<Result<object?>> ExecuteAsync(
        string operation,
        Dictionary<string, object?> parameters,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        // Validate operation exists
        if (!_operations.TryGetValue(operation, out var handler))
            return ErrorCode.SDKNotSupported.ToFailure<object?>(
                $"Operation '{operation}' not supported by {SDKName}");

        // Validate parameters
        var validation = await ValidateParametersAsync(operation, parameters, ct);
        if (validation is Result.Failure failure)
            return new Result<object?>.Failure(failure.ErrorCode, failure.Message, failure.InnerException);

        // Execute with retry and metrics
        return await AsyncOperationPattern.ExecuteWithRetryAsync(
            $"SDK_{SDKName}_{operation}",
            ct => ExecuteWithMetricsAsync(operation, handler, parameters, ct),
            _logger,
            _metrics,
            new RetryPolicyConfig(
                MaxRetries: 2,
                InitialDelay: TimeSpan.FromMilliseconds(200)),
            ct);
    }

    private async Task<Result<object?>> ExecuteWithMetricsAsync(
        string operation,
        Func<Dictionary<string, object?>, CancellationToken, Task<Result<object?>>> handler,
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        _logger.Information($"{SDKName}.{operation} executing");
        var result = await handler(parameters, ct);
        
        if (result is Result<object?>.Success)
            _metrics.IncrementCounter($"sdk_{SDKName.ToLower()}_success", tags: ("op", operation));
        else
            _metrics.IncrementCounter($"sdk_{SDKName.ToLower()}_error", tags: ("op", operation));

        return result;
    }

    public async Task<Result<string[]>> GetAvailableOperationsAsync(CancellationToken ct = default) =>
        _operations.Keys.ToArray().ToSuccess();

    protected virtual Task<Result> ValidateParametersAsync(
        string operation,
        Dictionary<string, object?> parameters,
        CancellationToken ct) =>
        Task.FromResult(Result.Success());

    /// <summary>Registers operation handler.</summary>
    protected void RegisterOperation(
        string name,
        Func<Dictionary<string, object?>, CancellationToken, Task<Result<object?>>> handler) =>
        _operations[name] = handler;
}

/// <summary>Example AWS SDK provider (eliminates duplication across AWS services).</summary>
public class AWSSDKProvider : BaseSDKProvider
{
    private string? _accessKey;
    private string? _secretKey;
    private string? _region;

    public override string TargetPlatform => "AWS";
    public override Version SDKVersion => new(3, 7, 200);

    public AWSSDKProvider(IServiceContext context)
        : base(context, "AWS")
    {
        SDKName = "AWS SDK for .NET";
        RegisterOperations();
    }

    private void RegisterOperations()
    {
        RegisterOperation("ListEC2Instances",
            async (p, ct) => await ListEC2InstancesAsync(p, ct));

        RegisterOperation("CreateEC2Instance",
            async (p, ct) => await CreateEC2InstanceAsync(p, ct));

        RegisterOperation("DescribeS3Bucket",
            async (p, ct) => await DescribeS3BucketAsync(p, ct));

        // 20+ more operations per service...
    }

    protected override async Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        var keyResult = context.Configuration.GetRequired<string>("AWS:AccessKey");
        if (keyResult is Result<string>.Failure failure)
            return failure;

        _accessKey = (keyResult as Result<string>.Success)!.Value;
        _secretKey = context.Configuration.Get("AWS:SecretKey", "");
        _region = context.Configuration.Get("AWS:Region", "us-east-1");

        _logger.Information($"AWS provider initialized for region {_region}");
        return Result.Success();
    }

    private async Task<Result<object?>> ListEC2InstancesAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        // Simulated AWS call
        await Task.Delay(100, ct);
        return new { instances = new[] { "i-123", "i-456" } }.ToSuccess<object?>();
    }

    private async Task<Result<object?>> CreateEC2InstanceAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        if (!parameters.ContainsKey("ImageId"))
            return ErrorCode.ValidationFailed.ToFailure<object?>("ImageId required");

        await Task.Delay(100, ct);
        return new { instanceId = "i-" + Guid.NewGuid().ToString().Substring(0, 8) }.ToSuccess<object?>();
    }

    private async Task<Result<object?>> DescribeS3BucketAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        if (!parameters.ContainsKey("BucketName"))
            return ErrorCode.ValidationFailed.ToFailure<object?>("BucketName required");

        await Task.Delay(50, ct);
        return new { bucketName = parameters["BucketName"], size = 1024 * 1024 * 100 }.ToSuccess<object?>();
    }
}

/// <summary>Azure SDK provider (same pattern as AWS).</summary>
public class AzureSDKProvider : BaseSDKProvider
{
    public override string TargetPlatform => "Azure";
    public override Version SDKVersion => new(4, 2, 0);

    public AzureSDKProvider(IServiceContext context)
        : base(context, "Azure")
    {
        SDKName = "Azure SDK for .NET";
        RegisterOperations();
    }

    private void RegisterOperations()
    {
        RegisterOperation("ListVirtualMachines",
            async (p, ct) => await ListVirtualMachinesAsync(p, ct));

        RegisterOperation("CreateVirtualMachine",
            async (p, ct) => await CreateVirtualMachineAsync(p, ct));

        // 30+ more Azure operations...
    }

    protected override async Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        var tenantId = context.Configuration.Get("Azure:TenantId", "");
        var clientId = context.Configuration.Get("Azure:ClientId", "");

        _logger.Information("Azure provider initialized");
        return Result.Success();
    }

    private async Task<Result<object?>> ListVirtualMachinesAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return new { vms = new[] { "vm-1", "vm-2" } }.ToSuccess<object?>();
    }

    private async Task<Result<object?>> CreateVirtualMachineAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return new { vmId = Guid.NewGuid().ToString() }.ToSuccess<object?>();
    }
}

/// <summary>
/// SDK Aggregator - unified interface to all 50+ SDKs.
/// Routes operations to correct provider automatically.
/// </summary>
public class SDKAggregator : ServiceComponentBase
{
    private readonly Dictionary<string, ISDKProvider> _providers = new();

    public SDKAggregator(IServiceContext context)
        : base(context, "SDKAggregator")
    {
        ComponentType = "SDKAggregator";
    }

    public async Task<Result> RegisterProviderAsync(ISDKProvider provider, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var initResult = await provider.InitializeAsync(_context, ct);
        if (initResult is Result.Failure failure)
            return failure;

        lock (_providers)
        {
            _providers[$"{provider.TargetPlatform}:{provider.SDKName}"] = provider;
        }

        _logger.Information($"Registered SDK: {provider.SDKName} ({provider.TargetPlatform})");
        return Result.Success();
    }

    /// <summary>
    /// Executes operation across any SDK platform.
    /// Example: ExecuteAsync("AWS:ListEC2Instances", ...)
    /// </summary>
    public async Task<Result<object?>> ExecuteAsync(
        string providerKey,
        string operation,
        Dictionary<string, object?> parameters,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        ISDKProvider? provider;
        lock (_providers)
        {
            _providers.TryGetValue(providerKey, out provider);
        }

        if (provider == null)
            return ErrorCode.SDKNotSupported.ToFailure<object?>(
                $"Provider '{providerKey}' not registered");

        return await provider.ExecuteAsync(operation, parameters, ct);
    }

    /// <summary>Gets all registered providers grouped by platform.</summary>
    public Dictionary<string, string[]> GetRegisteredProviders()
    {
        lock (_providers)
        {
            return _providers
                .GroupBy(kvp => kvp.Value.TargetPlatform)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(kvp => kvp.Value.SDKName).ToArray());
        }
    }

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        ISDKProvider[] providers;
        lock (_providers)
        {
            providers = _providers.Values.ToArray();
        }

        if (providers.Length == 0)
            return HealthStatus.Degraded(ComponentId, "No SDK providers registered");

        var results = await Task.WhenAll(
            providers.Select(p => p.GetHealthAsync(ct)));

        var healthy = results.Count(r => r.State == HealthState.Healthy);
        var degraded = results.Count(r => r.State == HealthState.Degraded);

        return new HealthStatus(
            ComponentId,
            degraded > 0 ? HealthState.Degraded : HealthState.Healthy,
            $"SDKs: {healthy} healthy, {degraded} degraded, {providers.Length - healthy - degraded} unhealthy",
            new() { { "total", providers.Length }, { "healthy", healthy } });
    }
}
