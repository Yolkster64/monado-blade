using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI
{
    /// <summary>
    /// Detects anomalies in system behavior and provides early warnings for potential issues.
    /// </summary>
    public class AnomalyDetector
    {
        private readonly List<DataPoint> _dataPoints = new();
        private readonly Dictionary<string, Baseline> _baselines = new();
        private readonly List<AnomalyEvent> _detectedAnomalies = new();
        private double _sensitivityThreshold = 2.0; // Standard deviations
        private bool _isEnabled = true;
        private const int MinDataPointsForBaseline = 30;

        public class DataPoint
        {
            public DateTime Timestamp { get; set; }
            public string MetricName { get; set; } = string.Empty;
            public double Value { get; set; }
            public Dictionary<string, double>? Context { get; set; }
        }

        public class Baseline
        {
            public string MetricName { get; set; } = string.Empty;
            public double Mean { get; set; }
            public double StdDev { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public DateTime CalculatedAt { get; set; }
            public int DataPointsUsed { get; set; }
        }

        public class AnomalyEvent
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public DateTime Timestamp { get; set; }
            public string MetricName { get; set; } = string.Empty;
            public double ObservedValue { get; set; }
            public double ExpectedValue { get; set; }
            public double Deviation { get; set; } // Standard deviations
            public AnomalyType Type { get; set; }
            public double Severity { get; set; } // 0.0 to 1.0
            public bool RootCauseAnalyzed { get; set; }
            public List<string> SuggestedCauses { get; set; } = new();
        }

        public enum AnomalyType
        {
            Statistical,
            Behavioral,
            Outlier,
            TrendBreak,
            SpikeDetected
        }

        public void RecordMetric(string metricName, double value, Dictionary<string, double>? context = null)
        {
            if (!_isEnabled) return;

            lock (_dataPoints)
            {
                _dataPoints.Add(new DataPoint
                {
                    Timestamp = DateTime.UtcNow,
                    MetricName = metricName,
                    Value = value,
                    Context = context
                });

                if (_dataPoints.Count > 5000)
                    _dataPoints.RemoveAt(0);
            }
        }

        public async Task CalculateBaselines()
        {
            if (!_isEnabled || _dataPoints.Count < MinDataPointsForBaseline)
                return;

            await Task.Run(() =>
            {
                lock (_dataPoints)
                {
                    var grouped = _dataPoints.GroupBy(p => p.MetricName);

                    foreach (var group in grouped)
                    {
                        var values = group.Select(p => p.Value).ToList();
                        if (values.Count < MinDataPointsForBaseline)
                            continue;

                        var mean = values.Average();
                        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
                        var stdDev = Math.Sqrt(variance);

                        _baselines[group.Key] = new Baseline
                        {
                            MetricName = group.Key,
                            Mean = mean,
                            StdDev = stdDev,
                            Min = values.Min(),
                            Max = values.Max(),
                            CalculatedAt = DateTime.UtcNow,
                            DataPointsUsed = values.Count
                        };
                    }
                }
            });
        }

        public async Task<List<AnomalyEvent>> DetectAnomalies()
        {
            var anomalies = new List<AnomalyEvent>();

            if (!_isEnabled || _baselines.Count == 0)
                return anomalies;

            await Task.Run(() =>
            {
                lock (_dataPoints)
                {
                    var recentPoints = _dataPoints.Where(p => p.Timestamp > DateTime.UtcNow.AddMinutes(-10)).ToList();

                    foreach (var point in recentPoints)
                    {
                        if (_baselines.TryGetValue(point.MetricName, out var baseline))
                        {
                            var deviation = (point.Value - baseline.Mean) / (baseline.StdDev + 0.0001);

                            if (Math.Abs(deviation) > _sensitivityThreshold)
                            {
                                var anomaly = new AnomalyEvent
                                {
                                    Timestamp = point.Timestamp,
                                    MetricName = point.MetricName,
                                    ObservedValue = point.Value,
                                    ExpectedValue = baseline.Mean,
                                    Deviation = Math.Abs(deviation),
                                    Type = ClassifyAnomaly(point, baseline),
                                    Severity = CalculateSeverity(Math.Abs(deviation))
                                };

                                anomaly.SuggestedCauses = AnalyzeRootCause(anomaly, point, baseline);
                                anomaly.RootCauseAnalyzed = true;

                                anomalies.Add(anomaly);
                                _detectedAnomalies.Add(anomaly);
                            }
                        }
                    }
                }
            });

            return anomalies;
        }

        private AnomalyType ClassifyAnomaly(DataPoint point, Baseline baseline)
        {
            var deviation = point.Value - baseline.Mean;

            if (Math.Abs(deviation) > baseline.StdDev * 4)
                return AnomalyType.Outlier;

            if (deviation > baseline.StdDev * 3)
                return AnomalyType.SpikeDetected;

            return AnomalyType.Statistical;
        }

        private double CalculateSeverity(double deviation)
        {
            if (deviation < 2.0) return 0.2;
            if (deviation < 3.0) return 0.5;
            if (deviation < 4.0) return 0.7;
            return 0.95;
        }

        private List<string> AnalyzeRootCause(AnomalyEvent anomaly, DataPoint point, Baseline baseline)
        {
            var causes = new List<string>();

            if (point.MetricName.Contains("CPU"))
            {
                if (anomaly.ObservedValue > baseline.Mean * 1.5)
                    causes.Add("Possible CPU-intensive operation running");
                if (anomaly.ObservedValue > baseline.Mean * 2)
                    causes.Add("Critical CPU spike - check for infinite loops or heavy computation");
            }

            if (point.MetricName.Contains("Memory"))
            {
                if (anomaly.ObservedValue > baseline.Mean * 1.5)
                    causes.Add("Memory leak suspected - check for unreleased resources");
                if (anomaly.ObservedValue > baseline.Max)
                    causes.Add("Memory limit exceeded - immediate action recommended");
            }

            if (point.MetricName.Contains("Response"))
            {
                if (anomaly.ObservedValue > baseline.Mean * 2)
                    causes.Add("Database query performance degradation");
                causes.Add("Check for network latency issues");
            }

            if (point.MetricName.Contains("Error"))
            {
                causes.Add("Recent deployment may have introduced bugs");
                causes.Add("External service dependency failure");
            }

            if (point.Context != null && point.Context.ContainsKey("user_count"))
            {
                var userCount = point.Context["user_count"];
                if (userCount > 1000)
                    causes.Add($"High user load ({userCount} concurrent users)");
            }

            if (causes.Count == 0)
                causes.Add("Root cause unknown - manual investigation required");

            return causes;
        }

        public List<AnomalyEvent> GetRecentAnomalies(int count = 10)
        {
            lock (_dataPoints)
            {
                return _detectedAnomalies
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count)
                    .ToList();
            }
        }

        public List<AnomalyEvent> GetSevereAnomalies(double minSeverity = 0.7)
        {
            lock (_dataPoints)
            {
                return _detectedAnomalies
                    .Where(a => a.Severity >= minSeverity)
                    .OrderByDescending(a => a.Severity)
                    .ToList();
            }
        }

        public AnomalyReport GenerateReport()
        {
            lock (_dataPoints)
            {
                var allAnomalies = _detectedAnomalies;
                var recentAnomalies = allAnomalies.Where(a => a.Timestamp > DateTime.UtcNow.AddHours(-1)).ToList();

                var report = new AnomalyReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    TotalAnomaliesDetected = allAnomalies.Count,
                    AnomaliesInLastHour = recentAnomalies.Count,
                    SevereAnomalies = allAnomalies.Count(a => a.Severity > 0.7),
                    AnomaliesByType = allAnomalies.GroupBy(a => a.Type)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    MostAffectedMetrics = allAnomalies.GroupBy(a => a.MetricName)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    HealthScore = CalculateHealthScore()
                };

                return report;
            }
        }

        private double CalculateHealthScore()
        {
            var recentAnomalies = _detectedAnomalies.Where(a => a.Timestamp > DateTime.UtcNow.AddHours(-1)).ToList();

            if (recentAnomalies.Count == 0)
                return 1.0;

            var severitySum = recentAnomalies.Sum(a => a.Severity);
            var avgSeverity = severitySum / recentAnomalies.Count;

            return Math.Max(0.0, 1.0 - avgSeverity);
        }

        public void SetSensitivity(double stdDevThreshold)
        {
            _sensitivityThreshold = Math.Clamp(stdDevThreshold, 1.0, 5.0);
        }

        public void ClearAnomalies()
        {
            lock (_dataPoints)
            {
                _detectedAnomalies.Clear();
            }
        }

        public void EnableDisable(bool enabled) => _isEnabled = enabled;

        public class AnomalyReport
        {
            public DateTime GeneratedAt { get; set; }
            public int TotalAnomaliesDetected { get; set; }
            public int AnomaliesInLastHour { get; set; }
            public int SevereAnomalies { get; set; }
            public Dictionary<string, int> AnomaliesByType { get; set; } = new();
            public Dictionary<string, int> MostAffectedMetrics { get; set; } = new();
            public double HealthScore { get; set; }
        }
    }
}
