# MONADO BLADE Week 6 - Quick Reference

## Components Overview

| Component | Interface | Service | Purpose |
|-----------|-----------|---------|---------|
| Cost Tracker | `ICostTracker` | `CostTrackerService` | Record & analyze LLM costs |
| Budget Manager | `IBudgetManager` | `BudgetManagerService` | Enforce spending limits |
| Optimization | `IOptimizationRecommendationEngine` | `OptimizationEngine` | Suggest cost-saving changes |
| Cleanup | `ICleanupAutomationFramework` | `CleanupFramework` | Automated maintenance tasks |
| Scheduler | `IMaintenanceScheduler` | `MaintenanceScheduler` | Schedule & execute tasks |
| Monitor | `IResourceMonitor` | `ResourceMonitor` | Watch CPU/memory/disk/network |
| Dashboard | `OptimizationDashboard` | `OptimizationDashboard` | Real-time metrics UI |

## Quick Start

### 1. Register Services
```csharp
services.AddScoped<ICostTracker, CostTrackerService>();
services.AddScoped<IBudgetManager, BudgetManagerService>();
services.AddScoped<IOptimizationRecommendationEngine, OptimizationRecommendationEngine>();
services.AddScoped<ICleanupAutomationFramework, CleanupAutomationFramework>();
services.AddScoped<IResourceMonitor, ResourceMonitor>();
services.AddScoped<IMaintenanceScheduler, MaintenanceScheduler>();
```

### 2. Record Costs
```csharp
await costTracker.RecordCostAsync(new CostEvent
{
    Provider = "Claude",
    Model = "claude-3-opus",
    UserId = "user@company.com",
    ProjectId = "ProjectA",
    Amount = 2.50m,
    TokensUsed = 10000
});
```

### 3. Set Budgets
```csharp
await budgetManager.SetMonthlyBudgetAsync(10000m, BudgetLimitType.Soft);
await budgetManager.SetProviderBudgetAsync("GPT-4", 5000m, BudgetLimitType.Hard);
```

### 4. Check Before Spending
```csharp
var canRequest = await budgetManager.CanMakeRequestAsync("GPT-4", 100m, "user1");
if (!canRequest.Allowed)
    throw new BudgetExceededException(canRequest.Reason);
```

### 5. Get Dashboard
```csharp
var dashboard = new OptimizationDashboard(tracker, budget, engine, monitor, scheduler);
var data = await dashboard.GetDashboardDataAsync();
```

## Cost Saving Strategies

| Strategy | Savings | Effort | Auto? |
|----------|---------|--------|-------|
| Provider switch (GPT-4→Claude) | 30% | Low | ✅ |
| Enable caching | 20% | Medium | ✅ |
| Use local Hermes | 50-100% | High | ❌ |
| Batch requests | 10% | Medium | ❌ |

## Maintenance Schedule

| Task | Frequency | Duration | Window |
|------|-----------|----------|--------|
| Cache cleanup | Daily | 15 min | 3 AM |
| Service health | Daily | 5 min | 3:30 AM |
| Log compression | Weekly | 30 min | Sun 2 AM |
| DB index rebuild | Weekly | 1 hour | Sun 2 AM |
| Full defrag | Monthly | 2 hours | 1st Mon 2 AM |

## Thresholds & Alerts

| Metric | Threshold | Alert | Action |
|--------|-----------|-------|--------|
| Memory | >80% | Warning | Auto-GC |
| CPU | >85% | Warning | Throttle |
| Disk | >90% | Critical | Cleanup |
| Budget | >90% | Warning | Notification |
| Budget | 100% | Critical | Block (hard) |

## File Locations

```
Week6_CostOptimization/
├── Interfaces/           (6 files, ~25 KB total)
├── Services/            (6 files, ~60 KB total)
├── Dashboard/           (1 file, ~6 KB)
├── MonadoBlade.Week6.csproj
└── week6-config.json

Tests:
└── tests/Unit/Week6_OptimizationTests.cs (50+ tests)

Documentation:
├── docs/WEEK6_DELIVERY_SUMMARY.md (18 KB)
├── docs/WEEK6_ADMIN_PLAYBOOK.md (10 KB)
└── docs/WEEK6_QUICK_REFERENCE.md (this file)
```

## Cost Calculation Formula

```
Total Cost = Sum of (TokensUsed × CostPerToken)

Monthly Projection:
  DailyAverage = CurrentCost / DaysElapsed
  ProjectedTotal = DailyAverage × DaysInMonth
  
Savings Potential:
  Provider Switch = CurrentCost × (1 - NewProviderCost/OldProviderCost)
  Caching = HitRateLoss × TotalCost × (1 - CacheHitRate)
```

## Common Queries

### Top-Cost Operations
```csharp
var operations = await engine.IdentifyHighCostOperationsAsync();
var topOp = operations.First();
// topOp.Model, topOp.TotalCost, topOp.Recommendation
```

### Budget Status
```csharp
var status = await budgetManager.GetBudgetStatusAsync();
// RemainingMonth, RemainingToday, BurnRate, DaysUntilExceeded
```

### Optimization Opportunities
```csharp
var recs = await engine.AnalyzeAndRecommendAsync();
var totalSavings = recs.Sum(r => r.EstimatedMonthlySavings);
// Apply auto-compatible recommendations
foreach (var rec in recs.Where(r => r.CanBeAutoApplied))
    await engine.ApplyOptimizationAsync(rec);
```

### Storage Analysis
```csharp
var trend = await framework.AnalyzeStorageUsageTrendAsync();
// DaysUntilFull: 50, GrowthRate: 5 GB/day
if (trend.DaysUntilFull < 30)
    Console.WriteLine("Expand storage immediately!");
```

### Resource Forecast
```csharp
var forecast = await monitor.ForecastCapacityAsync();
// PredictedDiskFullDate: 2026-06-12
// PredictedMemoryFullDate: 2027-04-15
// RecommendedActions: [...]
```

## Error Handling

```csharp
try
{
    var result = await budgetManager.CanMakeRequestAsync("Claude", 100m, "user1");
    if (!result.Allowed)
        Console.WriteLine($"Blocked: {result.Reason}");
}
catch (Exception ex)
{
    Console.WriteLine($"Budget check failed: {ex.Message}");
}
```

## Testing

```bash
# Run all Week 6 tests
dotnet test ./tests/Unit/Week6_OptimizationTests.cs

# Run specific test
dotnet test ./tests/Unit/Week6_OptimizationTests.cs -k "CacheCleanup"

# Verbose output
dotnet test ./tests/Unit/Week6_OptimizationTests.cs -v detailed
```

## Configuration Keys

```json
{
  "costTracking": { "trackingInterval": "immediate" },
  "budgetManagement": { 
    "monthlyBudget": 10000.0,
    "monthlyLimitType": "Soft",
    "alertThresholds": { "warning": 90, "critical": 100 }
  },
  "optimization": { "autoOptimizationEnabled": true },
  "cleanup": { "automaticCleanup": true },
  "monitoring": { "autoRemediation": true }
}
```

## Support

- 📖 Full guide: `WEEK6_ADMIN_PLAYBOOK.md`
- 📋 Test examples: `Week6_OptimizationTests.cs`
- ⚙️ Configuration: `week6-config.json`
- 📊 Delivery details: `WEEK6_DELIVERY_SUMMARY.md`
