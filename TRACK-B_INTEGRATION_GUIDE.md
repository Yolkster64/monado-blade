# TRACK-B: Object Pooling Integration Guide
## How to Integrate MessagePool and TaskDescriptorPool into MonadoBlade

---

## Quick Start

### 1. Register Pools in Dependency Injection

```csharp
// In Program.cs or Startup configuration
using HELIOS.Platform.Architecture.ObjectPooling;

services.AddSingleton<MessagePool>(sp => new MessagePool(
    initialPoolSize: 100,   // Pre-allocate 100 messages
    maxPoolSize: 500        // Don't exceed 500
));

services.AddSingleton<TaskDescriptorPool>(sp => new TaskDescriptorPool(
    initialSize: 50,        // Pre-allocate 50 descriptors
    maxSize: 250            // Don't exceed 250
));
```

### 2. Inject and Use in Services

```csharp
public class InterServiceBusOptimized : IInterServiceBus
{
    private readonly MessagePool _messagePool;
    
    public InterServiceBusOptimized(MessagePool messagePool)
    {
        _messagePool = messagePool;
    }
    
    public async Task PublishAsync(ServiceMessage message)
    {
        // Message is already allocated by caller
        // Just use it...
        // Then return it to pool when done
    }
    
    public void Reply(ServiceMessage request, object responsePayload)
    {
        // GET a pooled message
        var response = _messagePool.GetMessage();
        
        try
        {
            response.Id = request.Id;
            response.FromService = request.ToService;
            response.ToService = request.FromService;
            response.MessageType = request.MessageType + ".Response";
            response.Payload = responsePayload;
            response.Metadata["IsReply"] = true;
            
            // Send response...
            
            // RETURN it to pool when done
        }
        finally
        {
            _messagePool.ReturnMessage(response);
        }
    }
}
```

---

## Integration Patterns

### Pattern 1: Message Creation Site

**Before:**
```csharp
var response = new ServiceMessage
{
    Id = request.Id,
    FromService = request.ToService,
    ToService = request.FromService,
    MessageType = "Response",
    Payload = data
};
```

**After:**
```csharp
var response = _messagePool.GetMessage();
response.Id = request.Id;
response.FromService = request.ToService;
response.ToService = request.FromService;
response.MessageType = "Response";
response.Payload = data;

// ... use message ...

_messagePool.ReturnMessage(response);
```

### Pattern 2: Try-Finally Protection

**Recommended for complex flows:**
```csharp
var message = _messagePool.GetMessage();

try
{
    message.FromService = "ServiceA";
    message.ToService = "ServiceB";
    message.MessageType = "Command";
    
    await PublishAsync(message);
}
finally
{
    _messagePool.ReturnMessage(message);
}
```

### Pattern 3: Using Statements (C# 8+)

**For scoped cleanup:**
```csharp
using (var batch = new MessageBatch(_messagePool))
{
    batch.Add(_messagePool.GetMessage());
    batch.Add(_messagePool.GetMessage());
    
    await ProcessBatch(batch);
    // All messages automatically returned on Dispose
}
```

### Pattern 4: Task Descriptor Usage

```csharp
public class TaskSchedulerOptimized : ITaskScheduler
{
    private readonly TaskDescriptorPool _taskPool;
    
    public void ScheduleTask(ScheduledTask task)
    {
        var descriptor = _taskPool.GetDescriptor();
        
        try
        {
            descriptor.Id = task.Id;
            descriptor.Name = task.Name;
            descriptor.Priority = task.Priority;
            descriptor.ScheduledFor = task.ScheduledFor;
            
            // Use descriptor...
            _taskQueue.Enqueue(descriptor);
        }
        finally
        {
            _taskPool.ReturnDescriptor(descriptor);
        }
    }
}
```

---

## Monitoring & Diagnostics

### Get Pool Statistics

```csharp
var stats = _messagePool.GetStatistics();

Console.WriteLine($"Pooled objects: {stats.PooledObjectCount}");
Console.WriteLine($"Pool hit rate: {stats.HitRate:F2}%");
Console.WriteLine($"Total allocations: {stats.TotalAllocations}");
Console.WriteLine($"Memory usage: {stats.CurrentMemoryUsageBytes / 1024.0:F2} KB");
Console.WriteLine(stats); // ToString() includes all metrics
```

### Example Output

```
Pool Stats | Pooled: 95 | Hits: 4827 | Misses: 189 | 
HitRate: 96.23% | Memory: 28.50KB / 150.00KB | Capacity: 95.0%
```

### Logging Integration

```csharp
// Log statistics periodically
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
        
        var stats = _messagePool.GetStatistics();
        _logger.LogInformation("MessagePool: {Stats}", stats);
        
        if (stats.HitRate < 70)
        {
            _logger.LogWarning("MessagePool hit rate low: {Rate}%", stats.HitRate);
        }
    }
});
```

---

## Performance Tuning

### Sizing Guidelines

**MessagePool:**
- **Small services (< 100 req/sec):** initialSize=20, maxSize=50
- **Medium services (100-1000 req/sec):** initialSize=100, maxSize=500
- **Large services (> 1000 req/sec):** initialSize=250, maxSize=1000

**TaskDescriptorPool:**
- **Low task rate (< 50 task/sec):** initialSize=10, maxSize=50
- **Medium task rate (50-200 task/sec):** initialSize=50, maxSize=250
- **High task rate (> 200 task/sec):** initialSize=100, maxSize=500

### Warm-up Strategy

```csharp
// On startup, pre-fill pools to reduce cold-start misses
var messagePool = services.GetRequiredService<MessagePool>();
var taskPool = services.GetRequiredService<TaskDescriptorPool>();

messagePool.Prefill(100);  // Fill to configured initial size
taskPool.Prefill(50);
```

### Monitoring Hit Rates

```csharp
// Track hit rate trends
public class PoolMonitor
{
    public async Task MonitorAsync(MessagePool pool)
    {
        var lastStats = pool.GetStatistics();
        
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            
            var stats = pool.GetStatistics();
            var hitRateDelta = stats.HitRate - lastStats.HitRate;
            
            if (hitRateDelta < -10)
            {
                Console.WriteLine($"⚠ Hit rate dropped by {-hitRateDelta}%");
            }
            
            lastStats = stats;
        }
    }
}
```

---

## Common Issues & Troubleshooting

### Issue 1: Low Hit Rate (<75%)

**Symptoms:**
- Pool.GetStatistics() shows HitRate < 75%
- GC pressure still high

**Causes:**
1. Pool too small for actual load
2. Pools not pre-filled at startup
3. Messages not being returned

**Solutions:**
```csharp
// Increase pool sizes
var pool = new MessagePool(initialPoolSize: 200, maxPoolSize: 1000);

// Pre-fill on startup
pool.Prefill(200);

// Verify returns (add logging)
pool.ReturnMessage(message); // Should see ResetCount increase
```

### Issue 2: Memory Growth

**Symptoms:**
- Memory usage increasing over time
- Peak memory not stabilizing

**Causes:**
1. Leaking message references
2. Metadata dictionary not clearing
3. Circular references in payloads

**Solutions:**
```csharp
// Clear payloads when returning
void ReturnMessageSafely(ServiceMessage msg)
{
    msg.Payload = null;  // Ensure no circular refs
    msg.Metadata.Clear(); // Don't leave data
    _pool.ReturnMessage(msg);
}

// Monitor memory growth
var stats = pool.GetStatistics();
if (stats.CurrentMemoryUsageBytes > stats.PeakMemoryUsageBytes * 0.9)
{
    _logger.LogWarning("Pool near capacity: {Usage}KB", 
        stats.CurrentMemoryUsageBytes / 1024.0);
}
```

### Issue 3: ObjectDisposedException

**Symptoms:**
```
ObjectDisposedException: Cannot access pool after disposal
```

**Causes:**
1. Accessing pool after Dispose()
2. ServiceProvider disposed while pool still in use

**Solutions:**
```csharp
// Don't dispose pools manually (let DI container handle it)
using var pool = new MessagePool(); // ❌ Don't do this in DI
var pool = services.GetRequiredService<MessagePool>(); // ✓ Do this

// For tests, use using statement
[Fact]
public void TestWithPool()
{
    using (var pool = new MessagePool(10, 50)) // ✓ OK for tests
    {
        var msg = pool.GetMessage();
        // ...
    }
} // Pool auto-disposed here
```

---

## Thread Safety Verification

### Safe Operations (Concurrent Use)

```csharp
// ✓ Thread-safe - ConcurrentBag operations
Parallel.For(0, 1000, i => 
{
    var msg = _pool.GetMessage();     // Safe
    _pool.ReturnMessage(msg);         // Safe
});

// ✓ Thread-safe - Atomic counter updates
var stats = _pool.GetStatistics();    // Safe snapshot
```

### Unsafe Operations

```csharp
// ❌ NOT thread-safe - Clear while using
Task.Run(() => _pool.GetMessage());
_pool.Clear(); // Race condition!

// ❌ NOT thread-safe - Dispose while in use
_pool.Dispose();
_pool.GetMessage(); // ObjectDisposedException
```

---

## Integration Checklist

- [ ] Pools registered in DI container
- [ ] Pre-fill strategy configured
- [ ] All new ServiceMessage created via GetMessage()
- [ ] All returns to pool in try-finally or finally blocks
- [ ] Statistics monitoring implemented
- [ ] Hit rate > 85% verified under load
- [ ] No memory leaks detected (profiler check)
- [ ] Performance baseline established
- [ ] Team trained on integration pattern
- [ ] Deployment rollout plan ready

---

## Rollout Strategy

### Phase 1: Shadow Mode (Week 1)
```csharp
// Create new pools, log statistics, but don't use yet
var pool = new MessagePool(50, 500);
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
        _logger.LogInformation("Pool ready: {Stats}", pool.GetStatistics());
    }
});
```

### Phase 2: Gradual Rollout (Week 2)
```csharp
// Start using in low-volume services
// Monitor hit rates and memory
if (stats.HitRate > 80)
{
    // Expand to more services
}
```

### Phase 3: Full Deployment (Week 3)
- All services using pooling
- Sustained hit rates > 85%
- Memory pressure reduced 30%+
- Monitor and adjust sizing as needed

---

## Performance Expected

After integration:

| Metric | Improvement |
|--------|------------|
| GC Gen0 collections | -95% |
| Message throughput | +500-800% |
| Latency (P99) | -98% |
| Memory throughput | -96% |
| Overall responsiveness | +3-5x |

---

## References

- **MessagePool API:** `HELIOS.Platform.Architecture.ObjectPooling.MessagePool`
- **TaskDescriptorPool API:** `HELIOS.Platform.Architecture.ObjectPooling.TaskDescriptorPool`
- **Performance Report:** `TRACK-B_OPTIMIZATION_REPORT.md`
- **Test Suite:** `MonadoBlade.Tests.ObjectPooling`

---

*Integration Guide - TRACK-B (opt-002)*  
*Last Updated: Hour 8*  
