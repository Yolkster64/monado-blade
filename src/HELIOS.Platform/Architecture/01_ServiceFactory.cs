using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Service instantiation mode (in-process or remote)
    /// </summary>
    public enum ServiceMode
    {
        InProcess,
        Remote
    }

    /// <summary>
    /// Service descriptor for factory registration
    /// </summary>
    public class ServiceDescriptor
    {
        public string ServiceName { get; set; }
        public Type ServiceInterface { get; set; }
        public Type ServiceImplementation { get; set; }
        public ServiceMode Mode { get; set; }
        public string RemoteEndpoint { get; set; }
        public Func<IServiceFactory, object> Factory { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// Service factory interface for creating services
    /// </summary>
    public interface IServiceFactory
    {
        T CreateService<T>(string serviceName) where T : class;
        object CreateService(string serviceName, Type serviceType);
        void RegisterService(ServiceDescriptor descriptor);
        void RegisterServiceFactory(string serviceName, Func<IServiceFactory, object> factory);
        bool IsServiceRegistered(string serviceName);
        ServiceDescriptor GetServiceDescriptor(string serviceName);
    }

    /// <summary>
    /// Service factory implementation supporting in-process and remote instantiation
    /// </summary>
    public class ServiceFactory : IServiceFactory
    {
        private readonly Dictionary<string, ServiceDescriptor> _registeredServices = new();
        private readonly Dictionary<string, object> _singletonCache = new();
        private readonly object _registryLock = new();

        public ServiceFactory()
        {
            RegisterBuiltInServices();
        }

        public T CreateService<T>(string serviceName) where T : class
        {
            return CreateService(serviceName, typeof(T)) as T;
        }

        public object CreateService(string serviceName, Type serviceType)
        {
            lock (_registryLock)
            {
                if (!_registeredServices.TryGetValue(serviceName, out var descriptor))
                {
                    throw new InvalidOperationException($"Service '{serviceName}' not registered");
                }

                switch (descriptor.Mode)
                {
                    case ServiceMode.InProcess:
                        return CreateInProcessService(descriptor);

                    case ServiceMode.Remote:
                        return CreateRemoteServiceProxy(descriptor, serviceType);

                    default:
                        throw new InvalidOperationException($"Unknown service mode: {descriptor.Mode}");
                }
            }
        }

        public void RegisterService(ServiceDescriptor descriptor)
        {
            lock (_registryLock)
            {
                _registeredServices[descriptor.ServiceName] = descriptor;
            }
        }

        public void RegisterServiceFactory(string serviceName, Func<IServiceFactory, object> factory)
        {
            lock (_registryLock)
            {
                var descriptor = new ServiceDescriptor
                {
                    ServiceName = serviceName,
                    Factory = factory,
                    Mode = ServiceMode.InProcess
                };
                _registeredServices[serviceName] = descriptor;
            }
        }

        public bool IsServiceRegistered(string serviceName)
        {
            lock (_registryLock)
            {
                return _registeredServices.ContainsKey(serviceName);
            }
        }

        public ServiceDescriptor GetServiceDescriptor(string serviceName)
        {
            lock (_registryLock)
            {
                if (_registeredServices.TryGetValue(serviceName, out var descriptor))
                {
                    return descriptor;
                }
                return null;
            }
        }

        private object CreateInProcessService(ServiceDescriptor descriptor)
        {
            if (descriptor.Factory != null)
            {
                return descriptor.Factory(this);
            }

            if (descriptor.ServiceImplementation == null)
            {
                throw new InvalidOperationException($"No factory or implementation for service '{descriptor.ServiceName}'");
            }

            return Activator.CreateInstance(descriptor.ServiceImplementation);
        }

        private object CreateRemoteServiceProxy(ServiceDescriptor descriptor, Type serviceType)
        {
            // Placeholder for remote service proxy creation
            // In production, this would use gRPC, HTTP, or other RPC mechanism
            throw new NotImplementedException("Remote service proxies will be implemented in Phase 3");
        }

        private void RegisterBuiltInServices()
        {
            // Registry will be populated by ServiceRegistry
        }
    }
}
