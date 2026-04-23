using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonadoBlade.Week6.Interfaces;

namespace MonadoBlade.Week6.Dashboard
{
    /// <summary>
    /// Dashboard for displaying optimization metrics and insights.
    /// </summary>
    public class OptimizationDashboard
    {
        private readonly ICostTracker _costTracker;
        private readonly IBudgetManager _budgetManager;
        private readonly IOptimizationRecommendationEngine _optimizationEngine;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IMaintenanceScheduler _maintenanceScheduler;

        public OptimizationDashboard(
            ICostTracker costTracker,
            IBudgetManager budgetManager,
            IOptimizationRecommendationEngine optimizationEngine,
            IResourceMonitor resourceMonitor,
            IMaintenanceScheduler maintenanceScheduler)
        {
            _costTracker = costTracker;
            _budgetManager = budgetManager;
            _optimizationEngine = optimizationEngine;
            _resourceMonitor = resourceMonitor;
            _maintenanceScheduler = maintenanceScheduler;
        }

        /// <summary>Gets complete dashboard data for UI rendering.</summary>
        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var projection = await _costTracker.GetMonthlyProjectionAsync();
            var costs = await _costTracker.GetCostsByProviderAsync();
            var budget = await _budgetManager.GetBudgetStatusAsync();
            var recs = await _optimizationEngine.AnalyzeAndRecommendAsync();
            var memory = await _resourceMonitor.GetMemoryUsageAsync();
            var disk = await _resourceMonitor.GetDiskUsageAsync();
            var maintenance = await _maintenanceScheduler.GetCurrentStatusAsync();

            return new DashboardData
            {
                Timestamp = DateTime.UtcNow,
                CostProjection = projection,
                CostsByProvider = costs,
                BudgetStatus = budget,
                OptimizationRecommendations = recs,
                MemoryUsage = memory,
                DiskUsage = disk,
                MaintenanceStatus = maintenance
            };
        }

        /// <summary>Gets cost trend for chart visualization.</summary>
        public async Task<List<CostTrendPoint>> GetCostTrendChartAsync(int days = 30)
        {
            var dailyTrends = await _costTracker.GetDailyTrendsAsync();
            var points = new List<CostTrendPoint>();

            for (int i = days; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i).Date;
                var cost = dailyTrends.ContainsKey(date) ? dailyTrends[date] : 0m;
                points.Add(new CostTrendPoint { Date = date, Cost = cost });
            }

            return await Task.FromResult(points);
        }

        /// <summary>Gets provider cost distribution for pie chart.</summary>
        public async Task<List<ProviderCostData>> GetProviderCostDistributionAsync()
        {
            var costs = await _costTracker.GetCostsByProviderAsync();
            var total = 0m;
            foreach (var cost in costs.Values) total += cost;

            var data = new List<ProviderCostData>();
            foreach (var provider in costs)
            {
                data.Add(new ProviderCostData
                {
                    Provider = provider.Key,
                    Cost = provider.Value,
                    Percentage = total > 0 ? (provider.Value / total) * 100 : 0
                });
            }

            return data;
        }

        /// <summary>Gets resource utilization gauges.</summary>
        public async Task<ResourceGauges> GetResourceGaugesAsync()
        {
            var memory = await _resourceMonitor.GetMemoryUsageAsync();
            var disk = await _resourceMonitor.GetDiskUsageAsync();

            return new ResourceGauges
            {
                MemoryUsagePercent = memory.UsagePercentage,
                DiskUsagePercent = disk.UsagePercentage,
                MemoryAlertLevel = memory.UsagePercentage > 80 ? "Critical" : memory.UsagePercentage > 60 ? "Warning" : "OK",
                DiskAlertLevel = disk.UsagePercentage > 90 ? "Critical" : disk.UsagePercentage > 75 ? "Warning" : "OK"
            };
        }
    }

    public class DashboardData
    {
        public DateTime Timestamp { get; set; }
        public CostProjection? CostProjection { get; set; }
        public Dictionary<string, decimal>? CostsByProvider { get; set; }
        public BudgetStatus? BudgetStatus { get; set; }
        public List<OptimizationRecommendation>? OptimizationRecommendations { get; set; }
        public MemoryUsageInfo? MemoryUsage { get; set; }
        public DiskUsageInfo? DiskUsage { get; set; }
        public MaintenanceStatus? MaintenanceStatus { get; set; }
    }

    public class CostTrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
    }

    public class ProviderCostData
    {
        public string? Provider { get; set; }
        public decimal Cost { get; set; }
        public decimal Percentage { get; set; }
    }

    public class ResourceGauges
    {
        public double MemoryUsagePercent { get; set; }
        public double DiskUsagePercent { get; set; }
        public string? MemoryAlertLevel { get; set; }
        public string? DiskAlertLevel { get; set; }
    }
}
