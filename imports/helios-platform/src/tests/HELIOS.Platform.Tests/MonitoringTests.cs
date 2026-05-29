using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Core.Monitoring;

namespace HELIOS.Platform.Tests
{
    public class MonitoringTests
    {
        [Fact]
        public void CpuMonitor_InitializesSuccessfully()
        {
            using var monitor = new CpuMonitor();
            Assert.NotNull(monitor);
            Assert.True(monitor.CoreCount > 0);
        }

        [Fact]
        public void CpuMonitor_StartAndStop()
        {
            using var monitor = new CpuMonitor();
            monitor.Start();
            Assert.True(monitor.IsMonitoring);
            
            System.Threading.Thread.Sleep(500);
            monitor.Stop();
            Assert.False(monitor.IsMonitoring);
        }

        [Fact]
        public void CpuMonitor_CollectsMetrics()
        {
            using var monitor = new CpuMonitor();
            monitor.Start();
            System.Threading.Thread.Sleep(1000);
            
            Assert.True(monitor.TotalCpuUsage >= 0);
            Assert.True(monitor.TotalCpuUsage <= 100);
            Assert.Equal(Environment.ProcessorCount, monitor.PerCoreCpuUsage.Length);
            monitor.Stop();
        }

        [Fact]
        public void GpuMonitor_InitializesSuccessfully()
        {
            using var monitor = new GpuMonitor();
            Assert.NotNull(monitor);
        }

        [Fact]
        public void GpuMonitor_StartAndStop()
        {
            using var monitor = new GpuMonitor();
            monitor.Start();
            Assert.True(monitor.IsMonitoring);
            
            System.Threading.Thread.Sleep(500);
            monitor.Stop();
            Assert.False(monitor.IsMonitoring);
        }

        [Fact]
        public void MemoryProfiler_InitializesSuccessfully()
        {
            using var profiler = new MemoryProfiler();
            Assert.NotNull(profiler);
        }

        [Fact]
        public void MemoryProfiler_TracksCpuMemory()
        {
            using var profiler = new MemoryProfiler();
            profiler.Start();
            System.Threading.Thread.Sleep(500);
            
            Assert.True(profiler.WorkingSetMB > 0);
            Assert.True(profiler.PrivateBytesMB >= 0);
            profiler.Stop();
        }

        [Fact]
        public void MemoryProfiler_DetectsThreshold()
        {
            using var profiler = new MemoryProfiler(256); // 256MB threshold
            profiler.Start();
            System.Threading.Thread.Sleep(500);
            
            // Most modern apps use more than 256MB
            bool isAboveThreshold = profiler.WorkingSetMB > 256;
            Assert.Equal(isAboveThreshold, profiler.IsAboveThreshold);
            profiler.Stop();
        }

        [Fact]
        public void ThermalMonitor_InitializesSuccessfully()
        {
            using var monitor = new ThermalMonitor();
            Assert.NotNull(monitor);
        }

        [Fact]
        public void PowerMonitor_InitializesSuccessfully()
        {
            using var monitor = new PowerMonitor();
            Assert.NotNull(monitor);
        }

        [Fact]
        public void PowerMonitor_TracksBattery()
        {
            using var monitor = new PowerMonitor();
            monitor.Start();
            System.Threading.Thread.Sleep(1000);
            
            // Battery info may not be available on desktop
            Assert.False(string.IsNullOrEmpty(monitor.BatteryStatus));
            monitor.Stop();
        }

        [Fact]
        public void BottleneckDetector_IdentifiesCpuBound()
        {
            using var cpuMon = new CpuMonitor();
            using var gpuMon = new GpuMonitor();
            using var memMon = new MemoryProfiler();
            using var thermalMon = new ThermalMonitor();
            using var powerMon = new PowerMonitor();

            var detector = new BottleneckDetector(cpuMon, gpuMon, memMon, thermalMon, powerMon);
            
            // Artificially set high CPU, low GPU
            cpuMon.TotalCpuUsage = 90;
            gpuMon.GpuUtilization = 30;

            var result = detector.DetectBottleneck();
            Assert.NotNull(result);
            Assert.True(result.Confidence >= 0 && result.Confidence <= 1);
        }

        [Fact]
        public void BottleneckDetector_ReturnsRecommendations()
        {
            using var cpuMon = new CpuMonitor();
            using var gpuMon = new GpuMonitor();
            using var memMon = new MemoryProfiler();
            using var thermalMon = new ThermalMonitor();
            using var powerMon = new PowerMonitor();

            var detector = new BottleneckDetector(cpuMon, gpuMon, memMon, thermalMon, powerMon);
            var result = detector.DetectBottleneck();
            
            Assert.NotNull(result.Recommendations);
            Assert.NotEmpty(result.Recommendations);
        }

        [Fact]
        public void StructuredLogger_LogsSuccessfully()
        {
            using var logger = new StructuredLogger();
            
            logger.Log("Info", "Test", "Test message");
            logger.FlushLog();
            
            var history = logger.GetLogHistory(10, "Info");
            Assert.NotEmpty(history);
        }

        [Fact]
        public void StructuredLogger_TrackOperations()
        {
            using var logger = new StructuredLogger();
            
            var correlationId = logger.StartOperation("TestOp", "Testing");
            System.Threading.Thread.Sleep(100);
            logger.EndOperation(correlationId, "TestOp", true);
            logger.FlushLog();
            
            var history = logger.GetLogHistory();
            Assert.NotEmpty(history);
        }

        [Fact]
        public void PerformanceAggregator_InitializesAll()
        {
            using var agg = new PerformanceAggregator();
            Assert.NotNull(agg);
        }

        [Fact]
        public void PerformanceAggregator_StartStop()
        {
            using var agg = new PerformanceAggregator();
            agg.Start();
            System.Threading.Thread.Sleep(500);
            
            var snapshot = agg.GetCurrentSnapshot();
            Assert.NotNull(snapshot);
            Assert.True(snapshot.OverallHealthScore >= 0);
            
            agg.Stop();
        }

        [Fact]
        public void PerformanceAggregator_GeneratesReport()
        {
            using var agg = new PerformanceAggregator();
            agg.Start();
            System.Threading.Thread.Sleep(500);
            
            string report = agg.GenerateFullReport();
            Assert.NotEmpty(report);
            Assert.Contains("CPU METRICS", report);
            Assert.Contains("MEMORY METRICS", report);
            
            agg.Stop();
        }

        [Fact]
        public void MonitoringBase_CalculatesStats()
        {
            using var monitor = new CpuMonitor();
            monitor.Start();
            System.Threading.Thread.Sleep(1000);
            
            var stats = monitor.GetDetailedStats();
            Assert.NotNull(stats);
            Assert.True(stats.Average >= 0);
            Assert.True(stats.Min <= stats.Max);
            monitor.Stop();
        }

        [Fact]
        public void Monitoring_HasLowOverhead()
        {
            // This test verifies monitoring doesn't consume excessive resources
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                using var agg = new PerformanceAggregator();
                agg.Start();
                // Minimal work
                agg.GetCurrentSnapshot();
                agg.Stop();
            }
            
            sw.Stop();
            double timePerOp = sw.Elapsed.TotalMilliseconds / 1000.0;
            
            // Each monitoring operation should take < 10ms
            Assert.True(timePerOp < 10, $"Monitoring overhead too high: {timePerOp}ms per operation");
        }

        [Fact]
        public void CpuMonitor_TopCoresIdentification()
        {
            using var monitor = new CpuMonitor();
            monitor.Start();
            System.Threading.Thread.Sleep(500);
            
            var topCores = monitor.GetTopCores(2);
            Assert.NotEmpty(topCores);
            Assert.True(topCores.Count <= 2);
            monitor.Stop();
        }

        [Fact]
        public void MemoryProfiler_ForceGC()
        {
            using var profiler = new MemoryProfiler();
            profiler.Start();
            
            long before = profiler.WorkingSetMB;
            profiler.ForceGarbageCollection();
            System.Threading.Thread.Sleep(100);
            long after = profiler.WorkingSetMB;
            
            // Memory should decrease or stay same after GC
            Assert.True(after <= before + 10); // Allow 10MB variance
            profiler.Stop();
        }
    }
}
