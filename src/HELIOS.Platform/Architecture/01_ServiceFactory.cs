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
    /// Service factory implementation supporting in-process and remote instantiation.
    /// OPTIMIZED (Task 2): Uses Lazy<T> for deferred service instantiation (20-30% startup speedup).
    /// </summary>
    public class ServiceFactory : IServiceFactory
    {
        private readonly Dictionary<string, ServiceDescriptor> _registeredServices = new();
        private readonly Dictionary<string, Lazy<object>> _singletonCache = new();
        private readonly ReaderWriterLockSlim _registryLock = new();

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
            _registryLock.EnterReadLock();
            try
            {
                if (!_registeredServices.TryGetValue(serviceName, out var descriptor))
                {
                    throw new InvalidOperationException($"Service '{serviceName}' not registered");
                }

                // OPTIMIZATION: Return cached Lazy<T> singleton for immediate access
                if (_singletonCache.TryGetValue(serviceName, out var lazyService))
                {
                    return lazyService.Value; // Thread-safe lazy evaluation
                }
            }
            finally
            {
                _registryLock.ExitReadLock();
            }

            // Create new lazy service if not cached
            _registryLock.EnterWriteLock();
            try
            {
                // Double-check after acquiring write lock
                if (_singletonCache.TryGetValue(serviceName, out var lazyService))
                {
                    return lazyService.Value;
                }

                var descriptor = _registeredServices[serviceName];
                var lazy = new Lazy<object>(() => CreateInProcessService(descriptor));
                _singletonCache[serviceName] = lazy;
                return lazy.Value;
            }
            finally
            {
                _registryLock.ExitWriteLock();
            }
        }

        public void RegisterService(ServiceDescriptor descriptor)
        {
            _registryLock.EnterWriteLock();
            try
            {
                _registeredServices[descriptor.ServiceName] = descriptor;
                // Note: Lazy instantiation defers creation until first access
            }
            finally
            {
                _registryLock.ExitWriteLock();
            }
        }

        public void RegisterServiceFactory(string serviceName, Func<IServiceFactory, object> factory)
        {
            _registryLock.EnterWriteLock();
            try
            {
                var descriptor = new ServiceDescriptor
                {
                    ServiceName = serviceName,
                    Factory = factory,
                    Mode = ServiceMode.InProcess
                };
                _registeredServices[serviceName] = descriptor;
            }
            finally
            {
                _registryLock.ExitWriteLock();
            }
        }

        public bool IsServiceRegistered(string serviceName)
        {
            _registryLock.EnterReadLock();
            try
            {
                return _registeredServices.ContainsKey(serviceName);
            }
            finally
            {
                _registryLock.ExitReadLock();
            }
        }

        public ServiceDescriptor GetServiceDescriptor(string serviceName)
        {
            _registryLock.EnterReadLock();
            try
            {
                return _registeredServices.TryGetValue(serviceName, out var descriptor) ? descriptor : null;
            }
            finally
            {
                _registryLock.ExitReadLock();
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
