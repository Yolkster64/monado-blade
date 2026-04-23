# Monado Blade v2.2.0 - Implementation Guide

## QUICK START (First Developer Setup)

### 1. Clone and Structure
```powershell
cd MonadoBlade
dotnet sln add src/Core/MonadoBlade.Core.csproj
dotnet sln add src/Tracks/*/MonadoBlade.*.csproj
dotnet restore
```

### 2. First Service Implementation
```csharp
// 1. Define your service interface
public interface IMyService : IServiceComponent
{
    Task<Result<T>> DoWorkAsync<T>(TRequest request);
}

// 2. Implement using ServiceComponentBase
public class MyService : ServiceComponentBase, IMyService
{
    public MyService(IServiceContext context) 
        : base(context, "MyService") { }
    
    public async Task<Result<T>> DoWorkAsync<T>(TRequest request)
    {
        ThrowIfNotInitialized();
        
        // Use AsyncOperationPattern for automatic retry + metrics
        return await AsyncOperationPattern.ExecuteWithRetryAsync(
            "MyService.DoWork",
            ct => OnDoWorkAsync(request, ct),
            _logger,
            _metrics);
    }
    
    protected abstract Task<Result<T>> OnDoWorkAsync(TRequest request, CancellationToken ct);
}

// 3. Register in DI container
services.AddScoped<IMyService, MyService>();

// 4. Use in code
var result = await myService.DoWorkAsync(request);
var response = result.Match(
    success => new { data = success },
    (code, msg, ex) => new { error = msg });
```

## TRACK-SPECIFIC EXAMPLES

### Track A: AI Hub
All AI systems derive from `BaseAIProviderService`

```csharp
public class MyAIProvider : BaseAIProviderService
{
    public override AIProviderCapabilities Capabilities => new(
        SupportedModels: new[] { "my-model-v1" },
        MaxInputTokens: 4096,
        MaxOutputTokens: 2048,
        SupportsStreaming: false,
        SupportsFunctionCalling: false,
        SupportsVision: false);

    protected override async Task<Result<AIInferenceResult>> OnInferenceAsync(
        AIInferenceRequest request,
        CancellationToken ct)
    {
        // Your implementation
        return new AIInferenceResult(
            Content: "AI response",
            Usage: new(50, 30, 80)).ToSuccess();
    }
}

// Use via AIHubService
await aiHub.RegisterProviderAsync(new MyAIProvider(context));
var result = await aiHub.InferenceAsync(
    new AIInferenceRequest("my-model-v1", "What is X?"),
    AIHubStrategy.BestAvailable);
```

### Track B: Cross-Partition SDK
All SDKs derive from `BaseSDKProvider`

```csharp
public class MyPlatformSDK : BaseSDKProvider
{
    public override string TargetPlatform => "MyPlatform";
    public override Version SDKVersion => new(1, 0, 0);

    public MyPlatformSDK(IServiceContext context)
        : base(context, "MyPlatformSDK")
    {
        RegisterOperation("ListResources",
            async (p, ct) => await ListResourcesAsync(p, ct));
        RegisterOperation("CreateResource",
            async (p, ct) => await CreateResourceAsync(p, ct));
    }

    private async Task<Result<object?>> ListResourcesAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        return new { resources = new[] { "r1", "r2" } }.ToSuccess<object?>();
    }

    private async Task<Result<object?>> CreateResourceAsync(
        Dictionary<string, object?> parameters,
        CancellationToken ct)
    {
        var name = parameters.GetValueOrDefault("Name") as string;
        if (string.IsNullOrEmpty(name))
            return ErrorCode.ValidationFailed.ToFailure<object?>("Name required");

        return new { id = Guid.NewGuid() }.ToSuccess<object?>();
    }
}

// Use via SDKAggregator
await aggregator.RegisterProviderAsync(new MyPlatformSDK(context));
var result = await aggregator.ExecuteAsync(
    "MyPlatform:MyPlatformSDK",
    "ListResources",
    new());
```

### Track C: Multi-VM Orchestration
All VM managers derive from `IVirtualMachineManager`

```csharp
public class VMOrchestrator : ServiceComponentBase, IVirtualMachineManager
{
    public async Task<Result<string>> CreateAsync(VMConfiguration config, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        // Atomic operation - all or nothing
        var atomic = new AtomicOperation(_logger);

        string vmId = Guid.NewGuid().ToString();

        // Step 1: Create VM
        var vmResult = await atomic.ExecuteAsync(
            "CreateVM",
            async () => await CreateVMAsync(vmId, config),
            async () => await DeleteVMAsync(vmId));

        // Step 2: Configure network
        var netResult = await atomic.ExecuteAsync(
            "ConfigureNetwork",
            async () => await ConfigureNetworkAsync(vmId, config),
            async () => await UnconfigureNetworkAsync(vmId));

        // Step 3: Start VM
        var startResult = await atomic.ExecuteAsync(
            "StartVM",
            async () => await StartVMAsync(vmId),
            async () => await StopVMAsync(vmId));

        await atomic.CommitAsync();
        return vmId.ToSuccess();
    }

    public async Task<Result> StartAsync(string vmId, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();
        return await AsyncOperationPattern.ExecuteWithRetryAsync(
            "StartVM",
            ct => StartVMAsync(vmId, ct),
            _logger,
            _metrics);
    }
}
```

### Track D: UI/UX Automation
All UI components derive from `IUIComponent`

```csharp
public class DashboardComponent : ServiceComponentBase, IUIComponent
{
    public UIComponentType ComponentType => UIComponentType.Dashboard;
    
    private object? _currentState;

    public async Task<Result<string>> RenderAsync(object? state = null, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();
        
        if (state != null)
            _currentState = state;

        // Cache rendered output
        return await CachingPattern.GetOrComputeAsync(
            $"dashboard_render_{_currentState?.GetHashCode()}",
            ct => RenderHtmlAsync(_currentState, ct),
            _context.Cache,
            TimeSpan.FromMinutes(5));
    }

    public async Task<Result<UIEventResponse>> HandleInputAsync(UIEvent input, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var response = await OnHandleInputAsync(input, ct);

        // Publish event to other components
        await _context.GetService<IEventBus>()
            ?.PublishAsync(new UIEventOccurred(input)) ?? Task.CompletedTask;

        return response;
    }

    private async Task<Result<string>> RenderHtmlAsync(object? state, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        return $"<div>{state}</div>".ToSuccess();
    }
}
```

## PATTERNS IN PRACTICE

### Pattern 1: Service with Caching
```csharp
public async Task<Result<User>> GetUserAsync(string userId)
{
    return await CachingPattern.GetOrComputeAsync(
        $"user:{userId}",
        ct => LoadUserFromDatabaseAsync(userId, ct),
        _context.Cache,
        TimeSpan.FromHours(1));
}
```

### Pattern 2: Transactional Operation
```csharp
var atomic = new AtomicOperation(_logger);

var r1 = await atomic.ExecuteAsync(
    "DebitAccount",
    () => DebitAsync(fromAccount, amount),
    () => CreditAsync(fromAccount, amount));

var r2 = await atomic.ExecuteAsync(
    "CreditAccount",
    () => CreditAsync(toAccount, amount),
    () => DebitAsync(toAccount, amount));

if (r1 is Result.Success && r2 is Result.Success)
    await atomic.CommitAsync();
```

### Pattern 3: Resource Management
```csharp
using var scope = new ResourceScope();

var connection = await scope.AcquireAsync(
    () => pool.AcquireConnectionAsync());

var transaction = await scope.AcquireAsync(
    () => connection.BeginTransactionAsync());

// All resources auto-disposed when scope ends
```

### Pattern 4: Resilient Operation
```csharp
var result = await AsyncOperationPattern.ExecuteWithRetryAsync(
    "ExternalAPICall",
    ct => externalService.CallAsync(ct),
    _logger,
    _metrics,
    new RetryPolicyConfig(
        MaxRetries: 3,
        InitialDelay: TimeSpan.FromMilliseconds(100),
        BackoffMultiplier: 2.0,
        IsRetryable: code => code == ErrorCode.Timeout));
```

### Pattern 5: Configuration Access
```csharp
var apiUrl = config.Get("ExternalAPI:Url", "http://default");
var timeout = config.Get("Timeouts:External", TimeSpan.FromSeconds(30));
var apiKey = config.GetRequired<string>("ExternalAPI:Key");

// Watch for changes
using var watch = config.Watch("Timeouts:External", newValue =>
{
    _timeout = (TimeSpan)newValue;
});
```

## TESTING INFRASTRUCTURE

### Unit Test Example
```csharp
public class MyServiceTests
{
    private Mock<IServiceContext> _contextMock;
    private Mock<ILoggingProvider> _loggerMock;
    private Mock<ICacheProvider> _cacheMock;
    private MyService _service;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<IServiceContext>();
        _loggerMock = new Mock<ILoggingProvider>();
        _cacheMock = new Mock<ICacheProvider>();
        
        _contextMock.Setup(c => c.Logger).Returns(_loggerMock.Object);
        _contextMock.Setup(c => c.Cache).Returns(_cacheMock.Object);
        
        _service = new MyService(_contextMock.Object);
    }

    [Test]
    public async Task DoWorkAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        await _service.InitializeAsync(_contextMock.Object);
        var request = new MyRequest();

        // Act
        var result = await _service.DoWorkAsync<string>(request);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<string>.Success>());
    }

    [Test]
    public async Task DoWorkAsync_WithInvalidRequest_ReturnsFa ilure()
    {
        // Arrange
        await _service.InitializeAsync(_contextMock.Object);
        var request = new MyRequest { /* invalid */ };

        // Act
        var result = await _service.DoWorkAsync<string>(request);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<string>.Failure>());
    }
}
```

### Integration Test Example
```csharp
public class AIHubIntegrationTests
{
    [Test]
    public async Task AIHub_WithMultipleProviders_SelectsBestAvailable()
    {
        // Create service context
        var context = CreateTestContext();
        var aiHub = new AIHubService(context, new EventBusStub());

        // Register providers
        await aiHub.RegisterProviderAsync(new OpenAIProvider(context));
        await aiHub.RegisterProviderAsync(new ClaudeProvider(context));

        // Execute
        var result = await aiHub.InferenceAsync(
            new AIInferenceRequest("gpt-4", "Hello"),
            AIHubStrategy.BestAvailable);

        // Assert
        Assert.That(result, Is.InstanceOf<Result<AIInferenceResult>.Success>());
    }
}
```

## DEPENDENCY INJECTION SETUP

### Startup Configuration
```csharp
public static void ConfigureMonadoServices(this IServiceCollection services)
{
    // Core infrastructure
    services.AddSingleton<IConfigurationProvider>(sp => 
        new ConfigurationProvider(sp.GetRequiredService<IConfiguration>()));
    services.AddSingleton<ILoggingProvider, StructuredLoggingProvider>();
    services.AddSingleton<IMetricsCollector, MetricsCollector>();
    services.AddSingleton<ICacheProvider>(sp => 
        new DistributedCacheProvider(sp.GetRequiredService<IDistributedCache>()));
    services.AddSingleton<IEventBus, EventBus>();

    // Service context factory
    services.AddScoped<IServiceContext>(sp => new ServiceContext(
        sp.GetRequiredService<IConfigurationProvider>(),
        sp.GetRequiredService<ILoggingProvider>(),
        sp.GetRequiredService<IMetricsCollector>(),
        sp.GetRequiredService<ICacheProvider>(),
        sp,
        Guid.NewGuid().ToString()));

    // Track A: AI Hub
    services.AddSingleton<IAIProvider, OpenAIProvider>();
    services.AddSingleton<IAIProvider, ClaudeProvider>();
    services.AddSingleton<AIHubService>();

    // Track B: SDK
    services.AddSingleton<ISDKProvider, AWSSDKProvider>();
    services.AddSingleton<ISDKProvider, AzureSDKProvider>();
    services.AddSingleton<SDKAggregator>();

    // Track C: Orchestration
    services.AddSingleton<IVirtualMachineManager, VMOrchestrator>();
    services.AddSingleton<ILoadBalancer, AdaptiveLoadBalancer>();

    // Track D: UI
    services.AddSingleton<IUIComponent, DashboardComponent>();
}
```

## ERROR CODES REFERENCE

```csharp
ErrorCode.Success                    // 0 - Success
ErrorCode.ValidationFailed           // 100 - Validation error
ErrorCode.AIProviderError            // 1000 - AI provider error
ErrorCode.AIProviderRateLimited      // 1002 - Rate limit
ErrorCode.SDKNotSupported            // 2002 - SDK not supported
ErrorCode.VMBootError                // 3000 - VM boot failed
ErrorCode.EncryptionError            // 5000 - Encryption error
```

## SECURITY BEST PRACTICES

### Input Validation
```csharp
// Always validate at boundaries
var validInput = SecurityPattern.ValidateStringInput(
    input,
    minLength: 1,
    maxLength: 1000,
    allowedCharacters: "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

// Sanitize for database
var safe = SecurityPattern.SanitizeForDatabase(userInput);
```

### Principle of Least Privilege
```csharp
// Only request needed permissions
var config = context.Configuration.Get<string>("ApiKey"); // Single key
// NOT: var allConfig = context.Configuration.GetAll(); // All config
```

### Encryption
```csharp
var encrypted = await securityProvider.EncryptAsync(sensitiveData);
var decrypted = await securityProvider.DecryptAsync(encrypted);
```

## PERFORMANCE GUIDELINES

### Connection Pooling
```csharp
var pool = new ResourcePoolBase<DatabaseConnection>(context, "DbPool", maxPoolSize: 100);
var conn = await pool.AcquireAsync();
// Use connection
await pool.ReleaseAsync(conn);
```

### Caching Strategy
```csharp
// Cache expensive operations
// Expire after 1 hour
// Invalidate on update events
var result = await CachingPattern.GetOrComputeAsync(
    cacheKey,
    factory,
    cache,
    TimeSpan.FromHours(1));
```

### Batch Operations
```csharp
// Process items in batches for efficiency
var items = GetItemsToProcess().Batch(100);
foreach (var batch in items)
{
    await ProcessBatchAsync(batch);
}
```

## MONITORING & METRICS

### Key Metrics to Track
```csharp
// Operation duration
_metrics.RecordDuration("operation_name", duration);

// Success/failure rates
_metrics.IncrementCounter("operation_success", tags: ("service", "ServiceName"));
_metrics.IncrementCounter("operation_error", tags: ("error_code", "InvalidArgument"));

// Resource utilization
_metrics.SetGauge("pool_available", available_count);
_metrics.SetGauge("cache_size", cache_item_count);
```

### Health Checks
All services provide health checks via `GetHealthAsync()` returning `HealthStatus.Healthy/Degraded/Unhealthy`.

```csharp
var health = await service.GetHealthAsync();
if (health.State == HealthState.Unhealthy)
    // Take action
```

## DEPLOYMENT CHECKLIST

- [ ] All services initialize successfully
- [ ] Health checks pass for all components
- [ ] Configuration validated at startup
- [ ] TPM 2.0 initialized (if applicable)
- [ ] Encryption keys loaded securely
- [ ] Cache backends connected
- [ ] External API endpoints reachable
- [ ] Metrics collection verified
- [ ] Logging infrastructure operational
- [ ] Event bus subscriptions active

This architecture is now ready for 200+ developers to work on 9 parallel tracks without conflicts or duplication.
