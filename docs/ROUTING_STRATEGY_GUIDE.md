# Routing Strategy Guide - Monado Blade v2.4.0

**Complete guide to intelligent provider selection, routing strategies, and optimization**

*Read time: 25-35 minutes | Skill level: Intermediate-Advanced*

## Table of Contents

1. [Overview](#overview)
2. [Routing Algorithm](#routing-algorithm)
3. [Strategy Types](#strategy-types)
4. [Decision Matrix](#decision-matrix)
5. [Cost Optimization](#cost-optimization)
6. [Latency Optimization](#latency-optimization)
7. [Quality Optimization](#quality-optimization)
8. [Advanced Strategies](#advanced-strategies)
9. [Performance Tuning](#performance-tuning)

---

## Overview

### What is Smart Routing?

Smart Routing automatically selects the best provider for each request based on:
- **Cost** - Minimize API spend
- **Latency** - Minimize response time
- **Quality** - Maximize response quality
- **Availability** - Avoid failed requests
- **Capabilities** - Match features required

### Why Smart Routing?

Without routing, you'd need to:
```csharp
// Manual provider selection - tedious and inflexible
if (needsCheapest)
    provider = new CheapProvider();
else if (needsFastest)
    provider = new FastProvider();
else if (needsQuality)
    provider = new PremiumProvider();
else
    provider = new DefaultProvider();
```

With Smart Routing:
```csharp
// Automatic provider selection based on strategy
var response = await smartRouter.InferenceAsync(request, routingStrategy);
// Router picks the best provider automatically
```

---

## Routing Algorithm

### The Decision Tree

```
┌─────────────────────────────────┐
│      Incoming Request           │
├─────────────────────────────────┤
│ - Model type                    │
│ - Max latency                   │
│ - Budget                        │
│ - Preferred providers           │
└────────────┬────────────────────┘
             │
             ▼
    ┌────────────────────┐
    │ Filter Providers   │ - Supports model?
    │ (Capabilities)     │ - Supports features?
    │                    │ - Currently healthy?
    └────────┬───────────┘
             │
             ▼
    ┌────────────────────────────┐
    │ Rank Providers             │ Based on strategy:
    │ (Scoring)                  │ - Cost
    │                            │ - Latency
    │                            │ - Quality
    │                            │ - Health
    └────────┬───────────────────┘
             │
             ▼
    ┌────────────────────────────┐
    │ Pick Best Provider         │
    │ (Weighted Score)           │
    └────────┬───────────────────┘
             │
             ▼
    ┌────────────────────────────┐
    │ Execute Request            │
    │ Track Metrics              │
    └────────────────────────────┘
```

### Scoring Function

Each provider receives a score:

```
Score = (Capability_Match × 100) +
        (Health_Factor × 50) +
        (Strategy_Factor × 50) +
        (Performance_Bonus × 20)

Where:
- Capability_Match = 0-100 (100 = perfect match)
- Health_Factor = 0-50 (50 = fully healthy)
- Strategy_Factor = 0-50 (depends on selected strategy)
- Performance_Bonus = 0-20 (for consistent good performance)
```

---

## Strategy Types

### 1. Cost Optimization Strategy

**Use Case:** Budget-conscious applications, batch processing

**Goal:** Minimize total API spend

```csharp
public class CostOptimizationStrategy : IRoutingStrategy
{
    public string Name => "CostOptimization";
    public int Priority => 100;  // Highest priority to cost
    
    public decimal CalculateScore(ProviderCandidate provider, Context context)
    {
        var costPerRequest = provider.Capabilities.CostPerInputToken * 
                           context.EstimatedInputTokens +
                           provider.Capabilities.CostPerOutputToken * 
                           context.EstimatedOutputTokens;
        
        // Lower cost = higher score
        return 100m - (costPerRequest * 1000);  // Normalize
    }
}
```

**Scoring Example:**
```
Provider          Cost/Req    Score   Selected?
─────────────────────────────────────────────
Groq              $0.0001     99      ✓ Best
LLaMA2 (local)    $0.0000     98      Alternative
GPT-3.5           $0.0015     75      Too expensive
GPT-4             $0.0300     10      Way too expensive
```

**Example Configuration:**
```json
{
  "strategy": "cost",
  "maxBudget": 100.00,  // per day
  "costWeighting": 0.9,
  "qualityWeighting": 0.1,
  "latencyWeighting": 0.0,
  "preferredProviders": ["Groq", "OpenAI-Turbo"],
  "fallbackProviders": ["LMStudio"]
}
```

**When to Use:**
- ✅ Batch processing (content generation, summarization)
- ✅ Non-real-time applications
- ✅ Budget-limited projects
- ✅ Volume-based usage
- ❌ Real-time chat applications (latency matters)
- ❌ High-quality requirements (quality matters)

---

### 2. Latency Optimization Strategy

**Use Case:** Real-time applications, conversational interfaces

**Goal:** Minimize response time

```csharp
public class LatencyOptimizationStrategy : IRoutingStrategy
{
    public string Name => "LatencyOptimization";
    public int Priority => 100;
    
    public decimal CalculateScore(ProviderCandidate provider, Context context)
    {
        // Weight recent latency more heavily
        var recentLatency = provider.Metrics.Recent50thLatency;
        var p99Latency = provider.Metrics.P99Latency;
        
        // Penalize providers with high variance
        var variance = p99Latency - recentLatency;
        
        // Lower latency = higher score
        return 1000m / (recentLatency + variance * 0.5);
    }
}
```

**Scoring Example:**
```
Provider          P50 Latency  P99 Latency  Score   Selected?
────────────────────────────────────────────────────────────
Groq              80ms         150ms        98      ✓ Best
Local LLaMA       150ms        800ms        60      Inconsistent
OpenAI            250ms        500ms        40      Acceptable
Replicate         2000ms       4000ms       5       Too slow
```

**Example Configuration:**
```json
{
  "strategy": "latency",
  "maxLatencyMs": 1000,
  "targetLatencyMs": 500,
  "costWeighting": 0.2,
  "qualityWeighting": 0.2,
  "latencyWeighting": 0.6,
  "trackMetrics": true,
  "adaptiveTimeout": true
}
```

**When to Use:**
- ✅ Real-time chat applications
- ✅ Streaming responses
- ✅ User-facing features
- ✅ Mobile applications
- ❌ Batch processing (cost matters more)
- ❌ Complex reasoning tasks (quality matters more)

---

### 3. Quality Optimization Strategy

**Use Case:** High-stakes applications, reasoning, analysis

**Goal:** Maximize response quality

```csharp
public class QualityOptimizationStrategy : IRoutingStrategy
{
    public string Name => "QualityOptimization";
    public int Priority => 100;
    
    public decimal CalculateScore(ProviderCandidate provider, Context context)
    {
        // Prefer more capable models
        var contextLength = provider.Capabilities.MaxContextLength;
        var parameterCount = provider.Model.ParameterCount;
        var toolSupport = provider.Capabilities.SupportsTools ? 50 : 0;
        
        return parameterCount / 1_000_000 +  // Larger = better
               contextLength / 100_000 +      // Longer = better
               toolSupport;                   // Tools supported
    }
}
```

**Scoring Example:**
```
Provider          Model Size   Context   Tools?  Score   Selected?
─────────────────────────────────────────────────────────────────
GPT-4             1.76T        128K      Yes     95      ✓ Best
Claude-3          100B         200K      Yes     98      Actually best
Mixtral           56B          32K       No      65      Good value
GPT-3.5           175B         16K       No      45      Adequate
Groq              8x7B         32K       No      35      Too limited
```

**Example Configuration:**
```json
{
  "strategy": "quality",
  "minModelSize": "50B",
  "minContextLength": 32000,
  "requiredCapabilities": ["tools", "vision"],
  "costWeighting": 0.1,
  "qualityWeighting": 0.8,
  "latencyWeighting": 0.1,
  "preferredProviders": ["GPT-4", "Claude-3"],
  "rejectedProviders": ["Groq"]
}
```

**When to Use:**
- ✅ Complex reasoning (math, logic)
- ✅ Content analysis
- ✅ Code generation
- ✅ Long-form writing
- ✅ Creative tasks
- ❌ Simple classification
- ❌ Speed-critical tasks

---

### 4. Balanced Strategy

**Use Case:** General-purpose applications

**Goal:** Balance all factors

```csharp
public class BalancedStrategy : IRoutingStrategy
{
    public string Name => "Balanced";
    public int Priority => 100;
    
    public decimal CalculateScore(ProviderCandidate provider, Context context)
    {
        var costScore = 100m - (provider.CostPer1k * 100);
        var latencyScore = 1000m / (provider.AverageLatencyMs + 1);
        var qualityScore = provider.Model.ParameterCount / 1_000_000;
        var healthScore = provider.HealthStatus == HealthStatus.Healthy ? 100 : 20;
        
        // Weighted average
        return (costScore * 0.2) +
               (latencyScore * 0.3) +
               (qualityScore * 0.3) +
               (healthScore * 0.2);
    }
}
```

**Scoring Example:**
```
Provider          Cost    Latency   Quality   Health   Score   Selected?
─────────────────────────────────────────────────────────────────────────
Groq              99      98        35        100      78      ✓ Good choice
GPT-3.5           75      80        65        100      76      Close second
Claude            60      70        95        100      73      Too expensive
LMStudio          95      40        45        80       62      Inconsistent
```

---

## Decision Matrix

### Choosing the Right Strategy

| Scenario | Strategy | Why |
|----------|----------|-----|
| Chatbot | Latency | Users expect fast responses |
| Content generation batch | Cost | Volume justifies slower processing |
| Legal document analysis | Quality | Accuracy critical |
| Internal API | Balanced | No specific bottleneck |
| Real-time translation | Latency | User-facing, sub-second required |
| Code analysis | Quality | Correctness matters |
| Summarization | Cost | Many documents, budget limited |
| Customer support | Balanced | Balance response time and quality |

### Request-Level Decision Matrix

```csharp
// Automatic strategy selection based on request metadata
public class AdaptiveRoutingStrategy : IRoutingStrategy
{
    public decimal CalculateScore(ProviderCandidate provider, Context context)
    {
        return context.RequestMetadata switch
        {
            // Real-time user request → Latency
            { IsUserFacing: true, MaxLatencyMs: < 1000 } => LatencyScore(provider),
            
            // Batch processing → Cost
            { IsBatch: true, IsUrgent: false } => CostScore(provider),
            
            // Complex reasoning → Quality
            { RequiresReasoning: true } => QualityScore(provider),
            
            // Default → Balanced
            _ => BalancedScore(provider)
        };
    }
}
```

---

## Cost Optimization

### Strategy: Tiered Providers

Use different providers for different tiers:

```json
{
  "tiers": [
    {
      "name": "budget",
      "providers": ["Groq", "LMStudio"],
      "maxCostPer1K": 0.001,
      "useFor": ["summarization", "classification"]
    },
    {
      "name": "standard",
      "providers": ["OpenAI-GPT3.5", "Replicate"],
      "maxCostPer1K": 0.01,
      "useFor": ["chat", "general tasks"]
    },
    {
      "name": "premium",
      "providers": ["OpenAI-GPT4", "Claude-3"],
      "maxCostPer1K": 1.0,
      "useFor": ["complex reasoning", "quality required"]
    }
  ]
}
```

**Example Implementation:**

```csharp
public async Task<InferenceResponse> OptimizedInferenceAsync(
    InferenceRequest request,
    CancellationToken ct)
{
    // Route based on request complexity
    var complexity = EstimateComplexity(request);
    
    var strategy = complexity switch
    {
        < 0.3 => CostOptimizationStrategy(),    // Simple - use cheap provider
        < 0.7 => BalancedStrategy(),             // Medium - balanced
        _ => QualityOptimizationStrategy()       // Complex - use best
    };
    
    return await _router.InferenceAsync(request, strategy, ct);
}

private float EstimateComplexity(InferenceRequest request)
{
    float complexity = 0;
    
    // Complex tasks have longer prompts
    complexity += Math.Min(request.Messages.Length / 10f, 1.0f);
    
    // Tools increase complexity
    if (request.Tools?.Any() ?? false)
        complexity += 0.3f;
    
    // Multi-turn conversation is more complex
    if (request.Messages.Length > 5)
        complexity += 0.2f;
    
    return Math.Min(complexity, 1.0f);
}
```

### Cost Tracking & Budgeting

```csharp
public class CostTracker
{
    private decimal _dailySpend = 0;
    private readonly decimal _dailyBudget;
    
    public async Task<Result<InferenceResponse>> TrackAndExecuteAsync(
        InferenceRequest request,
        IRoutingStrategy strategy,
        CancellationToken ct)
    {
        // Get cost estimate from routing
        var provider = _router.SelectProvider(request, strategy);
        var estimatedCost = EstimateCost(request, provider);
        
        // Check budget
        if (_dailySpend + estimatedCost > _dailyBudget)
        {
            _logger.Warning($"Daily budget exceeded: {_dailySpend + estimatedCost:F4}");
            
            // Fall back to cheapest provider
            return await _router.InferenceAsync(
                request,
                CostOptimizationStrategy(),
                ct);
        }
        
        // Execute and track actual cost
        var response = await provider.InferenceAsync(request, ct);
        if (response.IsSuccess)
        {
            _dailySpend += response.Value.Metrics.EstimatedCost;
            _logger.Information(
                $"Cost tracked: {response.Value.Metrics.EstimatedCost:F4} " +
                $"(total today: {_dailySpend:F4})");
        }
        
        return response;
    }
}
```

---

## Latency Optimization

### Strategy: Parallel Invocation

Invoke multiple providers in parallel, use the first response:

```csharp
public class ParallelRoutingStrategy : IRoutingStrategy
{
    public async Task<InferenceResponse> InferenceAsync(
        InferenceRequest request,
        int parallelCount = 2,
        CancellationToken ct = default)
    {
        // Select top N providers
        var topProviders = _providers
            .OrderByDescending(p => CalculateScore(p, request))
            .Take(parallelCount)
            .ToList();
        
        _logger.Information($"Invoking {parallelCount} providers in parallel");
        
        // Invoke all in parallel
        var tasks = topProviders
            .Select(p => InvokeWithTimeoutAsync(p, request, ct))
            .ToList();
        
        // Wait for first to complete
        var completedTask = await Task.WhenAny(tasks);
        var response = completedTask.Result;
        
        if (response.IsSuccess)
        {
            _logger.Information(
                $"Got response from {response.Value.Metrics.ProviderId} " +
                $"in {response.Value.Metrics.LatencyMs}ms");
            
            // Cancel remaining tasks
            foreach (var task in tasks.Where(t => !t.IsCompleted))
            {
                try { await task; } catch { }
            }
        }
        
        return response;
    }
    
    private async Task<InferenceResponse> InvokeWithTimeoutAsync(
        IProviderAdapter provider,
        InferenceRequest request,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource
            .CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2));
        
        try
        {
            return await provider.InferenceAsync(request, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return InferenceResponse.Timeout();
        }
    }
}
```

**When to Use:**
- Real-time applications
- Max latency is strict requirement
- Can tolerate duplicate API calls
- Cost is secondary concern

### Strategy: Request Decomposition

Break request into smaller parts for faster processing:

```csharp
public class DecompositionStrategy
{
    public async Task<InferenceResponse> InferenceAsync(
        InferenceRequest request,
        CancellationToken ct)
    {
        if (request.Messages.Length <= 3)
        {
            // Simple request - use fast provider
            return await _fastProvider.InferenceAsync(request, ct);
        }
        
        // Complex request - decompose
        var summaryMessages = request.Messages
            .Take(request.Messages.Length - 3)
            .ToList();
        
        // Summarize context (faster)
        var summaryRequest = new InferenceRequest
        {
            Messages = summaryMessages,
            MaxTokens = 200
        };
        
        var summary = await _fastProvider.InferenceAsync(
            summaryRequest, 
            ct);
        
        // Now use quality provider with shortened context
        var finalMessages = new List<Message>
        {
            new() { Role = "system", Content = $"Summary: {summary.Content}" },
            request.Messages.Last()
        };
        
        var finalRequest = new InferenceRequest
        {
            Messages = finalMessages,
            MaxTokens = request.MaxTokens
        };
        
        return await _qualityProvider.InferenceAsync(finalRequest, ct);
    }
}
```

---

## Quality Optimization

### Strategy: Consensus Routing

Use multiple providers and combine results:

```csharp
public class ConsensusRoutingStrategy
{
    public async Task<ConsensusResponse> ConsensusInferenceAsync(
        InferenceRequest request,
        int providerCount = 3,
        CancellationToken ct = default)
    {
        // Select top quality providers
        var providers = _providers
            .OrderByDescending(p => QualityScore(p))
            .Take(providerCount)
            .ToList();
        
        _logger.Information($"Running consensus with {providerCount} providers");
        
        // Invoke all
        var tasks = providers
            .Select(p => p.InferenceAsync(request, ct))
            .ToList();
        
        var responses = await Task.WhenAll(tasks);
        
        // Analyze responses for consensus
        var responses_List = responses
            .Where(r => r.IsSuccess)
            .Select(r => r.Value.Content)
            .ToList();
        
        // Find most common response
        var consensus = responses_List
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
        
        var confidence = (float)responses_List
            .Count(r => r == consensus) / responses_List.Count;
        
        return new ConsensusResponse
        {
            Content = consensus,
            Confidence = confidence,
            ProviderCount = providerCount,
            AllResponses = responses_List,
            IsConsensus = confidence >= 0.66f
        };
    }
}
```

**When to Use:**
- Critical decisions (legal, medical)
- Verification needed
- High accuracy required
- Can tolerate increased latency and cost

### Strategy: Cascading Quality

Start with fast provider, escalate if needed:

```csharp
public class CascadingQualityStrategy
{
    public async Task<InferenceResponse> InferenceAsync(
        InferenceRequest request,
        CancellationToken ct)
    {
        // Start with fast provider
        var fastResponse = await _fastProvider.InferenceAsync(request, ct);
        if (fastResponse.IsSuccess)
        {
            // Check if response is good enough
            var quality = EvaluateQuality(fastResponse.Value);
            if (quality > 0.8f)
            {
                _logger.Information("Fast provider response acceptable");
                return fastResponse;
            }
        }
        
        _logger.Information("Escalating to quality provider");
        
        // Escalate to quality provider
        var qualityResponse = await _qualityProvider
            .InferenceAsync(request, ct);
        
        return qualityResponse;
    }
    
    private float EvaluateQuality(InferenceResponse response)
    {
        float quality = 1.0f;
        
        // Reduce score if too short
        if (response.OutputTokens < 20)
            quality -= 0.3f;
        
        // Reduce if took too long
        if (response.Metrics.LatencyMs > 2000)
            quality -= 0.2f;
        
        return quality;
    }
}
```

---

## Advanced Strategies

### Geographic Routing

Route based on latency from user location:

```csharp
public class GeographicRoutingStrategy
{
    public IProviderAdapter SelectProvider(
        InferenceRequest request,
        string userLocation)
    {
        var userLatLon = _geoService.GetLocation(userLocation);
        
        var providers = _providers
            .Select(p => new
            {
                Provider = p,
                Distance = CalculateDistance(userLatLon, p.Location),
                BaseLatency = p.AverageLatencyMs
            })
            .OrderBy(x => x.Distance + x.BaseLatency)
            .First();
        
        return providers.Provider;
    }
}
```

### Dynamic Provider Adjustment

Adjust routing based on real-time performance:

```csharp
public class DynamicRoutingStrategy
{
    private Dictionary<string, ProviderScore> _scoreHistory;
    
    public void UpdateProviderScores(InferenceResponse response)
    {
        var providerId = response.Metrics.ProviderId;
        
        if (!_scoreHistory.ContainsKey(providerId))
            _scoreHistory[providerId] = new ProviderScore();
        
        var score = _scoreHistory[providerId];
        
        // Update running metrics
        score.TotalRequests++;
        score.AverageLatency = 
            (score.AverageLatency * (score.TotalRequests - 1) + 
             response.Metrics.LatencyMs) / score.TotalRequests;
        
        score.TotalCost += response.Metrics.EstimatedCost;
        
        // Adjust weight if performance degrades
        if (score.AverageLatency > 1000)
            score.Weight *= 0.9f;  // Reduce by 10%
        
        if (score.AverageLatency < 100)
            score.Weight *= 1.05f;  // Increase by 5%
    }
}
```

---

## Performance Tuning

### Caching Responses

```csharp
// Cache identical requests
var cacheKey = $"{request.Model}:{Hash(request.Messages)}";

if (_cache.TryGetValue(cacheKey, out var cached))
{
    _logger.Information("Cache hit - returning cached response");
    return cached;
}

var response = await _router.InferenceAsync(request);
_cache.Set(cacheKey, response, TimeSpan.FromHours(1));
```

### Provider Preheating

```csharp
// Keep connections warm
public async Task PreheatProvidersAsync(CancellationToken ct)
{
    var warmupRequest = new InferenceRequest
    {
        Messages = new() { new() { Role = "user", Content = "Hi" } },
        MaxTokens = 10
    };
    
    foreach (var provider in _providers)
    {
        _ = await provider.InferenceAsync(warmupRequest, ct);
    }
    
    _logger.Information("Providers preheated");
}
```

### Metrics Dashboard

```csharp
public class RoutingMetrics
{
    public decimal AverageCost { get; set; }
    public int AverageLatency { get; set; }
    public float SuccessRate { get; set; }
    public Dictionary<string, ProviderMetrics> PerProvider { get; set; }
    
    public void LogMetrics(InferenceResponse response)
    {
        // Track provider performance
        var key = response.Metrics.ProviderId;
        if (!PerProvider.ContainsKey(key))
            PerProvider[key] = new ProviderMetrics();
        
        var metrics = PerProvider[key];
        metrics.RequestCount++;
        metrics.TotalCost += response.Metrics.EstimatedCost;
        metrics.TotalLatency += response.Metrics.LatencyMs;
        
        _logger.Information(
            $"Metrics: Avg Cost=${AverageCost:F4}, " +
            $"Avg Latency={AverageLatency}ms, " +
            $"Success={SuccessRate:P}");
    }
}
```

---

## Configuration Examples

### Development Configuration

```json
{
  "routing": {
    "strategy": "balanced",
    "providers": [
      {
        "name": "LMStudio",
        "weight": 1.0,
        "enabled": true
      }
    ],
    "metrics": {
      "trackLatency": true,
      "trackCost": false,
      "sampleRate": 1.0
    }
  }
}
```

### Production Configuration

```json
{
  "routing": {
    "strategy": "adaptive",
    "costWeighting": 0.2,
    "latencyWeighting": 0.3,
    "qualityWeighting": 0.5,
    "providers": [
      {
        "name": "Groq",
        "weight": 0.3,
        "maxConcurrent": 100,
        "enabled": true,
        "failoverPriority": 3
      },
      {
        "name": "OpenAI-GPT4",
        "weight": 0.5,
        "maxConcurrent": 50,
        "enabled": true,
        "failoverPriority": 1
      },
      {
        "name": "LMStudio",
        "weight": 0.2,
        "maxConcurrent": 10,
        "enabled": true,
        "failoverPriority": 2
      }
    ],
    "metrics": {
      "trackLatency": true,
      "trackCost": true,
      "trackQuality": true,
      "sampleRate": 0.1,
      "exportInterval": 60
    },
    "failover": {
      "enabled": true,
      "maxRetries": 3,
      "backoffMultiplier": 2.0
    }
  }
}
```

---

## Next Steps

1. **Assess your needs** - Cost? Speed? Quality?
2. **Choose a strategy** - Use decision matrix
3. **Configure providers** - Set up for your strategy
4. **Monitor metrics** - Track what matters
5. **Adjust weights** - Tune based on real data
6. **Iterate** - Optimize over time

**Ready to route intelligently? Start with your primary constraint and adjust from there!**

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
