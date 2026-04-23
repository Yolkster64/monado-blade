using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Week6.Interfaces
{
    /// <summary>
    /// Analyzes cost patterns and recommends optimizations.
    /// </summary>
    public interface IOptimizationRecommendationEngine
    {
        /// <summary>Analyzes usage patterns and returns cost optimization suggestions.</summary>
        Task<List<OptimizationRecommendation>> AnalyzeAndRecommendAsync();

        /// <summary>Gets high-cost operations that could be optimized.</summary>
        Task<List<HighCostOperation>> IdentifyHighCostOperationsAsync();

        /// <summary>Identifies underutilized resources that could be disabled.</summary>
        Task<List<UnderutilizedResource>> IdentifyUnderutilizedResourcesAsync();

        /// <summary>Analyzes caching efficiency and redundant calls.</summary>
        Task<CachingAnalysis> AnalyzeCachingEfficiencyAsync();

        /// <summary>Applies recommended optimizations automatically.</summary>
        Task<bool> ApplyOptimizationAsync(OptimizationRecommendation recommendation);

        /// <summary>Gets potential savings from all recommendations.</summary>
        Task<decimal> CalculatePotentialSavingsAsync();

        /// <summary>Gets savings already achieved from previous optimizations.</summary>
        Task<decimal> GetAchievedSavingsAsync();

        /// <summary>Suggests provider switches based on cost/capability.</summary>
        Task<List<ProviderSwitchRecommendation>> RecommendProviderSwitchesAsync();
    }

    /// <summary>A specific optimization recommendation.</summary>
    public class OptimizationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal EstimatedMonthlySavings { get; set; }
        public double ImplementationEffort { get; set; } // 0-100 (100=complex)
        public string RecommendationType { get; set; } // "ProviderSwitch", "Caching", "Batching", "LocalModel"
        public OptimizationPriority Priority { get; set; }
        public bool CanBeAutoApplied { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Applied { get; set; }
    }

    public enum OptimizationPriority { Low, Medium, High, Critical }

    /// <summary>High-cost operation that could be optimized.</summary>
    public class HighCostOperation
    {
        public string OperationName { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public int CallCount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerCall { get; set; }
        public double UsagePercentage { get; set; }
        public string Recommendation { get; set; }
        public decimal PotentialSavings { get; set; }
    }

    /// <summary>Resource that is underutilized and could be disabled.</summary>
    public class UnderutilizedResource
    {
        public string ResourceName { get; set; }
        public string ResourceType { get; set; } // "Service", "VM", "Database"
        public decimal MonthlyCost { get; set; }
        public double UtilizationPercentage { get; set; }
        public int DaysSinceLastUsed { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>Analysis of caching effectiveness.</summary>
    public class CachingAnalysis
    {
        public int TotalRequests { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public double HitRate { get; set; }
        public decimal SavingsFromCaching { get; set; }
        public decimal PotentialAdditionalSavings { get; set; }
        public List<string> RedundantQueryPatterns { get; set; } = new();
    }

    /// <summary>Recommendation to switch providers for cost or performance.</summary>
    public class ProviderSwitchRecommendation
    {
        public string CurrentProvider { get; set; }
        public string SuggestedProvider { get; set; }
        public string Model { get; set; }
        public decimal CurrentMonthlyCost { get; set; }
        public decimal EstimatedCostAfterSwitch { get; set; }
        public decimal MonthlySavings { get; set; }
        public double CapabilityDifference { get; set; } // 1.0 = same, 0.9 = 10% loss
        public string Reasoning { get; set; }
    }
}
