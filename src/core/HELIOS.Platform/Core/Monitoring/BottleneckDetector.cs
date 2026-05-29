using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Detects performance bottlenecks by analyzing CPU, GPU, and memory metrics.
    /// </summary>
    public class BottleneckDetector
    {
        public class BottleneckResult
        {
            public BottleneckType Type { get; set; }
            public double Confidence { get; set; }
            public string Description { get; set; }
            public List<string> Recommendations { get; set; } = new List<string>();
            public DateTime DetectedAt { get; set; } = DateTime.Now;
        }

        public enum BottleneckType
        {
            None,
            CpuBound,
            GpuBound,
            MemoryBound,
            IoBound,
            Thermal,
            PowerLimited
        }

        private CpuMonitor cpuMonitor;
        private GpuMonitor gpuMonitor;
        private MemoryProfiler memoryProfiler;
        private ThermalMonitor thermalMonitor;
        private PowerMonitor powerMonitor;

        public BottleneckDetector(CpuMonitor cpu, GpuMonitor gpu, MemoryProfiler memory, 
                                  ThermalMonitor thermal, PowerMonitor power)
        {
            cpuMonitor = cpu;
            gpuMonitor = gpu;
            memoryProfiler = memory;
            thermalMonitor = thermal;
            powerMonitor = power;
        }

        public BottleneckResult DetectBottleneck()
        {
            // Analyze current metrics
            double cpuUsage = cpuMonitor?.TotalCpuUsage ?? 0;
            double gpuUsage = gpuMonitor?.GpuUtilization ?? 0;
            long memoryUsage = memoryProfiler?.WorkingSetMB ?? 0;
            double cpuTemp = thermalMonitor?.CpuTemperatureCelsius ?? 0;
            bool isThrottling = thermalMonitor?.IsThrottling ?? false;

            var result = new BottleneckResult();

            // Check thermal issues first
            if (isThrottling || cpuTemp > 85)
            {
                result.Type = BottleneckType.Thermal;
                result.Confidence = Math.Min(1.0, (cpuTemp / 100.0));
                result.Description = "System is thermally throttling";
                result.Recommendations.AddRange(new[]
                {
                    "Check CPU/GPU cooler for dust buildup",
                    "Ensure adequate case airflow",
                    "Consider thermal paste replacement",
                    "Close background applications"
                });
                return result;
            }

            // Check for CPU-bound bottleneck
            if (cpuUsage > 80 && gpuUsage < 60)
            {
                result.Type = BottleneckType.CpuBound;
                result.Confidence = (cpuUsage - 80) / 20.0;
                result.Description = "CPU is the limiting factor";
                result.Recommendations.AddRange(new[]
                {
                    "Profile CPU-heavy functions",
                    "Parallelize CPU work across cores",
                    "Optimize algorithmic complexity",
                    "Consider upgrading CPU or reducing quality settings"
                });
                return result;
            }

            // Check for GPU-bound bottleneck
            if (gpuUsage > 85 && cpuUsage < 60)
            {
                result.Type = BottleneckType.GpuBound;
                result.Confidence = (gpuUsage - 85) / 15.0;
                result.Description = "GPU is the limiting factor";
                result.Recommendations.AddRange(new[]
                {
                    "Reduce render resolution or quality settings",
                    "Optimize shader performance",
                    "Reduce draw call count",
                    "Use lower quality textures or effects",
                    "Consider GPU upgrade"
                });
                return result;
            }

            // Check for memory-bound bottleneck
            if (memoryUsage > 1024 * 0.8) // 80% of 1GB threshold
            {
                result.Type = BottleneckType.MemoryBound;
                result.Confidence = Math.Min(1.0, memoryUsage / (1024.0 * 1.2));
                result.Description = "High memory usage limiting performance";
                result.Recommendations.AddRange(new[]
                {
                    "Close background applications",
                    "Check for memory leaks",
                    "Reduce cached object count",
                    "Optimize data structures",
                    "Consider increasing system RAM"
                });
                return result;
            }

            // Check for balanced usage (good scaling)
            if (cpuUsage > 60 && gpuUsage > 60 && cpuUsage < 95 && gpuUsage < 95)
            {
                result.Type = BottleneckType.None;
                result.Confidence = 0.9;
                result.Description = "System is well-balanced and scaling efficiently";
                result.Recommendations.AddRange(new[]
                {
                    "Performance is optimal",
                    "Monitor for thermal limits",
                    "Continue with current settings"
                });
                return result;
            }

            // Default: Idle or low utilization
            result.Type = BottleneckType.None;
            result.Confidence = 0.0;
            result.Description = "System is idle or lightly loaded";
            result.Recommendations.Add("No bottleneck detected");
            return result;
        }

        public List<BottleneckResult> DetectMultipleBottlenecks()
        {
            var bottlenecks = new List<BottleneckResult>();
            var primary = DetectBottleneck();
            bottlenecks.Add(primary);

            // Check for secondary bottlenecks
            double cpuUsage = cpuMonitor?.TotalCpuUsage ?? 0;
            long memoryUsage = memoryProfiler?.WorkingSetMB ?? 0;

            if (cpuUsage > 85)
            {
                var cpuBottleneck = new BottleneckResult
                {
                    Type = BottleneckType.CpuBound,
                    Confidence = (cpuUsage - 70) / 30.0,
                    Description = "Secondary CPU bottleneck detected",
                    DetectedAt = DateTime.Now
                };
                bottlenecks.Add(cpuBottleneck);
            }

            if (memoryUsage > 512)
            {
                var memBottleneck = new BottleneckResult
                {
                    Type = BottleneckType.MemoryBound,
                    Confidence = memoryUsage / (1024.0 * 1.5),
                    Description = "Secondary memory pressure detected",
                    DetectedAt = DateTime.Now
                };
                bottlenecks.Add(memBottleneck);
            }

            return bottlenecks;
        }

        public string GetDetailedReport()
        {
            var bottleneck = DetectBottleneck();
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== Performance Bottleneck Analysis ===");
            report.AppendLine($"Bottleneck Type: {bottleneck.Type}");
            report.AppendLine($"Confidence: {bottleneck.Confidence:P}");
            report.AppendLine($"Description: {bottleneck.Description}");
            report.AppendLine();
            report.AppendLine("Recommendations:");
            foreach (var rec in bottleneck.Recommendations)
            {
                report.AppendLine($"  • {rec}");
            }
            report.AppendLine();
            report.AppendLine("Current Metrics:");
            report.AppendLine($"  CPU Usage: {cpuMonitor?.TotalCpuUsage ?? 0:F1}%");
            report.AppendLine($"  GPU Usage: {gpuMonitor?.GpuUtilization ?? 0:F1}%");
            report.AppendLine($"  Memory: {memoryProfiler?.WorkingSetMB ?? 0} MB");
            report.AppendLine($"  CPU Temp: {thermalMonitor?.CpuTemperatureCelsius ?? 0:F1}°C");
            report.AppendLine($"  Total Power: {powerMonitor?.TotalPowerWatts ?? 0:F1}W");

            return report.ToString();
        }
    }
}
