using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Monitors CPU utilization per core and overall system load.
    /// </summary>
    public class CpuMonitor : MonitoringBase
    {
        private PerformanceCounter totalCpuCounter;
        private PerformanceCounter[] coreCounters;
        private ProcessThreadCollection threadCollection;
        private Process currentProcess;
        private double[] coreValues;

        public int CoreCount { get; private set; }
        public double TotalCpuUsage { get; private set; }
        public double[] PerCoreCpuUsage { get; private set; }
        public int ThreadCount { get; private set; }

        public CpuMonitor() : base("CPU Monitor", 100)
        {
            InitializeCounters();
            CoreCount = Environment.ProcessorCount;
            PerCoreCpuUsage = new double[CoreCount];
            coreValues = new double[CoreCount];
        }

        private void InitializeCounters()
        {
            try
            {
                totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                coreCounters = new PerformanceCounter[Environment.ProcessorCount];
                for (int i = 0; i < CoreCount; i++)
                {
                    coreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString(), true);
                }
                currentProcess = Process.GetCurrentProcess();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CPU Monitor initialization error: {ex.Message}");
            }
        }

        protected override void SampleMetrics(object state)
        {
            try
            {
                // Sample total CPU
                TotalCpuUsage = totalCpuCounter?.NextValue() ?? 0;
                AddMetric(TotalCpuUsage);

                // Sample per-core CPU
                for (int i = 0; i < CoreCount && i < coreCounters.Length; i++)
                {
                    if (coreCounters[i] != null)
                    {
                        PerCoreCpuUsage[i] = coreCounters[i].NextValue();
                    }
                }

                // Get thread information
                if (currentProcess != null)
                {
                    try
                    {
                        currentProcess.Refresh();
                        ThreadCount = currentProcess.Threads.Count;
                    }
                    catch { }
                }

                var stats = CalculateStats();
                OnMetricUpdated(stats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CPU Monitor sampling error: {ex.Message}");
            }
        }

        public PerformanceStats GetDetailedStats()
        {
            var stats = CalculateStats();
            return stats;
        }

        public List<(int CoreIndex, double Usage)> GetTopCores(int count = 4)
        {
            var coreUsage = PerCoreCpuUsage
                .Select((usage, index) => (index, usage))
                .OrderByDescending(x => x.usage)
                .Take(count)
                .ToList();
            return coreUsage;
        }

        public override void Dispose()
        {
            base.Dispose();
            totalCpuCounter?.Dispose();
            if (coreCounters != null)
            {
                foreach (var counter in coreCounters)
                {
                    counter?.Dispose();
                }
            }
            currentProcess?.Dispose();
        }
    }
}
