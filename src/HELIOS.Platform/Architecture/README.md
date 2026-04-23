# HELIOS Platform Phase 2 Architecture

## Overview

Phase 2 introduces a comprehensive, production-ready architecture that transforms Monado Blade into a scalable, observable, and plugin-extensible platform. All components are designed to be composable, testable, and future-proof.

## Architecture Components

### 1. Microservices-Ready Architecture

**Components:**
- `ServiceFactory` - Creates services in-process or as remote proxies
- `ServiceRegistry` - Discovers and manages service metadata
- `InterServiceBus` - Message passing between services

**Key Features:**
- Services can be instantiated locally or remotely without code changes
- Service discovery and health tracking
- Request-reply pattern for synchronous communication
- Publish-subscribe for asynchronous messaging

**Usage Example:**
```csharp
var factory = new ServiceFactory();
var registry = new ServiceRegistry();
var bus = new InterServiceBus();

var myService = factory.CreateService<IMyService>("myservice");
```

### 2. Observability Framework

**Components:**
- `MetricsCollector` - System and process metrics
- `HealthChecker` - Service and resource health checks
- `PerformanceMonitor` - Latency, throughput, error tracking
- `TelemetryAggregator` - Unified metric collection

**Metrics Collected:**
- CPU (overall, kernel, user)
- Memory (system, process, private)
- Disk (usage, I/O operations)
- Network (packets, bytes, latency)
- Process-specific (threads, handles, memory)
- Operations (latency, throughput, error rates)

**Export Formats:**
- Prometheus format for integration with monitoring stacks
- Raw JSON snapshots

**Usage Example:**
```csharp
var metrics = new MetricsCollector();
var cpu = metrics.GetCpuMetrics();
var memory = metrics.GetMemoryMetrics();
var disks = metrics.GetDiskMetrics();
```

### 3. Multi-Profile Support

**Profiles:**
1. **Gamer** - High performance, GPU-focused
2. **Developer** - Balanced resources, development tools
3. **AI Research** - Maximum resources, memory overcommit
4. **Secure** - Restricted networking, minimal GPU
5. **Enterprise** - Stability, comprehensive monitoring

**Features Per Profile:**
- CPU affinity settings
- Memory and resource constraints
- GPU allocation percentages
- Service priorities
- Network rules
- Custom settings

**Runtime Profile Switching:**
- Atomic profile transitions
- Service pause/reload/restart
- No system reboot required

**Usage Example:**
```csharp
var profileManager = new ProfileManager(registry, eventBus);
var gamerProfile = profileManager.GetProfile(ProfileType.Gamer);
await profileManager.SwitchProfileAsync(gamerProfile);
```

### 4. Advanced Task Scheduling

**Capabilities:**
- Priority-based task execution (0-10 scale)
- CPU affinity mapping per task
- Memory limits per task
- I/O priority scheduling
- Profile-aware resource allocation

**Scheduling Features:**
- One-time and recurring tasks
- Guard conditions for conditional execution
- Resource constraint enforcement
- Status tracking (Pending/Running/Completed/Failed)

**Usage Example:**
```csharp
var scheduler = new TaskScheduler(eventBus);
var task = new ScheduledTask
{
    Name = "HealthCheck",
    TaskFunc = CheckHealthAsync,
    Priority = 8,
    MaxMemoryBytes = 1024 * 1024 * 100, // 100 MB
    AffinityMask = new[] { 0, 1 }, // Cores 0-1
};
scheduler.ScheduleTask(task);
```

### 5. State Machine Framework

**Components:**
- `State` - Named states with entry/exit actions
- `StateMachine` - Manages transitions and validates rules
- `Transition` - Event, target state, guard conditions, actions

**Pre-built State Machines:**
- **Boot State Machine**: Off → BIOS → Loader → Kernel → Services → Ready
- **Update State Machine**: Check → Download → Verify → Stage → Install → Activate → Cleanup

**Features:**
- Guard conditions prevent invalid transitions
- Entry/exit actions execute on state changes
- Event-driven transition triggering
- Graphviz DOT visualization

**Usage Example:**
```csharp
var bootSM = BootStateMachineFactory.CreateBootStateMachine(eventBus);
await bootSM.FireEventAsync("PowerOn"); // Off → BIOS
await bootSM.FireEventAsync("BiosDone"); // BIOS → Loader
```

### 6. Event System

**Components:**
- `EventBus` - Central event publication hub
- `Event` - Event metadata (type, data, timestamp, ID)
- `EventTypeFilter` / `WildcardEventFilter` - Event routing
- `CommonEventTypes` - Standard event types

**Common Event Types:**
- BootStarted, BootProgress, BootCompleted, BootFailed
- UpdateStarted, UpdateProgress, UpdateCompleted, UpdateFailed
- ServiceStarted, ServiceStopped, ServiceFailed
- ProfileChanged, ProfileSwitched
- HealthCheckStarted, HealthDegraded, HealthRecovered
- ResourceWarning, ResourceCritical
- PluginLoaded, PluginUnloaded

**Features:**
- Type-based and filter-based subscriptions
- Event history tracking
- Async event publishing
- Error isolation between handlers

**Usage Example:**
```csharp
var eventBus = new EventBus();
eventBus.Subscribe("ServiceStarted", async (evt) =>
{
    var serviceName = evt.Data.ServiceName;
    // Handle event
});

eventBus.PublishEvent(new Event
{
    EventType = "ServiceStarted",
    Data = new { ServiceName = "MyService" }
});
```

### 7. Plugin Architecture

**Components:**
- `IPlugin` - Plugin interface
- `PluginBase` - Base class for plugin implementations
- `PluginLoader` - Assembly discovery and loading
- `PluginManager` - Lifecycle and registration
- `PluginMetadata` - Plugin information tracking

**Plugin Lifecycle:**
1. **Discovery** - Find plugin assemblies
2. **Loading** - Load assembly into AppDomain
3. **Validation** - Check plugin interface implementation
4. **Initialization** - Initialize plugin
5. **Starting** - Start plugin services
6. **Running** - Active operation
7. **Stopping** - Graceful shutdown
8. **Cleanup** - Resource cleanup
9. **Unloading** - Remove from registry

**Plugin Types Supported:**
- Custom drivers
- Monitoring integrations
- Profile providers
- Update handlers
- Diagnostic tools

**Isolation:**
- Plugins loaded in isolated context
- Security boundary between plugins
- Failed plugins don't crash host

**Usage Example:**
```csharp
var loader = new PluginLoader(eventBus);
var plugin = await loader.LoadPluginAsync("path/to/plugin.dll");

var manager = new PluginManager(eventBus);
await manager.RegisterPluginAsync(plugin);
await manager.StartPluginAsync(plugin.Name);
```

## Architecture Patterns

### 1. Service Location Pattern
```csharp
// Factory creates services transparently
var service = factory.CreateService<IService>("ServiceName");
// Service could be in-process or remote
```

### 2. Observer Pattern
```csharp
// Event-driven architecture with subscribers
eventBus.Subscribe("StateChanged", handler);
eventBus.PublishEvent(new Event { EventType = "StateChanged" });
```

### 3. State Machine Pattern
```csharp
// Explicit state transitions with guards
await stateMachine.FireEventAsync("Transition");
var nextState = stateMachine.CurrentState;
```

### 4. Strategy Pattern
```csharp
// Profile-based configuration switching
await profileManager.SwitchProfileAsync(newProfile);
var config = profileManager.GetCurrentProfile();
```

### 5. Plugin Pattern
```csharp
// Dynamic behavior loading
await pluginManager.RegisterPluginAsync(plugin);
await pluginManager.StartPluginAsync(pluginName);
```

## Integration Example

```csharp
// Initialize all components
var bootstrapper = new ArchitectureBootstrapper();
await bootstrapper.InitializeAsync();

// Get all services
var factory = bootstrapper.GetServiceFactory();
var registry = bootstrapper.GetServiceRegistry();
var eventBus = bootstrapper.GetEventBus();
var profiles = bootstrapper.GetProfileManager();
var scheduler = bootstrapper.GetTaskScheduler();
var plugins = bootstrapper.GetPluginManager();

// Publish system event
eventBus.PublishEvent(new Event
{
    EventType = CommonEventTypes.BootStarted,
    Data = new { Timestamp = DateTime.UtcNow }
});

// Switch profile
var profile = profiles.GetProfile(ProfileType.Gamer);
await profiles.SwitchProfileAsync(profile);

// Schedule recurring health check
var task = new ScheduledTask
{
    Name = "HealthCheck",
    TaskFunc = async () => { /* check health */ },
    Priority = 8,
    RecurrenceInterval = TimeSpan.FromSeconds(30)
};
scheduler.ScheduleTask(task);
```

## File Structure

```
src/HELIOS.Platform/Architecture/
├── 01_ServiceFactory.cs           # Microservices instantiation
├── 02_ServiceRegistry.cs          # Service discovery & health
├── 03_InterServiceBus.cs          # Inter-service messaging
├── 04_MetricsCollector.cs         # System metrics collection
├── 05_HealthChecker.cs            # Health monitoring
├── 06_PerformanceMonitor.cs       # Performance tracking
├── 07_EventSystem.cs              # Event bus & events
├── 08_StateMachine.cs             # State machine framework
├── 09_ProfileConfiguration.cs     # Multi-profile support
├── 10_TaskScheduler.cs            # Advanced scheduling
├── 11_PluginArchitecture.cs       # Plugin system
└── 12_Integration.cs              # Component integration
```

## Design Principles

1. **Composability** - All components work independently or together
2. **Observability** - Full visibility into system behavior
3. **Extensibility** - Plugins and custom integrations
4. **Resilience** - Failure isolation and recovery
5. **Performance** - Minimal overhead, efficient scheduling
6. **Security** - Plugin isolation, resource constraints
7. **Simplicity** - Clean APIs, clear patterns

## Thread Safety

All components use appropriate synchronization:
- `ReaderWriterLockSlim` for read-heavy scenarios
- `lock` for simple mutual exclusion
- Async/await for non-blocking operations
- Event handlers execute outside locks where possible

## Future Enhancements

Phase 3 will add:
- Remote service proxies (gRPC, HTTP)
- Distributed tracing
- Advanced scheduling (constraint solving)
- Plugin sandboxing improvements
- Persistent event log
- Metrics database integration

## Testing

All components are designed for testability:
- Interfaces for dependency injection
- Mock implementations possible
- Deterministic behavior
- No global state
- Clear boundaries between components

## Performance Considerations

- Metrics collection uses Win32 APIs efficiently
- Event history is bounded (max 1000 events)
- Task scheduler uses priority queue
- Service registry uses concurrent collections
- Minimal lock contention through design
