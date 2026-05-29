using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI
{
    /// <summary>
    /// Analyzes system patterns and predicts optimization opportunities using statistical models.
    /// </summary>
    public class PredictiveOptimizer
    {
        private readonly List<PerformanceMetric> _metrics = new();
        private readonly Dictionary<string, OptimizationRecommendation> _recommendations = new();
        private readonly int _bufferSize = 1000;
        private double _cpuThreshold = 0.75;
        private double _memoryThreshold = 0.80;
        private bool _isEnabled = true;

        public event EventHandler<OptimizationRecommendationEventArgs>? RecommendationGenerated;

        public class PerformanceMetric
        {
            public DateTime Timestamp { get; set; }
            public double CpuUsage { get; set; }
            public double MemoryUsage { get; set; }
            public double DiskIo { get; set; }
            public int ThreadCount { get; set; }
            public long RequestCount { get; set; }
            public double AverageResponseTime { get; set; }
        }

        public class OptimizationRecommendation
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Category { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public double ImpactScore { get; set; }
            public double ConfidenceLevel { get; set; }
            public DateTime GeneratedAt { get; set; }
            public bool Applied { get; set; }
            public EstimatedImprovement? Improvement { get; set; }
        }

        public class EstimatedImprovement
        {
            public double CpuReduction { get; set; }
            public double MemoryReduction { get; set; }
            public double ResponseTimeImprovement { get; set; }
        }

        public void RecordMetric(PerformanceMetric metric)
        {
            if (!_isEnabled) return;

            lock (_metrics)
            {
                _metrics.Add(metric);
                if (_metrics.Count > _bufferSize)
                    _metrics.RemoveAt(0);
            }
        }

        public async Task<List<OptimizationRecommendation>> AnalyzeAndRecommend()
        {
            if (!_isEnabled || _metrics.Count < 10)
                return new List<OptimizationRecommendation>();

            var recommendations = new List<OptimizationRecommendation>();

            await Task.Run(() =>
            {
                lock (_metrics)
                {
                    var sorted = _metrics.OrderBy(m => m.Timestamp).ToList();

                    // CPU overload prediction
                    var cpuRec = AnalyzeCpuPattern(sorted);
                    if (cpuRec != null) recommendations.Add(cpuRec);

                    // Memory optimization
                    var memRec = AnalyzeMemoryPattern(sorted);
                    if (memRec != null) recommendations.Add(memRec);

                    // Thread pool tuning
                    var threadRec = AnalyzeThreadingPattern(sorted);
                    if (threadRec != null) recommendations.Add(threadRec);

                    // I/O optimization
                    var ioRec = AnalyzeIoPattern(sorted);
                    if (ioRec != null) recommendations.Add(ioRec);

                    // Response time optimization
                    var responseRec = AnalyzeResponseTimePattern(sorted);
                    if (responseRec != null) recommendations.Add(responseRec);
                }
            });

            foreach (var rec in recommendations)
            {
                _recommendations[rec.Id] = rec;
                RecommendationGenerated?.Invoke(this, new OptimizationRecommendationEventArgs { Recommendation = rec });
            }

            return recommendations;
        }

        private OptimizationRecommendation? AnalyzeCpuPattern(List<PerformanceMetric> sorted)
        {
            var highCpuPeriods = sorted.Where(m => m.CpuUsage > _cpuThreshold).ToList();
            if (highCpuPeriods.Count < 3) return null;

            var avgCpuHigh = highCpuPeriods.Average(m => m.CpuUsage);
            var variance = CalculateVariance(highCpuPeriods.Select(m => m.CpuUsage).ToList());

            return new OptimizationRecommendation
            {
                Category = "CPU Optimization",
                Description = "CPU usage frequently exceeds optimal threshold. Consider thread pool optimization or workload balancing.",
                ImpactScore = Math.Min(1.0, avgCpuHigh - _cpuThreshold),
                ConfidenceLevel = Math.Min(1.0, highCpuPeriods.Count / 10.0),
                GeneratedAt = DateTime.UtcNow,
                Improvement = new EstimatedImprovement { CpuReduction = 0.15 }
            };
        }

        private OptimizationRecommendation? AnalyzeMemoryPattern(List<PerformanceMetric> sorted)
        {
            var highMemPeriods = sorted.Where(m => m.MemoryUsage > _memoryThreshold).ToList();
            if (highMemPeriods.Count < 3) return null;

            var avgMemHigh = highMemPeriods.Average(m => m.MemoryUsage);

            return new OptimizationRecommendation
            {
                Category = "Memory Optimization",
                Description = "Memory usage exceeds safe levels. Implement aggressive cache invalidation or increase available memory.",
                ImpactScore = Math.Min(1.0, avgMemHigh - _memoryThreshold),
                ConfidenceLevel = Math.Min(1.0, highMemPeriods.Count / 10.0),
                GeneratedAt = DateTime.UtcNow,
                Improvement = new EstimatedImprovement { MemoryReduction = 0.20 }
            };
        }

        private OptimizationRecommendation? AnalyzeThreadingPattern(List<PerformanceMetric> sorted)
        {
            var threadCounts = sorted.Select(m => (double)m.ThreadCount).ToList();
            var avgThreads = threadCounts.Average();
            var maxThreads = threadCounts.Max();

            if (maxThreads < 50) return null;

            var underutilized = sorted.Count(m => m.CpuUsage < 0.3 && m.ThreadCount > 20);
            var ratio = underutilized / (double)sorted.Count;

            if (ratio > 0.3)
            {
                return new OptimizationRecommendation
                {
                    Category = "Thread Pool Tuning",
                    Description = "Thread pool appears oversized relative to actual CPU utilization. Reduce min thread count.",
                    ImpactScore = ratio,
                    ConfidenceLevel = 0.85,
                    GeneratedAt = DateTime.UtcNow,
                    Improvement = new EstimatedImprovement { CpuReduction = 0.10 }
                };
            }

            return null;
        }

        private OptimizationRecommendation? AnalyzeIoPattern(List<PerformanceMetric> sorted)
        {
            var ioMetrics = sorted.Select(m => m.DiskIo).ToList();
            var avgIo = ioMetrics.Average();
            var maxIo = ioMetrics.Max();

            if (maxIo < 0.5) return null;

            var spikes = ioMetrics.Count(x => x > avgIo * 2);

            if (spikes > sorted.Count * 0.2)
            {
                return new OptimizationRecommendation
                {
                    Category = "I/O Optimization",
                    Description = "Disk I/O shows significant spikes. Consider implementing caching or batching strategies.",
                    ImpactScore = Math.Min(1.0, spikes / (double)sorted.Count),
                    ConfidenceLevel = 0.80,
                    GeneratedAt = DateTime.UtcNow,
                    Improvement = new EstimatedImprovement { ResponseTimeImprovement = 0.25 }
                };
            }

            return null;
        }

        private OptimizationRecommendation? AnalyzeResponseTimePattern(List<PerformanceMetric> sorted)
        {
            var responseTimes = sorted.Select(m => m.AverageResponseTime).ToList();
            var avgTime = responseTimes.Average();
            var stdDev = Math.Sqrt(responseTimes.Sum(x => Math.Pow(x - avgTime, 2)) / responseTimes.Count);

            if (avgTime < 100) return null;

            var slowRequests = sorted.Count(m => m.AverageResponseTime > avgTime + stdDev * 2);
            var ratio = slowRequests / (double)sorted.Count;

            if (ratio > 0.15)
            {
                return new OptimizationRecommendation
                {
                    Category = "Response Time Optimization",
                    Description = "Significant variance in response times detected. Implement caching or database query optimization.",
                    ImpactScore = ratio,
                    ConfidenceLevel = 0.75,
                    GeneratedAt = DateTime.UtcNow,
                    Improvement = new EstimatedImprovement { ResponseTimeImprovement = 0.30 }
                };
            }

            return null;
        }

        public void MarkRecommendationAsApplied(string recommendationId)
        {
            if (_recommendations.TryGetValue(recommendationId, out var rec))
                rec.Applied = true;
        }

        public double GetRecommendationAccuracy()
        {
            if (_recommendations.Count == 0) return 0.0;
            var applied = _recommendations.Values.Where(r => r.Applied).ToList();
            return applied.Count / (double)_recommendations.Count;
        }

        public void SetThresholds(double cpuThreshold, double memoryThreshold)
        {
            _cpuThreshold = Math.Clamp(cpuThreshold, 0.0, 1.0);
            _memoryThreshold = Math.Clamp(memoryThreshold, 0.0, 1.0);
        }

        public void EnableDisable(bool enabled) => _isEnabled = enabled;

        public List<OptimizationRecommendation> GetActiveRecommendations() =>
            _recommendations.Values.Where(r => !r.Applied).OrderByDescending(r => r.ImpactScore).ToList();

        private static double CalculateVariance(List<double> values)
        {
            if (values.Count == 0) return 0.0;
            var mean = values.Average();
            return values.Sum(x => Math.Pow(x - mean, 2)) / values.Count;
        }
    }

    public class OptimizationRecommendationEventArgs : EventArgs
    {
        public PredictiveOptimizer.OptimizationRecommendation? Recommendation { get; set; }
    }
}
