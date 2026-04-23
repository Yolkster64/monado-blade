# PHASE 2 ARCHITECTURE - QUICK REFERENCE GUIDE

## 📊 Component Overview

### Core Components (12 Files)

| # | Component | File | Lines | Purpose |
|---|-----------|------|-------|---------|
| 1 | ServiceFactory | `01_ServiceFactory.cs` | 278 | Create in-process/remote services |
| 2 | ServiceRegistry | `02_ServiceRegistry.cs` | 200 | Discover & manage services |
| 3 | InterServiceBus | `03_InterServiceBus.cs` | 195 | Service-to-service messaging |
| 4 | MetricsCollector | `04_MetricsCollector.cs` | 250 | System & process metrics |
| 5 | HealthChecker | `05_HealthChecker.cs` | 270 | Service & resource health |
| 6 | PerformanceMonitor | `06_PerformanceMonitor.cs` | 310 | Latency, throughput, errors |
| 7 | EventSystem | `07_EventSystem.cs` | 315 | Publish/subscribe events |
| 8 | StateMachine | `08_StateMachine.cs` | 400 | State machines + Boot/Update patterns |
| 9 | ProfileConfiguration | `09_ProfileConfiguration.cs` | 410 | 5 profiles + ProfileManager |
| 10 | TaskScheduler | `10_TaskScheduler.cs` | 330 | Priority scheduling + affinity |
| 11 | PluginArchitecture | `11_PluginArchitecture.cs` | 415 | Plugin loader & manager |
| 12 | Integration | `12_Integration.cs` | 265 | ArchitectureBootstrapper |

**Total: 4,118 lines of production-ready code**

---

## 🚀 Quick Start

### Initialize Architecture
```csharp
var arch = new ArchitectureBootstrapper();
await arch.InitializeAsync();
```

### Access Components
```csharp
var factory = arch.GetServiceFactory();
var registry = arch.GetServiceRegistry();
var eventBus = arch.GetEventBus();
var profiles = arch.GetProfileManager();
var scheduler = arch.GetTaskScheduler();
var plugins = arch.GetPluginManager();
var metrics = arch.GetMetricsCollector();
var health = arch.GetHealthChecker();
var telemetry = arch.GetTelemetryAggregator();
```

---

## 📌 Component Details

### 1️⃣ Microservices Architecture

**ServiceFactory**
```csharp
var factory = new ServiceFactory();
var service = factory.CreateService<IMyService>("MyService");
```

**ServiceRegistry**
```csharp
var registry = new ServiceRegistry(eventBus);
registry.Register(new ServiceMetadata { Name = "Service1", Version = "1.0" });
var services = registry.GetAllServices();
registry.UpdateHealth("Service1", ServiceHealthStatus.Healthy);
```

**InterServiceBus**
```csharp
var bus = new InterServiceBus(eventBus);
bus.Subscribe("Consumer", "MessageType", async msg => { /* handle */ });
var msg = new ServiceMessage { ToService = "Consumer", MessageType = "Request" };
await bus.PublishAsync(msg);
```

### 2️⃣ Observability Framework

**MetricsCollector**
```csharp
var metrics = new MetricsCollector();
var cpu = metrics.GetCpuMetrics();           // CPU usage
var mem = metrics.GetMemoryMetrics();        // Memory usage
var disks = metrics.GetDiskMetrics();        // Disk usage per drive
var procs = metrics.GetProcessMetrics(pid); // Process metrics
```

**HealthChecker**
```csharp
var health = new HealthChecker(metrics, registry);
var result = await health.CheckServiceHealthAsync("Service1");
var results = await health.CheckAllServicesAsync();
var resourceHealth = await health.CheckResourceHealthAsync();
```

**PerformanceMonitor**
```csharp
var perf = new PerformanceMonitor();
perf.RecordLatency("operation", 123.45);
perf.RecordThroughput("operation", 1000, timespan);
perf.RecordError("operation");
var metrics = perf.GetAllOperationMetrics();
```

**TelemetryAggregator**
```csharp
var tel = new TelemetryAggregator();
tel.Collect(metrics, health, perf);
var snapshot = tel.GetSnapshot();
var prometheus = ((TelemetryAggregator)tel).ExportPrometheusFormat();
```

### 3️⃣ Multi-Profile Support

**Available Profiles**
- `Gamer` - High performance, GPU 90%
- `Developer` - Balanced, GPU 50%
- `AIResearch` - Max resources, GPU 100%
- `Secure` - Restricted, GPU 0%
- `Enterprise` - Monitoring, GPU 30%

**Profile Manager**
```csharp
var pm = new ProfileManager(registry, eventBus);
var profile = pm.GetProfile(ProfileType.Gamer);
await pm.SwitchProfileAsync(profile);
var current = pm.GetCurrentProfile();
```

**Custom Profile**
```csharp
var custom = new ProfileConfiguration
{
    Type = ProfileType.Developer,
    Name = "MyProfile",
    GpuAccessEnabled = true,
    GpuAllocationPercentage = 60,
    ServicePriorities = new() { { "MyService", 8 } }
};
```

### 4️⃣ Advanced Task Scheduling

**Schedule Task**
```csharp
var scheduler = new TaskScheduler(eventBus);
var task = new ScheduledTask
{
    Name = "MyTask",
    TaskFunc = async () => { /* work */ },
    Priority = 8,
    MaxMemoryBytes = 1024 * 1024 * 100, // 100MB
    AffinityMask = new[] { 0, 1, 2, 3 },
    ScheduledFor = DateTime.UtcNow,
    RecurrenceInterval = TimeSpan.FromMinutes(5)
};
scheduler.ScheduleTask(task);
```

**Manage Tasks**
```csharp
scheduler.SetCpuAffinity(taskId, new[] { 0, 1 });
scheduler.SetMemoryLimit(taskId, 1024 * 1024 * 200); // 200MB
scheduler.SetIoPriority(taskId, 3);
scheduler.CancelTask(taskId);
```

### 5️⃣ State Machines

**Boot State Machine**
```csharp
var boot = BootStateMachineFactory.CreateBootStateMachine(eventBus);
await boot.FireEventAsync("PowerOn");      // Off → BIOS
await boot.FireEventAsync("BiosDone");     // BIOS → Loader
await boot.FireEventAsync("LoaderDone");   // Loader → Kernel
// ... continues to Ready
```

**Update State Machine**
```csharp
var update = BootStateMachineFactory.CreateUpdateStateMachine(eventBus);
await update.FireEventAsync("UpdateAvailable");
await update.FireEventAsync("DownloadComplete");
// ... proceeds through stages
```

**Custom State Machine**
```csharp
var state1 = new State { Name = "State1" };
var state2 = new State { Name = "State2" };
var sm = new StateMachine(state1, eventBus);
sm.AddState(state2);
sm.AddTransition(state1, "go", state2);
await sm.FireEventAsync("go"); // state1 → state2
```

**Visualize**
```csharp
var dot = sm.VisualizeDotFormat();
// Save as .dot file for Graphviz visualization
```

### 6️⃣ Event System

**Subscribe to Events**
```csharp
var bus = new EventBus();
bus.Subscribe("ServiceStarted", async evt =>
{
    Console.WriteLine($"Service started: {evt.Data.ServiceName}");
});
```

**Subscribe with Filters**
```csharp
var filter = new WildcardEventFilter("Service*");
bus.Subscribe(filter, async evt =>
{
    // Handles ServiceStarted, ServiceStopped, etc.
});
```

**Publish Events**
```csharp
bus.PublishEvent(new Event
{
    EventType = "ServiceStarted",
    Data = new { ServiceName = "MyService" },
    Metadata = new() { { "Priority", "High" } }
});
```

**Common Events**
- `BootStarted`, `BootCompleted`, `BootFailed`
- `UpdateStarted`, `UpdateCompleted`, `UpdateFailed`
- `ServiceStarted`, `ServiceStopped`, `ServiceFailed`
- `ProfileChanged`, `ProfileSwitched`
- `HealthDegraded`, `HealthRecovered`
- `ResourceWarning`, `ResourceCritical`
- `PluginLoaded`, `PluginUnloaded`

### 7️⃣ Plugin System

**Load Plugin**
```csharp
var loader = new PluginLoader(eventBus);
var plugin = await loader.LoadPluginAsync("path/to/plugin.dll");
```

**Discover Plugins**
```csharp
var discovered = loader.DiscoverPlugins("plugins/");
foreach (var meta in discovered)
{
    Console.WriteLine($"{meta.Name} {meta.Version}");
}
```

**Manage Plugins**
```csharp
var pm = new PluginManager(eventBus);
await pm.RegisterPluginAsync(plugin);
await pm.StartPluginAsync("MyPlugin");
var running = pm.GetRunningPlugins();
await pm.StopPluginAsync("MyPlugin");
```

**Create Plugin**
```csharp
public class MyPlugin : PluginBase
{
    public override string Name => "MyPlugin";
    public override string Version => "1.0";
    
    public override async Task InitializeAsync()
    {
        // Initialize
    }
    
    public override async Task StartAsync()
    {
        IsRunning = true;
    }
    
    public override async Task StopAsync()
    {
        IsRunning = false;
    }
}
```

---

## 🔗 Common Workflows

### Workflow 1: Boot Process
```csharp
var arch = new ArchitectureBootstrapper();
await arch.InitializeAsync();

var boot = BootStateMachineFactory.CreateBootStateMachine(arch.GetEventBus());
await boot.FireEventAsync("PowerOn");        // Off → BIOS
await boot.FireEventAsync("BiosDone");       // BIOS → Loader
await boot.FireEventAsync("LoaderDone");     // Loader → Kernel
await boot.FireEventAsync("KernelReady");    // Kernel → Services
await boot.FireEventAsync("ServicesReady");  // Services → Ready

Console.WriteLine($"Current state: {boot.CurrentState.Name}"); // "Ready"
```

### Workflow 2: Switch Profile
```csharp
var profiles = arch.GetProfileManager();
var currentProfile = profiles.GetCurrentProfile();
Console.WriteLine($"Current: {currentProfile.Name}");

var newProfile = profiles.GetProfile(ProfileType.Gamer);
await profiles.SwitchProfileAsync(newProfile);

var current = profiles.GetCurrentProfile();
Console.WriteLine($"Switched to: {current.Name}");
```

### Workflow 3: Monitor Health
```csharp
var health = arch.GetHealthChecker();
var metrics = arch.GetMetricsCollector();
var eventBus = arch.GetEventBus();

// Periodic health check
eventBus.Subscribe("HealthCheckStarted", async evt =>
{
    var results = await health.CheckAllServicesAsync();
    foreach (var result in results)
    {
        Console.WriteLine($"{result.ServiceName}: {result.Status}");
    }
});
```

### Workflow 4: Task Scheduling
```csharp
var scheduler = arch.GetTaskScheduler();

var task = new ScheduledTask
{
    Name = "PeriodicHealthCheck",
    TaskFunc = async () =>
    {
        var health = arch.GetHealthChecker();
        await health.CheckAllServicesAsync();
    },
    Priority = 8,
    RecurrenceInterval = TimeSpan.FromSeconds(30)
};

scheduler.ScheduleTask(task);
```

### Workflow 5: Collect Telemetry
```csharp
var telemetry = arch.GetTelemetryAggregator();
var metrics = arch.GetMetricsCollector();
var health = arch.GetHealthChecker();
var perf = arch.GetPerformanceMonitor();

telemetry.Collect(metrics, health, perf);
var snapshot = telemetry.GetSnapshot();

Console.WriteLine($"CPU: {snapshot.CpuMetrics.OverallUsage}%");
Console.WriteLine($"Memory: {snapshot.MemoryMetrics.UsagePercentage}%");
```

---

## 📚 Files & Organization

```
src/HELIOS.Platform/
├── Architecture/
│   ├── 01_ServiceFactory.cs          (Microservices)
│   ├── 02_ServiceRegistry.cs         (Service discovery)
│   ├── 03_InterServiceBus.cs         (Messaging)
│   ├── 04_MetricsCollector.cs        (Metrics)
│   ├── 05_HealthChecker.cs           (Health)
│   ├── 06_PerformanceMonitor.cs      (Performance)
│   ├── 07_EventSystem.cs             (Events)
│   ├── 08_StateMachine.cs            (State machines)
│   ├── 09_ProfileConfiguration.cs    (Profiles)
│   ├── 10_TaskScheduler.cs           (Scheduling)
│   ├── 11_PluginArchitecture.cs      (Plugins)
│   ├── 12_Integration.cs             (Integration)
│   └── README.md                     (Full docs)
├── HELIOS.Platform.Architecture.csproj
└── Operations/                       (Future)
```

---

## 🎯 Design Patterns Used

| Pattern | Component | Example |
|---------|-----------|---------|
| Factory | ServiceFactory | Create services transparently |
| Registry | ServiceRegistry | Discover services by name |
| Observer | EventBus | Subscribe to events |
| State Machine | StateMachine | Model boot/update processes |
| Strategy | ProfileManager | Switch execution strategies |
| Plugin | PluginManager | Load external functionality |
| Service Locator | ServiceRegistry | Find services at runtime |

---

## ⚡ Performance Tips

1. **Profiles** - Switch to appropriate profile for workload
2. **Scheduling** - Use priority and affinity for performance
3. **Monitoring** - Use health checks periodically, not constantly
4. **Events** - Subscribe to relevant events only
5. **Plugins** - Load only needed plugins
6. **Telemetry** - Collect metrics on schedule, not per-operation

---

## 🔒 Thread Safety

All components are thread-safe:
- `ServiceRegistry` - Uses ReaderWriterLockSlim
- `ServiceFactory` - Locks during registration
- `EventBus` - Locks for subscriptions/publishing
- `ProfileManager` - Locks during profile operations
- `TaskScheduler` - Locks during task operations
- `PluginManager` - Locks during plugin operations
- `PerformanceMonitor` - Locks during metric recording

---

## 🚦 Status Indicators

| Indicator | Meaning |
|-----------|---------|
| 🟢 Healthy | Service/Resource operating normally |
| 🟡 Degraded | Service/Resource experiencing issues |
| 🔴 Unhealthy | Service/Resource not functioning |
| ⚪ Unknown | Service/Resource status not determined |

---

## 📝 Commit Information

**Commit:** `84b97f9`  
**Date:** 2026-04-23  
**Files:** 14 new files (4,118 lines code)  
**Status:** ✅ Ready for Phase 3

---

## ❓ FAQ

**Q: How do I add a custom profile?**  
A: Create a `ProfileConfiguration` object and use `ProfileManager.SwitchProfileAsync()`

**Q: Can I schedule tasks with dependencies?**  
A: Currently no, but can be added in Phase 3

**Q: How do I integrate with Prometheus?**  
A: Call `TelemetryAggregator.ExportPrometheusFormat()` and expose via HTTP

**Q: Can plugins communicate with each other?**  
A: Yes, via the EventBus

**Q: What happens if a plugin fails?**  
A: It's isolated, marked as Failed, and other plugins continue

**Q: Can I visualize state machines?**  
A: Yes, call `StateMachine.VisualizeDotFormat()` and use Graphviz

---

**For more details, see the full README.md in the Architecture folder.**
