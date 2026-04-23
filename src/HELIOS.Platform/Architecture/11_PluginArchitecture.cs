using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Plugin interface
    /// </summary>
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string[] Dependencies { get; }
        Task InitializeAsync();
        Task StartAsync();
        Task StopAsync();
        Task CleanupAsync();
        bool IsRunning { get; }
    }

    /// <summary>
    /// Base plugin class
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        public virtual string Name { get; protected set; }
        public virtual string Version { get; protected set; }
        public virtual string[] Dependencies { get; protected set; } = Array.Empty<string>();
        public virtual bool IsRunning { get; protected set; }

        public virtual async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public virtual async Task StartAsync()
        {
            IsRunning = true;
            await Task.CompletedTask;
        }

        public virtual async Task StopAsync()
        {
            IsRunning = false;
            await Task.CompletedTask;
        }

        public virtual async Task CleanupAsync()
        {
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Plugin metadata
    /// </summary>
    public class PluginMetadata
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string[] Dependencies { get; set; } = Array.Empty<string>();
        public string AssemblyPath { get; set; }
        public DateTime LoadedAt { get; set; }
        public PluginStatus Status { get; set; }
    }

    public enum PluginStatus
    {
        Loaded,
        Initialized,
        Running,
        Stopped,
        Failed,
        Unloaded
    }

    /// <summary>
    /// Plugin loader interface
    /// </summary>
    public interface IPluginLoader
    {
        Task<IPlugin> LoadPluginAsync(string assemblyPath);
        IEnumerable<PluginMetadata> DiscoverPlugins(string searchPath);
        bool ValidatePlugin(IPlugin plugin);
    }

    /// <summary>
    /// Plugin loader implementation
    /// </summary>
    public class PluginLoader : IPluginLoader
    {
        private readonly EventBus _eventBus;

        public PluginLoader(EventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public async Task<IPlugin> LoadPluginAsync(string assemblyPath)
        {
            try
            {
                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException($"Plugin assembly not found: {assemblyPath}");
                }

                var assembly = Assembly.LoadFrom(assemblyPath);
                var pluginTypes = FindPluginTypes(assembly);

                if (pluginTypes.Length == 0)
                {
                    throw new InvalidOperationException($"No IPlugin implementations found in {assemblyPath}");
                }

                var plugin = (IPlugin)Activator.CreateInstance(pluginTypes[0]);
                await plugin.InitializeAsync();

                return plugin;
            }
            catch (Exception ex)
            {
                _eventBus?.PublishEvent(new Event
                {
                    EventType = "PluginLoadFailed",
                    Data = new { AssemblyPath = assemblyPath, Error = ex.Message }
                });
                throw;
            }
        }

        public IEnumerable<PluginMetadata> DiscoverPlugins(string searchPath)
        {
            var metadata = new List<PluginMetadata>();

            if (!Directory.Exists(searchPath))
            {
                return metadata;
            }

            foreach (var dll in Directory.GetFiles(searchPath, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    var pluginTypes = FindPluginTypes(assembly);

                    if (pluginTypes.Length > 0)
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(pluginTypes[0]);
                        metadata.Add(new PluginMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = plugin.Name,
                            Version = plugin.Version,
                            AssemblyPath = dll,
                            Dependencies = plugin.Dependencies,
                            Status = PluginStatus.Loaded
                        });
                    }
                }
                catch
                {
                    // Skip assemblies that aren't valid plugins
                }
            }

            return metadata;
        }

        public bool ValidatePlugin(IPlugin plugin)
        {
            if (plugin == null)
                return false;

            if (string.IsNullOrEmpty(plugin.Name) || string.IsNullOrEmpty(plugin.Version))
                return false;

            return true;
        }

        private Type[] FindPluginTypes(Assembly assembly)
        {
            var pluginInterface = typeof(IPlugin);
            var types = assembly.GetTypes();
            var pluginTypes = new List<Type>();

            foreach (var type in types)
            {
                if (pluginInterface.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    pluginTypes.Add(type);
                }
            }

            return pluginTypes.ToArray();
        }
    }

    /// <summary>
    /// Plugin manager interface
    /// </summary>
    public interface IPluginManager
    {
        Task RegisterPluginAsync(IPlugin plugin);
        Task UnregisterPluginAsync(string pluginName);
        Task<bool> StartPluginAsync(string pluginName);
        Task<bool> StopPluginAsync(string pluginName);
        IPlugin GetPlugin(string pluginName);
        IEnumerable<PluginMetadata> GetAllPlugins();
        IEnumerable<PluginMetadata> GetRunningPlugins();
    }

    /// <summary>
    /// Plugin manager implementation
    /// </summary>
    public class PluginManager : IPluginManager
    {
        private readonly Dictionary<string, IPlugin> _plugins = new();
        private readonly Dictionary<string, PluginMetadata> _metadata = new();
        private readonly object _lock = new();
        private readonly EventBus _eventBus;

        public PluginManager(EventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public async Task RegisterPluginAsync(IPlugin plugin)
        {
            lock (_lock)
            {
                if (_plugins.ContainsKey(plugin.Name))
                {
                    throw new InvalidOperationException($"Plugin '{plugin.Name}' already registered");
                }

                _plugins[plugin.Name] = plugin;
                _metadata[plugin.Name] = new PluginMetadata
                {
                    Name = plugin.Name,
                    Version = plugin.Version,
                    Dependencies = plugin.Dependencies,
                    Status = PluginStatus.Initialized,
                    LoadedAt = DateTime.UtcNow
                };

                _eventBus?.PublishEvent(new Event
                {
                    EventType = CommonEventTypes.PluginLoaded,
                    Data = new { PluginName = plugin.Name, Version = plugin.Version }
                });
            }
        }

        public async Task UnregisterPluginAsync(string pluginName)
        {
            lock (_lock)
            {
                if (_plugins.TryGetValue(pluginName, out var plugin))
                {
                    if (plugin.IsRunning)
                    {
                        await plugin.StopAsync();
                    }

                    await plugin.CleanupAsync();
                    _plugins.Remove(pluginName);
                    _metadata.Remove(pluginName);

                    _eventBus?.PublishEvent(new Event
                    {
                        EventType = CommonEventTypes.PluginUnloaded,
                        Data = new { PluginName = pluginName }
                    });
                }
            }
        }

        public async Task<bool> StartPluginAsync(string pluginName)
        {
            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginName, out var plugin))
                {
                    return false;
                }

                try
                {
                    await plugin.StartAsync();
                    _metadata[pluginName].Status = PluginStatus.Running;
                    return true;
                }
                catch (Exception ex)
                {
                    _metadata[pluginName].Status = PluginStatus.Failed;
                    _eventBus?.PublishEvent(new Event
                    {
                        EventType = "PluginStartFailed",
                        Data = new { PluginName = pluginName, Error = ex.Message }
                    });
                    return false;
                }
            }
        }

        public async Task<bool> StopPluginAsync(string pluginName)
        {
            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginName, out var plugin))
                {
                    return false;
                }

                try
                {
                    await plugin.StopAsync();
                    _metadata[pluginName].Status = PluginStatus.Stopped;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public IPlugin GetPlugin(string pluginName)
        {
            lock (_lock)
            {
                _plugins.TryGetValue(pluginName, out var plugin);
                return plugin;
            }
        }

        public IEnumerable<PluginMetadata> GetAllPlugins()
        {
            lock (_lock)
            {
                return new List<PluginMetadata>(_metadata.Values);
            }
        }

        public IEnumerable<PluginMetadata> GetRunningPlugins()
        {
            lock (_lock)
            {
                var running = new List<PluginMetadata>();
                foreach (var metadata in _metadata.Values)
                {
                    if (metadata.Status == PluginStatus.Running)
                    {
                        running.Add(metadata);
                    }
                }
                return running;
            }
        }
    }
}
