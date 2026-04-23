# Implementation Guide: Adding a Plugin

## Overview

Plugins extend Monado Blade functionality without modifying core code. This guide covers plugin development and integration.

## Plugin Architecture

Plugins implement specific interfaces and are discovered and loaded at runtime.

### 1. Define Plugin Interface

```csharp
namespace MonadoBlade.Core.Plugins
{
    /// <summary>
    /// Base interface for all plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Plugin name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plugin version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Plugin description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Required permissions for this plugin.
        /// </summary>
        IEnumerable<string> RequiredPermissions { get; }

        /// <summary>
        /// Initializes plugin.
        /// </summary>
        Task InitializeAsync(IServiceContainer container);

        /// <summary>
        /// Starts plugin.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops plugin.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Gets plugin metadata.
        /// </summary>
        PluginMetadata GetMetadata();
    }

    public class PluginMetadata
    {
        public string Author { get; set; }
        public string License { get; set; }
        public string Repository { get; set; }
        public Dictionary<string, string> Settings { get; set; }
    }
}
```

### 2. Create Plugin Class

Create in plugin project:

```csharp
using MonadoBlade.Core.Plugins;

namespace MyCompany.MonadoBladePlugins
{
    /// <summary>
    /// Example monitoring plugin that collects system metrics.
    /// </summary>
    public class SystemMonitoringPlugin : IPlugin
    {
        private readonly ILogger _logger;
        private IEventBus _eventBus;
        private CancellationTokenSource _cancellation;

        public string Name => "System Monitoring Plugin";
        public string Version => "1.0.0";
        public string Description => 
            "Collects system metrics and performance data";

        public IEnumerable<string> RequiredPermissions =>
            new[] { "system.read_performance", "logging.write" };

        public SystemMonitoringPlugin()
        {
            // Can be constructed with default options
            _logger = NullLogger.Instance;
        }

        public async Task InitializeAsync(IServiceContainer container)
        {
            _logger = container.Resolve<ILogger>();
            _eventBus = container.Resolve<IEventBus>();

            _logger.LogInfo("System Monitoring Plugin initializing");

            // Subscribe to events
            _eventBus.Subscribe<BootStateChangedEvent>(
                new BootStateHandler(_logger));

            await Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            _logger.LogInfo("System Monitoring Plugin starting");

            _cancellation = new CancellationTokenSource();

            // Start monitoring loop
            _ = MonitoringLoop(_cancellation.Token);

            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _logger.LogInfo("System Monitoring Plugin stopping");

            _cancellation?.Cancel();
            _cancellation?.Dispose();

            await Task.CompletedTask;
        }

        public PluginMetadata GetMetadata()
        {
            return new PluginMetadata
            {
                Author = "My Company",
                License = "MIT",
                Repository = "https://github.com/mycompany/...",
                Settings = new Dictionary<string, string>
                {
                    ["interval"] = "5000",
                    ["enabled"] = "true"
                }
            };
        }

        private async Task MonitoringLoop(CancellationToken cancellation)
        {
            const int intervalMs = 5000;

            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    var metrics = CollectMetrics();
                    _logger.LogDebug(
                        "Collected metrics: CPU={Cpu}%, Memory={Memory}%",
                        metrics.CpuUsage, metrics.MemoryUsage);

                    await Task.Delay(intervalMs, cancellation);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Monitoring error", ex);
                }
            }
        }

        private SystemMetrics CollectMetrics()
        {
            // Collect actual system metrics
            var cpuCounter = new PerformanceCounter(
                "Processor", "% Processor Time", "_Total");
            
            return new SystemMetrics
            {
                CpuUsage = (int)cpuCounter.NextValue(),
                MemoryUsage = GetMemoryUsage()
            };
        }

        private int GetMemoryUsage()
        {
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = Process.GetCurrentProcess().WorkingSet64;
            return (int)(workingSet * 100 / totalMemory);
        }

        private class BootStateHandler : 
            IEventHandler<BootStateChangedEvent>
        {
            private readonly ILogger _logger;

            public BootStateHandler(ILogger logger)
            {
                _logger = logger;
            }

            public async Task HandleAsync(BootStateChangedEvent @event)
            {
                _logger.LogInfo(
                    "Boot state changed: {State}", @event.NewState);
                await Task.CompletedTask;
            }
        }
    }

    public class SystemMetrics
    {
        public int CpuUsage { get; set; }
        public int MemoryUsage { get; set; }
    }
}
```

### 3. Create Plugin Manifest

Create `manifest.json`:

```json
{
  "id": "system-monitoring-plugin",
  "name": "System Monitoring Plugin",
  "version": "1.0.0",
  "author": "My Company",
  "description": "Monitors system performance metrics",
  "targetVersionRange": "1.0.0-2.0.0",
  "mainClass": "MyCompany.MonadoBladePlugins.SystemMonitoringPlugin",
  "assembly": "MyCompany.MonadoBladePlugins.dll",
  "permissions": [
    "system.read_performance",
    "logging.write",
    "events.subscribe"
  ],
  "dependencies": [],
  "configuration": {
    "interval": {
      "type": "integer",
      "default": 5000,
      "description": "Monitoring interval in milliseconds"
    },
    "enabled": {
      "type": "boolean",
      "default": true
    }
  },
  "license": "MIT",
  "repository": "https://github.com/mycompany/...",
  "signature": "base64-encoded-signature"
}
```

### 4. Sign Plugin (for distribution)

```csharp
public class PluginSigner
{
    public string SignPlugin(string pluginPath, string privateKeyPath)
    {
        var pluginData = File.ReadAllBytes(pluginPath);
        var privateKey = File.ReadAllText(privateKeyPath);

        using (var rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKey);
            var signature = rsa.SignData(
                pluginData, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signature);
        }
    }
}
```

### 5. Plugin Project Structure

```
MyCompany.MonadoBladePlugins/
├── MyCompany.MonadoBladePlugins.csproj
├── manifest.json
├── plugin.key (private signing key, not in source control)
├── Plugins/
│   ├── SystemMonitoringPlugin.cs
│   └── SystemMonitoringPlugin.EventHandlers.cs
├── Models/
│   └── SystemMetrics.cs
└── packages/
    └── MyCompany.MonadoBladePlugins-1.0.0.zip
        ├── manifest.json
        ├── MyCompany.MonadoBladePlugins.dll
        └── dependencies/
```

### 6. Build and Package

```xml
<!-- In .csproj -->
<Target Name="PackagePlugin" AfterTargets="Build">
  <ItemGroup>
    <PluginFiles Include="$(OutputPath)*.dll" />
    <PluginFiles Include="manifest.json" />
  </ItemGroup>
  
  <Exec Command="powershell -Command &quot;
    $zip = 'MyCompany.MonadoBladePlugins-$(Version).zip'
    if (Test-Path $zip) { Remove-Item $zip }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory('$(OutputPath)', $zip)
  &quot;" />
</Target>
```

### 7. Deploy Plugin

Copy plugin package to deployment location:

```bash
# Development: Plugin directory
cp MyCompany.MonadoBladePlugins-1.0.0.zip \
   C:\MonadoBlade\Plugins\

# Load and verify
monado-cli plugin install \
  C:\MonadoBlade\Plugins\MyCompany.MonadoBladePlugins-1.0.0.zip
```

### 8. Plugin Management

```csharp
// List installed plugins
var plugins = pluginManager.GetInstalledPlugins();

// Enable/disable plugin
await pluginManager.EnablePluginAsync("system-monitoring-plugin");
await pluginManager.DisablePluginAsync("system-monitoring-plugin");

// Update plugin
await pluginManager.UpdatePluginAsync(
    "system-monitoring-plugin", 
    "1.0.1");

// Uninstall plugin
await pluginManager.UninstallPluginAsync(
    "system-monitoring-plugin");
```

## Plugin Security Considerations

1. **Code Signing**: All plugins must be signed
2. **Permissions**: Declare required permissions in manifest
3. **Sandboxing**: Plugins run in isolated context
4. **Resource Limits**: Monitor CPU and memory usage
5. **Validation**: Verify plugin manifest and signature

## Testing Plugin

Create unit tests:

```csharp
[TestFixture]
public class SystemMonitoringPluginTests
{
    private Mock<IServiceContainer> _containerMock;
    private Mock<ILogger> _loggerMock;
    private Mock<IEventBus> _eventBusMock;
    private SystemMonitoringPlugin _plugin;

    [SetUp]
    public void Setup()
    {
        _containerMock = new Mock<IServiceContainer>();
        _loggerMock = new Mock<ILogger>();
        _eventBusMock = new Mock<IEventBus>();

        _containerMock.Setup(c => c.Resolve<ILogger>())
            .Returns(_loggerMock.Object);
        _containerMock.Setup(c => c.Resolve<IEventBus>())
            .Returns(_eventBusMock.Object);

        _plugin = new SystemMonitoringPlugin();
    }

    [Test]
    public void Plugin_HasCorrectMetadata()
    {
        Assert.That(_plugin.Name, Contains.Substring("Monitoring"));
        Assert.That(_plugin.Version, Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task InitializeAsync_Succeeds()
    {
        await _plugin.InitializeAsync(_containerMock.Object);
        _eventBusMock.Verify(
            e => e.Subscribe(It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task StartStop_Succeeds()
    {
        await _plugin.InitializeAsync(_containerMock.Object);
        await _plugin.StartAsync();
        await _plugin.StopAsync();

        _loggerMock.Verify(
            l => l.LogInfo(It.IsAny<string>()), 
            Times.AtLeastOnce);
    }
}
```

## Best Practices

1. **Keep plugins focused**: Single responsibility
2. **Minimal dependencies**: Reduce external dependencies
3. **Graceful shutdown**: Handle cancellation properly
4. **Error handling**: Comprehensive error handling
5. **Logging**: Use provided logger
6. **Documentation**: Document all settings and permissions
7. **Testing**: Unit tests for core logic
8. **Performance**: Monitor resource usage

## Related Documentation

- ADR-011: Profile Extension Points
- ADR-015: Plugin Security Model
- PLUGIN_DEVELOPMENT_GUIDE.md
