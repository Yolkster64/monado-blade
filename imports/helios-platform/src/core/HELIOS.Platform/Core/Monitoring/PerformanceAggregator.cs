using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Aggregates all performance metrics and provides unified diagnostics.
    /// </summary>
    public class PerformanceAggregator : IDisposable
    {
        private CpuMonitor cpuMonitor;
        private GpuMonitor gpuMonitor;
        private MemoryProfiler memoryProfiler;
        private ThermalMonitor thermalMonitor;
        private PowerMonitor powerMonitor;
        private BottleneckDetector bottleneckDetector;
        private StructuredLogger logger;
        private bool isMonitoring;

        public event EventHandler<PerformanceSnapshotEventArgs> PerformanceUpdated;

        public class PerformanceSnapshot
        {
            public DateTime Timestamp { get; set; }
            public CpuMetrics Cpu { get; set; }
            public GpuMetrics Gpu { get; set; }
            public MemoryMetrics Memory { get; set; }
            public ThermalMetrics Thermal { get; set; }
            public PowerMetrics Power { get; set; }
            public BottleneckDetector.BottleneckResult Bottleneck { get; set; }
            public double OverallHealthScore { get; set; }
        }

        public class CpuMetrics
        {
            public double TotalUsage { get; set; }
            public double[] PerCoreUsage { get; set; }
            public int ThreadCount { get; set; }
            public double AverageUsage { get; set; }
        }

        public class GpuMetrics
        {
            public double Utilization { get; set; }
            public ulong UsedMemoryMB { get; set; }
            public ulong TotalMemoryMB { get; set; }
            public double Temperature { get; set; }
        }

        public class MemoryMetrics
        {
            public long WorkingSetMB { get; set; }
            public long ManagedMemoryMB { get; set; }
            public long NativeMemoryMB { get; set; }
            public long GcCollectionCount { get; set; }
        }

        public class ThermalMetrics
        {
            public double CpuTemperature { get; set; }
            public double GpuTemperature { get; set; }
            public bool IsThrottling { get; set; }
        }

        public class PowerMetrics
        {
            public double CpuPowerWatts { get; set; }
            public double GpuPowerWatts { get; set; }
            public double TotalPowerWatts { get; set; }
            public int BatteryPercentage { get; set; }
            public string BatteryStatus { get; set; }
        }

        public PerformanceAggregator()
        {
            InitializeMonitors();
        }

        private void InitializeMonitors()
        {
            cpuMonitor = new CpuMonitor();
            gpuMonitor = new GpuMonitor();
            memoryProfiler = new MemoryProfiler();
            thermalMonitor = new ThermalMonitor();
            powerMonitor = new PowerMonitor();
            bottleneckDetector = new BottleneckDetector(cpuMonitor, gpuMonitor, 
                memoryProfiler, thermalMonitor, powerMonitor);
            logger = new StructuredLogger();
        }

        public void Start()
        {
            if (isMonitoring) return;

            isMonitoring = true;
            cpuMonitor.Start();
            gpuMonitor.Start();
            memoryProfiler.Start();
            thermalMonitor.Start();
            powerMonitor.Start();

            logger.Log("Info", "Aggregator", "Performance monitoring started");
        }

        public void Stop()
        {
            if (!isMonitoring) return;

            isMonitoring = false;
            cpuMonitor.Stop();
            gpuMonitor.Stop();
            memoryProfiler.Stop();
            thermalMonitor.Stop();
            powerMonitor.Stop();

            logger.Log("Info", "Aggregator", "Performance monitoring stopped");
            logger.FlushLog();
        }

        public PerformanceSnapshot GetCurrentSnapshot()
        {
            return new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                Cpu = new CpuMetrics
                {
                    TotalUsage = cpuMonitor.TotalCpuUsage,
                    PerCoreUsage = cpuMonitor.PerCoreCpuUsage,
                    ThreadCount = cpuMonitor.ThreadCount,
                    AverageUsage = cpuMonitor.TotalCpuUsage
                },
                Gpu = new GpuMetrics
                {
                    Utilization = gpuMonitor.GpuUtilization,
                    UsedMemoryMB = gpuMonitor.UsedMemoryMB,
                    TotalMemoryMB = gpuMonitor.DedicatedMemoryMB,
                    Temperature = gpuMonitor.GpuTemperature
                },
                Memory = new MemoryMetrics
                {
                    WorkingSetMB = memoryProfiler.WorkingSetMB,
                    ManagedMemoryMB = memoryProfiler.ManagedMemoryMB,
                    NativeMemoryMB = memoryProfiler.NativeMemoryMB,
                    GcCollectionCount = memoryProfiler.GcCollectionCount
                },
                Thermal = new ThermalMetrics
                {
                    CpuTemperature = thermalMonitor.CpuTemperatureCelsius,
                    GpuTemperature = thermalMonitor.GpuTemperatureCelsius,
                    IsThrottling = thermalMonitor.IsThrottling
                },
                Power = new PowerMetrics
                {
                    CpuPowerWatts = powerMonitor.CpuPowerWatts,
                    GpuPowerWatts = powerMonitor.GpuPowerWatts,
                    TotalPowerWatts = powerMonitor.TotalPowerWatts,
                    BatteryPercentage = powerMonitor.BatteryPercentage,
                    BatteryStatus = powerMonitor.BatteryStatus
                },
                Bottleneck = bottleneckDetector.DetectBottleneck(),
                OverallHealthScore = CalculateHealthScore()
            };
        }

        private double CalculateHealthScore()
        {
            // Score from 0 (critical) to 100 (excellent)
            double score = 100.0;

            // CPU impact (-5 per 10% over 70%)
            if (cpuMonitor.TotalCpuUsage > 70)
                score -= (cpuMonitor.TotalCpuUsage - 70) / 10.0 * 5;

            // GPU impact (-5 per 10% over 70%)
            if (gpuMonitor.GpuUtilization > 70)
                score -= (gpuMonitor.GpuUtilization - 70) / 10.0 * 5;

            // Memory impact (-10 per 100MB over 512MB)
            if (memoryProfiler.WorkingSetMB > 512)
                score -= Math.Min(30, (memoryProfiler.WorkingSetMB - 512) / 100.0);

            // Thermal impact (-10 per 5°C over 60°C)
            double maxTemp = Math.Max(thermalMonitor.CpuTemperatureCelsius, 
                                     thermalMonitor.GpuTemperatureCelsius);
            if (maxTemp > 60)
                score -= Math.Min(40, (maxTemp - 60) / 5.0 * 10);

            return Math.Max(0, Math.Min(100, score));
        }

        public List<PerformanceSnapshot> GetPerformanceHistory(int lastN = 60)
        {
            var history = new List<PerformanceSnapshot>();
            // This would be extended with actual history tracking
            history.Add(GetCurrentSnapshot());
            return history;
        }

        public string GenerateFullReport()
        {
            var snapshot = GetCurrentSnapshot();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║           PERFORMANCE DIAGNOSTICS REPORT                      ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Timestamp: {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Health Score: {snapshot.OverallHealthScore:F1}/100");
            sb.AppendLine();

            // CPU Section
            sb.AppendLine("─── CPU METRICS ───");
            sb.AppendLine($"  Total Usage:     {snapshot.Cpu.TotalUsage:F1}%");
            sb.AppendLine($"  Thread Count:    {snapshot.Cpu.ThreadCount}");
            sb.AppendLine("  Per-Core Usage:");
            for (int i = 0; i < snapshot.Cpu.PerCoreUsage.Length && i < 4; i++)
                sb.AppendLine($"    Core {i}: {snapshot.Cpu.PerCoreUsage[i]:F1}%");
            sb.AppendLine();

            // Memory Section
            sb.AppendLine("─── MEMORY METRICS ───");
            sb.AppendLine($"  Working Set:     {snapshot.Memory.WorkingSetMB} MB");
            sb.AppendLine($"  Managed Memory:  {snapshot.Memory.ManagedMemoryMB} MB");
            sb.AppendLine($"  Native Memory:   {snapshot.Memory.NativeMemoryMB} MB");
            sb.AppendLine($"  GC Collections:  {snapshot.Memory.GcCollectionCount}");
            sb.AppendLine();

            // GPU Section
            sb.AppendLine("─── GPU METRICS ───");
            sb.AppendLine($"  Utilization:     {snapshot.Gpu.Utilization:F1}%");
            sb.AppendLine($"  VRAM Used:       {snapshot.Gpu.UsedMemoryMB} MB");
            sb.AppendLine($"  VRAM Total:      {snapshot.Gpu.TotalMemoryMB} MB");
            sb.AppendLine($"  Temperature:     {snapshot.Gpu.Temperature:F1}°C");
            sb.AppendLine();

            // Thermal Section
            sb.AppendLine("─── THERMAL METRICS ───");
            sb.AppendLine($"  CPU Temp:        {snapshot.Thermal.CpuTemperature:F1}°C");
            sb.AppendLine($"  GPU Temp:        {snapshot.Thermal.GpuTemperature:F1}°C");
            sb.AppendLine($"  Throttling:      {(snapshot.Thermal.IsThrottling ? "YES ⚠️" : "NO")}");
            sb.AppendLine();

            // Power Section
            sb.AppendLine("─── POWER METRICS ───");
            sb.AppendLine($"  CPU Power:       {snapshot.Power.CpuPowerWatts:F1}W");
            sb.AppendLine($"  GPU Power:       {snapshot.Power.GpuPowerWatts:F1}W");
            sb.AppendLine($"  Total Power:     {snapshot.Power.TotalPowerWatts:F1}W");
            sb.AppendLine($"  Battery:         {snapshot.Power.BatteryStatus} ({snapshot.Power.BatteryPercentage}%)");
            sb.AppendLine();

            // Bottleneck Analysis
            sb.AppendLine("─── BOTTLENECK ANALYSIS ───");
            sb.AppendLine($"  Type:            {snapshot.Bottleneck.Type}");
            sb.AppendLine($"  Confidence:      {snapshot.Bottleneck.Confidence:P}");
            sb.AppendLine($"  Description:     {snapshot.Bottleneck.Description}");
            sb.AppendLine();

            return sb.ToString();
        }

        public void Dispose()
        {
            Stop();
            cpuMonitor?.Dispose();
            gpuMonitor?.Dispose();
            memoryProfiler?.Dispose();
            thermalMonitor?.Dispose();
            powerMonitor?.Dispose();
            logger?.Dispose();
        }
    }

    public class PerformanceSnapshotEventArgs : EventArgs
    {
        public PerformanceAggregator.PerformanceSnapshot Snapshot { get; set; }
    }
}
