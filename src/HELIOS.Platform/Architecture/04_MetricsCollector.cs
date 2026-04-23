using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// CPU metrics
    /// </summary>
    public class CpuMetrics
    {
        public double OverallUsage { get; set; } // Percentage 0-100
        public double KernelUsage { get; set; } // Percentage 0-100
        public double UserUsage { get; set; } // Percentage 0-100
        public int ProcessorCount { get; set; }
        public DateTime SampledAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Memory metrics
    /// </summary>
    public class MemoryMetrics
    {
        public long TotalMemory { get; set; } // Bytes
        public long UsedMemory { get; set; } // Bytes
        public long AvailableMemory { get; set; } // Bytes
        public double UsagePercentage { get; set; } // 0-100
        public long PrivateMemory { get; set; } // Process private memory (bytes)
        public long WorkingSet { get; set; } // Process working set (bytes)
        public DateTime SampledAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Disk metrics
    /// </summary>
    public class DiskMetrics
    {
        public string DriveName { get; set; }
        public long TotalSize { get; set; } // Bytes
        public long UsedSize { get; set; } // Bytes
        public long AvailableSize { get; set; } // Bytes
        public double UsagePercentage { get; set; } // 0-100
        public long IoReadsPerSec { get; set; }
        public long IoWritesPerSec { get; set; }
        public DateTime SampledAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Network metrics
    /// </summary>
    public class NetworkMetrics
    {
        public string AdapterName { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public long PacketsSent { get; set; }
        public long PacketsReceived { get; set; }
        public long Errors { get; set; }
        public double LatencyMs { get; set; }
        public DateTime SampledAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Process-specific metrics
    /// </summary>
    public class ProcessMetrics
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public double CpuUsage { get; set; } // Percentage
        public long MemoryUsage { get; set; } // Bytes
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime SampledAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Metrics collection interface
    /// </summary>
    public interface IMetricsCollector
    {
        CpuMetrics GetCpuMetrics();
        MemoryMetrics GetMemoryMetrics();
        IEnumerable<DiskMetrics> GetDiskMetrics();
        IEnumerable<NetworkMetrics> GetNetworkMetrics();
        ProcessMetrics GetProcessMetrics(int processId);
        ProcessMetrics GetProcessMetrics(string processName);
    }

    /// <summary>
    /// Metrics collector implementation
    /// </summary>
    public class MetricsCollector : IMetricsCollector
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private readonly Process _currentProcess;

        public MetricsCollector()
        {
            _currentProcess = Process.GetCurrentProcess();
            InitializeCounters();
        }

        private void InitializeCounters()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", null, true);
            }
            catch
            {
                // Performance counters might not be available in all environments
            }
        }

        public CpuMetrics GetCpuMetrics()
        {
            var cpuUsage = _cpuCounter?.NextValue() ?? 0;
            
            return new CpuMetrics
            {
                OverallUsage = cpuUsage,
                ProcessorCount = Environment.ProcessorCount,
                SampledAt = DateTime.UtcNow
            };
        }

        public MemoryMetrics GetMemoryMetrics()
        {
            var totalMemory = GC.GetTotalMemory(false);
            
            return new MemoryMetrics
            {
                TotalMemory = GC.GetTotalMemory(true),
                UsedMemory = totalMemory,
                PrivateMemory = _currentProcess.PrivateMemorySize64,
                WorkingSet = _currentProcess.WorkingSet64,
                SampledAt = DateTime.UtcNow
            };
        }

        public IEnumerable<DiskMetrics> GetDiskMetrics()
        {
            var metrics = new List<DiskMetrics>();
            
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    metrics.Add(new DiskMetrics
                    {
                        DriveName = drive.Name,
                        TotalSize = drive.TotalSize,
                        AvailableSize = drive.AvailableFreeSpace,
                        UsedSize = drive.TotalSize - drive.AvailableFreeSpace,
                        UsagePercentage = ((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100,
                        SampledAt = DateTime.UtcNow
                    });
                }
            }
            
            return metrics;
        }

        public IEnumerable<NetworkMetrics> GetNetworkMetrics()
        {
            var metrics = new List<NetworkMetrics>();
            // Network metrics implementation would require additional Windows API calls
            // This is a placeholder for the interface contract
            return metrics;
        }

        public ProcessMetrics GetProcessMetrics(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return ExtractProcessMetrics(process);
            }
            catch
            {
                return null;
            }
        }

        public ProcessMetrics GetProcessMetrics(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    return ExtractProcessMetrics(processes[0]);
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return null;
        }

        private ProcessMetrics ExtractProcessMetrics(Process process)
        {
            return new ProcessMetrics
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                MemoryUsage = process.PrivateMemorySize64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                SampledAt = DateTime.UtcNow
            };
        }
    }
}
