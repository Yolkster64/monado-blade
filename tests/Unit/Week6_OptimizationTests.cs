using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MonadoBlade.Week6.Interfaces;
using MonadoBlade.Week6.Services;
using MonadoBlade.Week6.Dashboard;

namespace MonadoBlade.Week6.Tests
{
    /// <summary>Comprehensive test suite for Week 6 cost optimization components.</summary>
    public class CostOptimizationTests
    {
        // ==================== COST TRACKER TESTS ====================

        [Fact]
        public async Task RecordCostAsync_ShouldStoreEvent()
        {
            var tracker = new CostTrackerService();
            var costEvent = new CostEvent
            {
                Provider = "Claude",
                Model = "Claude-3-Opus",
                UserId = "user1",
                ProjectId = "proj1",
                Amount = 10m,
                TokensUsed = 5000
            };

            await tracker.RecordCostAsync(costEvent);
            var costs = await tracker.GetCostsByProviderAsync();

            Assert.NotEmpty(costs);
            Assert.Equal(10m, costs["Claude"]);
        }

        [Fact]
        public async Task GetCostsByProviderAsync_ShouldAggregateByProvider()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { Provider = "Claude", Amount = 100m });
            await tracker.RecordCostAsync(new CostEvent { Provider = "Claude", Amount = 50m });
            await tracker.RecordCostAsync(new CostEvent { Provider = "GPT-4", Amount = 200m });

            var costs = await tracker.GetCostsByProviderAsync();

            Assert.Equal(150m, costs["Claude"]);
            Assert.Equal(200m, costs["GPT-4"]);
        }

        [Fact]
        public async Task GetCostsByModelAsync_ShouldGroupByModel()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { Model = "GPT-4", Amount = 100m });
            await tracker.RecordCostAsync(new CostEvent { Model = "GPT-4", Amount = 50m });

            var costs = await tracker.GetCostsByModelAsync();

            Assert.Equal(150m, costs["GPT-4"]);
        }

        [Fact]
        public async Task GetCostsByUserAsync_ShouldAttributeToCosts()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { UserId = "alice", Amount = 100m });
            await tracker.RecordCostAsync(new CostEvent { UserId = "alice", Amount = 50m });
            await tracker.RecordCostAsync(new CostEvent { UserId = "bob", Amount = 200m });

            var aliceCosts = await tracker.GetCostsByUserAsync("alice");
            var bobCosts = await tracker.GetCostsByUserAsync("bob");

            Assert.Equal(150m, aliceCosts);
            Assert.Equal(200m, bobCosts);
        }

        [Fact]
        public async Task GetCostsByProjectAsync_ShouldGroupByProject()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { ProjectId = "ProjectA", Amount = 500m });
            await tracker.RecordCostAsync(new CostEvent { ProjectId = "ProjectB", Amount = 300m });

            var costs = await tracker.GetCostsByProjectAsync();

            Assert.Equal(500m, costs["ProjectA"]);
            Assert.Equal(300m, costs["ProjectB"]);
        }

        [Fact]
        public async Task GetDailyTrendsAsync_ShouldShowTrendOverTime()
        {
            var tracker = new CostTrackerService();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                await tracker.RecordCostAsync(new CostEvent { Amount = 100m, Timestamp = date });
            }

            var trends = await tracker.GetDailyTrendsAsync();

            Assert.NotEmpty(trends);
        }

        [Fact]
        public async Task GetMonthlyProjectionAsync_ShouldProjectEndOfMonth()
        {
            var tracker = new CostTrackerService();

            for (int i = 0; i < 10; i++)
            {
                await tracker.RecordCostAsync(new CostEvent { Amount = 100m });
            }

            var projection = await tracker.GetMonthlyProjectionAsync();

            Assert.True(projection.ProjectedMonthCost > projection.CurrentMonthCost);
            Assert.True(projection.DaysRemaining > 0);
        }

        [Fact]
        public async Task AnalyzeTrendAsync_ShouldDetectTrends()
        {
            var tracker = new CostTrackerService();

            // Record higher costs in recent week
            for (int i = 3; i >= 0; i--)
            {
                await tracker.RecordCostAsync(new CostEvent { Amount = 50m, Timestamp = DateTime.UtcNow.AddDays(-i) });
            }

            var trend = await tracker.AnalyzeTrendAsync();

            Assert.NotNull(trend);
            Assert.True(trend.Direction == TrendDirection.Stable || trend.Direction == TrendDirection.Increasing);
        }

        [Fact]
        public async Task ExportCsvAsync_ShouldReturnValidCsv()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { Provider = "Claude", Amount = 100m });

            var csv = await tracker.ExportCsvAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

            Assert.Contains("Provider,Model,UserId", csv);
            Assert.Contains("Claude", csv);
        }

        [Fact]
        public async Task ExportJsonAsync_ShouldReturnValidJson()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { Provider = "Claude", Amount = 100m });

            var json = await tracker.ExportJsonAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

            Assert.Contains("Claude", json);
            Assert.Contains("Amount", json);
        }

        [Fact]
        public async Task ExportBiReportAsync_ShouldCreateBiReport()
        {
            var tracker = new CostTrackerService();

            await tracker.RecordCostAsync(new CostEvent { Provider = "Claude", Amount = 100m, Model = "Opus" });

            var report = await tracker.ExportBiReportAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

            Assert.NotNull(report);
            Assert.NotEmpty(report.DataPoints);
            Assert.Equal("Claude", report.DataPoints[0].Provider);
        }

        // ==================== BUDGET MANAGER TESTS ====================

        [Fact]
        public async Task SetMonthlyBudgetAsync_ShouldUpdateBudget()
        {
            var budgetManager = new BudgetManagerService();

            await budgetManager.SetMonthlyBudgetAsync(5000m, BudgetLimitType.Hard);
            var status = await budgetManager.GetBudgetStatusAsync();

            Assert.Equal(5000m, status.MonthlyBudget);
            Assert.Equal(BudgetLimitType.Hard, status.MonthlyLimitType);
        }

        [Fact]
        public async Task CanMakeRequestAsync_WithinBudget_ShouldAllow()
        {
            var budgetManager = new BudgetManagerService();

            await budgetManager.SetMonthlyBudgetAsync(1000m, BudgetLimitType.Hard);
            var result = await budgetManager.CanMakeRequestAsync("Claude", 100m, "user1");

            Assert.True(result.Allowed);
        }

        [Fact]
        public async Task CanMakeRequestAsync_ExceedingHardLimit_ShouldDeny()
        {
            var budgetManager = new BudgetManagerService();

            await budgetManager.SetMonthlyBudgetAsync(100m, BudgetLimitType.Hard);
            budgetManager.RecordSpend(90m);

            var result = await budgetManager.CanMakeRequestAsync("Claude", 50m, "user1");

            Assert.False(result.Allowed);
        }

        [Fact]
        public async Task CanMakeRequestAsync_ExceedingSoftLimit_ShouldAlertButAllow()
        {
            var budgetManager = new BudgetManagerService();

            await budgetManager.SetMonthlyBudgetAsync(100m, BudgetLimitType.Soft);
            budgetManager.RecordSpend(90m);

            var result = await budgetManager.CanMakeRequestAsync("Claude", 50m, "user1");

            Assert.True(result.Allowed);
            Assert.True(result.ApproachingLimit);
        }

        [Fact]
        public async Task GetBudgetStatusAsync_ShouldReturnCompleteStatus()
        {
            var budgetManager = new BudgetManagerService();

            var status = await budgetManager.GetBudgetStatusAsync();

            Assert.NotNull(status);
            Assert.True(status.RemainingMonth > 0);
            Assert.True(status.RemainingToday > 0);
        }

        [Fact]
        public async Task SetProviderBudgetAsync_ShouldLimitProvider()
        {
            var budgetManager = new BudgetManagerService();

            await budgetManager.SetProviderBudgetAsync("GPT-4", 1000m, BudgetLimitType.Hard);
            var status = await budgetManager.GetBudgetStatusAsync();

            Assert.NotEmpty(status.ProviderBudgets);
        }

        [Fact]
        public async Task SetUserBudgetAsync_ShouldTrackPerUser()
        {
            var budgetManager = new BudgetManagerService();

            await budgetManager.SetUserBudgetAsync("alice", 500m, BudgetLimitType.Hard);
            var status = await budgetManager.GetBudgetStatusAsync();

            Assert.NotEmpty(status.UserBudgets);
        }

        [Fact]
        public async Task ResetBudgetAsync_ShouldResetDailySpend()
        {
            var budgetManager = new BudgetManagerService();

            budgetManager.RecordSpend(100m);
            await budgetManager.ResetBudgetAsync(BudgetResetType.Daily);
            var status = await budgetManager.GetBudgetStatusAsync();

            Assert.Equal(0m, status.SpentToday);
        }

        [Fact]
        public async Task OverrideLimitAsync_ShouldIncreaseBudget()
        {
            var budgetManager = new BudgetManagerService();

            var originalStatus = await budgetManager.GetBudgetStatusAsync();
            await budgetManager.OverrideLimitAsync(1000m, "Emergency increase");
            var newStatus = await budgetManager.GetBudgetStatusAsync();

            Assert.True(newStatus.MonthlyBudget > originalStatus.MonthlyBudget);
        }

        // ==================== OPTIMIZATION ENGINE TESTS ====================

        [Fact]
        public async Task AnalyzeAndRecommendAsync_ShouldReturnRecommendations()
        {
            var tracker = new CostTrackerService();
            var engine = new OptimizationRecommendationEngine(tracker);

            var recommendations = await engine.AnalyzeAndRecommendAsync();

            Assert.NotNull(recommendations);
        }

        [Fact]
        public async Task IdentifyHighCostOperationsAsync_ShouldFlagExpensiveOperations()
        {
            var tracker = new CostTrackerService();
            var engine = new OptimizationRecommendationEngine(tracker);

            await tracker.RecordCostAsync(new CostEvent { Model = "GPT-4", Amount = 5000m });
            var operations = await engine.IdentifyHighCostOperationsAsync();

            Assert.NotEmpty(operations);
        }

        [Fact]
        public async Task CalculatePotentialSavingsAsync_ShouldComputeTotalSavings()
        {
            var tracker = new CostTrackerService();
            var engine = new OptimizationRecommendationEngine(tracker);

            var savings = await engine.CalculatePotentialSavingsAsync();

            Assert.True(savings >= 0);
        }

        [Fact]
        public async Task RecommendProviderSwitchesAsync_ShouldSuggestCheaperOptions()
        {
            var tracker = new CostTrackerService();
            var engine = new OptimizationRecommendationEngine(tracker);

            var switches = await engine.RecommendProviderSwitchesAsync();

            Assert.NotEmpty(switches);
            Assert.True(switches[0].MonthlySavings > 0);
        }

        [Fact]
        public async Task ApplyOptimizationAsync_ShouldMarkAsApplied()
        {
            var tracker = new CostTrackerService();
            var engine = new OptimizationRecommendationEngine(tracker);

            var recommendations = await engine.AnalyzeAndRecommendAsync();
            if (recommendations.Any())
            {
                var success = await engine.ApplyOptimizationAsync(recommendations[0]);
                Assert.True(success);
                Assert.True(recommendations[0].Applied);
            }
        }

        // ==================== CLEANUP FRAMEWORK TESTS ====================

        [Fact]
        public async Task CacheCleanupAsync_ShouldReturnCleanupResult()
        {
            var framework = new CleanupAutomationFramework();

            var result = await framework.CacheCleanupAsync(7);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.BytesFreed > 0);
        }

        [Fact]
        public async Task TempFileCleanupAsync_ShouldFreeDiskSpace()
        {
            var framework = new CleanupAutomationFramework();

            var result = await framework.TempFileCleanupAsync();

            Assert.True(result.Success);
            Assert.True(result.ItemsDeleted > 0);
        }

        [Fact]
        public async Task FindDuplicateFilesAsync_ShouldIdentifyDuplicates()
        {
            var framework = new CleanupAutomationFramework();

            var duplicates = await framework.FindDuplicateFilesAsync();

            Assert.NotEmpty(duplicates);
            Assert.True(duplicates[0].Count > 1);
        }

        [Fact]
        public async Task AnalyzeStorageUsageTrendAsync_ShouldProjectDiskFull()
        {
            var framework = new CleanupAutomationFramework();

            var trend = await framework.AnalyzeStorageUsageTrendAsync();

            Assert.NotNull(trend);
            Assert.True(trend.DaysUntilFull > 0);
        }

        [Fact]
        public async Task DefragmentNtfsAsync_ShouldReduceFragmentation()
        {
            var framework = new CleanupAutomationFramework();

            var result = await framework.DefragmentNtfsAsync("C:");

            Assert.True(result.Success);
            Assert.True(result.FragmentationAfter < result.FragmentationBefore);
        }

        [Fact]
        public async Task RestartUnhealthyServicesAsync_ShouldRecoverServices()
        {
            var framework = new CleanupAutomationFramework();

            var result = await framework.RestartUnhealthyServicesAsync();

            Assert.True(result.Success);
        }

        [Fact]
        public async Task RebuildDatabaseIndexesAsync_ShouldImprovePerformance()
        {
            var framework = new CleanupAutomationFramework();

            var result = await framework.RebuildDatabaseIndexesAsync();

            Assert.True(result.Success);
            Assert.True(result.IndexesRebuilt > 0);
        }

        // ==================== RESOURCE MONITOR TESTS ====================

        [Fact]
        public async Task GetMemoryUsageAsync_ShouldReturnMemoryInfo()
        {
            var monitor = new ResourceMonitor();

            var memory = await monitor.GetMemoryUsageAsync();

            Assert.NotNull(memory);
            Assert.True(memory.UsagePercentage >= 0);
            Assert.True(memory.UsagePercentage <= 100);
        }

        [Fact]
        public async Task GetDiskUsageAsync_ShouldReturnDiskInfo()
        {
            var monitor = new ResourceMonitor();

            var disk = await monitor.GetDiskUsageAsync();

            Assert.NotNull(disk);
            Assert.True(disk.UsagePercentage > 0);
        }

        [Fact]
        public async Task ForecastCapacityAsync_ShouldPredictCapacityIssues()
        {
            var monitor = new ResourceMonitor();

            var forecast = await monitor.ForecastCapacityAsync();

            Assert.NotNull(forecast);
            Assert.True(forecast.DaysUntilCritical >= 0);
        }

        [Fact]
        public async Task GetTrendsAsync_ShouldShowResourceTrends()
        {
            var monitor = new ResourceMonitor();

            var trends = await monitor.GetTrendsAsync();

            Assert.NotEmpty(trends.CpuTrend);
            Assert.NotEmpty(trends.MemoryTrend);
            Assert.NotEmpty(trends.DiskTrend);
        }

        // ==================== MAINTENANCE SCHEDULER TESTS ====================

        [Fact]
        public async Task ScheduleTaskAsync_ShouldCreateScheduledTask()
        {
            var scheduler = new MaintenanceScheduler();

            var taskId = await scheduler.ScheduleTaskAsync(new MaintenanceTask
            {
                Name = "Cache Cleanup",
                Type = MaintenanceTaskType.CacheCleanup,
                CronExpression = "0 3 * * ?"
            });

            Assert.NotNull(taskId);
            Assert.NotEqual("", taskId);
        }

        [Fact]
        public async Task GetScheduledTasksAsync_ShouldReturnAllTasks()
        {
            var scheduler = new MaintenanceScheduler();

            await scheduler.ScheduleTaskAsync(new MaintenanceTask { Name = "Task1", CronExpression = "0 3 * * ?" });
            var tasks = await scheduler.GetScheduledTasksAsync();

            Assert.NotEmpty(tasks);
        }

        [Fact]
        public async Task CancelTaskAsync_ShouldRemoveTask()
        {
            var scheduler = new MaintenanceScheduler();

            var taskId = await scheduler.ScheduleTaskAsync(new MaintenanceTask { Name = "Task1", CronExpression = "0 3 * * ?" });
            var success = await scheduler.CancelTaskAsync(taskId);

            Assert.True(success);
        }

        [Fact]
        public async Task GetCurrentStatusAsync_ShouldReturnMaintenanceStatus()
        {
            var scheduler = new MaintenanceScheduler();

            var status = await scheduler.GetCurrentStatusAsync();

            Assert.NotNull(status);
        }

        // ==================== DASHBOARD TESTS ====================

        [Fact]
        public async Task GetDashboardDataAsync_ShouldAggregateAllMetrics()
        {
            var tracker = new CostTrackerService();
            var budget = new BudgetManagerService();
            var engine = new OptimizationRecommendationEngine(tracker);
            var monitor = new ResourceMonitor();
            var scheduler = new MaintenanceScheduler();

            var dashboard = new OptimizationDashboard(tracker, budget, engine, monitor, scheduler);
            var data = await dashboard.GetDashboardDataAsync();

            Assert.NotNull(data);
            Assert.NotNull(data.CostProjection);
            Assert.NotNull(data.BudgetStatus);
        }

        [Fact]
        public async Task GetCostTrendChartAsync_ShouldReturnChartData()
        {
            var tracker = new CostTrackerService();
            var dashboard = new OptimizationDashboard(tracker, new BudgetManagerService(), 
                new OptimizationRecommendationEngine(tracker), new ResourceMonitor(), new MaintenanceScheduler());

            var trends = await dashboard.GetCostTrendChartAsync(30);

            Assert.NotEmpty(trends);
        }
    }
}
