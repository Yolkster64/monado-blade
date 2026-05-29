using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Manages time-aware theme transitions with day/night cycles,
    /// time-of-day specific colors, and seasonal variations.
    /// </summary>
    public class TimeAwareTheme
    {
        public class TimeThemeDefinition
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public Color PrimaryColor { get; set; }
            public Color SecondaryColor { get; set; }
            public Color BackgroundColor { get; set; }
            public string ThemeName { get; set; }
            public double Brightness { get; set; }
            public double Saturation { get; set; }
        }

        private readonly List<TimeThemeDefinition> _timeThemes;
        private TimeThemeDefinition _currentTheme;
        private DateTime _lastUpdateTime;
        private TimeSpan _transitionSmoothness;
        private Season _currentSeason;

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public enum Season
        {
            Spring,
            Summer,
            Autumn,
            Winter
        }

        public TimeAwareTheme()
        {
            _timeThemes = new List<TimeThemeDefinition>();
            _lastUpdateTime = DateTime.Now;
            _transitionSmoothness = TimeSpan.FromSeconds(30);
            InitializeDefaultTimeThemes();
            DetermineSeason();
            UpdateTheme();
        }

        /// <summary>
        /// Initializes the default time-based themes.
        /// </summary>
        private void InitializeDefaultTimeThemes()
        {
            // Early Morning: 5:00 - 6:30 AM
            _timeThemes.Add(new TimeThemeDefinition
            {
                StartTime = TimeSpan.FromHours(5),
                EndTime = TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(30)),
                ThemeName = "EarlyMorning",
                PrimaryColor = Color.FromRgb(255, 140, 100), // Warm orange
                SecondaryColor = Color.FromRgb(200, 100, 60), // Darker orange
                BackgroundColor = Color.FromRgb(20, 40, 60), // Dark blue
                Brightness = 0.6,
                Saturation = 0.7
            });

            // Morning: 6:30 - 9:00 AM
            _timeThemes.Add(new TimeThemeDefinition
            {
                StartTime = TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(30)),
                EndTime = TimeSpan.FromHours(9),
                ThemeName = "Morning",
                PrimaryColor = Color.FromRgb(255, 180, 100), // Light orange
                SecondaryColor = Color.FromRgb(200, 150, 80), // Muted orange
                BackgroundColor = Color.FromRgb(40, 70, 100), // Medium blue
                Brightness = 0.75,
                Saturation = 0.8
            });

            // Daytime: 9:00 AM - 5:00 PM
            _timeThemes.Add(new TimeThemeDefinition
            {
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17),
                ThemeName = "Daytime",
                PrimaryColor = Color.FromRgb(0, 217, 255), // Cyan
                SecondaryColor = Color.FromRgb(0, 100, 150), // Dark cyan
                BackgroundColor = Color.FromRgb(245, 247, 250), // Light background
                Brightness = 1.0,
                Saturation = 1.0
            });

            // Evening: 5:00 - 7:00 PM
            _timeThemes.Add(new TimeThemeDefinition
            {
                StartTime = TimeSpan.FromHours(17),
                EndTime = TimeSpan.FromHours(19),
                ThemeName = "Evening",
                PrimaryColor = Color.FromRgb(255, 140, 80), // Warm orange
                SecondaryColor = Color.FromRgb(200, 100, 50), // Deep orange
                BackgroundColor = Color.FromRgb(30, 50, 70), // Dark blue
                Brightness = 0.7,
                Saturation = 0.85
            });

            // Night: 7:00 PM - 5:00 AM
            _timeThemes.Add(new TimeThemeDefinition
            {
                StartTime = TimeSpan.FromHours(19),
                EndTime = TimeSpan.FromHours(5).AddDays(1), // Wraps to next day
                ThemeName = "Night",
                PrimaryColor = Color.FromRgb(0, 200, 255), // Bright cyan
                SecondaryColor = Color.FromRgb(0, 150, 200), // Medium cyan
                BackgroundColor = Color.FromRgb(10, 20, 40), // Very dark blue
                Brightness = 0.5,
                Saturation = 0.9
            });
        }

        /// <summary>
        /// Determines the current season based on date.
        /// </summary>
        private void DetermineSeason()
        {
            int month = DateTime.Now.Month;
            if (month >= 3 && month <= 5)
                _currentSeason = Season.Spring;
            else if (month >= 6 && month <= 8)
                _currentSeason = Season.Summer;
            else if (month >= 9 && month <= 11)
                _currentSeason = Season.Autumn;
            else
                _currentSeason = Season.Winter;
        }

        /// <summary>
        /// Updates the current theme based on time of day.
        /// </summary>
        public void UpdateTheme()
        {
            var now = DateTime.Now.TimeOfDay;
            var newTheme = GetThemeForTime(now);

            if (newTheme != _currentTheme)
            {
                var oldTheme = _currentTheme;
                _currentTheme = newTheme;
                OnThemeChanged(new ThemeChangedEventArgs
                {
                    OldThemeName = oldTheme?.ThemeName ?? "None",
                    NewThemeName = newTheme.ThemeName,
                    Reason = "TimeUpdate",
                    TransitionDuration = _transitionSmoothness
                });
            }

            _lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// Gets the theme definition for a specific time.
        /// </summary>
        public TimeThemeDefinition GetThemeForTime(TimeSpan time)
        {
            // Handle night theme that wraps past midnight
            var nightTheme = _timeThemes.FirstOrDefault(t => t.ThemeName == "Night");
            if (nightTheme != null && time >= nightTheme.StartTime)
                return nightTheme;

            return _timeThemes.FirstOrDefault(t =>
                time >= t.StartTime && time < t.EndTime) ?? _timeThemes.First();
        }

        /// <summary>
        /// Gets interpolated colors for smooth transitions between time periods.
        /// </summary>
        public (Color primary, Color secondary, Color background) GetInterpolatedColors(TimeSpan time)
        {
            var currentTheme = GetThemeForTime(time);
            var nextTheme = GetNextTheme(currentTheme);

            if (nextTheme == currentTheme)
                return (currentTheme.PrimaryColor, currentTheme.SecondaryColor, currentTheme.BackgroundColor);

            // Calculate progress through the current theme period
            var periodStart = currentTheme.StartTime;
            var periodEnd = nextTheme.StartTime;

            if (periodEnd <= periodStart)
                periodEnd = periodEnd.Add(TimeSpan.FromHours(24));

            var periodDuration = periodEnd - periodStart;
            var timeInPeriod = time - periodStart;
            if (timeInPeriod.TotalSeconds < 0)
                timeInPeriod = timeInPeriod.Add(TimeSpan.FromHours(24));

            var progress = Math.Min(1.0, timeInPeriod.TotalSeconds / periodDuration.TotalSeconds);

            return InterpolateColors(currentTheme, nextTheme, progress);
        }

        /// <summary>
        /// Interpolates between two color sets based on progress (0-1).
        /// </summary>
        private (Color primary, Color secondary, Color background) InterpolateColors(
            TimeThemeDefinition from,
            TimeThemeDefinition to,
            double progress)
        {
            return (
                InterpolateColor(from.PrimaryColor, to.PrimaryColor, progress),
                InterpolateColor(from.SecondaryColor, to.SecondaryColor, progress),
                InterpolateColor(from.BackgroundColor, to.BackgroundColor, progress)
            );
        }

        /// <summary>
        /// Linearly interpolates between two colors.
        /// </summary>
        private Color InterpolateColor(Color from, Color to, double progress)
        {
            return Color.FromRgb(
                (byte)(from.R + (to.R - from.R) * progress),
                (byte)(from.G + (to.G - from.G) * progress),
                (byte)(from.B + (to.B - from.B) * progress)
            );
        }

        /// <summary>
        /// Gets the next theme in sequence.
        /// </summary>
        private TimeThemeDefinition GetNextTheme(TimeThemeDefinition current)
        {
            var index = _timeThemes.IndexOf(current);
            return _timeThemes[(index + 1) % _timeThemes.Count];
        }

        /// <summary>
        /// Applies seasonal color tints to the current theme.
        /// </summary>
        public void ApplySeasonalVariation(TimeThemeDefinition theme)
        {
            switch (_currentSeason)
            {
                case Season.Spring:
                    // Increase saturation, add slight green tint
                    theme.Saturation = Math.Min(1.0, theme.Saturation + 0.1);
                    break;
                case Season.Summer:
                    // Increase brightness
                    theme.Brightness = Math.Min(1.0, theme.Brightness + 0.15);
                    break;
                case Season.Autumn:
                    // Shift toward warm colors
                    var warmTint = Color.FromRgb(
                        (byte)Math.Min(255, theme.PrimaryColor.R + 30),
                        (byte)Math.Max(0, theme.PrimaryColor.G - 10),
                        (byte)Math.Max(0, theme.PrimaryColor.B - 10)
                    );
                    theme.PrimaryColor = warmTint;
                    break;
                case Season.Winter:
                    // Increase cool tones, reduce brightness slightly
                    theme.Brightness = Math.Max(0.3, theme.Brightness - 0.1);
                    break;
            }
        }

        /// <summary>
        /// Gets the current season.
        /// </summary>
        public Season CurrentSeason => _currentSeason;

        /// <summary>
        /// Gets the current active theme.
        /// </summary>
        public TimeThemeDefinition CurrentTheme => _currentTheme;

        /// <summary>
        /// Sets the transition smoothness duration.
        /// </summary>
        public void SetTransitionSmoothness(TimeSpan duration)
        {
            _transitionSmoothness = duration;
        }

        /// <summary>
        /// Gets all available themes.
        /// </summary>
        public IReadOnlyList<TimeThemeDefinition> AllThemes => _timeThemes.AsReadOnly();

        /// <summary>
        /// Adds a custom time theme.
        /// </summary>
        public void AddCustomTheme(TimeThemeDefinition theme)
        {
            if (theme != null && !string.IsNullOrEmpty(theme.ThemeName))
            {
                _timeThemes.Add(theme);
            }
        }

        protected virtual void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event args for theme change notifications.
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public string OldThemeName { get; set; }
        public string NewThemeName { get; set; }
        public string Reason { get; set; }
        public TimeSpan TransitionDuration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
