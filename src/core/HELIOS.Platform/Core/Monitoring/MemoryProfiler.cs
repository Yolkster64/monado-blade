using System;
using System.Diagnostics;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Monitors memory usage including managed and native allocations.
    /// </summary>
    public class MemoryProfiler : MonitoringBase
    {
        private Process currentProcess;
        private PerformanceCounter privateBytesCounter;
        private long lastCollectionCount;
        private long gcCollections;

        public long WorkingSetMB { get; private set; }
        public long PrivateBytesMB { get; private set; }
        public long ManagedMemoryMB { get; private set; }
        public long NativeMemoryMB { get; private set; }
        public long GcCollectionCount { get; private set; }
        public double GcPauseTimeMs { get; private set; }
        public bool IsAboveThreshold { get; private set; }

        private readonly long MemoryThresholdMB = 1024; // 1GB default threshold

        public MemoryProfiler(long thresholdMb = 1024) : base("Memory Profiler", 500)
        {
            MemoryThresholdMB = thresholdMb;
            InitializeCounters();
        }

        private void InitializeCounters()
        {
            try
            {
                currentProcess = Process.GetCurrentProcess();
                privateBytesCounter = new PerformanceCounter(
                    "Process", "Private Bytes", currentProcess.ProcessName, true);
                lastCollectionCount = GC.CollectionCount(2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Memory Profiler initialization error: {ex.Message}");
            }
        }

        protected override void SampleMetrics(object state)
        {
            try
            {
                if (currentProcess != null)
                {
                    currentProcess.Refresh();
                    WorkingSetMB = currentProcess.WorkingSet64 / (1024 * 1024);
                    PrivateBytesMB = (long)(privateBytesCounter?.NextValue() ?? 0) / (1024 * 1024);
                }

                // Get managed memory info
                long totalMemory = GC.GetTotalMemory(false);
                ManagedMemoryMB = totalMemory / (1024 * 1024);
                NativeMemoryMB = (WorkingSetMB > ManagedMemoryMB) ? 
                    (WorkingSetMB - ManagedMemoryMB) : 0;

                // Track GC collections
                long currentCollectionCount = GC.CollectionCount(2);
                if (currentCollectionCount > lastCollectionCount)
                {
                    gcCollections += (currentCollectionCount - lastCollectionCount);
                    lastCollectionCount = currentCollectionCount;
                }
                GcCollectionCount = gcCollections;

                // Check threshold
                IsAboveThreshold = WorkingSetMB > MemoryThresholdMB;

                AddMetric(WorkingSetMB);
                var stats = CalculateStats();
                OnMetricUpdated(stats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Memory Profiler sampling error: {ex.Message}");
            }
        }

        public void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public PerformanceStats GetDetailedStats()
        {
            return CalculateStats();
        }

        public override void Dispose()
        {
            base.Dispose();
            privateBytesCounter?.Dispose();
            currentProcess?.Dispose();
        }
    }
}
