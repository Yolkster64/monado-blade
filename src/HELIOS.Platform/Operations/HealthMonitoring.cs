using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Operations
{
    /// <summary>
    /// System health monitoring and status aggregation
    /// </summary>
    public class HealthMonitoring
    {
        public enum ComponentHealth { Healthy, Degraded, Unhealthy, Unknown }

        public class ComponentStatus
        {
            public string ComponentId { get; set; }
            public string ComponentName { get; set; }
            public ComponentHealth HealthStatus { get; set; }
            public DateTime LastCheckedAt { get; set; }
            public string Details { get; set; }
            public List<string> Issues { get; set; }
            public Dictionary<string, double> Metrics { get; set; }
        }

        public class HealthCheckResult
        {
            public string CheckId { get; set; }
            public DateTime CheckedAt { get; set; }
            public List<ComponentStatus> ComponentStatuses { get; set; }
            public ComponentHealth OverallHealth { get; set; }
            public string StatusPageMessage { get; set; }
            public int HealthyComponents { get; set; }
            public int DegradedComponents { get; set; }
            public int UnhealthyComponents { get; set; }
        }

        public class StatusPageEntry
        {
            public string EntryId { get; set; }
            public DateTime Timestamp { get; set; }
            public string ComponentId { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
            public bool IsIncident { get; set; }
        }

        private readonly Dictionary<string, ComponentStatus> _componentStatuses = new();
        private readonly List<StatusPageEntry> _statusHistory = new();

        public HealthMonitoring()
        {
            InitializeComponents();
        }

        public async Task<ComponentStatus> CheckComponentHealth(string componentId)
        {
            if (!_componentStatuses.TryGetValue(componentId, out var status))
                return null;

            status.LastCheckedAt = DateTime.UtcNow;
            status.Metrics = new Dictionary<string, double>
            {
                { "cpu_usage", 45.5 },
                { "memory_usage", 62.3 },
                { "response_time_ms", 125 },
                { "error_rate", 0.002 }
            };

            var cpu = status.Metrics["cpu_usage"];
            var memory = status.Metrics["memory_usage"];
            var errorRate = status.Metrics["error_rate"];

            if (cpu > 90 || memory > 90 || errorRate > 0.05)
                status.HealthStatus = ComponentHealth.Unhealthy;
            else if (cpu > 75 || memory > 75 || errorRate > 0.02)
                status.HealthStatus = ComponentHealth.Degraded;
            else
                status.HealthStatus = ComponentHealth.Healthy;

            status.Issues.Clear();
            if (cpu > 80)
                status.Issues.Add($"High CPU usage: {cpu}%");
            if (memory > 80)
                status.Issues.Add($"High memory usage: {memory}%");

            await Task.CompletedTask;
            return status;
        }

        public async Task<HealthCheckResult> PerformFullHealthCheck()
        {
            var result = new HealthCheckResult
            {
                CheckId = Guid.NewGuid().ToString(),
                CheckedAt = DateTime.UtcNow,
                ComponentStatuses = new List<ComponentStatus>(),
                HealthyComponents = 0,
                DegradedComponents = 0,
                UnhealthyComponents = 0
            };

            foreach (var componentId in _componentStatuses.Keys)
            {
                await CheckComponentHealth(componentId);
                var status = _componentStatuses[componentId];
                result.ComponentStatuses.Add(status);

                switch (status.HealthStatus)
                {
                    case ComponentHealth.Healthy: result.HealthyComponents++; break;
                    case ComponentHealth.Degraded: result.DegradedComponents++; break;
                    case ComponentHealth.Unhealthy: result.UnhealthyComponents++; break;
                }
            }

            result.OverallHealth = result.UnhealthyComponents > 0 ? ComponentHealth.Unhealthy :
                                  result.DegradedComponents > 0 ? ComponentHealth.Degraded :
                                  ComponentHealth.Healthy;

            result.StatusPageMessage = result.OverallHealth switch
            {
                ComponentHealth.Healthy => "All systems operational",
                ComponentHealth.Degraded => "Some services experiencing elevated latency",
                ComponentHealth.Unhealthy => "Critical service outage detected",
                _ => "Status unknown"
            };

            var pageEntry = new StatusPageEntry
            {
                EntryId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Status = result.OverallHealth.ToString(),
                Message = result.StatusPageMessage,
                IsIncident = result.UnhealthyComponents > 0
            };

            _statusHistory.Add(pageEntry);
            return result;
        }

        public async Task<List<StatusPageEntry>> GetStatusHistory(int lastHours = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-lastHours);
            var history = _statusHistory.FindAll(e => e.Timestamp >= cutoffTime);
            await Task.CompletedTask;
            return history;
        }

        private void InitializeComponents()
        {
            var components = new[] { "API", "Database", "Cache", "Queue", "Storage", "LoadBalancer" };
            foreach (var component in components)
            {
                _componentStatuses[component] = new ComponentStatus
                {
                    ComponentId = component,
                    ComponentName = component,
                    HealthStatus = ComponentHealth.Healthy,
                    LastCheckedAt = DateTime.UtcNow,
                    Details = "Operational",
                    Issues = new List<string>(),
                    Metrics = new Dictionary<string, double>()
                };
            }
        }
    }
}
