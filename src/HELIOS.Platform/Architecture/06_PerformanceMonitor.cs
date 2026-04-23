using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Performance metric for tracking latency, throughput, error rates
    /// </summary>
    public class PerformanceMetric
    {
        public string MetricName { get; set; }
        public string ServiceName { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; } // ms, ops/sec, %, etc.
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Operation performance tracking
    /// </summary>
    public class OperationMetrics
    {
        public string OperationName { get; set; }
        public long InvocationCount { get; set; }
        public long ErrorCount { get; set; }
        public double AverageLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double ThroughputOpsPerSec { get; set; }
        public double ErrorRate { get; set; } // 0-1
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Performance monitoring interface
    /// </summary>
    public interface IPerformanceMonitor
    {
        void RecordMetric(string metricName, double value, string unit);
        void RecordLatency(string operationName, double latencyMs);
        void RecordThroughput(string operationName, long operationCount, TimeSpan duration);
        void RecordError(string operationName);
        OperationMetrics GetOperationMetrics(string operationName);
        IEnumerable<PerformanceMetric> GetRecentMetrics(int limit = 100);
        IEnumerable<OperationMetrics> GetAllOperationMetrics();
        void Reset(string operationName = null);
    }

    /// <summary>
    /// Performance monitor implementation
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly Dictionary<string, OperationMetrics> _operationMetrics = new();
        private readonly List<PerformanceMetric> _metricHistory = new();
        private readonly object _lock = new();
        private readonly int _maxHistorySize = 10000;

        public void RecordMetric(string metricName, double value, string unit)
        {
            lock (_lock)
            {
                _metricHistory.Add(new PerformanceMetric
                {
                    MetricName = metricName,
                    Value = value,
                    Unit = unit,
                    RecordedAt = DateTime.UtcNow
                });

                // Trim history if too large
                if (_metricHistory.Count > _maxHistorySize)
                {
                    _metricHistory.RemoveRange(0, _metricHistory.Count - _maxHistorySize);
                }
            }
        }

        public void RecordLatency(string operationName, double latencyMs)
        {
            lock (_lock)
            {
                if (!_operationMetrics.ContainsKey(operationName))
                {
                    _operationMetrics[operationName] = new OperationMetrics { OperationName = operationName };
                }

                var metrics = _operationMetrics[operationName];
                var totalLatency = metrics.AverageLatencyMs * metrics.InvocationCount;
                metrics.InvocationCount++;
                metrics.AverageLatencyMs = (totalLatency + latencyMs) / metrics.InvocationCount;
                metrics.MinLatencyMs = metrics.MinLatencyMs == 0 ? latencyMs : Math.Min(metrics.MinLatencyMs, latencyMs);
                metrics.MaxLatencyMs = Math.Max(metrics.MaxLatencyMs, latencyMs);
                metrics.LastUpdated = DateTime.UtcNow;
            }
        }

        public void RecordThroughput(string operationName, long operationCount, TimeSpan duration)
        {
            lock (_lock)
            {
                if (!_operationMetrics.ContainsKey(operationName))
                {
                    _operationMetrics[operationName] = new OperationMetrics { OperationName = operationName };
                }

                var metrics = _operationMetrics[operationName];
                if (duration.TotalSeconds > 0)
                {
                    metrics.ThroughputOpsPerSec = operationCount / duration.TotalSeconds;
                }
                metrics.LastUpdated = DateTime.UtcNow;
            }
        }

        public void RecordError(string operationName)
        {
            lock (_lock)
            {
                if (!_operationMetrics.ContainsKey(operationName))
                {
                    _operationMetrics[operationName] = new OperationMetrics { OperationName = operationName };
                }

                var metrics = _operationMetrics[operationName];
                metrics.ErrorCount++;
                metrics.ErrorRate = (double)metrics.ErrorCount / Math.Max(metrics.InvocationCount, 1);
                metrics.LastUpdated = DateTime.UtcNow;
            }
        }

        public OperationMetrics GetOperationMetrics(string operationName)
        {
            lock (_lock)
            {
                _operationMetrics.TryGetValue(operationName, out var metrics);
                return metrics;
            }
        }

        public IEnumerable<PerformanceMetric> GetRecentMetrics(int limit = 100)
        {
            lock (_lock)
            {
                var count = Math.Min(limit, _metricHistory.Count);
                return _metricHistory.GetRange(_metricHistory.Count - count, count);
            }
        }

        public IEnumerable<OperationMetrics> GetAllOperationMetrics()
        {
            lock (_lock)
            {
                return new List<OperationMetrics>(_operationMetrics.Values);
            }
        }

        public void Reset(string operationName = null)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(operationName))
                {
                    _operationMetrics.Clear();
                    _metricHistory.Clear();
                }
                else
                {
                    _operationMetrics.Remove(operationName);
                }
            }
        }
    }

    /// <summary>
    /// Telemetry aggregator collecting all metrics
    /// </summary>
    public interface ITelemetryAggregator
    {
        void Collect(IMetricsCollector metricsCollector, IHealthChecker healthChecker, IPerformanceMonitor performanceMonitor);
        TelemetrySnapshot GetSnapshot();
    }

    /// <summary>
    /// Telemetry snapshot containing all collected data
    /// </summary>
    public class TelemetrySnapshot
    {
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
        public CpuMetrics CpuMetrics { get; set; }
        public MemoryMetrics MemoryMetrics { get; set; }
        public List<DiskMetrics> DiskMetrics { get; set; } = new();
        public List<NetworkMetrics> NetworkMetrics { get; set; } = new();
        public List<HealthCheckResult> ServiceHealth { get; set; } = new();
        public List<OperationMetrics> OperationMetrics { get; set; } = new();
    }

    /// <summary>
    /// Telemetry aggregator implementation
    /// </summary>
    public class TelemetryAggregator : ITelemetryAggregator
    {
        private TelemetrySnapshot _lastSnapshot;

        public void Collect(IMetricsCollector metricsCollector, IHealthChecker healthChecker, IPerformanceMonitor performanceMonitor)
        {
            _lastSnapshot = new TelemetrySnapshot
            {
                CollectedAt = DateTime.UtcNow,
                CpuMetrics = metricsCollector.GetCpuMetrics(),
                MemoryMetrics = metricsCollector.GetMemoryMetrics(),
                DiskMetrics = new List<DiskMetrics>(metricsCollector.GetDiskMetrics()),
                NetworkMetrics = new List<NetworkMetrics>(metricsCollector.GetNetworkMetrics()),
                OperationMetrics = new List<OperationMetrics>(performanceMonitor.GetAllOperationMetrics())
            };

            // Collect health checks asynchronously would be needed for production
        }

        public TelemetrySnapshot GetSnapshot()
        {
            return _lastSnapshot ?? new TelemetrySnapshot();
        }

        /// <summary>
        /// Export telemetry in Prometheus format
        /// </summary>
        public string ExportPrometheusFormat()
        {
            var lines = new List<string>();
            var snapshot = GetSnapshot();

            // CPU metrics
            lines.Add($"# HELP system_cpu_usage System CPU usage percentage");
            lines.Add($"# TYPE system_cpu_usage gauge");
            lines.Add($"system_cpu_usage {snapshot.CpuMetrics.OverallUsage}");

            // Memory metrics
            lines.Add($"# HELP system_memory_usage_bytes System memory usage in bytes");
            lines.Add($"# TYPE system_memory_usage_bytes gauge");
            lines.Add($"system_memory_usage_bytes {snapshot.MemoryMetrics.UsedMemory}");

            // Operation metrics
            lines.Add($"# HELP operation_latency_ms Operation latency in milliseconds");
            lines.Add($"# TYPE operation_latency_ms gauge");
            foreach (var op in snapshot.OperationMetrics)
            {
                lines.Add($"operation_latency_ms{{operation=\"{op.OperationName}\"}} {op.AverageLatencyMs}");
            }

            return string.Join("\n", lines);
        }
    }
}
