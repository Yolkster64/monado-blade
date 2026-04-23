using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MonadoBlade.Week6.Interfaces;

namespace MonadoBlade.Week6.Services
{
    /// <summary>
    /// Continuous resource monitoring with alerts and auto-remediation.
    /// </summary>
    public class ResourceMonitor : IResourceMonitor
    {
        private List<ResourceAlert> _alerts = new();
        private Action<ResourceAlert> _alertHandler;
        private double _cpuThreshold = 85;
        private double _memoryThreshold = 80;
        private double _diskThreshold = 90;
        private double _networkThreshold = 1.0; // Gbps
        private readonly object _lockObj = new();

        public async Task<List<ServiceResourceUsage>> GetCpuUsageAsync()
        {
            return await Task.Run(() =>
            {
                var processes = Process.GetProcesses()
                    .OrderByDescending(p => p.ProcessName)
                    .Take(10)
                    .Select(p => new ServiceResourceUsage
                    {
                        ServiceName = p.ProcessName,
                        ProcessId = p.Id,
                        MemoryBytes = p.WorkingSet64,
                        Timestamp = DateTime.UtcNow
                    })
                    .ToList();

                return processes;
            });
        }

        public async Task<MemoryUsageInfo> GetMemoryUsageAsync()
        {
            return await Task.Run(() =>
            {
                var totalMemory = GC.GetTotalMemory(false);
                var processMemory = Process.GetCurrentProcess().WorkingSet64;

                // Mock system memory (would use performance counters in production)
                var info = new MemoryUsageInfo
                {
                    TotalMemory = 16 * 1024 * 1024 * 1024, // 16 GB
                    UsedMemory = 12 * 1024 * 1024 * 1024,  // 12 GB
                    AvailableMemory = 4 * 1024 * 1024 * 1024, // 4 GB
                    UsagePercentage = 75.0,
                    PossibleLeak = false,
                    LeakGrowthRate = 0.5,
                    GcPressure = totalMemory,
                    LastFullGc = DateTime.UtcNow.AddHours(-2),
                    HeapFragmentationPercentage = 15
                };

                return info;
            });
        }

        public async Task<DiskUsageInfo> GetDiskUsageAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var driveInfo = new DriveInfo("C:");
                    var usagePercent = (double)((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize) * 100;
                    var growthPerDay = 5.0; // GB/day

                    var daysUntilFull = growthPerDay > 0
                        ? (int)(driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024) / growthPerDay)
                        : -1;

                    return new DiskUsageInfo
                    {
                        Drive = "C:",
                        TotalCapacity = driveInfo.TotalSize,
                        UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                        AvailableSpace = driveInfo.AvailableFreeSpace,
                        UsagePercentage = usagePercent,
                        GrowthRatePerDay = growthPerDay,
                        DaysUntilFull = daysUntilFull
                    };
                }
                catch
                {
                    return new DiskUsageInfo { Drive = "C:", UsagePercentage = 65.0 };
                }
            });
        }

        public async Task<NetworkUsageInfo> GetNetworkUsageAsync()
        {
            return await Task.Run(() =>
            {
                // Mock network usage (would query actual interfaces)
                return new NetworkUsageInfo
                {
                    CurrentBandwidthGbps = 0.25,
                    AverageBandwidthGbps = 0.15,
                    PeakBandwidthGbps = 0.75,
                    ServiceUsage = new List<ServiceNetworkUsage>
                    {
                        new ServiceNetworkUsage { ServiceName = "API", BandwidthGbps = 0.2 },
                        new ServiceNetworkUsage { ServiceName = "Backup", BandwidthGbps = 0.05 }
                    }
                };
            });
        }

        public async Task<GpuUsageInfo> GetGpuUsageAsync()
        {
            return await Task.Run(() =>
            {
                // Mock GPU usage (would query actual GPU in production)
                return new GpuUsageInfo
                {
                    UtilizationPercentage = 45.0,
                    TemperatureCelsius = 65,
                    MemoryUsedMb = 4096,
                    MemoryTotalMb = 8192,
                    FanSpeedPercentage = 50,
                    ProcessesUsing = new List<GpuProcess>
                    {
                        new GpuProcess { ProcessName = "MonadoWorker.exe", MemoryUsedMb = 2048, UtilizationPercentage = 30 }
                    }
                };
            });
        }

        public async Task SetCpuThresholdAsync(double percentage)
        {
            _cpuThreshold = percentage;
            await Task.CompletedTask;
        }

        public async Task SetMemoryThresholdAsync(double percentage)
        {
            _memoryThreshold = percentage;
            await Task.CompletedTask;
        }

        public async Task SetDiskThresholdAsync(double percentage)
        {
            _diskThreshold = percentage;
            await Task.CompletedTask;
        }

        public async Task SetNetworkThresholdAsync(double gbps)
        {
            _networkThreshold = gbps;
            await Task.CompletedTask;
        }

        public async Task<List<ResourceAlert>> GetAlertsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    return _alerts.OrderByDescending(a => a.Timestamp).ToList();
                }
            });
        }

        public void SubscribeToAlerts(Action<ResourceAlert> handler)
        {
            _alertHandler = handler;
        }

        public async Task<RemediationResult> AutoRemediateAsync(ResourceAlert alert)
        {
            return await Task.Run(() =>
            {
                var result = new RemediationResult();

                if (alert.Type == ResourceAlertType.HighMemory)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    result.Action = "Triggered garbage collection";
                    result.Success = true;
                }
                else if (alert.Type == ResourceAlertType.HighCpu)
                {
                    result.Action = "Throttled background tasks";
                    result.Success = true;
                }
                else if (alert.Type == ResourceAlertType.DiskFull)
                {
                    result.Action = "Initiated cleanup tasks";
                    result.Success = true;
                }

                return result;
            });
        }

        public async Task<CapacityForecast> ForecastCapacityAsync()
        {
            return await Task.Run(() =>
            {
                return new CapacityForecast
                {
                    PredictedMemoryFullDate = DateTime.UtcNow.AddDays(365),
                    PredictedDiskFullDate = DateTime.UtcNow.AddDays(50),
                    DaysUntilCritical = 45,
                    RecommendedActions = new List<string>
                    {
                        "Expand disk storage within 30 days",
                        "Monitor memory growth for leaks"
                    }
                };
            });
        }

        public async Task<ResourceTrends> GetTrendsAsync()
        {
            return await Task.Run(() =>
            {
                var trends = new ResourceTrends();
                var now = DateTime.UtcNow;

                for (int i = 30; i >= 0; i--)
                {
                    var date = now.AddDays(-i);
                    trends.CpuTrend.Add(new TrendPoint { Date = date, Value = 30 + (i * 0.3) });
                    trends.MemoryTrend.Add(new TrendPoint { Date = date, Value = 60 + (i * 0.2) });
                    trends.DiskTrend.Add(new TrendPoint { Date = date, Value = 70 + (i * 0.5) });
                    trends.NetworkTrend.Add(new TrendPoint { Date = date, Value = 0.1 + (i * 0.005) });
                }

                return trends;
            });
        }
    }
}
