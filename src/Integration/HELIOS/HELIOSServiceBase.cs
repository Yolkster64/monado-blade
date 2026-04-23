using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonadoBlade.Integration.HELIOS
{
    /// <summary>
    /// Base interface for all HELIOS services
    /// Enables unified service orchestration across GPU, Cloud, Security, and Performance domains
    /// </summary>
    public interface IHELIOSService
    {
        string ServiceName { get; }
        string Version { get; }
        Task InitializeAsync();
        Task ShutdownAsync();
    }

    /// <summary>
    /// HELIOS service registry for service discovery and coordination
    /// </summary>
    public class HELIOSServiceRegistry
    {
        private readonly Dictionary<string, IHELIOSService> _services;
        private readonly Dictionary<string, ServiceStatus> _statusMap;

        public HELIOSServiceRegistry()
        {
            _services = new Dictionary<string, IHELIOSService>();
            _statusMap = new Dictionary<string, ServiceStatus>();
        }

        public void RegisterService(IHELIOSService service)
        {
            _services[service.ServiceName] = service;
            _statusMap[service.ServiceName] = ServiceStatus.Registered;
        }

        public IHELIOSService GetService(string serviceName)
        {
            if (!_services.ContainsKey(serviceName))
                throw new KeyNotFoundException($"Service '{serviceName}' not found");
            return _services[serviceName];
        }

        public async Task InitializeAllAsync()
        {
            foreach (var service in _services.Values)
            {
                try
                {
                    await service.InitializeAsync();
                    _statusMap[service.ServiceName] = ServiceStatus.Running;
                }
                catch (Exception ex)
                {
                    _statusMap[service.ServiceName] = ServiceStatus.Failed;
                    throw new InvalidOperationException($"Failed to initialize {service.ServiceName}: {ex.Message}", ex);
                }
            }
        }

        public async Task ShutdownAllAsync()
        {
            foreach (var service in _services.Values)
            {
                try
                {
                    await service.ShutdownAsync();
                    _statusMap[service.ServiceName] = ServiceStatus.Stopped;
                }
                catch (Exception ex)
                {
                    _statusMap[service.ServiceName] = ServiceStatus.Error;
                }
            }
        }

        public ServiceStatus GetServiceStatus(string serviceName)
        {
            return _statusMap.ContainsKey(serviceName) ? _statusMap[serviceName] : ServiceStatus.Unknown;
        }

        public IReadOnlyDictionary<string, ServiceStatus> GetAllStatuses() => _statusMap;
    }

    public enum ServiceStatus
    {
        Unknown,
        Registered,
        Initializing,
        Running,
        Stopping,
        Stopped,
        Failed,
        Error
    }
}
