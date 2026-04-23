using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy
    }

    /// <summary>
    /// Health check result
    /// </summary>
    public class HealthCheckResult
    {
        public string ServiceName { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
    }

    /// <summary>
    /// Health checker interface
    /// </summary>
    public interface IHealthChecker
    {
        Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName);
        Task<IEnumerable<HealthCheckResult>> CheckAllServicesAsync();
        Task<HealthCheckResult> CheckResourceHealthAsync();
        Task<HealthCheckResult> CheckConnectivityAsync(string endpoint);
    }

    /// <summary>
    /// Health checker implementation
    /// </summary>
    public class HealthChecker : IHealthChecker
    {
        private readonly IMetricsCollector _metricsCollector;
        private readonly IServiceRegistry _serviceRegistry;
        private readonly Dictionary<string, Func<Task<HealthCheckResult>>> _customChecks;

        public HealthChecker(IMetricsCollector metricsCollector, IServiceRegistry serviceRegistry)
        {
            _metricsCollector = metricsCollector;
            _serviceRegistry = serviceRegistry;
            _customChecks = new();
        }

        public async Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var metadata = _serviceRegistry.Discover(serviceName);
                if (metadata == null)
                {
                    stopwatch.Stop();
                    return new HealthCheckResult
                    {
                        ServiceName = serviceName,
                        Status = HealthStatus.Unhealthy,
                        Message = "Service not found in registry",
                        ResponseTime = stopwatch.Elapsed
                    };
                }

                // Check if custom check is registered
                if (_customChecks.TryGetValue(serviceName, out var customCheck))
                {
                    var result = await customCheck();
                    stopwatch.Stop();
                    result.ResponseTime = stopwatch.Elapsed;
                    return result;
                }

                stopwatch.Stop();
                return new HealthCheckResult
                {
                    ServiceName = serviceName,
                    Status = _serviceRegistry.IsHealthy(serviceName) ? HealthStatus.Healthy : HealthStatus.Degraded,
                    Message = "Service is responsive",
                    ResponseTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheckResult
                {
                    ServiceName = serviceName,
                    Status = HealthStatus.Unhealthy,
                    Message = $"Health check failed: {ex.Message}",
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<IEnumerable<HealthCheckResult>> CheckAllServicesAsync()
        {
            var results = new List<HealthCheckResult>();
            var services = _serviceRegistry.GetAllServices();

            foreach (var service in services)
            {
                var result = await CheckServiceHealthAsync(service.Name);
                results.Add(result);
            }

            return results;
        }

        public async Task<HealthCheckResult> CheckResourceHealthAsync()
        {
            return await Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var cpuMetrics = _metricsCollector.GetCpuMetrics();
                    var memMetrics = _metricsCollector.GetMemoryMetrics();
                    var diskMetrics = _metricsCollector.GetDiskMetrics();

                    var status = DetermineResourceHealth(cpuMetrics, memMetrics, diskMetrics);
                    var message = GenerateResourceHealthMessage(cpuMetrics, memMetrics, diskMetrics);

                    stopwatch.Stop();
                    return new HealthCheckResult
                    {
                        ServiceName = "SystemResources",
                        Status = status,
                        Message = message,
                        Details = new()
                        {
                            { "CPU", cpuMetrics.OverallUsage },
                            { "Memory", memMetrics.UsagePercentage },
                            { "DiskCount", diskMetrics.Count() }
                        },
                        ResponseTime = stopwatch.Elapsed
                    };
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    return new HealthCheckResult
                    {
                        ServiceName = "SystemResources",
                        Status = HealthStatus.Degraded,
                        Message = $"Resource check failed: {ex.Message}",
                        ResponseTime = stopwatch.Elapsed
                    };
                }
            });
        }

        public async Task<HealthCheckResult> CheckConnectivityAsync(string endpoint)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Basic HTTP connectivity check
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync(endpoint);
                    
                    stopwatch.Stop();
                    return new HealthCheckResult
                    {
                        ServiceName = endpoint,
                        Status = response.IsSuccessStatusCode ? HealthStatus.Healthy : HealthStatus.Degraded,
                        Message = $"HTTP {response.StatusCode}",
                        ResponseTime = stopwatch.Elapsed
                    };
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new HealthCheckResult
                {
                    ServiceName = endpoint,
                    Status = HealthStatus.Unhealthy,
                    Message = $"Connectivity check failed: {ex.Message}",
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }

        public void RegisterCustomCheck(string serviceName, Func<Task<HealthCheckResult>> check)
        {
            _customChecks[serviceName] = check;
        }

        private HealthStatus DetermineResourceHealth(CpuMetrics cpu, MemoryMetrics memory, IEnumerable<DiskMetrics> disks)
        {
            if (cpu.OverallUsage > 90 || memory.UsagePercentage > 90)
                return HealthStatus.Unhealthy;

            if (cpu.OverallUsage > 75 || memory.UsagePercentage > 75)
                return HealthStatus.Degraded;

            foreach (var disk in disks)
            {
                if (disk.UsagePercentage > 90)
                    return HealthStatus.Unhealthy;
                if (disk.UsagePercentage > 75)
                    return HealthStatus.Degraded;
            }

            return HealthStatus.Healthy;
        }

        private string GenerateResourceHealthMessage(CpuMetrics cpu, MemoryMetrics memory, IEnumerable<DiskMetrics> disks)
        {
            return $"CPU: {cpu.OverallUsage:F1}%, Memory: {memory.UsagePercentage:F1}%, Disks: {disks.Count()}";
        }
    }
}
