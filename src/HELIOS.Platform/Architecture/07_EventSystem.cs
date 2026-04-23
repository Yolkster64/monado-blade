using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Event published in the system
    /// </summary>
    public class Event
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; }
        public object Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Event handler delegate
    /// </summary>
    public delegate Task EventHandler(Event evt);

    /// <summary>
    /// Event filter for routing
    /// </summary>
    public interface IEventFilter
    {
        bool Matches(Event evt);
    }

    /// <summary>
    /// Simple event type filter
    /// </summary>
    public class EventTypeFilter : IEventFilter
    {
        private readonly string _eventType;

        public EventTypeFilter(string eventType)
        {
            _eventType = eventType;
        }

        public bool Matches(Event evt)
        {
            return evt.EventType == _eventType;
        }
    }

    /// <summary>
    /// Wildcard event filter
    /// </summary>
    public class WildcardEventFilter : IEventFilter
    {
        private readonly string _pattern;

        public WildcardEventFilter(string pattern)
        {
            _pattern = pattern; // e.g., "Service*" or "*Health*"
        }

        public bool Matches(Event evt)
        {
            // Simple wildcard matching
            if (_pattern.EndsWith("*") && _pattern.StartsWith("*"))
            {
                return evt.EventType.Contains(_pattern.Substring(1, _pattern.Length - 2));
            }
            if (_pattern.EndsWith("*"))
            {
                return evt.EventType.StartsWith(_pattern.Substring(0, _pattern.Length - 1));
            }
            if (_pattern.StartsWith("*"))
            {
                return evt.EventType.EndsWith(_pattern.Substring(1));
            }
            return evt.EventType == _pattern;
        }
    }

    /// <summary>
    /// Event bus interface
    /// </summary>
    public interface IEventBus
    {
        void Subscribe(string eventType, EventHandler handler);
        void Subscribe(IEventFilter filter, EventHandler handler);
        void Unsubscribe(string eventType, EventHandler handler);
        void PublishEvent(Event evt);
        Task PublishEventAsync(Event evt);
    }

    /// <summary>
    /// Event bus implementation supporting publish/subscribe pattern
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<string, List<EventHandler>> _typeSubscriptions = new();
        private readonly List<(IEventFilter filter, EventHandler handler)> _filterSubscriptions = new();
        private readonly object _lock = new();
        private readonly List<Event> _eventHistory = new();
        private readonly int _maxHistorySize = 1000;

        public void Subscribe(string eventType, EventHandler handler)
        {
            lock (_lock)
            {
                if (!_typeSubscriptions.ContainsKey(eventType))
                {
                    _typeSubscriptions[eventType] = new();
                }
                _typeSubscriptions[eventType].Add(handler);
            }
        }

        public void Subscribe(IEventFilter filter, EventHandler handler)
        {
            lock (_lock)
            {
                _filterSubscriptions.Add((filter, handler));
            }
        }

        public void Unsubscribe(string eventType, EventHandler handler)
        {
            lock (_lock)
            {
                if (_typeSubscriptions.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }

        public void PublishEvent(Event evt)
        {
            List<EventHandler> handlers = new();

            lock (_lock)
            {
                // Add type-based subscribers
                if (_typeSubscriptions.TryGetValue(evt.EventType, out var typeHandlers))
                {
                    handlers.AddRange(typeHandlers);
                }

                // Add filter-based subscribers
                foreach (var (filter, handler) in _filterSubscriptions)
                {
                    if (filter.Matches(evt))
                    {
                        handlers.Add(handler);
                    }
                }

                // Store in history
                _eventHistory.Add(evt);
                if (_eventHistory.Count > _maxHistorySize)
                {
                    _eventHistory.RemoveAt(0);
                }
            }

            // Execute handlers outside the lock
            foreach (var handler in handlers)
            {
                try
                {
                    handler(evt).Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other handlers
                }
            }
        }

        public async Task PublishEventAsync(Event evt)
        {
            List<EventHandler> handlers = new();

            lock (_lock)
            {
                if (_typeSubscriptions.TryGetValue(evt.EventType, out var typeHandlers))
                {
                    handlers.AddRange(typeHandlers);
                }

                foreach (var (filter, handler) in _filterSubscriptions)
                {
                    if (filter.Matches(evt))
                    {
                        handlers.Add(handler);
                    }
                }

                _eventHistory.Add(evt);
                if (_eventHistory.Count > _maxHistorySize)
                {
                    _eventHistory.RemoveAt(0);
                }
            }

            foreach (var handler in handlers)
            {
                try
                {
                    await handler(evt);
                }
                catch (Exception ex)
                {
                    // Log error but continue
                }
            }
        }

        public IEnumerable<Event> GetEventHistory(int limit = 100)
        {
            lock (_lock)
            {
                var count = Math.Min(limit, _eventHistory.Count);
                return _eventHistory.GetRange(_eventHistory.Count - count, count);
            }
        }
    }

    /// <summary>
    /// Common event types used throughout the system
    /// </summary>
    public static class CommonEventTypes
    {
        // Boot events
        public const string BootStarted = "BootStarted";
        public const string BootProgress = "BootProgress";
        public const string BootCompleted = "BootCompleted";
        public const string BootFailed = "BootFailed";

        // Update events
        public const string UpdateStarted = "UpdateStarted";
        public const string UpdateProgress = "UpdateProgress";
        public const string UpdateCompleted = "UpdateCompleted";
        public const string UpdateFailed = "UpdateFailed";

        // Service events
        public const string ServiceStarted = "ServiceStarted";
        public const string ServiceStopped = "ServiceStopped";
        public const string ServiceFailed = "ServiceFailed";

        // Profile events
        public const string ProfileChanged = "ProfileChanged";
        public const string ProfileSwitched = "ProfileSwitched";

        // Health events
        public const string HealthCheckStarted = "HealthCheckStarted";
        public const string HealthCheckCompleted = "HealthCheckCompleted";
        public const string HealthDegraded = "HealthDegraded";
        public const string HealthRecovered = "HealthRecovered";

        // Resource events
        public const string ResourceWarning = "ResourceWarning";
        public const string ResourceCritical = "ResourceCritical";

        // Plugin events
        public const string PluginLoaded = "PluginLoaded";
        public const string PluginUnloaded = "PluginUnloaded";
    }
}
