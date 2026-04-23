using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Service metadata for registry tracking
    /// </summary>
    public class ServiceMetadata
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public Type InterfaceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceMode Mode { get; set; }
        public DateTime RegisteredAt { get; set; }
        public ServiceHealthStatus HealthStatus { get; set; } = ServiceHealthStatus.Unknown;
        public Dictionary<string, object> Tags { get; set; } = new();
    }

    public enum ServiceHealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy,
        Offline
    }

    /// <summary>
    /// Service registry for discovering and managing services
    /// </summary>
    public interface IServiceRegistry
    {
        void Register(ServiceMetadata metadata);
        void Unregister(string serviceName);
        ServiceMetadata Discover(string serviceName);
        IEnumerable<ServiceMetadata> DiscoverByTag(string tagKey, object tagValue);
        IEnumerable<ServiceMetadata> GetAllServices();
        void UpdateHealth(string serviceName, ServiceHealthStatus status);
        ServiceHealthStatus GetHealth(string serviceName);
        bool IsHealthy(string serviceName);
    }

    /// <summary>
    /// Service registry implementation
    /// </summary>
    public class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<string, ServiceMetadata> _registry = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly EventBus _eventBus;

        public ServiceRegistry(EventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void Register(ServiceMetadata metadata)
        {
            _lock.EnterWriteLock();
            try
            {
                metadata.RegisteredAt = DateTime.UtcNow;
                _registry[metadata.Name] = metadata;
                _eventBus?.PublishEvent(new Event
                {
                    EventType = "ServiceRegistered",
                    Data = new { ServiceName = metadata.Name, Version = metadata.Version }
                });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Unregister(string serviceName)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_registry.Remove(serviceName))
                {
                    _eventBus?.PublishEvent(new Event
                    {
                        EventType = "ServiceUnregistered",
                        Data = new { ServiceName = serviceName }
                    });
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public ServiceMetadata Discover(string serviceName)
        {
            _lock.EnterReadLock();
            try
            {
                _registry.TryGetValue(serviceName, out var metadata);
                return metadata;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<ServiceMetadata> DiscoverByTag(string tagKey, object tagValue)
        {
            _lock.EnterReadLock();
            try
            {
                return _registry.Values
                    .Where(m => m.Tags.ContainsKey(tagKey) && m.Tags[tagKey].Equals(tagValue))
                    .ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<ServiceMetadata> GetAllServices()
        {
            _lock.EnterReadLock();
            try
            {
                return _registry.Values.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void UpdateHealth(string serviceName, ServiceHealthStatus status)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_registry.TryGetValue(serviceName, out var metadata))
                {
                    metadata.HealthStatus = status;
                    _eventBus?.PublishEvent(new Event
                    {
                        EventType = "ServiceHealthUpdated",
                        Data = new { ServiceName = serviceName, Status = status }
                    });
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public ServiceHealthStatus GetHealth(string serviceName)
        {
            _lock.EnterReadLock();
            try
            {
                if (_registry.TryGetValue(serviceName, out var metadata))
                {
                    return metadata.HealthStatus;
                }
                return ServiceHealthStatus.Unknown;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool IsHealthy(string serviceName)
        {
            return GetHealth(serviceName) == ServiceHealthStatus.Healthy;
        }
    }
}
