using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI
{
    /// <summary>
    /// Learns from user behavior and adapts system features and UI for better experience.
    /// </summary>
    public class AdaptiveFeatures
    {
        private readonly Dictionary<string, FeatureAdaptation> _adaptations = new();
        private readonly Dictionary<string, UserPreference> _preferences = new();
        private readonly List<InteractionEvent> _interactions = new();
        private bool _isEnabled = true;

        public class FeatureAdaptation
        {
            public string FeatureId { get; set; } = string.Empty;
            public bool IsVisible { get; set; } = true;
            public int DisplayOrder { get; set; }
            public double RelevanceScore { get; set; }
            public AdaptationReason LastAdaptationReason { get; set; }
            public DateTime LastAdapted { get; set; }
            public bool IsRecommended { get; set; }
        }

        public class UserPreference
        {
            public string PreferenceKey { get; set; } = string.Empty;
            public string PreferenceValue { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public int SampleSize { get; set; }
            public DateTime LearnedAt { get; set; }
        }

        public class InteractionEvent
        {
            public DateTime Timestamp { get; set; }
            public string FeatureId { get; set; } = string.Empty;
            public string InteractionType { get; set; } = string.Empty; // click, hover, focus, etc.
            public int DurationMs { get; set; }
            public bool WasSuccessful { get; set; }
        }

        public enum AdaptationReason
        {
            HighUsage,
            LowUsage,
            UserPreference,
            LearningBasedRecommendation,
            Performance,
            Personalization
        }

        public void RecordInteraction(string featureId, string interactionType, int durationMs, bool successful)
        {
            if (!_isEnabled) return;

            lock (_interactions)
            {
                _interactions.Add(new InteractionEvent
                {
                    Timestamp = DateTime.UtcNow,
                    FeatureId = featureId,
                    InteractionType = interactionType,
                    DurationMs = durationMs,
                    WasSuccessful = successful
                });

                if (_interactions.Count > 10000)
                    _interactions.RemoveAt(0);
            }
        }

        public async Task AdaptInterface()
        {
            if (!_isEnabled || _interactions.Count < 10)
                return;

            await Task.Run(() =>
            {
                lock (_interactions)
                {
                    var grouped = _interactions.GroupBy(i => i.FeatureId);

                    foreach (var group in grouped)
                    {
                        var events = group.ToList();
                        var featureId = group.Key;

                        if (!_adaptations.ContainsKey(featureId))
                        {
                            _adaptations[featureId] = new FeatureAdaptation
                            {
                                FeatureId = featureId,
                                DisplayOrder = _adaptations.Count
                            };
                        }

                        var adaptation = _adaptations[featureId];

                        // Calculate usage metrics
                        var usageCount = events.Count;
                        var successRate = events.Count(e => e.WasSuccessful) / (double)events.Count;
                        var avgDuration = events.Average(e => e.DurationMs);

                        // Adapt visibility
                        if (usageCount < 2 && (DateTime.UtcNow - events.Max(e => e.Timestamp)).TotalDays > 7)
                        {
                            adaptation.IsVisible = false;
                            adaptation.LastAdaptationReason = AdaptationReason.LowUsage;
                        }
                        else if (usageCount > 50)
                        {
                            adaptation.IsVisible = true;
                            adaptation.LastAdaptationReason = AdaptationReason.HighUsage;
                        }

                        // Calculate relevance score
                        adaptation.RelevanceScore = (Math.Min(1.0, usageCount / 100.0) * 0.5) +
                                                   (successRate * 0.3) +
                                                   (1.0 - Math.Min(1.0, avgDuration / 5000.0) * 0.2);

                        // Recommend if highly relevant
                        adaptation.IsRecommended = adaptation.RelevanceScore > 0.7;
                        adaptation.LastAdapted = DateTime.UtcNow;
                    }
                }
            });
        }

        public async Task LearnPreferences()
        {
            if (!_isEnabled || _interactions.Count < 20)
                return;

            await Task.Run(() =>
            {
                lock (_interactions)
                {
                    // Learn UI complexity preference
                    LearnUiComplexityPreference();

                    // Learn theme preference
                    LearnThemePreference();

                    // Learn feature preferences
                    LearnFeaturePreferences();

                    // Learn navigation preferences
                    LearnNavigationPreferences();
                }
            });
        }

        private void LearnUiComplexityPreference()
        {
            var advancedFeatureUsage = _interactions
                .Where(i => i.FeatureId.Contains("Advanced"))
                .Count();

            var totalUsage = _interactions.Count;
            var ratio = advancedFeatureUsage / (double)totalUsage;

            var preference = new UserPreference
            {
                PreferenceKey = "ui_complexity",
                PreferenceValue = ratio > 0.3 ? "advanced" : "beginner",
                Confidence = Math.Min(1.0, advancedFeatureUsage / 20.0),
                SampleSize = totalUsage,
                LearnedAt = DateTime.UtcNow
            };

            _preferences["ui_complexity"] = preference;
        }

        private void LearnThemePreference()
        {
            var eveningInteractions = _interactions
                .Where(i => i.Timestamp.Hour >= 18 || i.Timestamp.Hour < 6)
                .Count();

            var morningInteractions = _interactions
                .Where(i => i.Timestamp.Hour >= 6 && i.Timestamp.Hour < 18)
                .Count();

            var theme = eveningInteractions > morningInteractions ? "dark" : "light";

            var preference = new UserPreference
            {
                PreferenceKey = "theme",
                PreferenceValue = theme,
                Confidence = Math.Abs(eveningInteractions - morningInteractions) / (double)(_interactions.Count + 1),
                SampleSize = _interactions.Count,
                LearnedAt = DateTime.UtcNow
            };

            _preferences["theme"] = preference;
        }

        private void LearnFeaturePreferences()
        {
            var featureGroups = _interactions.GroupBy(i => i.FeatureId);

            foreach (var group in featureGroups)
            {
                var successRate = group.Count(i => i.WasSuccessful) / (double)group.Count();
                var prefKey = $"feature_satisfaction_{group.Key}";

                var preference = new UserPreference
                {
                    PreferenceKey = prefKey,
                    PreferenceValue = successRate.ToString("F2"),
                    Confidence = Math.Min(1.0, group.Count() / 50.0),
                    SampleSize = group.Count(),
                    LearnedAt = DateTime.UtcNow
                };

                _preferences[prefKey] = preference;
            }
        }

        private void LearnNavigationPreferences()
        {
            var keyboardInteractions = _interactions.Count(i => i.InteractionType == "keyboard");
            var mouseInteractions = _interactions.Count(i => i.InteractionType == "mouse");
            var touchInteractions = _interactions.Count(i => i.InteractionType == "touch");

            var total = keyboardInteractions + mouseInteractions + touchInteractions;
            var preferredInput = "mouse";

            if (keyboardInteractions > total * 0.5)
                preferredInput = "keyboard";
            else if (touchInteractions > total * 0.5)
                preferredInput = "touch";

            var preference = new UserPreference
            {
                PreferenceKey = "input_method",
                PreferenceValue = preferredInput,
                Confidence = Math.Max(keyboardInteractions, Math.Max(mouseInteractions, touchInteractions)) / (double)total,
                SampleSize = total,
                LearnedAt = DateTime.UtcNow
            };

            _preferences["input_method"] = preference;
        }

        public string GetRecommendedTheme()
        {
            if (_preferences.TryGetValue("theme", out var pref))
                return pref.PreferenceValue;

            var now = DateTime.Now.Hour;
            return now >= 18 || now < 6 ? "dark" : "light";
        }

        public string GetRecommendedUiComplexity()
        {
            if (_preferences.TryGetValue("ui_complexity", out var pref) && pref.Confidence > 0.6)
                return pref.PreferenceValue;

            return "beginner";
        }

        public List<string> GetRecommendedFeatures(int count = 5)
        {
            return _adaptations.Values
                .Where(a => a.IsRecommended && a.IsVisible)
                .OrderByDescending(a => a.RelevanceScore)
                .Take(count)
                .Select(a => a.FeatureId)
                .ToList();
        }

        public List<(string Feature, double Score)> RankFeaturesByUsability()
        {
            return _adaptations.Values
                .OrderByDescending(a => a.RelevanceScore)
                .Select(a => (a.FeatureId, a.RelevanceScore))
                .ToList();
        }

        public bool ShouldShowFeature(string featureId)
        {
            if (_adaptations.TryGetValue(featureId, out var adaptation))
                return adaptation.IsVisible;

            return true;
        }

        public int GetFeatureDisplayOrder(string featureId)
        {
            if (_adaptations.TryGetValue(featureId, out var adaptation))
                return adaptation.DisplayOrder;

            return int.MaxValue;
        }

        public Dictionary<string, string> GetAllLearnedPreferences()
        {
            return _preferences
                .Where(p => p.Value.Confidence > 0.6)
                .ToDictionary(p => p.Key, p => p.Value.PreferenceValue);
        }

        public double GetAdaptationConfidence(string featureId)
        {
            if (_adaptations.TryGetValue(featureId, out var adaptation))
                return adaptation.RelevanceScore;

            return 0.0;
        }

        public void ResetAdaptations()
        {
            lock (_interactions)
            {
                _adaptations.Clear();
                _preferences.Clear();
            }
        }

        public void EnableDisable(bool enabled) => _isEnabled = enabled;

        public int GetInteractionCount() => _interactions.Count;
    }
}
