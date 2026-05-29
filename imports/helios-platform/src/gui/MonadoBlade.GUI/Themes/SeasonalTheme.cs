using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Manages seasonal and event-based theme variations including
    /// holidays, special events, and temporary theme overlays.
    /// </summary>
    public class SeasonalTheme
    {
        public class EventThemeOverlay
        {
            public string EventName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public Color OverlayColor { get; set; }
            public double OverlayOpacity { get; set; }
            public bool IsActive { get; set; }
            public string DecorationType { get; set; } // "Particles", "Lights", "Animation", etc.
        }

        private readonly List<EventThemeOverlay> _eventOverlays;
        private readonly Dictionary<string, EventThemeOverlay> _predefinedEvents;
        private EventThemeOverlay _activeOverlay;

        public event EventHandler<EventThemeChangedEventArgs> EventThemeChanged;

        public SeasonalTheme()
        {
            _eventOverlays = new List<EventThemeOverlay>();
            _predefinedEvents = new Dictionary<string, EventThemeOverlay>();
            InitializePredefinedEvents();
        }

        /// <summary>
        /// Initializes predefined holiday and event themes.
        /// </summary>
        private void InitializePredefinedEvents()
        {
            // New Year's Day - January 1
            _predefinedEvents["NewYear"] = new EventThemeOverlay
            {
                EventName = "New Year's Day",
                StartDate = new DateTime(DateTime.Now.Year, 1, 1),
                EndDate = new DateTime(DateTime.Now.Year, 1, 3),
                OverlayColor = Color.FromRgb(255, 215, 0), // Gold
                OverlayOpacity = 0.15,
                DecorationType = "Lights"
            };

            // Valentine's Day - February 14
            _predefinedEvents["Valentines"] = new EventThemeOverlay
            {
                EventName = "Valentine's Day",
                StartDate = new DateTime(DateTime.Now.Year, 2, 14),
                EndDate = new DateTime(DateTime.Now.Year, 2, 15),
                OverlayColor = Color.FromRgb(255, 105, 180), // Hot pink
                OverlayOpacity = 0.12,
                DecorationType = "Particles"
            };

            // Easter - varies, approximate with Spring
            _predefinedEvents["Easter"] = new EventThemeOverlay
            {
                EventName = "Easter",
                StartDate = new DateTime(DateTime.Now.Year, 4, 1),
                EndDate = new DateTime(DateTime.Now.Year, 4, 30),
                OverlayColor = Color.FromRgb(144, 238, 144), // Light green
                OverlayOpacity = 0.1,
                DecorationType = "Animation"
            };

            // Independence Day - July 4
            _predefinedEvents["IndependenceDay"] = new EventThemeOverlay
            {
                EventName = "Independence Day",
                StartDate = new DateTime(DateTime.Now.Year, 7, 4),
                EndDate = new DateTime(DateTime.Now.Year, 7, 5),
                OverlayColor = Color.FromRgb(178, 34, 52), // Red
                OverlayOpacity = 0.2,
                DecorationType = "Lights"
            };

            // Halloween - October 31
            _predefinedEvents["Halloween"] = new EventThemeOverlay
            {
                EventName = "Halloween",
                StartDate = new DateTime(DateTime.Now.Year, 10, 25),
                EndDate = new DateTime(DateTime.Now.Year, 10, 31),
                OverlayColor = Color.FromRgb(255, 140, 0), // Dark orange
                OverlayOpacity = 0.25,
                DecorationType = "Animation"
            };

            // Christmas - December 25
            _predefinedEvents["Christmas"] = new EventThemeOverlay
            {
                EventName = "Christmas",
                StartDate = new DateTime(DateTime.Now.Year, 12, 15),
                EndDate = new DateTime(DateTime.Now.Year, 12, 26),
                OverlayColor = Color.FromRgb(220, 20, 60), // Crimson
                OverlayOpacity = 0.18,
                DecorationType = "Lights"
            };

            // New Year's Eve - December 31
            _predefinedEvents["NewYearsEve"] = new EventThemeOverlay
            {
                EventName = "New Year's Eve",
                StartDate = new DateTime(DateTime.Now.Year, 12, 30),
                EndDate = new DateTime(DateTime.Now.Year, 12, 31),
                OverlayColor = Color.FromRgb(255, 215, 0), // Gold
                OverlayOpacity = 0.2,
                DecorationType = "Lights"
            };
        }

        /// <summary>
        /// Updates active overlays based on current date.
        /// </summary>
        public void UpdateActiveOverlays()
        {
            var now = DateTime.Now;
            EventThemeOverlay newActiveOverlay = null;

            foreach (var overlay in _eventOverlays)
            {
                if (now >= overlay.StartDate && now <= overlay.EndDate)
                {
                    overlay.IsActive = true;
                    if (newActiveOverlay == null)
                        newActiveOverlay = overlay;
                }
                else
                {
                    overlay.IsActive = false;
                }
            }

            // Also check predefined events
            foreach (var predefined in _predefinedEvents.Values)
            {
                if (now >= predefined.StartDate && now <= predefined.EndDate)
                {
                    predefined.IsActive = true;
                    if (newActiveOverlay == null)
                        newActiveOverlay = predefined;
                }
                else
                {
                    predefined.IsActive = false;
                }
            }

            if (newActiveOverlay != _activeOverlay)
            {
                var oldOverlay = _activeOverlay;
                _activeOverlay = newActiveOverlay;
                OnEventThemeChanged(new EventThemeChangedEventArgs
                {
                    PreviousEventName = oldOverlay?.EventName ?? "None",
                    CurrentEventName = newActiveOverlay?.EventName ?? "None",
                    OverlayColor = newActiveOverlay?.OverlayColor,
                    OverlayOpacity = newActiveOverlay?.OverlayOpacity ?? 0,
                    IsEventActive = newActiveOverlay != null
                });
            }
        }

        /// <summary>
        /// Activates a predefined event theme by name.
        /// </summary>
        public bool ActivatePredefinedEvent(string eventName)
        {
            if (_predefinedEvents.TryGetValue(eventName, out var overlay))
            {
                _eventOverlays.Add(overlay);
                overlay.IsActive = true;
                OnEventThemeChanged(new EventThemeChangedEventArgs
                {
                    CurrentEventName = overlay.EventName,
                    OverlayColor = overlay.OverlayColor,
                    OverlayOpacity = overlay.OverlayOpacity,
                    IsEventActive = true
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a custom event theme overlay.
        /// </summary>
        public void AddCustomEventOverlay(EventThemeOverlay overlay)
        {
            if (overlay != null && !string.IsNullOrEmpty(overlay.EventName))
            {
                _eventOverlays.Add(overlay);
                UpdateActiveOverlays();
            }
        }

        /// <summary>
        /// Removes an event theme overlay.
        /// </summary>
        public bool RemoveEventOverlay(string eventName)
        {
            var overlay = _eventOverlays.Find(o => o.EventName == eventName);
            if (overlay != null)
            {
                _eventOverlays.Remove(overlay);
                if (_activeOverlay == overlay)
                {
                    _activeOverlay = null;
                }
                UpdateActiveOverlays();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the currently active event theme overlay.
        /// </summary>
        public EventThemeOverlay GetActiveOverlay()
        {
            return _activeOverlay;
        }

        /// <summary>
        /// Gets an overlay by event name.
        /// </summary>
        public EventThemeOverlay GetEventOverlay(string eventName)
        {
            var customOverlay = _eventOverlays.Find(o => o.EventName == eventName);
            if (customOverlay != null)
                return customOverlay;

            _predefinedEvents.TryGetValue(eventName, out var predefinedOverlay);
            return predefinedOverlay;
        }

        /// <summary>
        /// Gets the anniversary theme for a specific date.
        /// Useful for app launch anniversaries or special milestones.
        /// </summary>
        public EventThemeOverlay GetAnniversaryTheme(DateTime anniversaryDate, string name)
        {
            var now = DateTime.Now;
            var isAnniversary = now.Month == anniversaryDate.Month && now.Day == anniversaryDate.Day;

            if (isAnniversary)
            {
                return new EventThemeOverlay
                {
                    EventName = name,
                    StartDate = now.Date,
                    EndDate = now.Date.AddHours(23).AddMinutes(59),
                    OverlayColor = Color.FromRgb(0, 217, 255), // Cyan - Monado blue
                    OverlayOpacity = 0.2,
                    DecorationType = "Lights"
                };
            }

            return null;
        }

        /// <summary>
        /// Blends overlay color with base color.
        /// </summary>
        public Color BlendOverlayColor(Color baseColor, Color overlayColor, double opacity)
        {
            var inverseOpacity = 1.0 - opacity;
            return Color.FromRgb(
                (byte)(baseColor.R * inverseOpacity + overlayColor.R * opacity),
                (byte)(baseColor.G * inverseOpacity + overlayColor.G * opacity),
                (byte)(baseColor.B * inverseOpacity + overlayColor.B * opacity)
            );
        }

        /// <summary>
        /// Gets all registered event overlays.
        /// </summary>
        public IReadOnlyList<EventThemeOverlay> GetAllOverlays()
        {
            return _eventOverlays.AsReadOnly();
        }

        /// <summary>
        /// Gets all predefined events.
        /// </summary>
        public IReadOnlyDictionary<string, EventThemeOverlay> GetPredefinedEvents()
        {
            return _predefinedEvents;
        }

        /// <summary>
        /// Checks if any overlay is currently active.
        /// </summary>
        public bool IsEventThemeActive => _activeOverlay != null;

        protected virtual void OnEventThemeChanged(EventThemeChangedEventArgs e)
        {
            EventThemeChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event args for seasonal/event theme changes.
    /// </summary>
    public class EventThemeChangedEventArgs : EventArgs
    {
        public string PreviousEventName { get; set; }
        public string CurrentEventName { get; set; }
        public Color? OverlayColor { get; set; }
        public double OverlayOpacity { get; set; }
        public bool IsEventActive { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
