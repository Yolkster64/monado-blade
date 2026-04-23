# Monado Blade v2.2.0 - Developer Quick Reference

## Essential Files Location Map

```
Core Architecture:
├── src/Core/Common/CoreInterfaces.cs       ← All core interfaces
├── src/Core/Common/UnifiedInterfaces.cs    ← Track-specific interfaces  
├── src/Core/Common/BaseClasses.cs          ← Base classes for all services
├── src/Core/ErrorCode.cs                   ← Error definitions
├── src/Core/Patterns/ResultPattern.cs      ← Result<T> pattern
├── src/Core/Patterns/CommonPatterns.cs     ← 6 patterns

Track Examples:
├── src/Tracks/A_AIHub/Services/AIHubService.cs
├── src/Tracks/B_CrossPartitionSDK/Core/SDKProvider.cs

Documentation:
├── docs/COMPREHENSIVE_SUMMARY.md           ← Complete overview
├── docs/Architecture/ARCHITECTURE.md       ← Detailed architecture
├── docs/Architecture/VISUAL_ARCHITECTURE.md← Diagrams & flows
├── docs/Guides/IMPLEMENTATION_GUIDE.md     ← Step-by-step guide
```

## 30-Second Service Creation Template

```csharp
// 1. Define interface
public interface IMyService : IServiceComponent
{
    Task<Result<T>> DoWorkAsync(TRequest req);
}

// 2. Implement
public class MyService : ServiceComponentBase, IMyService
{
    public MyService(IServiceContext context) 
        : base(context, "MyService") { }
    
    public async Task<Result<T>> DoWorkAsync(TRequest req)
    {
        ThrowIfNotInitialized();
        return await AsyncOperationPattern.ExecuteWithRetryAsync(
            "MyService.DoWork",
            ct => OnDoWorkAsync(req, ct),
            _logger, _metrics);
    }
    
    protected abstract Task<Result<T>> OnDoWorkAsync(
        TRequest req, CancellationToken ct);
}

// 3. Register: services.AddScoped<IMyService, MyService>();

// 4. Use:
var result = await myService.DoWorkAsync(request);
```

## Code Snippets

### Error Handling (Choose Pattern)

**Pattern 1: Direct Return**
```csharp
if (condition) 
    return ErrorCode.ValidationFailed.ToFailure<MyType>("reason");
```

**Pattern 2: Pattern Matching**
```csharp
return result.Match(
    success => DoSomething(success),
    (code, msg, ex) => HandleError(code, msg));
```

**Pattern 3: Async Pattern Matching**
```csharp
return await result.MatchAsync(
    success => ProcessAsync(success),
    (code, msg, ex) => HandleErrorAsync(code, msg));
```

### Retry Pattern

```csharp
// Simple retry
var result = await AsyncOperationPattern.ExecuteWithRetryAsync(
    "OperationName",
    ct => operation(ct),
    _logger, _metrics);

// Custom retry config
var config = new RetryPolicyConfig(
    MaxRetries: 5,
    InitialDelay: TimeSpan.FromMilliseconds(200),
    BackoffMultiplier: 1.5,
    IsRetryable: code => code == ErrorCode.Timeout);

var result = await AsyncOperationPattern.ExecuteWithRetryAsync(
    "OperationName",
    ct => operation(ct),
    _logger, _metrics, config);
```

### Caching

```csharp
// Simple cache
var cached = await CachingPattern.GetOrComputeAsync(
    "key",
    ct => LoadAsync(ct),
    cache);

// With custom expiration
var cached = await CachingPattern.GetOrComputeAsync(
    "key",
    ct => LoadAsync(ct),
    cache,
    TimeSpan.FromMinutes(5));

// Invalidate pattern
await CachingPattern.InvalidatePatternAsync("key:*", cache);
```

### Atomic Operations

```csharp
var atomic = new AtomicOperation(_logger);

var r1 = await atomic.ExecuteAsync(
    "Step1",
    () => Step1Async(),
    () => RollbackStep1());

var r2 = await atomic.ExecuteAsync(
    "Step2",
    () => Step2Async(),
    () => RollbackStep2());

if (r1 is Result.Success && r2 is Result.Success)
    await atomic.CommitAsync();
else
    // Automatic rollback happens
```

### Resource Pooling

```csharp
public class MyPool : ResourcePoolBase<Connection>
{
    public MyPool(IServiceContext ctx) 
        : base(ctx, "MyPool", maxPoolSize: 100) { }
    
    protected override async Task<Connection> CreateResourceAsync(CancellationToken ct)
    {
        return await Connection.CreateAsync(ct);
    }
}

// Usage
var conn = await pool.AcquireAsync();
try { /* use connection */ }
finally { await pool.ReleaseAsync(conn); }
```

### Configuration

```csharp
// Get with default
var timeout = config.Get("Timeout", TimeSpan.FromSeconds(30));

// Get required
var keyResult = config.GetRequired<string>("ApiKey");
if (keyResult is Result<string>.Failure f)
    return f;

var key = (keyResult as Result<string>.Success)!.Value;

// Watch for changes
using var watch = config.Watch("Timeout", newValue =>
{
    _timeout = (TimeSpan)newValue;
});
```

### Logging

```csharp
// Simple logging
_logger.Information("Operation started");
_logger.Warning("Unusual condition detected");
_logger.Error("Operation failed", exception);

// Operation with automatic timing
var result = await _logger.LogOperationAsync(
    "ImportData",
    async () => await ImportDataAsync());
```

### Metrics

```csharp
// Counter
_metrics.IncrementCounter("api_requests", tags: ("endpoint", "/users"));

// Gauge
_metrics.SetGauge("active_connections", count);

// Duration
_metrics.RecordDuration("operation_time", duration, 
    ("operation", "Import"), ("status", "success"));

// Histogram
_metrics.RecordHistogram("response_size_bytes", size);
```

## Common Error Codes Quick Lookup

| Code | Meaning | When | Resolution |
|------|---------|------|------------|
| 0 | Success | Operation succeeded | - |
| 1 | Unknown | Unexpected error | Check logs |
| 2 | InvalidArgument | Bad input | Validate input |
| 100 | ValidationFailed | Validation error | Fix data |
| 101 | RequiredFieldMissing | Missing field | Provide field |
| 1000 | AIProviderError | AI call failed | Retry or fallback |
| 1002 | AIProviderRateLimited | Rate limit | Wait & retry |
| 2000 | SDKInitError | SDK init failed | Check config |
| 3000 | VMBootError | VM boot failed | Check resources |
| 5000 | EncryptionError | Encryption failed | Check keys |

## Testing Checklist

```csharp
[TestFixture]
public class MyServiceTests
{
    private Mock<IServiceContext> _contextMock;
    private MyService _service;
    
    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<IServiceContext>();
        _service = new MyService(_contextMock.Object);
    }
    
    [Test]
    public async Task InitializesSuccessfully()
    {
        var result = await _service.InitializeAsync(_contextMock.Object);
        Assert.That(result, Is.InstanceOf<Result.Success>());
    }
    
    [Test]
    public async Task ThrowsIfNotInitialized()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DoWorkAsync(request));
    }
    
    [Test]
    public async Task ValidatesInput()
    {
        await _service.InitializeAsync(_contextMock.Object);
        var result = await _service.DoWorkAsync(invalidRequest);
        Assert.That(result, Is.InstanceOf<Result.Failure>());
    }
}
```

## Deployment Checklist

Before deploying:
- [ ] All services inherit from ServiceComponentBase
- [ ] All errors use ErrorCode enum (no exceptions for expected failures)
- [ ] All async methods accept CancellationToken
- [ ] Configuration is centralized (no hardcoded values)
- [ ] Secrets loaded securely (not from source)
- [ ] All external dependencies mocked for testing
- [ ] Health checks pass: `service.GetHealthAsync()`
- [ ] Metrics are flowing correctly
- [ ] Logging is structured (no format strings)
- [ ] Security: no SQL injection, XSS, or injection attacks possible

## Performance Guidelines

### Connection Pooling
```csharp
// Set pool size to: (peak_connections_per_sec * avg_response_time_sec) + 20%
// Example: (1000 * 0.1) * 1.2 = 120 connections
new ResourcePoolBase(context, "DbPool", maxPoolSize: 120);
```

### Caching Strategy
| Data | TTL | Invalidation |
|------|-----|--------------|
| User data | 1 hour | On update |
| Configuration | 5 minutes | On change event |
| API responses | 5 minutes | Per endpoint |
| Session data | 30 minutes | On logout |

### Batch Processing
```csharp
// Batch size = (max_memory_mb / avg_item_size_bytes)
var batches = items.Batch(100);  // Process in groups of 100
foreach (var batch in batches)
    await ProcessAsync(batch);
```

## Integration Between Tracks

### Track A → Track B
```csharp
// Track A (AI) uses Track B (SDKs) to store results
var aiResult = await aiHub.InferenceAsync(request);
await sdk.ExecuteAsync("AWS:S3Operations", "PutObject", 
    new() { { "Bucket", "results" }, { "Body", aiResult.Content } });
```

### Track B → Track C
```csharp
// Track B (SDKs) queries Track C (VMs) for available resources
var vms = await orchestrator.GetAvailableVMsAsync();
var result = await sdk.ExecuteAsync("AWS:EC2", "DescribeInstances", ...);
```

### Track C → Track D
```csharp
// Track C (Orchestration) sends status to Track D (UI)
await eventBus.PublishAsync(new VMStatusChanged(vmId, newStatus));
// Dashboard subscribes and updates display
```

## Essential Interfaces Reference

### Must Implement
```csharp
IServiceComponent   // All services
IAIProvider         // AI systems
ISDKProvider        // SDK wrappers
IVirtualMachineManager  // VM systems
IUIComponent        // UI components
```

### Commonly Used
```csharp
IServiceContext     // Passed everywhere
ILoggingProvider    // _logger
IMetricsCollector   // _metrics
ICacheProvider      // Caching
IEventBus           // Messaging
IValidator<T>       // Validation
```

## Environment Setup (First Time)

```powershell
# Clone repository
git clone <repo> MonadoBlade
cd MonadoBlade

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Start development
code .
```

## Troubleshooting

| Problem | Check | Fix |
|---------|-------|-----|
| Service not initializing | Configuration missing | Verify all required config keys |
| Health check fails | Dependencies unavailable | Check external services |
| Metrics not appearing | Collector not connected | Verify metrics backend |
| Caching not working | Cache backend down | Check cache connection |
| Operations timing out | Pool exhausted | Increase pool size |
| Errors in logs | Invalid input | Add validation |

---

**Remember**: 
1. Always inherit from ServiceComponentBase
2. Always return Result<T> (never throw for expected errors)
3. Always use AsyncOperationPattern for external calls
4. Always cache expensive operations
5. Always validate input at boundaries
6. Always use pre-defined ErrorCodes
7. Always log important events
8. Always dispose resources properly

For questions, see:
- ARCHITECTURE.md (overview)
- VISUAL_ARCHITECTURE.md (diagrams)
- IMPLEMENTATION_GUIDE.md (detailed examples)
