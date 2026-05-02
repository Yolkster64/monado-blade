using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonadoBlade.Integration.HELIOS;

namespace MonadoBlade.GUI.Designer
{
    /// <summary>
    /// GUI Visual Designer for optimizing LLM selection and parameters
    /// Shows which LLM to use, why, cost, latency, and allows visual tuning
    /// </summary>
    public class LLMVisualDesigner : IHELIOSService
    {
        public string ServiceName => "LLM Visual Designer";
        public string Version => "2.1";

        private readonly AIHubCore _aiHub;

        public LLMVisualDesigner(AIHubCore aiHub)
        {
            _aiHub = aiHub;
        }

        /// <summary>
        /// Get visual recommendations for LLM selection
        /// </summary>
        public async Task<LLMVisualization> GetVisualizationAsync(string taskType)
        {
            var guidance = await _aiHub.GetLLMSelectionGuidanceAsync(taskType);
            var providers = await _aiHub.GetAvailableProvidersAsync();

            return new LLMVisualization
            {
                TaskType = taskType,
                PrimaryRecommendation = CreateProviderCard(guidance.Primary, providers),
                SecondaryRecommendation = CreateProviderCard(guidance.Secondary, providers),
                TertiaryRecommendation = CreateProviderCard(guidance.Tertiary, providers),
                ComparisonChart = GenerateComparisonChart(providers),
                CostVsLatencyTrade = GenerateCostLatencyTrade(providers),
                RecommendationReason = guidance.Reason
            };
        }

        /// <summary>
        /// Interactive LLM parameter tuning
        /// </summary>
        public async Task<ParameterTuningResult> TuneParametersAsync(LLMTuningRequest request)
        {
            var result = new ParameterTuningResult
            {
                OriginalParameters = request.Parameters,
                TunedParameters = OptimizeParameters(request),
                ExpectedImprovement = CalculateImprovement(request),
                Visualizations = GenerateTuningVisuals(request)
            };

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Show cost breakdown for different LLM choices
        /// </summary>
        public async Task<CostAnalysis> AnalyzeCostsAsync(List<string> taskList)
        {
            var breakdown = new Dictionary<string, double>();
            var recommendations = new Dictionary<string, string>();

            foreach (var task in taskList)
            {
                var guidance = await _aiHub.GetLLMSelectionGuidanceAsync(task);
                // Parse cost from guidance
                breakdown[task] = 0.01; // Example
                recommendations[task] = guidance.Primary;
            }

            return new CostAnalysis
            {
                TotalEstimatedCost = breakdown.Values.Sum(),
                CostBreakdown = breakdown,
                Recommendations = recommendations,
                OptimizationSuggestions = new List<string>
                {
                    "Use local Phi-3 for quick tasks (saves 99% cost)",
                    "Batch expensive analysis queries",
                    "Cache common responses"
                }
            };
        }

        private ProviderCard CreateProviderCard(string providerName, List<AIHubCore.LLMProviderInfo> providers)
        {
            var provider = providers.FirstOrDefault(p => p.Name == providerName);
            if (provider == null)
                return new ProviderCard { Name = providerName, Error = "Not found" };

            return new ProviderCard
            {
                Name = provider.Name,
                Type = provider.Type,
                CostPerRequest = provider.CostPerRequest,
                LatencyMs = provider.AverageLatencyMs,
                MaxTokens = provider.MaxTokens,
                Specializations = provider.Specializations,
                AvailabilityPercentage = provider.AvailabilityPercentage,
                IsRecommended = true
            };
        }

        private ComparisonChart GenerateComparisonChart(List<AIHubCore.LLMProviderInfo> providers)
        {
            return new ComparisonChart
            {
                Providers = providers.Select(p => p.Name).ToList(),
                Costs = providers.Select(p => p.CostPerRequest).ToList(),
                Latencies = providers.Select(p => (double)p.AverageLatencyMs).ToList(),
                ChartType = "ScatterPlot"
            };
        }

        private CostLatencyTrade GenerateCostLatencyTrade(List<AIHubCore.LLMProviderInfo> providers)
        {
            var fastest = providers.OrderBy(p => p.AverageLatencyMs).First();
            var cheapest = providers.OrderBy(p => p.CostPerRequest).First();
            var mostBalanced = providers.OrderBy(p => 
                (p.CostPerRequest / 0.02) + (p.AverageLatencyMs / 500.0)).First();

            return new CostLatencyTrade
            {
                FastestOption = $"{fastest.Name} ({fastest.AverageLatencyMs}ms)",
                CheapestOption = $"{cheapest.Name} (${cheapest.CostPerRequest})",
                BalancedOption = $"{mostBalanced.Name} (recommended)"
            };
        }

        private Dictionary<string, object> OptimizeParameters(LLMTuningRequest request)
        {
            var optimized = new Dictionary<string, object>(request.Parameters);
            // Apply optimization logic
            return optimized;
        }

        private double CalculateImprovement(LLMTuningRequest request)
        {
            return 15.5; // Example: 15.5% improvement
        }

        private TuningVisuals GenerateTuningVisuals(LLMTuningRequest request)
        {
            return new TuningVisuals
            {
                ParameterCharts = new List<string> { "temperature", "top_p", "frequency_penalty" },
                ImpactMetrics = new Dictionary<string, double> 
                { 
                    { "Speed", 25.0 },
                    { "Quality", -5.0 },
                    { "Cost", 30.0 }
                }
            };
        }

        public async Task InitializeAsync() => await Task.CompletedTask;
        public async Task ShutdownAsync() => await Task.CompletedTask;
    }

    // Data Models
    public class LLMVisualization
    {
        public string TaskType { get; set; }
        public ProviderCard PrimaryRecommendation { get; set; }
        public ProviderCard SecondaryRecommendation { get; set; }
        public ProviderCard TertiaryRecommendation { get; set; }
        public ComparisonChart ComparisonChart { get; set; }
        public CostLatencyTrade CostVsLatencyTrade { get; set; }
        public string RecommendationReason { get; set; }
    }

    public class ProviderCard
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double CostPerRequest { get; set; }
        public int LatencyMs { get; set; }
        public int MaxTokens { get; set; }
        public string[] Specializations { get; set; }
        public double AvailabilityPercentage { get; set; }
        public bool IsRecommended { get; set; }
        public string Error { get; set; }
    }

    public class ComparisonChart
    {
        public List<string> Providers { get; set; }
        public List<double> Costs { get; set; }
        public List<double> Latencies { get; set; }
        public string ChartType { get; set; }
    }

    public class CostLatencyTrade
    {
        public string FastestOption { get; set; }
        public string CheapestOption { get; set; }
        public string BalancedOption { get; set; }
    }

    public class LLMTuningRequest
    {
        public string LLMName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public string OptimizationGoal { get; set; } // "speed", "quality", "cost", "balanced"
    }

    public class ParameterTuningResult
    {
        public Dictionary<string, object> OriginalParameters { get; set; }
        public Dictionary<string, object> TunedParameters { get; set; }
        public double ExpectedImprovement { get; set; }
        public TuningVisuals Visualizations { get; set; }
    }

    public class TuningVisuals
    {
        public List<string> ParameterCharts { get; set; }
        public Dictionary<string, double> ImpactMetrics { get; set; }
    }

    public class CostAnalysis
    {
        public double TotalEstimatedCost { get; set; }
        public Dictionary<string, double> CostBreakdown { get; set; }
        public Dictionary<string, string> Recommendations { get; set; }
        public List<string> OptimizationSuggestions { get; set; }
    }
}
