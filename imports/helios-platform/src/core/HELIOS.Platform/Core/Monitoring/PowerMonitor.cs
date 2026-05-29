using System;
using System.Diagnostics;
using System.Management;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Monitors system power consumption and battery status.
    /// </summary>
    public class PowerMonitor : MonitoringBase
    {
        private PerformanceCounter cpuPowerCounter;
        private PerformanceCounter gpuPowerCounter;

        public double CpuPowerWatts { get; private set; }
        public double GpuPowerWatts { get; private set; }
        public double TotalPowerWatts { get; private set; }
        public string BatteryStatus { get; private set; }
        public int BatteryPercentage { get; private set; }
        public TimeSpan BatteryRuntime { get; private set; }

        public PowerMonitor() : base("Power Monitor", 1000)
        {
            InitializeCounters();
        }

        private void InitializeCounters()
        {
            try
            {
                // Attempt to create performance counters for CPU and GPU power
                cpuPowerCounter = new PerformanceCounter("Processor", "% of Max Frequency", "_Total", true);
                // GPU power counter is typically not available without special drivers
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Power Monitor initialization error: {ex.Message}");
            }
        }

        protected override void SampleMetrics(object state)
        {
            try
            {
                EstimateCpuPower();
                EstimateGpuPower();
                SampleBatteryStatus();

                TotalPowerWatts = CpuPowerWatts + GpuPowerWatts;
                AddMetric(TotalPowerWatts);

                var stats = CalculateStats();
                OnMetricUpdated(stats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Power Monitor sampling error: {ex.Message}");
            }
        }

        private void EstimateCpuPower()
        {
            try
            {
                if (cpuPowerCounter != null)
                {
                    float frequency = cpuPowerCounter.NextValue();
                    // Rough estimation: base power + frequency scaling
                    // Typical CPU: 45W base + 5W per core * 8 cores
                    int coreCount = Environment.ProcessorCount;
                    CpuPowerWatts = 45 + (frequency / 100.0 * coreCount * 5);
                }
            }
            catch { }
        }

        private void EstimateGpuPower()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (ManagementObject gpu in searcher.Get())
                {
                    try
                    {
                        // Typical discrete GPU power range: 50-350W depending on model
                        // Default estimate for modern GPU
                        string deviceName = gpu["Name"]?.ToString() ?? "";
                        if (!deviceName.Contains("Intel") && !deviceName.Contains("AMD Radeon"))
                        {
                            GpuPowerWatts = 100; // NVIDIA discrete GPU estimate
                        }
                        else
                        {
                            GpuPowerWatts = 25; // Integrated GPU estimate
                        }
                    }
                    catch { }
                    gpu.Dispose();
                    break;
                }
                searcher.Dispose();
            }
            catch { }
        }

        private void SampleBatteryStatus()
        {
            try
            {
                var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Battery");

                foreach (ManagementObject battery in searcher.Get())
                {
                    try
                    {
                        BatteryPercentage = Convert.ToInt32(battery["EstimatedChargeRemaining"] ?? 0);
                        string status = battery["BatteryStatus"]?.ToString() ?? "Unknown";
                        
                        BatteryStatus = status switch
                        {
                            "1" => "Discharging",
                            "2" => "On AC Power",
                            "3" => "Fully Charged",
                            "4" => "Low",
                            "5" => "Critical",
                            _ => "Unknown"
                        };

                        if (battery["EstimatedRunTime"] is uint runtime && runtime != 4294967295) // Max uint
                        {
                            BatteryRuntime = TimeSpan.FromMinutes(runtime);
                        }
                    }
                    catch { }
                    battery.Dispose();
                    break;
                }
                searcher.Dispose();
            }
            catch
            {
                BatteryStatus = "No Battery";
                BatteryPercentage = 0;
            }
        }

        public PerformanceStats GetDetailedStats()
        {
            return CalculateStats();
        }

        public override void Dispose()
        {
            base.Dispose();
            cpuPowerCounter?.Dispose();
            gpuPowerCounter?.Dispose();
        }
    }
}
