# Code Examples - Monado Blade v2.4.0

**Complete working examples for common use cases**

*Read time: 20-30 minutes | Skill level: Beginner-Advanced*

## Table of Contents

1. [Quick Examples](#quick-examples)
2. [Chat Examples](#chat-examples)
3. [Routing Examples](#routing-examples)
4. [Advanced Examples](#advanced-examples)
5. [Error Handling](#error-handling)
6. [Monitoring Examples](#monitoring-examples)

---

## Quick Examples

### 1. The 10-Line Chat Example

```csharp
var router = serviceProvider.GetRequiredService<ISmartRouter>();
var request = new InferenceRequest 
{ 
    Model = "gpt-3.5-turbo",
    Messages = new() { new() { Role = "user", Content = "Hello!" } }
};
var result = await router.InferenceAsync(request, new BalancedStrategy());
if (result.IsSuccess)
    Console.WriteLine(result.Value.Content);
```

### 2. Simple Chat Loop

```csharp
var router = serviceProvider.GetRequiredService<ISmartRouter>();
var messages = new List<Message>();

while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();
    
    messages.Add(new() { Role = "user", Content = userInput });
    
    var request = new InferenceRequest
    {
        Model = "gpt-3.5-turbo",
        Messages = messages,
        MaxTokens = 1000
    };
    
    var result = await router.InferenceAsync(
        request, 
        new LatencyOptimizationStrategy());
    
    if (result.IsSuccess)
    {
        var response = result.Value.Content;
        messages.Add(new() { Role = "assistant", Content = response });
        Console.WriteLine($"Assistant: {response}\n");
    }
    else
    {
        Console.WriteLine($"Error: {result.Code}");
    }
}
```

### 3. Single Request with Error Handling

```csharp
try
{
    var provider = serviceProvider
        .GetRequiredService<IEnumerable<IProviderAdapter>>()
        .First(p => p.ProviderName == "OpenAI");
    
    var request = new InferenceRequest
    {
        Model = "gpt-3.5-turbo",
        Messages = new() { new() { Role = "user", Content = "Hi" } },
        MaxTokens = 50
    };
    
    var result = await provider.InferenceAsync(request);
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"Response: {result.Value.Content}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
```

---

## Chat Examples

### Multi-Turn Chat with Streaming

```csharp
public class StreamingChatExample
{
    private readonly ISmartRouter _router;
    private readonly List<Message> _conversationHistory;
    
    public StreamingChatExample(ISmartRouter router)
    {
        _router = router;
        _conversationHistory = new();
    }
    
    public async Task RunChatAsync()
    {
        Console.WriteLine("=== Streaming Chat ===");
        Console.WriteLine("Type 'exit' to quit\n");
        
        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();
            
            if (input?.ToLower() == "exit")
                break;
            
            _conversationHistory.Add(
                new() { Role = "user", Content = input });
            
            var request = new InferenceRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = _conversationHistory,
                MaxTokens = 500,
                Temperature = 0.7f,
                Stream = true
            };
            
            Console.Write("Assistant: ");
            
            var result = await _router.InferenceAsync(
                request,
                new LatencyOptimizationStrategy());
            
            if (result.IsSuccess)
            {
                var response = result.Value;
                Console.WriteLine(response.Content);
                
                _conversationHistory.Add(
                    new() { Role = "assistant", Content = response.Content });
                
                Console.WriteLine(
                    $"\n[Cost: ${response.Metrics.EstimatedCost:F4}, " +
                    $"Latency: {response.Metrics.LatencyMs}ms]\n");
            }
            else
            {
                Console.WriteLine($"Error: {result.Code}");
            }
        }
    }
}

// Usage
var example = new StreamingChatExample(router);
await example.RunChatAsync();
```

### System Prompt Example

```csharp
var messages = new List<Message>
{
    new()
    {
        Role = "system",
        Content = "You are a helpful assistant that specializes in mathematics. " +
                  "Provide clear, step-by-step solutions."
    },
    new()
    {
        Role = "user",
        Content = "Solve: 2x + 5 = 13"
    }
};

var request = new InferenceRequest
{
    Model = "gpt-4",
    Messages = messages,
    MaxTokens = 500
};

var result = await router.InferenceAsync(request, strategy);
```

### Multi-Language Chat

```csharp
var request = new InferenceRequest
{
    Model = "gpt-3.5-turbo",
    Messages = new()
    {
        new()
        {
            Role = "system",
            Content = "Translate the following English text to Spanish, French, and German. " +
                      "Provide only the translations."
        },
        new()
        {
            Role = "user",
            Content = "Hello, how are you today?"
        }
    },
    MaxTokens = 200
};

var result = await router.InferenceAsync(request, new BalancedStrategy());
if (result.IsSuccess)
{
    Console.WriteLine("Translations:");
    Console.WriteLine(result.Value.Content);
}
```

---

## Routing Examples

### Example 1: Cost Optimization for Batch Processing

```csharp
public class BatchProcessingExample
{
    private readonly ISmartRouter _router;
    
    public async Task ProcessDocumentsAsync(List<string> documents)
    {
        var strategy = new CostOptimizationStrategy();
        decimal totalCost = 0;
        
        foreach (var doc in documents)
        {
            var request = new InferenceRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new()
                {
                    new() { Role = "system", Content = "Summarize this document" },
                    new() { Role = "user", Content = doc }
                },
                MaxTokens = 200
            };
            
            var result = await _router.InferenceAsync(request, strategy);
            
            if (result.IsSuccess)
            {
                totalCost += result.Value.Metrics.EstimatedCost;
                Console.WriteLine($"Summary: {result.Value.Content}");
            }
        }
        
        Console.WriteLine($"Total cost: ${totalCost:F4}");
    }
}

// Usage
var example = new BatchProcessingExample(router);
await example.ProcessDocumentsAsync(documents);
```

### Example 2: Latency Optimization for Real-Time

```csharp
public class RealTimeChatExample
{
    private readonly ISmartRouter _router;
    
    public async Task RunRealtimeChatAsync()
    {
        var strategy = new LatencyOptimizationStrategy();
        var maxLatencyMs = 1000;  // 1 second max
        
        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();
            
            var request = new InferenceRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new()
                {
                    new() { Role = "user", Content = input }
                },
                MaxTokens = 100
            };
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var result = await _router.InferenceAsync(request, strategy);
            
            stopwatch.Stop();
            
            if (result.IsSuccess && stopwatch.ElapsedMilliseconds <= maxLatencyMs)
            {
                Console.WriteLine($"Assistant: {result.Value.Content}");
                Console.WriteLine(
                    $"[{stopwatch.ElapsedMilliseconds}ms - FAST]\n");
            }
            else if (result.IsSuccess)
            {
                Console.WriteLine($"Assistant: {result.Value.Content}");
                Console.WriteLine(
                    $"[{stopwatch.ElapsedMilliseconds}ms - SLOW]\n");
            }
            else
            {
                Console.WriteLine($"Error: {result.Code}");
            }
        }
    }
}

// Usage
var example = new RealTimeChatExample(router);
await example.RunRealtimeChatAsync();
```

### Example 3: Quality Optimization for Analysis

```csharp
public class QualityAnalysisExample
{
    private readonly ISmartRouter _router;
    
    public async Task AnalyzeComplexProblemAsync(string problem)
    {
        var strategy = new QualityOptimizationStrategy();
        
        var request = new InferenceRequest
        {
            Model = "gpt-4",  // Highest quality
            Messages = new()
            {
                new()
                {
                    Role = "system",
                    Content = "Analyze this problem in detail. " +
                              "Provide comprehensive reasoning with all steps."
                },
                new() { Role = "user", Content = problem }
            },
            MaxTokens = 2000,
            Temperature = 0.2f  // More deterministic
        };
        
        var result = await _router.InferenceAsync(request, strategy);
        
        if (result.IsSuccess)
        {
            Console.WriteLine("Analysis:");
            Console.WriteLine(result.Value.Content);
            Console.WriteLine(
                $"\nCost: ${result.Value.Metrics.EstimatedCost:F4}");
            Console.WriteLine(
                $"Tokens: {result.Value.OutputTokens}");
        }
    }
}

// Usage
var example = new QualityAnalysisExample(router);
await example.AnalyzeComplexProblemAsync("Explain quantum entanglement...");
```

### Example 4: Multi-Provider Routing

```csharp
public class MultiProviderExample
{
    private readonly ISmartRouter _router;
    private readonly IEnumerable<IProviderAdapter> _providers;
    
    public async Task CompareProvidersAsync(string prompt)
    {
        var request = new InferenceRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new()
            {
                new() { Role = "user", Content = prompt }
            },
            MaxTokens = 200
        };
        
        var results = new Dictionary<string, object>();
        
        foreach (var provider in _providers)
        {
            var result = await provider.InferenceAsync(request);
            
            if (result.IsSuccess)
            {
                var response = result.Value;
                results[provider.ProviderName] = new
                {
                    response = response.Content,
                    cost = response.Metrics.EstimatedCost,
                    latency = response.Metrics.LatencyMs
                };
            }
        }
        
        // Display comparison
        foreach (var (provider, data) in results)
        {
            var obj = data as dynamic;
            Console.WriteLine($"\n{provider}:");
            Console.WriteLine($"  Response: {obj.response}");
            Console.WriteLine($"  Cost: ${obj.cost:F4}");
            Console.WriteLine($"  Latency: {obj.latency}ms");
        }
    }
}

// Usage
var example = new MultiProviderExample(router, providers);
await example.CompareProvidersAsync("What is AI?");
```

---

## Advanced Examples

### Parallel Invocation Example

```csharp
public class ParallelInvocationExample
{
    private readonly ISmartRouter _router;
    private readonly IEnumerable<IProviderAdapter> _providers;
    
    public async Task InvokeParallelAsync(string prompt)
    {
        var request = new InferenceRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new()
            {
                new() { Role = "user", Content = prompt }
            },
            MaxTokens = 100
        };
        
        // Invoke all providers in parallel
        var tasks = _providers
            .Select(p => InvokeWithTimeoutAsync(p, request))
            .ToList();
        
        var results = await Task.WhenAll(tasks);
        
        // Use first successful response
        var winner = results
            .FirstOrDefault(r => r.IsSuccess);
        
        if (winner?.IsSuccess ?? false)
        {
            Console.WriteLine($"Winner: {winner.Value.Metrics.ProviderId}");
            Console.WriteLine($"Response: {winner.Value.Content}");
            Console.WriteLine($"Latency: {winner.Value.Metrics.LatencyMs}ms");
        }
    }
    
    private async Task<Result<InferenceResponse>> InvokeWithTimeoutAsync(
        IProviderAdapter provider,
        InferenceRequest request,
        int timeoutMs = 2000)
    {
        using var cts = CancellationTokenSource
            .CreateLinkedTokenSource();
        cts.CancelAfter(timeoutMs);
        
        try
        {
            return await provider.InferenceAsync(request, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return new Result<InferenceResponse>.Failure(
                ErrorCode.OperationTimeout,
                "Request timed out",
                null);
        }
    }
}

// Usage
var example = new ParallelInvocationExample(router, providers);
await example.InvokeParallelAsync("What is machine learning?");
```

### Consensus Routing Example

```csharp
public class ConsensusRoutingExample
{
    private readonly ISmartRouter _router;
    private readonly IEnumerable<IProviderAdapter> _providers;
    
    public async Task ConsensusVoteAsync(string question)
    {
        var request = new InferenceRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new()
            {
                new() { Role = "user", Content = question }
            },
            MaxTokens = 50
        };
        
        var responses = new List<string>();
        
        // Get responses from top 3 providers
        foreach (var provider in _providers.Take(3))
        {
            var result = await provider.InferenceAsync(request);
            if (result.IsSuccess)
                responses.Add(result.Value.Content);
        }
        
        // Find most common response
        var consensus = responses
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .First();
        
        var confidence = (float)consensus.Count() / responses.Count;
        
        Console.WriteLine($"Question: {question}");
        Console.WriteLine($"Consensus: {consensus.Key}");
        Console.WriteLine($"Confidence: {confidence:P}");
        Console.WriteLine($"Votes: {consensus.Count()}/{responses.Count}");
    }
}

// Usage
var example = new ConsensusRoutingExample(router, providers);
await example.ConsensusVoteAsync("Is the Earth round?");
```

### Cascading Quality Example

```csharp
public class CascadingQualityExample
{
    private readonly ISmartRouter _router;
    private readonly IProviderAdapter _fastProvider;
    private readonly IProviderAdapter _qualityProvider;
    
    public async Task<string> GetBestResponseAsync(string prompt)
    {
        var request = new InferenceRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new()
            {
                new() { Role = "user", Content = prompt }
            },
            MaxTokens = 200
        };
        
        // Try fast provider first
        var fastResult = await _fastProvider.InferenceAsync(request);
        
        if (fastResult.IsSuccess)
        {
            var quality = EvaluateQuality(fastResult.Value);
            
            if (quality > 0.8)  // Good enough
            {
                Console.WriteLine("Using fast provider - quality sufficient");
                return fastResult.Value.Content;
            }
        }
        
        // Fall back to quality provider
        Console.WriteLine("Escalating to quality provider...");
        var qualityResult = await _qualityProvider.InferenceAsync(request);
        
        if (qualityResult.IsSuccess)
        {
            return qualityResult.Value.Content;
        }
        
        return "Error: All providers failed";
    }
    
    private float EvaluateQuality(InferenceResponse response)
    {
        float quality = 1.0f;
        
        if (response.OutputTokens < 20)
            quality -= 0.3f;
        
        if (response.Metrics.LatencyMs > 2000)
            quality -= 0.2f;
        
        return quality;
    }
}

// Usage
var example = new CascadingQualityExample(router, fast, quality);
var response = await example.GetBestResponseAsync("Explain AI");
```

---

## Error Handling

### Comprehensive Error Handling

```csharp
public class ErrorHandlingExample
{
    private readonly ISmartRouter _router;
    
    public async Task HandleErrorsAsync()
    {
        var request = new InferenceRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new()
            {
                new() { Role = "user", Content = "Hello" }
            }
        };
        
        var result = await _router.InferenceAsync(
            request,
            new BalancedStrategy());
        
        if (result.IsFailure)
        {
            var failure = result as Result<InferenceResponse>.Failure;
            
            switch (failure.Code)
            {
                case ErrorCode.ProviderRateLimited:
                    Console.WriteLine("Rate limited - waiting 60s");
                    await Task.Delay(60000);
                    break;
                
                case ErrorCode.ProviderUnauthorized:
                    Console.WriteLine("Check API key configuration");
                    break;
                
                case ErrorCode.ProviderQuotaExceeded:
                    Console.WriteLine("Monthly quota exceeded");
                    break;
                
                case ErrorCode.OperationTimeout:
                    Console.WriteLine("Request timed out");
                    break;
                
                case ErrorCode.NoProvidersAvailable:
                    Console.WriteLine("All providers are down");
                    break;
                
                default:
                    Console.WriteLine($"Error: {failure.Message}");
                    break;
            }
        }
    }
}

// Usage
var example = new ErrorHandlingExample(router);
await example.HandleErrorsAsync();
```

### Retry Logic

```csharp
public class RetryLogicExample
{
    private readonly ISmartRouter _router;
    
    public async Task<InferenceResponse> InferenceWithRetryAsync(
        InferenceRequest request,
        IRoutingStrategy strategy,
        int maxRetries = 3)
    {
        int attempts = 0;
        
        while (attempts < maxRetries)
        {
            var result = await _router.InferenceAsync(request, strategy);
            
            if (result.IsSuccess)
                return result.Value;
            
            var failure = result as Result<InferenceResponse>.Failure;
            attempts++;
            
            Console.WriteLine($"Attempt {attempts} failed: {failure.Code}");
            
            if (attempts < maxRetries)
            {
                // Exponential backoff
                var delayMs = (int)Math.Pow(2, attempts) * 1000;
                Console.WriteLine($"Retrying in {delayMs}ms...");
                await Task.Delay(delayMs);
            }
        }
        
        throw new Exception("All retry attempts failed");
    }
}

// Usage
var example = new RetryLogicExample(router);
var response = await example.InferenceWithRetryAsync(
    request,
    new BalancedStrategy());
```

---

## Monitoring Examples

### Health Monitoring

```csharp
public class HealthMonitoringExample
{
    private readonly IHealthMonitor _monitor;
    
    public async Task MonitorHealthAsync()
    {
        var health = await _monitor.GetAllHealthAsync();
        
        foreach (var (provider, status) in health)
        {
            Console.WriteLine($"{provider}:");
            Console.WriteLine($"  State: {status.State}");
            Console.WriteLine($"  Reason: {status.Reason}");
            Console.WriteLine($"  Last checked: {status.LastChecked}");
        }
    }
    
    public void SubscribeToHealthEvents()
    {
        _monitor.OnHealthChanged("OpenAI", async evt =>
        {
            Console.WriteLine($"Health changed: {evt.OldState} → {evt.NewState}");
            if (evt.NewState == HealthState.Unhealthy)
            {
                Console.WriteLine("Alert: Provider is down!");
            }
        });
    }
}

// Usage
var example = new HealthMonitoringExample(monitor);
await example.MonitorHealthAsync();
example.SubscribeToHealthEvents();
```

### Cost Tracking

```csharp
public class CostTrackingExample
{
    private readonly ISmartRouter _router;
    
    public async Task TrackCostsAsync(List<InferenceRequest> requests)
    {
        decimal totalCost = 0;
        var perProvider = new Dictionary<string, decimal>();
        
        foreach (var request in requests)
        {
            var result = await _router.InferenceAsync(
                request,
                new BalancedStrategy());
            
            if (result.IsSuccess)
            {
                var cost = result.Value.Metrics.EstimatedCost;
                var provider = result.Value.Metrics.ProviderId;
                
                totalCost += cost;
                
                if (!perProvider.ContainsKey(provider))
                    perProvider[provider] = 0;
                
                perProvider[provider] += cost;
            }
        }
        
        Console.WriteLine($"Total cost: ${totalCost:F4}");
        Console.WriteLine("\nPer provider:");
        foreach (var (provider, cost) in perProvider)
        {
            Console.WriteLine($"  {provider}: ${cost:F4}");
        }
    }
}

// Usage
var example = new CostTrackingExample(router);
await example.TrackCostsAsync(requests);
```

### Performance Metrics

```csharp
public class PerformanceMetricsExample
{
    private readonly ISmartRouter _router;
    
    public async Task CollectMetricsAsync(List<InferenceRequest> requests)
    {
        var latencies = new List<int>();
        var costs = new List<decimal>();
        
        foreach (var request in requests)
        {
            var result = await _router.InferenceAsync(
                request,
                new BalancedStrategy());
            
            if (result.IsSuccess)
            {
                latencies.Add(result.Value.Metrics.LatencyMs);
                costs.Add(result.Value.Metrics.EstimatedCost);
            }
        }
        
        Console.WriteLine("=== Latency Stats ===");
        Console.WriteLine($"Min: {latencies.Min()}ms");
        Console.WriteLine($"Max: {latencies.Max()}ms");
        Console.WriteLine($"Avg: {latencies.Average():F0}ms");
        Console.WriteLine($"P95: {Percentile(latencies, 95)}ms");
        Console.WriteLine($"P99: {Percentile(latencies, 99)}ms");
        
        Console.WriteLine("\n=== Cost Stats ===");
        Console.WriteLine($"Total: ${costs.Sum():F4}");
        Console.WriteLine($"Avg: ${costs.Average():F4}");
        Console.WriteLine($"Min: ${costs.Min():F4}");
        Console.WriteLine($"Max: ${costs.Max():F4}");
    }
    
    private int Percentile(List<int> sorted, int p)
    {
        sorted.Sort();
        var index = (int)Math.Ceiling(sorted.Count * p / 100.0) - 1;
        return sorted[Math.Max(0, index)];
    }
}

// Usage
var example = new PerformanceMetricsExample(router);
await example.CollectMetricsAsync(requests);
```

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
