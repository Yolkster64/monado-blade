using System;
using System.Diagnostics;
using System.Management;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Monitors GPU VRAM usage and utilization metrics.
    /// </summary>
    public class GpuMonitor : MonitoringBase
    {
        private ManagementObjectSearcher gpuSearcher;
        private PerformanceCounter gpuUtilizationCounter;
        private ulong dedicatedMemory;
        private ulong usedMemory;

        public ulong DedicatedMemoryMB { get; private set; }
        public ulong UsedMemoryMB { get; private set; }
        public double GpuUtilization { get; private set; }
        public double GpuTemperature { get; private set; }

        public GpuMonitor() : base("GPU Monitor", 200)
        {
            InitializeCounters();
        }

        private void InitializeCounters()
        {
            try
            {
                var gpuQuery = new ObjectQuery("SELECT * FROM Win32_VideoController");
                gpuSearcher = new ManagementObjectSearcher(gpuQuery);
                
                // Try to get GPU utilization counter
                try
                {
                    gpuUtilizationCounter = new PerformanceCounter("GPU Engine", "Utilization %", "_Total", true);
                }
                catch
                {
                    gpuUtilizationCounter = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPU Monitor initialization error: {ex.Message}");
            }
        }

        protected override void SampleMetrics(object state)
        {
            try
            {
                SampleGpuMemory();
                SampleGpuUtilization();

                var stats = CalculateStats();
                OnMetricUpdated(stats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPU Monitor sampling error: {ex.Message}");
            }
        }

        private void SampleGpuMemory()
        {
            try
            {
                if (gpuSearcher == null) return;

                foreach (ManagementObject gpu in gpuSearcher.Get())
                {
                    try
                    {
                        if (gpu["AdapterRAM"] != null)
                        {
                            dedicatedMemory = Convert.ToUInt64(gpu["AdapterRAM"]) / (1024 * 1024); // Convert to MB
                            DedicatedMemoryMB = dedicatedMemory;
                        }
                    }
                    catch { }

                    gpu.Dispose();
                    break; // Just get first GPU
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPU Memory sampling error: {ex.Message}");
            }
        }

        private void SampleGpuUtilization()
        {
            try
            {
                if (gpuUtilizationCounter != null)
                {
                    GpuUtilization = gpuUtilizationCounter.NextValue();
                    AddMetric(GpuUtilization);
                }
            }
            catch { }

            // Attempt to get GPU temperature via WMI (NVidia/AMD specific)
            TrySampleGpuTemperature();
        }

        private void TrySampleGpuTemperature()
        {
            try
            {
                var tempQuery = new ObjectQuery("SELECT * FROM Win32_TemperatureProbe");
                var tempSearcher = new ManagementObjectSearcher(tempQuery);
                
                foreach (ManagementObject tempObject in tempSearcher.Get())
                {
                    try
                    {
                        if (tempObject["CurrentReading"] != null)
                        {
                            int tempRaw = Convert.ToInt32(tempObject["CurrentReading"]);
                            GpuTemperature = (tempRaw - 273.15); // Kelvin to Celsius
                            break;
                        }
                    }
                    catch { }
                    finally
                    {
                        tempObject.Dispose();
                    }
                }
                tempSearcher.Dispose();
            }
            catch { }
        }

        public PerformanceStats GetDetailedStats()
        {
            return CalculateStats();
        }

        public override void Dispose()
        {
            base.Dispose();
            gpuSearcher?.Dispose();
            gpuUtilizationCounter?.Dispose();
        }
    }
}
