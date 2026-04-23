# MONADO BLADE Week 6: Cost Optimization & SysOps Cleanup
## Administrator Playbook

### Table of Contents
1. [Setup Instructions](#setup)
2. [Configuration](#configuration)
3. [Maintenance Schedules](#maintenance)
4. [Cost Management](#costs)
5. [Troubleshooting](#troubleshooting)
6. [Best Practices](#best-practices)

---

## Setup Instructions

### 1. Project Registration
```csharp
// In Startup.cs or appsettings
services.AddScoped<ICostTracker, CostTrackerService>();
services.AddScoped<IBudgetManager, BudgetManagerService>();
services.AddScoped<IOptimizationRecommendationEngine, OptimizationRecommendationEngine>();
services.AddScoped<ICleanupAutomationFramework, CleanupAutomationFramework>();
services.AddScoped<IResourceMonitor, ResourceMonitor>();
services.AddScoped<IMaintenanceScheduler, MaintenanceScheduler>();
```

### 2. Initial Configuration
1. Copy `week6-config.json` to your config directory
2. Customize thresholds based on your infrastructure
3. Set budget limits appropriate for your organization
4. Define maintenance windows for your timezone

### 3. Start Services
```csharp
var costTracker = serviceProvider.GetRequiredService<ICostTracker>();
var budgetManager = serviceProvider.GetRequiredService<IBudgetManager>();
var scheduler = serviceProvider.GetRequiredService<IMaintenanceScheduler>();

// Initialize budgets
await budgetManager.SetMonthlyBudgetAsync(10000m, BudgetLimitType.Soft);
await budgetManager.SetDailyBudgetAsync(500m, BudgetLimitType.Soft);

// Schedule maintenance
await scheduler.DefineMaintenanceWindowAsync(new MaintenanceWindow
{
    StartTime = TimeSpan.FromHours(3),
    Duration = TimeSpan.FromMinutes(15),
    ApplicableDays = new List<DayOfWeek> { /* daily */ }
});
```

---

## Configuration

### Cost Tracking Configuration

#### Provider Setup
```json
"providers": [
  {
    "name": "Claude",
    "costPerToken": 0.003,
    "models": ["claude-3-opus", "claude-3-sonnet"]
  },
  {
    "name": "GPT-4",
    "costPerToken": 0.015,
    "models": ["gpt-4", "gpt-4-turbo"]
  }
]
```

#### Recording Costs
```csharp
await costTracker.RecordCostAsync(new CostEvent
{
    Provider = "Claude",
    Model = "claude-3-opus",
    UserId = "alice@company.com",
    ProjectId = "ProjectA",
    Amount = 2.50m,
    TokensUsed = 10000,
    Category = "LLM",
    Metadata = new Dictionary<string, object>
    {
        { "region", "us-east" },
        { "endpoint", "inference" }
    }
});
```

### Budget Configuration

#### Setting Monthly Budgets
```csharp
// Organization budget
await budgetManager.SetMonthlyBudgetAsync(10000m, BudgetLimitType.Soft);

// Provider-specific budget (GPT-4 is expensive)
await budgetManager.SetProviderBudgetAsync("GPT-4", 5000m, BudgetLimitType.Hard);

// User/Team budgets (chargeback model)
await budgetManager.SetUserBudgetAsync("team-ml@company.com", 3000m, BudgetLimitType.Soft);
```

#### Alert Thresholds
- **50% Usage**: Informational alert
- **90% Usage**: Warning alert
- **100% Usage**: Critical alert (may block requests if hard limit)

---

## Maintenance Schedules

### Default Maintenance Tasks

#### 1. Daily Cache Cleanup (3 AM, 15 min)
```csharp
var cacheTask = new MaintenanceTask
{
    Name = "Daily Cache Cleanup",
    Type = MaintenanceTaskType.CacheCleanup,
    CronExpression = "0 3 * * ?",
    EstimatedDurationMinutes = 15,
    ExecutionFunction = async () => await framework.CacheCleanupAsync(7)
};
await scheduler.ScheduleTaskAsync(cacheTask);
```

#### 2. Weekly Database Index Rebuild (Sunday 2 AM, 1 hour)
```csharp
var dbTask = new MaintenanceTask
{
    Name = "Weekly Database Maintenance",
    Type = MaintenanceTaskType.DatabaseMaintenance,
    CronExpression = "0 2 ? * 0",
    EstimatedDurationMinutes = 60,
    IsCritical = true,
    ExecutionFunction = async () => await framework.RebuildDatabaseIndexesAsync()
};
await scheduler.ScheduleTaskAsync(dbTask);
```

#### 3. Monthly Defragmentation (1st of month 2 AM, 2 hours)
```csharp
var defragTask = new MaintenanceTask
{
    Name = "Monthly Disk Defragmentation",
    Type = MaintenanceTaskType.Defragmentation,
    CronExpression = "0 2 1 * ?",
    EstimatedDurationMinutes = 120,
    ExecutionFunction = async () => await framework.DefragmentNtfsAsync("C:")
};
await scheduler.ScheduleTaskAsync(defragTask);
```

### Customizing Schedules

```csharp
// Cron Expression Format: "second minute hour day month dayOfWeek"
// Examples:
// "0 3 * * ?"      = 3 AM daily
// "0 2 ? * 0"      = 2 AM every Sunday
// "0 3 ? * 1-5"    = 3 AM Monday-Friday
// "0 0,12 * * ?"   = Midnight and noon every day
```

---

## Cost Management

### Monitoring Costs

#### Daily Cost Report
```csharp
var projection = await costTracker.GetMonthlyProjectionAsync();
Console.WriteLine($"Current Month: ${projection.CurrentMonthCost:F2}");
Console.WriteLine($"Projected End: ${projection.ProjectedMonthCost:F2}");
Console.WriteLine($"Days Remaining: {projection.DaysRemaining}");
```

#### Cost by Provider (Top Expenses)
```csharp
var costsByProvider = await costTracker.GetCostsByProviderAsync();
foreach (var provider in costsByProvider.OrderByDescending(x => x.Value))
{
    Console.WriteLine($"{provider.Key}: ${provider.Value:F2}");
}
```

#### User Attribution (Chargeback)
```csharp
var userCosts = new Dictionary<string, decimal>();
var allCosts = await costTracker.GetCostsByUserAsync("*");
// Calculate chargeback amounts and generate invoice
```

### Cost Optimization

#### Auto-Recommendations
```csharp
var recommendations = await optimizationEngine.AnalyzeAndRecommendAsync();
foreach (var rec in recommendations.OrderByDescending(r => r.EstimatedMonthlySavings))
{
    Console.WriteLine($"{rec.Title}: ${rec.EstimatedMonthlySavings:F2}/month");
    
    if (rec.CanBeAutoApplied)
    {
        var applied = await optimizationEngine.ApplyOptimizationAsync(rec);
    }
}
```

#### Manual Overrides
```csharp
// Temporarily increase budget for urgent project
await budgetManager.OverrideLimitAsync(5000m, "Q4 Research Initiative - expires 2026-12-31");

// Switch provider for specific operation
// (Implement custom logic based on recommendations)
```

---

## Troubleshooting

### Issue: High Memory Usage

**Symptoms**: Memory near 80% threshold
**Resolution**:
```csharp
// Automatic
var monitor = serviceProvider.GetRequiredService<IResourceMonitor>();
var memory = await monitor.GetMemoryUsageAsync();
if (memory.UsagePercentage > 80)
{
    var result = await monitor.AutoRemediateAsync(
        new ResourceAlert { Type = ResourceAlertType.HighMemory }
    );
}

// Manual
GC.Collect();
GC.WaitForPendingFinalizers();
```

### Issue: Disk Space Critical

**Symptoms**: Disk usage > 90%
**Resolution**:
```csharp
var framework = serviceProvider.GetRequiredService<ICleanupAutomationFramework>();

// Run emergency cleanup
await framework.CacheCleanupAsync(3);           // Delete files 3+ days old
await framework.LogCleanupAsync(3);              // Compress/delete logs
await framework.TempFileCleanupAsync();          // Clear temp files
await framework.FindDuplicateFilesAsync();       // Identify and remove duplicates
```

### Issue: Budget Exceeded

**Symptoms**: Hard limit reached, requests denied
**Resolution**:
```csharp
// Emergency override
await budgetManager.OverrideLimitAsync(2000m, "Emergency - critical business process");

// Review high-cost operations
var operations = await optimizationEngine.IdentifyHighCostOperationsAsync();

// Switch providers
var switches = await optimizationEngine.RecommendProviderSwitchesAsync();
```

### Issue: Maintenance Task Failed

**Symptoms**: Task shows failed status
**Resolution**:
```csharp
// Check execution history
var history = await scheduler.GetExecutionHistoryAsync(taskId, lastN: 5);
foreach (var exec in history.Where(e => !e.Success))
{
    Console.WriteLine($"Task: {exec.TaskName}");
    Console.WriteLine($"Error: {exec.ErrorMessage}");
    Console.WriteLine($"Retries: {exec.RetryCount}");
}

// Retry manually
var result = await scheduler.RunNowAsync(taskId);
```

---

## Best Practices

### 1. Budget Management
- ✅ Set **soft limits** by default (alerts but doesn't block)
- ✅ Use **hard limits** for runaway cost providers (GPT-4, Azure)
- ✅ Review and reset budgets **monthly**
- ✅ Set **user/team budgets** for cost attribution
- ✅ Monitor **daily burn rate** and adjust projections

### 2. Cost Optimization
- ✅ Enable **provider switching** when savings > 20%
- ✅ Implement **caching** for high-frequency queries
- ✅ Use **local Hermes** for internal/non-critical tasks
- ✅ **Batch requests** for bulk discounts
- ✅ Review recommendations **weekly**

### 3. Maintenance
- ✅ Schedule during **low-traffic windows** (3 AM UTC)
- ✅ **Test critical tasks** in staging first
- ✅ Notify users **24 hours before** maintenance
- ✅ Set **max retries = 3** for resilience
- ✅ Monitor **failure rates** and escalate if > 5% per week

### 4. Resource Monitoring
- ✅ Enable **auto-remediation** for memory/CPU spikes
- ✅ Review **capacity forecast** monthly
- ✅ Monitor **storage growth** and expand proactively
- ✅ Set up **alerts** for memory leaks (growth rate > 1 GB/day)
- ✅ Document **baseline metrics** for trend analysis

### 5. Logging & Audit
- ✅ Export **cost reports** weekly for finance
- ✅ Maintain **audit logs** for compliance (7 years)
- ✅ Archive **old backups** to cold storage after 30 days
- ✅ Compress **logs older than 7 days**
- ✅ Delete **logs older than 1 year**

### 6. Dashboards
- ✅ Display **cost trends** prominently
- ✅ Show **budget status** with time remaining
- ✅ Highlight **top optimization opportunities**
- ✅ Monitor **resource utilization gauges**
- ✅ List **upcoming maintenance** windows

---

## Support & Contact

**Questions?** Check the test suite (`Week6_OptimizationTests.cs`) for detailed usage examples.

**Issues?** Review logs in `/logs/week6-optimization.log`

**Metrics Available**:
- Cost tracking: daily, weekly, monthly, yearly
- Budget status: remaining, burn rate, projection
- Optimization savings: achieved vs. potential
- Resource usage: CPU, memory, disk, network, GPU
- Maintenance success rate: last 7, 30, 90 days
