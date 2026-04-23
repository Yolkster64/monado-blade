# Architecture Guide - Monado Blade v2.4.0

**System architecture, data flows, and component interactions**

*Read time: 20-25 minutes | Skill level: Advanced*

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Diagrams](#architecture-diagrams)
3. [Component Interactions](#component-interactions)
4. [Data Flow](#data-flow)
5. [Provider Lifecycle](#provider-lifecycle)
6. [Routing Decision Flow](#routing-decision-flow)
7. [Failover Logic](#failover-logic)
8. [Health Check Mechanism](#health-check-mechanism)

---

## System Overview

### Core Components

```
Monado Blade v2.4.0 System Architecture

┌─────────────────────────────────────────────────────────────┐
│                    Monado Core Platform                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌────────────────┐  ┌──────────────┐  ┌──────────────────┐ │
│  │ SmartRouter    │  │Health Monitor│  │FailoverCtrl      │ │
│  │ (Orchestration)│  │(Health Chk)  │  │ (Resilience)     │ │
│  └────────┬───────┘  └──────┬───────┘  └────────┬─────────┘ │
│           │                 │                    │           │
│  ┌────────▼─────────────────▼────────────────────▼─────────┐ │
│  │         ProviderRegistry & ProviderAdapter Interface     │ │
│  └────────┬──────────────────────────────────────────────┬──┘ │
│           │                                              │    │
│  ┌────────▼────────┬──────────────┬──────────────┬───────▼─┐ │
│  │   OpenAI        │    Groq      │   Claude     │ LMStudio│ │
│  │   Provider      │   Provider   │   Provider   │Provider │ │
│  └─────────────────┴──────────────┴──────────────┴────────┘ │
│           │                                              │    │
│  ┌────────▼────────────────────────────────────────────▼──┐ │
│  │      IServiceContext (Universal)                       │ │
│  │  ┌─────────────┬───────────┬─────────────┬──────────┐  │ │
│  │  │  Logger     │ Config    │ Metrics     │ Cache    │  │ │
│  │  └─────────────┴───────────┴─────────────┴──────────┘  │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **Provider Abstraction** - All providers implement same interface
2. **Intelligent Routing** - Choose best provider per request
3. **Resilience** - Automatic failover and retry logic
4. **Observability** - Health monitoring and metrics
5. **Extensibility** - Easy to add new providers

---

## Architecture Diagrams

### Request Flow Architecture

```
User Application
      │
      ▼
┌─────────────────────────────────────┐
│    SmartRouter.InferenceAsync()     │
│  - Select strategy                  │
│  - Filter providers                 │
│  - Rank by score                    │
│  - Pick winner                      │
└──────┬────────────────────────────┬─┘
       │                            │
       ▼                            ▼
┌──────────────────┐        ┌──────────────────┐
│  Primary Provider│        │ Backup Provider  │
│  (IProviderAdapt)│        │ (if first fails) │
└──────┬───────────┘        └──────┬───────────┘
       │                            │
       ▼                            ▼
  API Call                     API Call
       │                            │
       ├────────────┬───────────────┤
       │            │               │
       ▼            ▼               ▼
   Success      Error          Success
       │            │               │
       └────────────┼───────────────┘
                    │
                    ▼
          ┌────────────────────┐
          │ Response Metrics   │
          │ - Latency          │
          │ - Cost             │
          │ - Tokens           │
          │ - Provider ID      │
          └────────────────────┘
                    │
                    ▼
              Return to User
```

### Provider Resolution Flow

```
Request arrives
      │
      ▼
┌──────────────────────────────────────┐
│ Validate Request                     │
│ - Check model exists                 │
│ - Validate parameters                │
│ - Check rate limits                  │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│ Get Routing Strategy                 │
│ - Cost optimization?                 │
│ - Latency optimization?              │
│ - Quality optimization?              │
│ - Adaptive?                          │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│ Filter Candidates                    │
│ - Has model?                         │
│ - Supports features?                 │
│ - Currently healthy?                 │
│ - Has capacity?                      │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│ Rank Candidates                      │
│ - Score by strategy                  │
│ - Weight by health                   │
│ - Consider recent performance        │
│ - Sort by final score                │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│ Select Winner                        │
│ - Top-ranked provider                │
│ - Reserve 2nd for fallback           │
│ - Log selection                      │
└──────────┬───────────────────────────┘
           │
           ▼
       Execute Request
```

---

## Component Interactions

### SmartRouter Responsibilities

```csharp
// SmartRouter orchestrates all components
public class SmartRouter : ISmartRouter
{
    // Inject dependencies
    private readonly List<IProviderAdapter> _providers;
    private readonly IHealthMonitor _healthMonitor;
    private readonly IFailoverController _failover;
    private readonly IServiceContext _context;
    
    // Main entry point
    public async Task<Result<InferenceResponse>> InferenceAsync(
        InferenceRequest request,
        IRoutingStrategy strategy,
        CancellationToken ct)
    {
        // 1. Validate
        var validation = Validate(request);
        if (validation.IsFailure)
            return validation.AsFailure<InferenceResponse>();
        
        // 2. Get health status
        var health = await _healthMonitor.GetAllHealthAsync(ct);
        
        // 3. Filter providers
        var candidates = FilterProviders(request, health);
        if (!candidates.Any())
            return ErrorCode.NoProvidersAvailable.ToFailure<InferenceResponse>();
        
        // 4. Rank providers
        var ranked = RankProviders(candidates, strategy, request);
        
        // 5. Execute
        var primary = ranked.First();
        var result = await primary.InferenceAsync(request, ct);
        
        // 6. Handle failure with fallback
        if (result.IsFailure && ranked.Count > 1)
        {
            await _failover.TriggerFailoverAsync(primary.ProviderName, result.Error);
            var backup = ranked[1];
            result = await backup.InferenceAsync(request, ct);
        }
        
        return result;
    }
}
```

### Health Monitor Responsibilities

```csharp
public class HealthMonitor : IHealthMonitor
{
    // Monitors all providers
    private Dictionary<string, HealthStatus> _healthCache;
    
    public async Task<Dictionary<string, HealthStatus>> GetAllHealthAsync(CancellationToken ct)
    {
        var tasks = _providers
            .Select(p => GetProviderHealthAsync(p.ProviderName, ct))
            .ToList();
        
        var results = await Task.WhenAll(tasks);
        
        return results.ToDictionary(
            h => h.ComponentId,
            h => h);
    }
    
    public async Task<HealthStatus> GetProviderHealthAsync(
        string providerName,
        CancellationToken ct)
    {
        var provider = _registry.GetProvider(providerName);
        if (provider == null)
            return HealthStatus.Unhealthy(providerName, "Provider not found");
        
        try
        {
            return await provider.GetHealthAsync(ct);
        }
        catch (Exception ex)
        {
            return HealthStatus.Unhealthy(providerName, ex.Message, ex);
        }
    }
}
```

### Failover Controller Responsibilities

```csharp
public class FailoverController : IFailoverController
{
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private Dictionary<string, List<FailoverEvent>> _history;
    
    public async Task<Result> TriggerFailoverAsync(
        string fromProvider,
        string reason,
        CancellationToken ct)
    {
        _failureCount++;
        _logger.Error($"Failover triggered for {fromProvider}: {reason}");
        
        // Track failure
        RecordFailover(new FailoverEvent
        {
            FailedProvider = fromProvider,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        });
        
        // Check circuit breaker
        if (_failureCount >= _config.FailureThreshold)
        {
            _state = CircuitBreakerState.Open;
            _logger.Fatal($"Circuit breaker opened for {fromProvider}");
        }
        
        return Result.Success();
    }
}
```

---

## Data Flow

### Inference Request Data Flow

```
Input Request
    │
    ├─ Model: "gpt-3.5-turbo"
    ├─ Messages: [...]
    ├─ MaxTokens: 1000
    ├─ Temperature: 0.7
    └─ Stream: false
    
    ▼
SmartRouter.InferenceAsync()
    │
    ├─ Validate request structure
    ├─ Check model compatibility
    └─ Filter by capabilities
    
    ▼
Provider Selection
    │
    ├─ GetAllHealthAsync() → Health statuses
    ├─ FilterProviders() → Matching providers
    ├─ RankProviders() → Scored & sorted
    └─ SelectBest() → IProviderAdapter
    
    ▼
ProviderAdapter.InferenceAsync()
    │
    ├─ Transform request format
    │  └─ To provider's API format
    ├─ Execute HTTP request
    │  ├─ Add auth headers
    │  ├─ Set timeout
    │  └─ Send to provider
    └─ Parse response
       ├─ Extract content
       ├─ Count tokens
       └─ Calculate cost
    
    ▼
InferenceResponse
    │
    ├─ Content: "The answer is..."
    ├─ InputTokens: 15
    ├─ OutputTokens: 42
    └─ Metrics:
       ├─ LatencyMs: 250
       ├─ EstimatedCost: $0.0015
       ├─ RequestTime: 2024-04-15T...
       └─ ProviderId: "OpenAI"
    
    ▼
Return to Caller
```

### Metrics Data Flow

```
Request executed
      │
      ▼
┌─────────────────────────────────┐
│ Record in ProviderMetrics       │
│ - Latency                       │
│ - Cost                          │
│ - Token count                   │
│ - Provider ID                   │
└──────────┬──────────────────────┘
           │
           ▼
┌─────────────────────────────────┐
│ Store in MetricsCollector       │
│ - Update counters               │
│ - Update histograms             │
│ - Update gauges                 │
└──────────┬──────────────────────┘
           │
           ▼
┌─────────────────────────────────┐
│ Export to monitoring backends   │
│ - Prometheus                    │
│ - Application Insights          │
│ - Datadog                       │
└──────────┬──────────────────────┘
           │
           ▼
┌─────────────────────────────────┐
│ Available for queries           │
│ - Dashboard visualization       │
│ - Alerting rules                │
│ - Analytics                     │
└─────────────────────────────────┘
```

---

## Provider Lifecycle

### Complete Lifecycle Diagram

```
1. CREATION
   ↓
   new OpenAIProvider(context)
   
2. REGISTRATION
   ↓
   registry.RegisterAsync(provider)
   
3. INITIALIZATION
   ↓
   provider.InitializeAsync(context)
   ├─ Load config
   ├─ Authenticate
   ├─ Test connection
   └─ Ready
   
4. HEALTHY STATE
   ↓
   provider.InferenceAsync(request)
   ├─ Accept requests
   ├─ Execute inference
   ├─ Track metrics
   └─ Return response
   
5. HEALTH MONITORING
   ↓
   provider.GetHealthAsync()
   ├─ Run periodic checks
   ├─ Track performance
   ├─ Detect issues
   └─ Update status
   
6. DEGRADED STATE (if issues detected)
   ↓
   ├─ Log warnings
   ├─ Notify failover controller
   ├─ Reduce weight in routing
   └─ Attempt recovery
   
7. UNHEALTHY STATE (if critical failures)
   ↓
   ├─ Circuit breaker opens
   ├─ Failover to backup
   ├─ Alert operators
   └─ Stop accepting requests
   
8. RECOVERY
   ↓
   ├─ Health check passes
   ├─ Circuit breaker half-open
   ├─ Limited requests
   ├─ If successful → return to healthy
   └─ If failed → back to unhealthy
   
9. SHUTDOWN
   ↓
   provider.ShutdownAsync()
   ├─ Stop accepting requests
   ├─ Wait for in-flight requests
   ├─ Close connections
   ├─ Clean up resources
   └─ Done
```

---

## Routing Decision Flow

### Detailed Routing Algorithm

```
┌─────────────────────────────────────┐
│ Incoming InferenceRequest           │
└─────────┬───────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 1: Validation                  │
│ - Model specified?                  │
│ - Valid messages?                   │
│ - MaxTokens reasonable?             │
└─────────┬───────────────────────────┘
          │
          ├─ FAIL? → Return error
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 2: Get Current Health          │
│ - Call HealthMonitor.GetAllHealthAsync
│ - Get status of each provider       │
│ - Filter out Unhealthy ones         │
└─────────┬───────────────────────────┘
          │
          ├─ No healthy? → Return NoProvidersAvailable error
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 3: Capability Filtering        │
│ For each healthy provider:          │
│ - Has required model?               │
│ - Supports streaming? (if needed)   │
│ - Supports vision? (if needed)      │
│ - Max context length ok?            │
│ → Keep only matching                │
└─────────┬───────────────────────────┘
          │
          ├─ No matches? → Try degraded providers
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 4: Score by Strategy           │
│ For each candidate:                 │
│                                     │
│ If Cost strategy:                   │
│   score = 100 - (cost * 1000)       │
│                                     │
│ If Latency strategy:                │
│   score = 1000 / (latency + 1)      │
│                                     │
│ If Quality strategy:                │
│   score = paramCount / 1M + ...     │
│                                     │
│ If Balanced:                        │
│   score = (cost*0.2 + latency*0.3 + │
│            quality*0.5)             │
└─────────┬───────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 5: Health-Adjusted Scoring     │
│ Multiply score by health factor:    │
│ - Healthy: 1.0 (no penalty)         │
│ - Degraded: 0.75 (25% reduction)    │
│ - Unhealthy: 0.0 (excluded)         │
└─────────┬───────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 6: Performance Bonus           │
│ Add bonus for consistent performance:
│ - Recent avg < 100ms? +5 points     │
│ - 100% success rate? +3 points      │
│ - Trending better? +2 points        │
└─────────┬───────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 7: Sort & Select               │
│ - Sort by final score (highest first)
│ - Select top 1 as primary           │
│ - Reserve top 2-3 as failover       │
│ - Log selection with scores         │
└─────────┬───────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 8: Execute on Primary          │
│ - Call provider.InferenceAsync()    │
│ - Record latency                    │
│ - Track cost                        │
│ - Return response                   │
└─────────┬───────────────────────────┘
          │
          ├─ SUCCESS? → Return to caller
          │
          ├─ FAILURE? → Step 9
          │
          ▼
┌─────────────────────────────────────┐
│ STEP 9: Failover to Backup          │
│ - Trigger failover event            │
│ - Call next provider in reserve     │
│ - Retry request                     │
│ - Log failover                      │
└─────────┬───────────────────────────┘
          │
          └─→ Return response or error
```

---

## Failover Logic

### Circuit Breaker State Machine

```
CLOSED State (Normal)
    │
    ├─ All requests pass through
    ├─ Failures tracked
    ├─ If failures >= threshold:
    │  └─ → OPEN state
    └─ If all healthy:
       └─ Stay in CLOSED


OPEN State (Failing)
    │
    ├─ All requests BLOCKED
    ├─ Fast-fail to prevent cascading
    ├─ Wait for recovery timeout
    └─ After timeout:
       └─ → HALF_OPEN state


HALF_OPEN State (Testing)
    │
    ├─ Limited requests allowed
    ├─ Test if provider recovered
    ├─ If successful:
    │  ├─ Reset failure counter
    │  └─ → CLOSED state
    └─ If failed:
       ├─ Failure count++
       └─ → OPEN state


State Transitions Summary:
CLOSED ─(failures ≥ threshold)─→ OPEN
OPEN ─(recovery timeout)─→ HALF_OPEN
HALF_OPEN ─(success)─→ CLOSED
HALF_OPEN ─(failure)─→ OPEN
```

---

## Health Check Mechanism

### Health Check Flow

```
┌────────────────────────────────┐
│ Health Check Timer (every 30s) │
└────────────┬───────────────────┘
             │
             ▼
┌────────────────────────────────┐
│ For each registered provider:  │
│                                │
│ 1. Call provider.GetHealthAsync()
│                                │
│ 2. Parse response:             │
│    - State: Healthy/Degraded   │
│    - Reason: (if unhealthy)    │
│    - Latency of check          │
│                                │
│ 3. Update health cache         │
│                                │
│ 4. Check for state changes:    │
│    - Was healthy, now isn't?   │
│      → Fire event              │
│    - Was down, now healthy?    │
│      → Fire event              │
│                                │
│ 5. Update metrics:             │
│    - health_check_duration_ms  │
│    - provider_state            │
└────────────┬───────────────────┘
             │
             ▼
┌────────────────────────────────┐
│ Event System                   │
│                                │
│ OnHealthChanged() event fired  │
│ - HealthStatusChangeEvent      │
│ - Provider name                │
│ - Old state                    │
│ - New state                    │
│ - Timestamp                    │
│                                │
│ Subscribers notified:          │
│ - Failover controller          │
│ - Metrics collector            │
│ - Alerting system              │
│ - Logger                       │
└────────────┬───────────────────┘
             │
             ▼
┌────────────────────────────────┐
│ Decision Logic                 │
│                                │
│ If Unhealthy:                  │
│ - Remove from rotation         │
│ - Notify failover controller   │
│ - Fire CRITICAL alert          │
│                                │
│ If Degraded:                   │
│ - Reduce weight in routing     │
│ - Monitor closely              │
│ - Fire WARNING alert           │
│                                │
│ If Recovered:                  │
│ - Re-enable in rotation        │
│ - Reset failure count          │
│ - Fire INFO notification       │
└────────────────────────────────┘
```

---

**Last updated:** April 2026  
**Version:** 2.4.0  
**Status:** Production-Ready
