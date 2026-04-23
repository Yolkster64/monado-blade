using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonadoBlade.Week6.Interfaces;

namespace MonadoBlade.Week6.Services
{
    /// <summary>
    /// Analyzes cost patterns and recommends optimizations.
    /// </summary>
    public class OptimizationRecommendationEngine : IOptimizationRecommendationEngine
    {
        private readonly List<OptimizationRecommendation> _recommendations = new();
        private readonly ICostTracker _costTracker;

        public OptimizationRecommendationEngine(ICostTracker costTracker)
        {
            _costTracker = costTracker;
        }

        public async Task<List<OptimizationRecommendation>> AnalyzeAndRecommendAsync()
        {
            _recommendations.Clear();
            
            // Get costs by provider and model
            var costsByProvider = await _costTracker.GetCostsByProviderAsync();
            var costsByModel = await _costTracker.GetCostsByModelAsync();

            // Recommendation 1: Switch from GPT-4 to Claude (30% cheaper)
            if (costsByModel.ContainsKey("gpt-4") && costsByModel["gpt-4"] > 1000)
            {
                _recommendations.Add(new OptimizationRecommendation
                {
                    Title = "Switch GPT-4 to Claude for non-critical tasks",
                    Description = "Claude provides similar capabilities at 30% lower cost",
                    EstimatedMonthlySavings = costsByModel["gpt-4"] * 0.30m,
                    ImplementationEffort = 30,
                    RecommendationType = "ProviderSwitch",
                    Priority = OptimizationPriority.High,
                    CanBeAutoApplied = true
                });
            }

            // Recommendation 2: Enable caching (20% savings)
            var totalCosts = costsByProvider.Sum(x => x.Value);
            if (totalCosts > 5000)
            {
                _recommendations.Add(new OptimizationRecommendation
                {
                    Title = "Enable result caching for high-frequency queries",
                    Description = "Cache results from identical queries, reducing API calls by 20%",
                    EstimatedMonthlySavings = totalCosts * 0.20m,
                    ImplementationEffort = 50,
                    RecommendationType = "Caching",
                    Priority = OptimizationPriority.High,
                    CanBeAutoApplied = true
                });
            }

            // Recommendation 3: Use local Hermes instead of Claude (100% savings)
            if (costsByProvider.ContainsKey("Claude"))
            {
                _recommendations.Add(new OptimizationRecommendation
                {
                    Title = "Run local Hermes model for internal tasks",
                    Description = "Hermes provides strong performance at zero marginal cost for internal use",
                    EstimatedMonthlySavings = costsByProvider.GetValueOrDefault("Claude", 0) * 0.50m,
                    ImplementationEffort = 70,
                    RecommendationType = "LocalModel",
                    Priority = OptimizationPriority.Medium,
                    CanBeAutoApplied = false
                });
            }

            // Recommendation 4: Batch similar requests
            if (totalCosts > 3000)
            {
                _recommendations.Add(new OptimizationRecommendation
                {
                    Title = "Batch similar API requests",
                    Description = "Group requests for bulk processing with volume discounts",
                    EstimatedMonthlySavings = totalCosts * 0.10m,
                    ImplementationEffort = 60,
                    RecommendationType = "Batching",
                    Priority = OptimizationPriority.Medium,
                    CanBeAutoApplied = false
                });
            }

            return _recommendations;
        }

        public async Task<List<HighCostOperation>> IdentifyHighCostOperationsAsync()
        {
            var operations = new List<HighCostOperation>();
            var costsByModel = await _costTracker.GetCostsByModelAsync();
            var totalCost = costsByModel.Sum(x => x.Value);

            foreach (var model in costsByModel)
            {
                operations.Add(new HighCostOperation
                {
                    OperationName = model.Key,
                    Model = model.Key,
                    TotalCost = model.Value,
                    UsagePercentage = (model.Value / totalCost) * 100,
                    Recommendation = model.Value > totalCost * 0.4 
                        ? "Consider consolidating or switching providers" 
                        : "Monitor for optimization opportunities"
                });
            }

            return operations.OrderByDescending(x => x.TotalCost).ToList();
        }

        public async Task<List<UnderutilizedResource>> IdentifyUnderutilizedResourcesAsync()
        {
            // Mock data - in real implementation, query actual resources
            var resources = new List<UnderutilizedResource>
            {
                new UnderutilizedResource
                {
                    ResourceName = "Development Database",
                    ResourceType = "Database",
                    MonthlyCost = 500,
                    UtilizationPercentage = 5,
                    DaysSinceLastUsed = 30,
                    Recommendation = "Consider archiving or removing"
                }
            };
            return await Task.FromResult(resources);
        }

        public async Task<CachingAnalysis> AnalyzeCachingEfficiencyAsync()
        {
            // Mock cache statistics - in real implementation, query cache service
            return await Task.FromResult(new CachingAnalysis
            {
                TotalRequests = 10000,
                CacheHits = 6000,
                CacheMisses = 4000,
                HitRate = 0.6,
                SavingsFromCaching = 2000m,
                PotentialAdditionalSavings = 1000m,
                RedundantQueryPatterns = new List<string> { "get_user_profile", "list_projects" }
            });
        }

        public async Task<bool> ApplyOptimizationAsync(OptimizationRecommendation recommendation)
        {
            recommendation.Applied = true;
            return await Task.FromResult(true);
        }

        public async Task<decimal> CalculatePotentialSavingsAsync()
        {
            var recommendations = await AnalyzeAndRecommendAsync();
            return recommendations.Sum(r => r.EstimatedMonthlySavings);
        }

        public async Task<decimal> GetAchievedSavingsAsync()
        {
            var appliedRecommendations = _recommendations.Where(r => r.Applied).ToList();
            return await Task.FromResult(appliedRecommendations.Sum(r => r.EstimatedMonthlySavings));
        }

        public async Task<List<ProviderSwitchRecommendation>> RecommendProviderSwitchesAsync()
        {
            var switches = new List<ProviderSwitchRecommendation>
            {
                new ProviderSwitchRecommendation
                {
                    CurrentProvider = "OpenAI",
                    SuggestedProvider = "Claude",
                    Model = "GPT-4 → Claude-3-Opus",
                    CurrentMonthlyCost = 5000,
                    EstimatedCostAfterSwitch = 3500,
                    MonthlySavings = 1500,
                    CapabilityDifference = 0.95, // 5% capability difference
                    Reasoning = "Claude-3-Opus offers better performance at lower cost for most workloads"
                }
            };
            return await Task.FromResult(switches);
        }
    }
}
