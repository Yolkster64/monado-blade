using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Week6.Interfaces
{
    /// <summary>
    /// Continuously monitors CPU, memory, disk, network, and GPU resources.
    /// Provides thresholds, alerts, and auto-remediation.
    /// </summary>
    public interface IResourceMonitor
    {
        /// <summary>Gets current CPU usage per service.</summary>
        Task<List<ServiceResourceUsage>> GetCpuUsageAsync();

        /// <summary>Gets current memory usage with leak detection.</summary>
        Task<MemoryUsageInfo> GetMemoryUsageAsync();

        /// <summary>Gets disk usage and predicts when it will fill.</summary>
        Task<DiskUsageInfo> GetDiskUsageAsync();

        /// <summary>Gets network bandwidth usage.</summary>
        Task<NetworkUsageInfo> GetNetworkUsageAsync();

        /// <summary>Gets GPU utilization and temperature.</summary>
        Task<GpuUsageInfo> GetGpuUsageAsync();

        /// <summary>Sets threshold for CPU alerts (default 85%).</summary>
        Task SetCpuThresholdAsync(double percentage);

        /// <summary>Sets threshold for memory alerts (default 80%).</summary>
        Task SetMemoryThresholdAsync(double percentage);

        /// <summary>Sets threshold for disk alerts (default 90%).</summary>
        Task SetDiskThresholdAsync(double percentage);

        /// <summary>Sets threshold for network alerts (default 1Gbps).</summary>
        Task SetNetworkThresholdAsync(double gbps);

        /// <summary>Gets all active alerts.</summary>
        Task<List<ResourceAlert>> GetAlertsAsync();

        /// <summary>Subscribes to resource alerts.</summary>
        void SubscribeToAlerts(Action<ResourceAlert> handler);

        /// <summary>Triggers auto-remediation if possible.</summary>
        Task<RemediationResult> AutoRemediateAsync(ResourceAlert alert);

        /// <summary>Forecasts resource capacity (when disk/memory will fill).</summary>
        Task<CapacityForecast> ForecastCapacityAsync();

        /// <summary>Gets resource usage trends (last 30 days).</summary>
        Task<ResourceTrends> GetTrendsAsync();
    }

    public class ServiceResourceUsage
    {
        public string? ServiceName { get; set; }
        public double CpuPercentage { get; set; }
        public long MemoryBytes { get; set; }
        public int ProcessId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class MemoryUsageInfo
    {
        public long TotalMemory { get; set; }
        public long UsedMemory { get; set; }
        public long AvailableMemory { get; set; }
        public double UsagePercentage { get; set; }
        public bool PossibleLeak { get; set; }
        public double LeakGrowthRate { get; set; } // MB per hour
        public long GcPressure { get; set; }
        public DateTime LastFullGc { get; set; }
        public int HeapFragmentationPercentage { get; set; }
    }

    public class DiskUsageInfo
    {
        public string? Drive { get; set; }
        public long TotalCapacity { get; set; }
        public long UsedSpace { get; set; }
        public long AvailableSpace { get; set; }
        public double UsagePercentage { get; set; }
        public double GrowthRatePerDay { get; set; } // GB per day
        public int DaysUntilFull { get; set; } // -1 if won't fill
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class NetworkUsageInfo
    {
        public double CurrentBandwidthGbps { get; set; }
        public double AverageBandwidthGbps { get; set; }
        public double PeakBandwidthGbps { get; set; }
        public List<ServiceNetworkUsage> ServiceUsage { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ServiceNetworkUsage
    {
        public string? ServiceName { get; set; }
        public double BandwidthGbps { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }

    public class GpuUsageInfo
    {
        public double UtilizationPercentage { get; set; }
        public double TemperatureCelsius { get; set; }
        public long MemoryUsedMb { get; set; }
        public long MemoryTotalMb { get; set; }
        public double FanSpeedPercentage { get; set; }
        public List<GpuProcess> ProcessesUsing { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class GpuProcess
    {
        public int ProcessId { get; set; }
        public string? ProcessName { get; set; }
        public long MemoryUsedMb { get; set; }
        public double UtilizationPercentage { get; set; }
    }

    public class ResourceAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ResourceAlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string? Message { get; set; }
        public double CurrentValue { get; set; }
        public double Threshold { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Acknowledged { get; set; }
        public string? RecommendedAction { get; set; }
    }

    public enum ResourceAlertType { HighCpu, HighMemory, DiskFull, NetworkSaturated, GpuOvertemp, MemoryLeak }
    public enum AlertSeverity { Info, Warning, Critical }

    public class RemediationResult
    {
        public bool Success { get; set; }
        public string? Action { get; set; }
        public string? Details { get; set; }
        public ResourceAlert? AlertAfterRemediation { get; set; }
    }

    public class CapacityForecast
    {
        public DateTime PredictedMemoryFullDate { get; set; } // DateTime.MaxValue if won't fill
        public DateTime PredictedDiskFullDate { get; set; }
        public int DaysUntilCritical { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
    }

    public class ResourceTrends
    {
        public List<TrendPoint> CpuTrend { get; set; } = new();
        public List<TrendPoint> MemoryTrend { get; set; } = new();
        public List<TrendPoint> DiskTrend { get; set; } = new();
        public List<TrendPoint> NetworkTrend { get; set; } = new();
    }

    public class TrendPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }
}
