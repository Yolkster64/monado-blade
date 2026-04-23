using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Operations
{
    /// <summary>
    /// Comprehensive monitoring and alerting system with dashboards and custom rules
    /// </summary>
    public class MonitoringAndAlerting
    {
        public enum AlertSeverity { Critical, Warning, Info }
        public enum MetricType { Counter, Gauge, Histogram }
        public enum AlertState { Active, Acknowledged, Resolved, Suppressed }

        public class Metric
        {
            public string MetricId { get; set; }
            public string Name { get; set; }
            public MetricType Type { get; set; }
            public double Value { get; set; }
            public Dictionary<string, string> Labels { get; set; }
            public DateTime Timestamp { get; set; }
            public string Service { get; set; }
        }

        public class Alert
        {
            public string AlertId { get; set; }
            public string RuleId { get; set; }
            public string Title { get; set; }
            public AlertSeverity Severity { get; set; }
            public AlertState State { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ResolvedAt { get; set; }
            public string Description { get; set; }
            public List<string> AffectedServices { get; set; }
            public Dictionary<string, object> Context { get; set; }
            public int NotificationCount { get; set; }
        }

        public class AlertRule
        {
            public string RuleId { get; set; }
            public string Name { get; set; }
            public string MetricName { get; set; }
            public string Condition { get; set; }
            public double Threshold { get; set; }
            public int DurationSeconds { get; set; }
            public AlertSeverity Severity { get; set; }
            public NotificationRoute NotificationRoute { get; set; }
            public SuppressionPolicy SuppressionPolicy { get; set; }
            public bool Enabled { get; set; }
            public List<string> ApplicableServices { get; set; }
        }

        public class NotificationRoute
        {
            public string RouteId { get; set; }
            public List<string> SlackChannels { get; set; }
            public List<string> EmailRecipients { get; set; }
            public string PagerDutyIntegration { get; set; }
            public Dictionary<string, bool> NotificationMethods { get; set; }
        }

        public class SuppressionPolicy
        {
            public string PolicyId { get; set; }
            public TimeSpan Duration { get; set; }
            public int MaxSuppressionCount { get; set; }
            public List<string> WhitelistTriggers { get; set; }
            public bool AutoResolveAfterSuppression { get; set; }
        }

        public class MonitoringDashboard
        {
            public string DashboardId { get; set; }
            public string Name { get; set; }
            public List<DashboardWidget> Widgets { get; set; }
            public string Owner { get; set; }
            public List<string> Viewers { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class DashboardWidget
        {
            public string WidgetId { get; set; }
            public string Title { get; set; }
            public string MetricName { get; set; }
            public string VisualizationType { get; set; }
            public TimeSpan TimeRange { get; set; }
            public Dictionary<string, object> Configuration { get; set; }
        }

        public class SLA
        {
            public string SLAId { get; set; }
            public string ServiceName { get; set; }
            public decimal AvailabilityTarget { get; set; }
            public decimal ResponseTimeTarget { get; set; }
            public TimeSpan MeasurementWindow { get; set; }
            public List<SLAViolation> Violations { get; set; }
        }

        public class SLAViolation
        {
            public DateTime OccurredAt { get; set; }
            public decimal ActualValue { get; set; }
            public decimal TargetValue { get; set; }
            public string MetricName { get; set; }
        }

        private readonly Dictionary<string, Metric> _metrics = new();
        private readonly Dictionary<string, Alert> _alerts = new();
        private readonly Dictionary<string, AlertRule> _alertRules = new();
        private readonly Dictionary<string, MonitoringDashboard> _dashboards = new();
        private readonly Dictionary<string, SLA> _slas = new();
        private readonly Dictionary<string, Alert> _suppressedAlerts = new();

        public MonitoringAndAlerting()
        {
            InitializeDefaultRules();
            InitializeDefaultDashboards();
            InitializeDefaultSLAs();
        }

        public void RecordMetric(string metricName, double value, Dictionary<string, string> labels = null, string service = "Unknown")
        {
            var metric = new Metric
            {
                MetricId = Guid.NewGuid().ToString(),
                Name = metricName,
                Type = ClassifyMetricType(metricName),
                Value = value,
                Labels = labels ?? new Dictionary<string, string>(),
                Timestamp = DateTime.UtcNow,
                Service = service
            };

            _metrics[metric.MetricId] = metric;
        }

        public async Task<Alert> CreateAlert(string ruleId, string title, AlertSeverity severity, 
            List<string> affectedServices, Dictionary<string, object> context)
        {
            var alert = new Alert
            {
                AlertId = Guid.NewGuid().ToString(),
                RuleId = ruleId,
                Title = title,
                Severity = severity,
                State = AlertState.Active,
                CreatedAt = DateTime.UtcNow,
                Description = $"Alert triggered by rule {ruleId}",
                AffectedServices = affectedServices,
                Context = context,
                NotificationCount = 0
            };

            _alerts[alert.AlertId] = alert;

            if (_alertRules.TryGetValue(ruleId, out var rule))
            {
                await NotifyChannels(alert, rule.NotificationRoute);
                await CheckForSuppression(alert, rule.SuppressionPolicy);
            }

            return alert;
        }

        public AlertRule CreateAlertRule(string name, string metricName, string condition, double threshold, 
            int durationSeconds, AlertSeverity severity, List<string> services)
        {
            var route = new NotificationRoute
            {
                RouteId = Guid.NewGuid().ToString(),
                SlackChannels = new List<string> { "#alerts" },
                EmailRecipients = new List<string> { "ops@company.com" },
                NotificationMethods = new Dictionary<string, bool> { { "Slack", true }, { "Email", true } }
            };

            var suppression = new SuppressionPolicy
            {
                PolicyId = Guid.NewGuid().ToString(),
                Duration = TimeSpan.FromMinutes(15),
                MaxSuppressionCount = 3,
                WhitelistTriggers = new List<string>(),
                AutoResolveAfterSuppression = true
            };

            var rule = new AlertRule
            {
                RuleId = Guid.NewGuid().ToString(),
                Name = name,
                MetricName = metricName,
                Condition = condition,
                Threshold = threshold,
                DurationSeconds = durationSeconds,
                Severity = severity,
                NotificationRoute = route,
                SuppressionPolicy = suppression,
                Enabled = true,
                ApplicableServices = services
            };

            _alertRules[rule.RuleId] = rule;
            return rule;
        }

        public void EvaluateAlertRules()
        {
            foreach (var rule in _alertRules.Values.Where(r => r.Enabled))
            {
                var relevantMetrics = _metrics.Values
                    .Where(m => m.Name == rule.MetricName && m.Timestamp > DateTime.UtcNow.AddSeconds(-rule.DurationSeconds))
                    .ToList();

                if (relevantMetrics.Any())
                {
                    var meetsCondition = EvaluateCondition(relevantMetrics, rule.Condition, rule.Threshold);
                    
                    if (meetsCondition)
                    {
                        var existingAlert = _alerts.Values
                            .FirstOrDefault(a => a.RuleId == rule.RuleId && a.State == AlertState.Active);

                        if (existingAlert == null)
                        {
                            _ = CreateAlert(rule.RuleId, $"{rule.Name} triggered", rule.Severity, 
                                rule.ApplicableServices, new Dictionary<string, object> { { "metrics", relevantMetrics.Count } });
                        }
                    }
                }
            }
        }

        public MonitoringDashboard CreateDashboard(string name, string owner)
        {
            var dashboard = new MonitoringDashboard
            {
                DashboardId = Guid.NewGuid().ToString(),
                Name = name,
                Widgets = new List<DashboardWidget>(),
                Owner = owner,
                Viewers = new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            _dashboards[dashboard.DashboardId] = dashboard;
            return dashboard;
        }

        public void AddWidgetToDashboard(string dashboardId, string title, string metricName, 
            string visualizationType, TimeSpan timeRange)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                return;

            var widget = new DashboardWidget
            {
                WidgetId = Guid.NewGuid().ToString(),
                Title = title,
                MetricName = metricName,
                VisualizationType = visualizationType,
                TimeRange = timeRange,
                Configuration = new Dictionary<string, object> { { "refreshInterval", 60 } }
            };

            dashboard.Widgets.Add(widget);
        }

        public async Task<bool> AcknowledgeAlert(string alertId, string acknowledgedBy)
        {
            if (!_alerts.TryGetValue(alertId, out var alert))
                return false;

            alert.State = AlertState.Acknowledged;
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ResolveAlert(string alertId)
        {
            if (!_alerts.TryGetValue(alertId, out var alert))
                return false;

            alert.State = AlertState.Resolved;
            alert.ResolvedAt = DateTime.UtcNow;
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> SuppressAlert(string alertId, TimeSpan duration)
        {
            if (!_alerts.TryGetValue(alertId, out var alert))
                return false;

            alert.State = AlertState.Suppressed;
            _suppressedAlerts[alertId] = alert;
            await Task.Delay((int)duration.TotalMilliseconds);
            alert.State = AlertState.Resolved;
            _suppressedAlerts.Remove(alertId);
            return true;
        }

        public SLA CreateSLA(string serviceName, decimal availabilityTarget, decimal responseTimeTarget)
        {
            var sla = new SLA
            {
                SLAId = Guid.NewGuid().ToString(),
                ServiceName = serviceName,
                AvailabilityTarget = availabilityTarget,
                ResponseTimeTarget = responseTimeTarget,
                MeasurementWindow = TimeSpan.FromHours(1),
                Violations = new List<SLAViolation>()
            };

            _slas[sla.SLAId] = sla;
            return sla;
        }

        public void CheckSLACompliance()
        {
            foreach (var sla in _slas.Values)
            {
                var availabilityMetrics = _metrics.Values
                    .Where(m => m.Name == "availability" && m.Labels.ContainsValue(sla.ServiceName))
                    .ToList();

                if (availabilityMetrics.Any())
                {
                    var avgAvailability = availabilityMetrics.Average(m => m.Value);
                    if (avgAvailability < (double)sla.AvailabilityTarget)
                    {
                        sla.Violations.Add(new SLAViolation
                        {
                            OccurredAt = DateTime.UtcNow,
                            ActualValue = avgAvailability,
                            TargetValue = (double)sla.AvailabilityTarget,
                            MetricName = "availability"
                        });
                    }
                }
            }
        }

        public Dictionary<string, object> GetDashboardData(string dashboardId)
        {
            if (!_dashboards.TryGetValue(dashboardId, out var dashboard))
                return new Dictionary<string, object>();

            var data = new Dictionary<string, object>();
            foreach (var widget in dashboard.Widgets)
            {
                var widgetData = _metrics.Values
                    .Where(m => m.Name == widget.MetricName && m.Timestamp > DateTime.UtcNow.Subtract(widget.TimeRange))
                    .GroupBy(m => m.Timestamp.Minute)
                    .Select(g => new { Time = g.Key, Value = g.Average(m => m.Value) })
                    .ToList();

                data[widget.Title] = widgetData;
            }

            return data;
        }

        private MetricType ClassifyMetricType(string name)
        {
            return name.Contains("total") ? MetricType.Counter : 
                   name.Contains("current") ? MetricType.Gauge : 
                   MetricType.Histogram;
        }

        private bool EvaluateCondition(List<Metric> metrics, string condition, double threshold)
        {
            var avg = metrics.Average(m => m.Value);
            return condition switch
            {
                ">" => avg > threshold,
                "<" => avg < threshold,
                ">=" => avg >= threshold,
                "<=" => avg <= threshold,
                "==" => Math.Abs(avg - threshold) < 0.01,
                _ => false
            };
        }

        private async Task NotifyChannels(Alert alert, NotificationRoute route)
        {
            if (route?.NotificationMethods == null)
                return;

            if (route.NotificationMethods.TryGetValue("Slack", out var sendSlack) && sendSlack)
            {
                foreach (var channel in route.SlackChannels ?? new List<string>())
                {
                    await Task.CompletedTask;
                }
            }

            if (route.NotificationMethods.TryGetValue("Email", out var sendEmail) && sendEmail)
            {
                foreach (var recipient in route.EmailRecipients ?? new List<string>())
                {
                    await Task.CompletedTask;
                }
            }

            alert.NotificationCount++;
        }

        private async Task CheckForSuppression(Alert alert, SuppressionPolicy policy)
        {
            if (policy == null || alert.NotificationCount <= policy.MaxSuppressionCount)
            {
                await Task.CompletedTask;
                return;
            }

            alert.State = AlertState.Suppressed;
            await Task.Delay((int)policy.Duration.TotalMilliseconds / policy.MaxSuppressionCount);
        }

        private void InitializeDefaultRules()
        {
            var rules = new[]
            {
                ("HighCPU", "cpu_usage", ">", 80.0, 300, AlertSeverity.Warning),
                ("HighMemory", "memory_usage", ">", 85.0, 300, AlertSeverity.Warning),
                ("HighErrorRate", "error_rate", ">", 0.05, 600, AlertSeverity.Critical),
                ("HighLatency", "p99_latency", ">", 1000.0, 600, AlertSeverity.High)
            };

            foreach (var (name, metric, cond, thresh, duration, severity) in rules)
            {
                CreateAlertRule(name, metric, cond, thresh, duration, severity, new List<string> { "All" });
            }
        }

        private void InitializeDefaultDashboards()
        {
            var dashboard = CreateDashboard("System Overview", "SRE Team");
            AddWidgetToDashboard(dashboard.DashboardId, "CPU Usage", "cpu_usage", "LineChart", TimeSpan.FromHours(1));
            AddWidgetToDashboard(dashboard.DashboardId, "Memory Usage", "memory_usage", "LineChart", TimeSpan.FromHours(1));
            AddWidgetToDashboard(dashboard.DashboardId, "Error Rate", "error_rate", "Gauge", TimeSpan.FromHours(1));
            AddWidgetToDashboard(dashboard.DashboardId, "Latency (P99)", "p99_latency", "LineChart", TimeSpan.FromHours(1));
        }

        private void InitializeDefaultSLAs()
        {
            CreateSLA("API", 0.99m, 200m);
            CreateSLA("Database", 0.999m, 50m);
            CreateSLA("Cache", 0.995m, 10m);
        }
    }
}
