using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI
{
    /// <summary>
    /// Tracks and analyzes user behavior patterns to identify usage trends and preferences.
    /// </summary>
    public class UsageAnalyzer
    {
        private readonly List<UsageEvent> _events = new();
        private readonly Dictionary<string, FeatureUsageStats> _featureStats = new();
        private readonly int _bufferSize = 5000;
        private bool _isEnabled = true;

        public class UsageEvent
        {
            public DateTime Timestamp { get; set; }
            public string FeatureName { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public int DurationMs { get; set; }
            public string? Context { get; set; }
            public bool Success { get; set; }
        }

        public class FeatureUsageStats
        {
            public string FeatureName { get; set; } = string.Empty;
            public long TotalUsages { get; set; }
            public double AverageDurationMs { get; set; }
            public double SuccessRate { get; set; }
            public DateTime LastUsed { get; set; }
            public DateTime FirstUsed { get; set; }
            public List<PeakTime> PeakTimes { get; set; } = new();
            public Dictionary<int, int> HourlyUsage { get; set; } = new();
            public double UserPreferenceScore { get; set; }
        }

        public class PeakTime
        {
            public int Hour { get; set; }
            public double UsageCount { get; set; }
        }

        public void RecordUsage(UsageEvent usage)
        {
            if (!_isEnabled) return;

            lock (_events)
            {
                _events.Add(usage);
                if (_events.Count > _bufferSize)
                    _events.RemoveAt(0);
            }
        }

        public async Task<Dictionary<string, FeatureUsageStats>> AnalyzePatterns()
        {
            if (!_isEnabled || _events.Count < 5)
                return new Dictionary<string, FeatureUsageStats>();

            var stats = new Dictionary<string, FeatureUsageStats>();

            await Task.Run(() =>
            {
                lock (_events)
                {
                    var grouped = _events.GroupBy(e => e.FeatureName);

                    foreach (var group in grouped)
                    {
                        var events = group.ToList();
                        var featureName = group.Key;

                        var stat = new FeatureUsageStats
                        {
                            FeatureName = featureName,
                            TotalUsages = events.Count,
                            AverageDurationMs = events.Average(e => e.DurationMs),
                            SuccessRate = events.Count(e => e.Success) / (double)events.Count,
                            LastUsed = events.Max(e => e.Timestamp),
                            FirstUsed = events.Min(e => e.Timestamp),
                            PeakTimes = CalculatePeakTimes(events),
                            HourlyUsage = CalculateHourlyUsage(events),
                            UserPreferenceScore = CalculatePreferenceScore(events)
                        };

                        stats[featureName] = stat;
                        _featureStats[featureName] = stat;
                    }
                }
            });

            return stats;
        }

        private List<PeakTime> CalculatePeakTimes(List<UsageEvent> events)
        {
            var hourly = new Dictionary<int, int>();

            foreach (var evt in events)
            {
                var hour = evt.Timestamp.Hour;
                if (!hourly.ContainsKey(hour))
                    hourly[hour] = 0;
                hourly[hour]++;
            }

            return hourly
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => new PeakTime { Hour = kvp.Key, UsageCount = kvp.Value })
                .ToList();
        }

        private Dictionary<int, int> CalculateHourlyUsage(List<UsageEvent> events)
        {
            var hourly = new Dictionary<int, int>();

            foreach (var evt in events)
            {
                var hour = evt.Timestamp.Hour;
                if (!hourly.ContainsKey(hour))
                    hourly[hour] = 0;
                hourly[hour]++;
            }

            return hourly;
        }

        private double CalculatePreferenceScore(List<UsageEvent> events)
        {
            var frequency = Math.Min(1.0, events.Count / 100.0);
            var successRate = events.Count(e => e.Success) / (double)events.Count;
            var recentUsage = events.Count(e => e.Timestamp > DateTime.UtcNow.AddDays(-7)) / (double)events.Count;

            return (frequency * 0.4) + (successRate * 0.4) + (recentUsage * 0.2);
        }

        public List<(string Feature, double Score)> GetTopFeatures(int count = 10)
        {
            return _featureStats.Values
                .OrderByDescending(s => s.UserPreferenceScore)
                .Take(count)
                .Select(s => (s.FeatureName, s.UserPreferenceScore))
                .ToList();
        }

        public (int Hour, int Count)? GetPeakUsageHour()
        {
            if (_events.Count == 0) return null;

            var grouped = _events.GroupBy(e => e.Timestamp.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return grouped != null ? (grouped.Key, grouped.Count()) : null;
        }

        public bool IsFeaturePeak(string feature, DateTime time)
        {
            if (!_featureStats.TryGetValue(feature, out var stat))
                return false;

            var peaks = stat.PeakTimes.Select(p => p.Hour).ToHashSet();
            return peaks.Contains(time.Hour);
        }

        public double GetFeatureReliability(string feature)
        {
            if (!_featureStats.TryGetValue(feature, out var stat))
                return 0.0;

            return stat.SuccessRate;
        }

        public TimeSpan GetAverageSessionDuration(string? feature = null)
        {
            List<UsageEvent> targetEvents = feature != null
                ? _events.Where(e => e.FeatureName == feature).ToList()
                : _events;

            if (targetEvents.Count == 0)
                return TimeSpan.Zero;

            var avgMs = targetEvents.Average(e => e.DurationMs);
            return TimeSpan.FromMilliseconds(avgMs);
        }

        public List<SeasonalPattern> DetectSeasonalPatterns()
        {
            var patterns = new List<SeasonalPattern>();

            var grouped = _events.GroupBy(e => e.Timestamp.DayOfYear / 7);

            foreach (var week in grouped)
            {
                var weekEvents = week.ToList();
                var avgUsage = weekEvents.Count / 7.0;

                if (avgUsage > _events.Count / (365.0 / 7.0) * 1.5)
                {
                    patterns.Add(new SeasonalPattern
                    {
                        Week = week.Key,
                        PeakUsage = avgUsage,
                        Intensity = Math.Min(2.0, avgUsage / (_events.Count / 52.0))
                    });
                }
            }

            return patterns;
        }

        public Dictionary<string, int> GetFeatureContextFrequency(string feature)
        {
            var contexts = _events
                .Where(e => e.FeatureName == feature && e.Context != null)
                .GroupBy(e => e.Context)
                .ToDictionary(g => g.Key ?? "unknown", g => g.Count());

            return contexts;
        }

        public void EnableDisable(bool enabled) => _isEnabled = enabled;

        public int GetEventCount() => _events.Count;

        public class SeasonalPattern
        {
            public int Week { get; set; }
            public double PeakUsage { get; set; }
            public double Intensity { get; set; }
        }
    }
}
