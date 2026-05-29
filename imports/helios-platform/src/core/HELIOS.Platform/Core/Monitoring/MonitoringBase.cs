using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Base class for all monitoring components providing common infrastructure.
    /// </summary>
    public abstract class MonitoringBase : IDisposable
    {
        protected readonly string MonitorName;
        protected readonly int SamplingIntervalMs;
        protected bool IsMonitoring;
        protected Timer SamplingTimer;
        protected List<double> MetricHistory;
        protected object LockObject = new object();

        public event EventHandler<PerformanceEventArgs> MetricUpdated;

        protected MonitoringBase(string name, int samplingIntervalMs = 100)
        {
            MonitorName = name;
            SamplingIntervalMs = samplingIntervalMs;
            IsMonitoring = false;
            MetricHistory = new List<double>();
        }

        public virtual void Start()
        {
            if (IsMonitoring) return;
            
            IsMonitoring = true;
            SamplingTimer = new Timer(SampleMetrics, null, 0, SamplingIntervalMs);
        }

        public virtual void Stop()
        {
            IsMonitoring = false;
            SamplingTimer?.Dispose();
            SamplingTimer = null;
        }

        protected abstract void SampleMetrics(object state);

        protected void AddMetric(double value)
        {
            lock (LockObject)
            {
                MetricHistory.Add(value);
                if (MetricHistory.Count > 600) // Keep last ~60 seconds at 100ms intervals
                    MetricHistory.RemoveAt(0);
            }
        }

        protected double GetCurrentMetric()
        {
            lock (LockObject)
            {
                return MetricHistory.Count > 0 ? MetricHistory[MetricHistory.Count - 1] : 0;
            }
        }

        protected PerformanceStats CalculateStats()
        {
            lock (LockObject)
            {
                if (MetricHistory.Count == 0)
                    return new PerformanceStats();

                double sum = 0, min = double.MaxValue, max = double.MinValue;
                foreach (var value in MetricHistory)
                {
                    sum += value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }

                return new PerformanceStats
                {
                    Current = MetricHistory[MetricHistory.Count - 1],
                    Average = sum / MetricHistory.Count,
                    Min = min,
                    Max = max,
                    SampleCount = MetricHistory.Count
                };
            }
        }

        protected void OnMetricUpdated(PerformanceStats stats)
        {
            MetricUpdated?.Invoke(this, new PerformanceEventArgs { Stats = stats });
        }

        public virtual void Dispose()
        {
            Stop();
            MetricHistory?.Clear();
            SamplingTimer?.Dispose();
        }
    }

    public class PerformanceStats
    {
        public double Current { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int SampleCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class PerformanceEventArgs : EventArgs
    {
        public PerformanceStats Stats { get; set; }
    }
}
