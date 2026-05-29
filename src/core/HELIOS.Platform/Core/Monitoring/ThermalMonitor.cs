using System;
using System.Management;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Monitoring
{
    /// <summary>
    /// Monitors CPU and GPU temperatures with thermal throttling detection.
    /// </summary>
    public class ThermalMonitor : MonitoringBase
    {
        public double CpuTemperatureCelsius { get; private set; }
        public double GpuTemperatureCelsius { get; private set; }
        public double MaxTemperatureCelsius { get; private set; }
        public bool IsThrottling { get; private set; }
        public List<FanSpeed> FanSpeeds { get; private set; }

        private readonly double WarningThreshold = 80.0;
        private readonly double CriticalThreshold = 90.0;
        private readonly double ThrottleThreshold = 85.0;

        public event EventHandler<ThermalAlertEventArgs> ThermalAlert;

        public ThermalMonitor() : base("Thermal Monitor", 1000)
        {
            FanSpeeds = new List<FanSpeed>();
        }

        protected override void SampleMetrics(object state)
        {
            try
            {
                SampleTemperatures();
                DetectThrottling();
                SampleFanSpeeds();

                MaxTemperatureCelsius = Math.Max(CpuTemperatureCelsius, GpuTemperatureCelsius);
                AddMetric(MaxTemperatureCelsius);

                var stats = CalculateStats();
                OnMetricUpdated(stats);

                CheckThermalAlerts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Thermal Monitor sampling error: {ex.Message}");
            }
        }

        private void SampleTemperatures()
        {
            try
            {
                // Sample CPU temperature via WMI
                SampleCpuTemperature();
            }
            catch { }

            try
            {
                // Sample GPU temperature via WMI
                SampleGpuTemperature();
            }
            catch { }
        }

        private void SampleCpuTemperature()
        {
            try
            {
                var searcher = new ManagementObjectSearcher(@"root\WMI",
                    "SELECT * FROM MSAcpi_ThermalZoneTemperature");

                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        UInt32 rawTemp = Convert.ToUInt32(obj["CurrentTemperature"]);
                        CpuTemperatureCelsius = (rawTemp / 10.0) - 273.15;
                        obj.Dispose();
                        break;
                    }
                    catch { }
                }
                searcher.Dispose();
            }
            catch { }
        }

        private void SampleGpuTemperature()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_TemperatureProbe");
                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        // Look for GPU sensors
                        string description = obj["Description"]?.ToString() ?? "";
                        if (description.Contains("GPU") || description.Contains("Video"))
                        {
                            int rawTemp = Convert.ToInt32(obj["CurrentReading"]);
                            GpuTemperatureCelsius = (rawTemp - 273.15);
                            obj.Dispose();
                            break;
                        }
                    }
                    catch { }
                    obj.Dispose();
                }
                searcher.Dispose();
            }
            catch { }
        }

        private void DetectThrottling()
        {
            IsThrottling = MaxTemperatureCelsius > ThrottleThreshold;
        }

        private void SampleFanSpeeds()
        {
            try
            {
                FanSpeeds.Clear();
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Fan");
                
                foreach (ManagementObject fan in searcher.Get())
                {
                    try
                    {
                        var speed = new FanSpeed
                        {
                            Name = fan["Name"]?.ToString() ?? "Unknown",
                            CurrentSpeedRpm = Convert.ToUInt32(fan["CurrentSpeed"] ?? 0)
                        };
                        FanSpeeds.Add(speed);
                    }
                    catch { }
                    fan.Dispose();
                }
                searcher.Dispose();
            }
            catch { }
        }

        private void CheckThermalAlerts()
        {
            if (MaxTemperatureCelsius > CriticalThreshold)
            {
                ThermalAlert?.Invoke(this, new ThermalAlertEventArgs
                {
                    AlertLevel = ThermalAlertLevel.Critical,
                    Temperature = MaxTemperatureCelsius,
                    Message = $"CRITICAL: System temperature {MaxTemperatureCelsius:F1}°C"
                });
            }
            else if (MaxTemperatureCelsius > WarningThreshold)
            {
                ThermalAlert?.Invoke(this, new ThermalAlertEventArgs
                {
                    AlertLevel = ThermalAlertLevel.Warning,
                    Temperature = MaxTemperatureCelsius,
                    Message = $"Warning: System temperature {MaxTemperatureCelsius:F1}°C"
                });
            }
        }

        public PerformanceStats GetDetailedStats()
        {
            return CalculateStats();
        }

        public override void Dispose()
        {
            base.Dispose();
            FanSpeeds?.Clear();
        }
    }

    public class FanSpeed
    {
        public string Name { get; set; }
        public uint CurrentSpeedRpm { get; set; }
    }

    public enum ThermalAlertLevel
    {
        Normal,
        Warning,
        Critical
    }

    public class ThermalAlertEventArgs : EventArgs
    {
        public ThermalAlertLevel AlertLevel { get; set; }
        public double Temperature { get; set; }
        public string Message { get; set; }
    }
}
