# Troubleshooting Guide - Monado Blade v2.4.0

**Solutions to common issues and performance problems**

*Read time: 20-25 minutes | Skill level: Intermediate*

## Table of Contents

1. [Connection Issues](#connection-issues)
2. [Provider Issues](#provider-issues)
3. [Routing Issues](#routing-issues)
4. [Performance Issues](#performance-issues)
5. [Monitoring & Debugging](#monitoring--debugging)
6. [Deployment Issues](#deployment-issues)

---

## Connection Issues

### Issue: "Unable to connect to provider"

**Symptoms:**
- `ProviderError: Failed to connect`
- Timeout errors on all requests
- Connection refused

**Root Causes:**
1. Provider endpoint URL is wrong
2. Network connectivity issue
3. Firewall blocking connection
4. Provider is down

**Solutions:**

```powershell
# 1. Verify configuration
$config = Get-Content appsettings.json | ConvertFrom-Json
Write-Host "OpenAI endpoint: $($config.Monado.Providers[0].Configuration.Endpoint)"

# 2. Test network connectivity
Test-Connection api.openai.com -Verbose

# 3. Test specific endpoint
$response = Invoke-WebRequest -Uri "https://api.openai.com/v1/models" `
  -Headers @{"Authorization" = "Bearer $apiKey"} `
  -ErrorAction Stop
Write-Host "Status: $($response.StatusCode)"

# 4. Check firewall
netsh advfirewall firewall show rule name="Monado"

# 5. Monitor provider health
curl http://localhost:8080/health/detailed | ConvertFrom-Json | 
  Select-Object -ExpandProperty providers | 
  Format-Table name,status
```

**Diagnosis Code:**

```csharp
public async Task DiagnoseConnectivityAsync()
{
    var providers = serviceProvider
        .GetRequiredService<IEnumerable<IProviderAdapter>>();
    
    foreach (var provider in providers)
    {
        Console.WriteLine($"\n=== {provider.ProviderName} ===");
        
        // Test health
        var health = await provider.GetHealthAsync();
        Console.WriteLine($"Health: {health.State}");
        
        // Get status
        var status = await provider.GetStatusAsync();
        Console.WriteLine($"Status: {status}");
        
        // Try test request
        try
        {
            var testRequest = new InferenceRequest
            {
                Model = provider.Capabilities.MaxContextLength > 0 
                    ? "test" 
                    : null,
                Messages = new() 
                { 
                    new() { Role = "user", Content = "ping" } 
                },
                MaxTokens = 1
            };
            
            var result = await provider.InferenceAsync(testRequest);
            Console.WriteLine($"Test request: {(result.IsSuccess ? "Success" : "Failed")}");
            
            if (result.IsFailure)
            {
                var failure = result as Result<InferenceResponse>.Failure;
                Console.WriteLine($"  Error: {failure.Code}");
                Console.WriteLine($"  Message: {failure.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test request failed: {ex.Message}");
        }
    }
}
```

---

### Issue: "Connection timeout"

**Symptoms:**
- Requests hang for 30+ seconds
- Timeouts on every request
- Intermittent timeouts

**Solutions:**

```csharp
// Check timeout configuration
var config = context.Configuration.Get<HttpClientConfig>("Http");
Console.WriteLine($"Current timeout: {config.TimeoutSeconds}s");

// Increase timeout for slow providers
services.AddHttpClient<SlowProvider>()
    .ConfigureHttpClient(c => 
    {
        c.Timeout = TimeSpan.FromSeconds(60);  // Increased from 30
    });

// Add request logging to debug
services.AddHttpClient<MyProvider>()
    .AddLogging()
    .AddTransientHttpErrorPolicy(p => 
        p.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => 
                TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

---

## Provider Issues

### Issue: "API Key Invalid" (401 Unauthorized)

**Symptoms:**
- `ProviderUnauthorized` error
- All requests fail with 401
- Authentication error in logs

**Solutions:**

```powershell
# 1. Verify API key is set
$env:OPENAI_API_KEY
# Should output your key, not empty

# 2. Check for special characters
$key = $env:OPENAI_API_KEY
Write-Host "Length: $($key.Length)"
Write-Host "First 5: $($key.Substring(0,5))"

# 3. Test API key directly
$headers = @{"Authorization" = "Bearer $key"}
$response = Invoke-WebRequest -Uri "https://api.openai.com/v1/models" `
  -Headers $headers -ErrorAction Stop
Write-Host "Response: $($response.StatusCode)"

# 4. Verify key hasn't expired
# (Some providers rotate keys, check dashboard)

# 5. Check key has right permissions
# (May need specific scopes/roles configured)
```

**Diagnosis Code:**

```csharp
public class ApiKeyValidator
{
    public void ValidateApiKey(IServiceContext context, string providerName)
    {
        var keyName = $"{providerName.ToUpper()}_API_KEY";
        var keyValue = context.Configuration.Get<string>(keyName);
        
        Console.WriteLine($"Key name: {keyName}");
        Console.WriteLine($"Key set: {!string.IsNullOrEmpty(keyValue)}");
        
        if (!string.IsNullOrEmpty(keyValue))
        {
            Console.WriteLine($"Length: {keyValue.Length}");
            Console.WriteLine($"Format: {keyValue.Substring(0, 5)}...");
            Console.WriteLine($"Has spaces: {keyValue.Contains(" ")}");
            Console.WriteLine($"Has newlines: {keyValue.Contains("\n")}");
        }
    }
}
```

---

### Issue: "Rate limit exceeded" (429 Too Many Requests)

**Symptoms:**
- `ProviderRateLimited` error
- Intermittent failures under load
- Specific time patterns (consistent per minute)

**Solutions:**

```csharp
// 1. Implement exponential backoff
public class RateLimitHandler
{
    public async Task<Result<InferenceResponse>> WithRetryAsync(
        Func<Task<Result<InferenceResponse>>> operation,
        int maxRetries = 3)
    {
        int attempt = 0;
        
        while (attempt < maxRetries)
        {
            var result = await operation();
            
            if (result.IsSuccess)
                return result;
            
            var failure = result as Result<InferenceResponse>.Failure;
            
            if (failure.Code != ErrorCode.ProviderRateLimited)
                return result;  // Not a rate limit issue
            
            attempt++;
            
            if (attempt < maxRetries)
            {
                // Exponential backoff: 1s, 2s, 4s
                var delayMs = (int)Math.Pow(2, attempt) * 1000;
                Console.WriteLine($"Rate limited - retrying in {delayMs}ms");
                await Task.Delay(delayMs);
            }
        }
        
        return result;
    }
}

// 2. Distribute requests across providers
var router = serviceProvider.GetRequiredService<ISmartRouter>();
var strategy = new BalancedStrategy();  // Spreads load

// 3. Implement request queuing
public class RequestQueue
{
    private readonly SemaphoreSlim _semaphore = new(10);  // 10 concurrent
    
    public async Task<T> QueueAsync<T>(Func<Task<T>> operation)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

---

### Issue: "Quota exceeded" (402 Payment Required)

**Symptoms:**
- `ProviderQuotaExceeded` error
- Suddenly stops working mid-month
- All requests fail with same code

**Solutions:**

```csharp
// 1. Track monthly spending
public class CostTracker
{
    private decimal _monthlySpend = 0;
    private decimal _monthlyBudget = 100.00m;
    
    public async Task<Result<InferenceResponse>> CheckBudgetAsync(
        InferenceRequest request,
        IProviderAdapter provider)
    {
        var estimatedCost = EstimateCost(request, provider);
        
        if (_monthlySpend + estimatedCost > _monthlyBudget)
        {
            var remaining = _monthlyBudget - _monthlySpend;
            Console.WriteLine($"Budget exceeded! Remaining: ${remaining:F2}");
            
            // Route to cheapest provider only
            return null;  // Handle appropriately
        }
        
        var result = await provider.InferenceAsync(request);
        if (result.IsSuccess)
        {
            _monthlySpend += result.Value.Metrics.EstimatedCost;
        }
        
        return result;
    }
    
    private decimal EstimateCost(InferenceRequest request, IProviderAdapter provider)
    {
        var inputTokens = EstimateTokens(
            string.Join("\n", request.Messages.Select(m => m.Content)));
        var outputTokens = request.MaxTokens ?? 100;
        
        return (inputTokens * provider.Capabilities.CostPerInputToken) +
               (outputTokens * provider.Capabilities.CostPerOutputToken);
    }
}

// 2. Reduce usage
var cheapStrategy = new CostOptimizationStrategy();

// 3. Wait for reset
Console.WriteLine("Waiting for monthly reset...");
await Task.Delay(TimeSpan.FromDays(30));
```

---

## Routing Issues

### Issue: "No providers available"

**Symptoms:**
- `ErrorCode.NoProvidersAvailable`
- All providers unhealthy simultaneously
- Can't make any requests

**Solutions:**

```csharp
// 1. Check provider health
var monitor = serviceProvider.GetRequiredService<IHealthMonitor>();
var health = await monitor.GetAllHealthAsync();

foreach (var (name, status) in health)
{
    Console.WriteLine($"{name}: {status.State}");
    if (status.State != HealthState.Healthy)
    {
        Console.WriteLine($"  Reason: {status.Reason}");
        Console.WriteLine($"  Exception: {status.Exception?.Message}");
    }
}

// 2. Get health history
var history = await monitor.GetHealthHistoryAsync("OpenAI", 60);
foreach (var entry in history.OrderByDescending(h => h.Timestamp))
{
    Console.WriteLine($"{entry.Timestamp}: {entry.State}");
}

// 3. Manual provider recovery
var failover = serviceProvider.GetRequiredService<IFailoverController>();
await failover.TriggerFailoverAsync("OpenAI", "Manual recovery");

// 4. Restart unhealthy provider
var registry = serviceProvider.GetRequiredService<IProviderRegistry>();
var provider = registry.GetProvider("OpenAI");
await provider.ShutdownAsync();
await provider.InitializeAsync(context);
```

---

### Issue: "Wrong provider selected"

**Symptoms:**
- Slow provider used when fast available
- Expensive provider used when cheap available
- Low-quality model used for complex task

**Solutions:**

```csharp
// 1. Check routing strategy
var router = serviceProvider.GetRequiredService<ISmartRouter>();
var stats = await router.GetStatsAsync();

Console.WriteLine("Routing statistics:");
foreach (var (provider, count) in stats.ProviderSelectionCounts)
{
    var percent = (float)count / stats.TotalRequests * 100;
    Console.WriteLine($"  {provider}: {count} ({percent:F1}%)");
}

// 2. Verify provider weights
var config = context.Configuration.GetSection("Routing");
var providers = config.GetSection("Providers").Get<List<ProviderConfig>>();
foreach (var p in providers)
{
    Console.WriteLine($"{p.Name}: weight={p.Weight}, priority={p.Priority}");
}

// 3. Manually specify provider
var specificProvider = registry.GetProvider("Groq");
var result = await specificProvider.InferenceAsync(request);

// 4. Review strategy scoring
public class StrategyDebugger
{
    public void DebugStrategyScoring(IRoutingStrategy strategy)
    {
        var providers = /* get providers */;
        var context = /* get context */;
        
        var scores = providers
            .Select(p => new
            {
                Provider = p.ProviderName,
                Score = strategy.CalculateScore(p, context),
                Cost = p.CostPer1kTokens,
                Latency = p.AverageLatencyMs,
                Health = /* health status */
            })
            .OrderByDescending(x => x.Score);
        
        foreach (var s in scores)
        {
            Console.WriteLine($"{s.Provider}: {s.Score:F2}");
            Console.WriteLine($"  Cost: {s.Cost:F6}, Latency: {s.Latency}ms");
        }
    }
}
```

---

## Performance Issues

### Issue: "Slow inference responses"

**Symptoms:**
- P95 latency > 5 seconds
- Users experience timeouts
- Requests take progressively longer

**Solutions:**

```csharp
// 1. Identify bottleneck
var metrics = await GetMetricsAsync();

Console.WriteLine("Latency breakdown:");
Console.WriteLine($"  Provider processing: {metrics.ProviderLatencyMs}ms");
Console.WriteLine($"  Serialization: {metrics.SerializationMs}ms");
Console.WriteLine($"  Network: {metrics.NetworkLatencyMs}ms");

// 2. Provider selection optimization
var latencyStrategy = new LatencyOptimizationStrategy();
var result = await router.InferenceAsync(request, latencyStrategy);

// 3. Use parallel invocation for time-sensitive requests
var parallelStrategy = new ParallelInvocationStrategy();
var fastResponse = await router.InferenceAsync(request, parallelStrategy);

// 4. Cache responses
public class ResponseCache
{
    public async Task<InferenceResponse> GetOrComputeAsync(
        InferenceRequest request,
        Func<Task<InferenceResponse>> compute)
    {
        var key = ComputeKey(request);
        var cached = await cache.GetAsync<InferenceResponse>(key);
        
        if (cached != null)
        {
            Console.WriteLine("Cache hit!");
            return cached;
        }
        
        var result = await compute();
        await cache.SetAsync(key, result, TimeSpan.FromHours(1));
        
        return result;
    }
}

// 5. Reduce context size
// Long conversations = slower responses
if (messages.Count > 20)
{
    // Summarize old messages
    var summary = await SummarizeOldMessagesAsync(messages.Take(messages.Count - 10));
    messages = new() { summary }.Concat(messages.TakeLast(10)).ToList();
}
```

---

### Issue: "High memory usage"

**Symptoms:**
- Memory grows over time
- Eventually crashes with OutOfMemory
- Memory not released after requests

**Solutions:**

```csharp
// 1. Configure memory cache limits
services.AddMemoryCache(options =>
{
    options.SizeLimit = 536870912;  // 512MB
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

// 2. Use distributed cache (Redis) instead
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// 3. Implement cache size tracking
services.AddMemoryCache();
app.Use(async (context, next) =>
{
    var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
    
    // Clear old entries
    if (cache is MemoryCache mc)
    {
        mc.Compact(0.25);
    }
    
    await next();
});

// 4. Monitor conversation history size
public class ConversationManager
{
    private const int MaxHistoryTokens = 8000;
    
    public async Task AddMessageAsync(string content)
    {
        var tokenCount = EstimateTokens(string.Join("\n", 
            conversations.Select(m => m.Content)));
        
        if (tokenCount > MaxHistoryTokens)
        {
            // Remove oldest messages
            conversations.RemoveAt(0);
        }
        
        conversations.Add(new Message { Content = content });
    }
}
```

---

## Monitoring & Debugging

### Enable Debug Logging

```csharp
// Configure detailed logging
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
    builder.AddFile("/var/log/monado/debug.log");
});

// Log all routing decisions
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger>();
    logger.LogDebug("Request metadata: {@Metadata}", new
    {
        Path = context.Request.Path,
        Method = context.Request.Method,
        CorrelationId = context.TraceIdentifier
    });
    
    var startTime = DateTime.UtcNow;
    await next();
    
    logger.LogDebug("Response time: {ElapsedMs}ms", 
        (DateTime.UtcNow - startTime).TotalMilliseconds);
});
```

### Request Tracing

```csharp
public class RequestTracer
{
    public async Task TraceRequestAsync(InferenceRequest request)
    {
        var logger = context.Logger;
        var correlationId = context.CorrelationId;
        
        logger.Information(
            "[{CorrelationId}] Request: Model={Model}, Messages={Count}",
            correlationId, request.Model, request.Messages.Count);
        
        var router = context.ServiceProvider.GetRequiredService<ISmartRouter>();
        var result = await router.InferenceAsync(request, strategy);
        
        if (result.IsSuccess)
        {
            logger.Information(
                "[{CorrelationId}] Response: Tokens={Tokens}, " +
                "Cost=${Cost:F4}, Latency={LatencyMs}ms, Provider={Provider}",
                correlationId,
                result.Value.OutputTokens,
                result.Value.Metrics.EstimatedCost,
                result.Value.Metrics.LatencyMs,
                result.Value.Metrics.ProviderId);
        }
        else
        {
            logger.Error(
                "[{CorrelationId}] Error: Code={Code}, Message={Message}",
                correlationId,
                (result as Result<InferenceResponse>.Failure).Code,
                (result as Result<InferenceResponse>.Failure).Message);
        }
    }
}
```

---

## Deployment Issues

### Issue: "Service won't start"

**Solutions:**

```powershell
# 1. Check service status
Get-Service MonadoService

# 2. View service logs
Get-EventLog -LogName Application -Source MonadoService -Newest 20

# 3. Try running manually
C:\monado-blade\bin\Release\net8.0\Monado.Server.exe

# 4. Check configuration file
Test-Path C:\monado-blade\appsettings.json
Get-Content C:\monado-blade\appsettings.json | ConvertFrom-Json

# 5. Verify ports are available
netstat -ano | findstr :8080

# 6. Check file permissions
Get-Acl C:\monado-blade\bin\Release | Format-List
```

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
