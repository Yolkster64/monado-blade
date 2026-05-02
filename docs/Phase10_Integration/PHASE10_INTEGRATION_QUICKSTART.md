# PHASE 10: INTEGRATION QUICK START GUIDE

**Status**: Ready to implement
**Time Estimate**: 8-10 hours
**Files to Create**: 5 new integration files
**Breaking Changes**: None (additive only)

---

## QUICK START CHECKLIST

### ✅ STEP 1: Create Integration Infrastructure (1.5 hours)

```powershell
# Create integration directory
New-Item -ItemType Directory -Path "src/MonadoBlade.Core/Services/Integration" -Force

# Create files from code snippets in PHASE10_INTEGRATION_CODE.md
# 1. Copy ServiceBus.cs code → src/MonadoBlade.Core/Services/Integration/ServiceBus.cs
# 2. Copy ServiceOrchestrator.cs code → src/MonadoBlade.Core/Services/Integration/ServiceOrchestrator.cs
# 3. Copy ResilientServiceIntegration.cs code → src/MonadoBlade.Core/Services/Integration/ResilientServiceIntegration.cs

# Update DependencyInjection.cs
# Replace src/MonadoBlade.Core/DependencyInjection.cs with PHASE10_INTEGRATION_CODE.md content
```

### ✅ STEP 2: Update Service Interfaces (1 hour)

Add interfaces to consolidated services:

```csharp
// In HermesFleetOrchestrator.cs
public interface IFleetOrchestrator
{
    Task RegisterAgentAsync(string agentName);
    Task StartBootSequenceAsync();
    Task<List<string>> GetActiveAgentsAsync();
    Task<List<string>> GetDeadAgentsAsync(TimeSpan timeout);
}

// In HermesMonitoringService.cs
public interface IMonitoringService
{
    Task<SystemMetrics> CollectSystemMetricsAsync();
    Task<double> GetAverageCpuUsageAsync();
    Task<double> GetPeakCpuUsageAsync();
    Task<IEnumerable<SystemMetrics>> GetMetricsHistoryAsync(int count);
}

// In HermesSecurityService.cs
public interface ISecurityService
{
    Task VerifyTPMAsync();
    Task VerifyBitLockerAsync();
    Task QuarantineThreatAsync(string threatPath);
}
```

### ✅ STEP 3: Update Console Program (1 hour)

Replace `Program.cs` with code from PHASE10_INTEGRATION_CODE.md

### ✅ STEP 4: Wire ViewModels to Services (2 hours)

Update existing ViewModels to use service bus:

```csharp
// DashboardViewModel.cs - Add event subscription
public DashboardViewModel(IMonitoringService service, IServiceBus bus, ILogger logger)
{
    _serviceBus = bus;
    
    // Subscribe to real-time metric updates
    _serviceBus.Subscribe<MetricsCollectedEvent>(OnMetricsUpdated);
}

private void OnMetricsUpdated(MetricsCollectedEvent evt)
{
    CurrentCpuUsage = evt.CpuUsage;
    CurrentMemoryUsage = evt.MemoryUsage;
}
```

### ✅ STEP 5: Create Integration Tests (2 hours)

Add tests to verify end-to-end flows

```csharp
// src/MonadoBlade.Tests/Integration/ServiceIntegrationTests.cs
[TestClass]
public class ServiceIntegrationTests
{
    private IServiceProvider _services;
    
    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection()
            .AddMonadoBladeServices()
            .BuildServiceProvider();
    }
    
    [TestMethod]
    public async Task AllServicesInitialize()
    {
        var orchestrator = _services.GetRequiredService<IServiceOrchestrator>();
        await orchestrator.InitializeAsync();
        
        var health = await orchestrator.GetHealthAsync();
        Assert.AreEqual("Ready", health.Status);
    }
}
```

### ✅ STEP 6: Build & Test (1-2 hours)

```powershell
# Build
dotnet build src/MonadoBlade.sln

# Run tests
dotnet test src/MonadoBlade.Tests

# Run console
dotnet run --project src/MonadoBlade.Console
```

---

## INTEGRATION LAYERS SUMMARY

### Layer 1: Services (Business Logic)
```
HermesFleetOrchestrator ──┐
HermesMonitoringService ─┼─→ IServiceBus (events) ─→ ServiceBus
HermesSecurityService ───┘

↓ (Dependencies)

IUnitOfWork (database)
ILogger (logging)
```

### Layer 2: ViewModels (State Management)
```
DashboardViewModel ───┐
FleetViewModel ───────┼─→ IServiceBus (subscribe) ─→ Events
SettingsViewModel ────┘

↓ (Method calls)

IMonitoringService
IFleetOrchestrator
ISecurityService
```

### Layer 3: UI (Presentation)
```
DashboardPage ────┐
FleetPage ────────┼─→ ViewModels (binding) ─→ Properties
SettingsPage ─────┘

↓ (Commands)

ViewModels.LoadAsync()
ViewModels.RegisterAsync()
```

### Layer 4: Persistence (Data)
```
HermesDbContext (EF Core)
└── 6 DbSets
    ├── MetricsSnapshots
    ├── LearningPatterns
    ├── SecurityEvents
    ├── AuditLogs
    ├── AgentStatuses
    └── BootSequenceLogs
```

---

## DATA FLOW EXAMPLES

### Example 1: User Refreshes Metrics

```
User clicks "Refresh" button
    ↓
DashboardPage_RefreshButton_Click()
    ↓
DashboardViewModel.RefreshMetricsCommand
    ↓
DashboardViewModel.LoadMetricsAsync()
    ↓
HermesMonitoringService.CollectSystemMetricsAsync()
    ↓
[Collect from system + Save to DB]
    ↓
ServiceBus.Publish(MetricsCollectedEvent)
    ↓
[Other services receive event]
    ↓
Return SystemMetrics to ViewModel
    ↓
ViewModel updates: CurrentCpuUsage, CurrentMemoryUsage
    ↓
UI bindings auto-update (no code needed)
    ↓
User sees updated CPU/Memory values
```

### Example 2: Agent Registers

```
User clicks "Register Agent" button
    ↓
FleetViewModel.RegisterAgentCommand("MyAgent")
    ↓
HermesFleetOrchestrator.RegisterAgentAsync("MyAgent")
    ↓
[Register in-memory]
    ↓
[Persist to database]
    ↓
ServiceBus.Publish(AgentRegisteredEvent)
    ↓
HermesMonitoringService receives event
    ↓
Monitoring updates tracking
    ↓
FleetViewModel receives event (subscribed)
    ↓
Load agents list: LoadAgentsAsync()
    ↓
Update Agents collection in ViewModel
    ↓
UI ItemsControl auto-updates
    ↓
User sees new agent in list
```

---

## KEY INTERFACES

### Services Must Implement

```csharp
// Fleet orchestration
public interface IFleetOrchestrator
{
    Task RegisterAgentAsync(string agentName);
    Task StartBootSequenceAsync();
    Task<double> GetBootProgressAsync();
    Task OptimizeProcessAsync();
    Task<List<string>> GetActiveAgentsAsync();
    Task<List<string>> GetDeadAgentsAsync(TimeSpan timeout);
    Task StopAsync();
}

// Monitoring & metrics
public interface IMonitoringService
{
    Task<SystemMetrics> CollectSystemMetricsAsync();
    Task<double> GetAverageCpuUsageAsync();
    Task<double> GetPeakCpuUsageAsync();
    Task<IEnumerable<SystemMetrics>> GetMetricsHistoryAsync(int count);
    Task StopAsync();
}

// Security & hardening
public interface ISecurityService
{
    Task VerifyTPMAsync();
    Task VerifyBitLockerAsync();
    Task ApplySecurityHardeningAsync();
    Task QuarantineThreatAsync(string threatPath);
    Task StopAsync();
}
```

---

## COMMON INTEGRATION PATTERNS

### Pattern 1: Service-to-Service Communication
```csharp
// In Service A
await _serviceBus.PublishAsync(new ServiceAEvent { /* data */ });

// In Service B
_serviceBus.Subscribe<ServiceAEvent>(evt => 
{
    // Handle event
});
```

### Pattern 2: ViewModel-to-Service Binding
```csharp
// In ViewModel
await ExecuteAsync(async () =>
{
    var result = await _service.DoSomethingAsync();
    MyProperty = result;
    IsEmpty = result == null;
});
```

### Pattern 3: Service-to-Database Persistence
```csharp
// In Service
var entity = new MyEntity { /* data */ };
await _unitOfWork.MyEntities.AddAsync(entity);
await _unitOfWork.SaveChangesAsync();
```

### Pattern 4: Event-Driven UI Updates
```csharp
// In ViewModel
_serviceBus.Subscribe<MyEvent>(evt =>
{
    // Update properties (triggers UI binding)
    MyCollectionProperty.Add(evt.Item);
});
```

---

## TESTING STRATEGY

### Unit Tests (per service)
```csharp
[TestClass]
public class HermesMonitoringServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private HermesMonitoringService _service;
    
    [TestMethod]
    public async Task CollectMetrics_SavesToDatabase()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.Metrics.AddAsync(It.IsAny<SystemMetricsSnapshot>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _service.CollectSystemMetricsAsync();
        
        // Assert
        _mockUnitOfWork.Verify(u => u.Metrics.AddAsync(It.IsAny<SystemMetricsSnapshot>()), 
            Times.Once);
    }
}
```

### Integration Tests (cross-layer)
```csharp
[TestClass]
public class ServiceIntegrationTests
{
    [TestMethod]
    public async Task MetricsFlowEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddMonadoBladeServices()
            .BuildServiceProvider();
        
        // Act
        var orchestrator = services.GetRequiredService<IServiceOrchestrator>();
        await orchestrator.InitializeAsync();
        
        var monitoring = services.GetRequiredService<IMonitoringService>();
        var metrics = await monitoring.CollectSystemMetricsAsync();
        
        // Assert
        Assert.IsNotNull(metrics);
        Assert.IsTrue(metrics.CpuUsage >= 0);
    }
}
```

---

## DEPLOYMENT CHECKLIST

- [ ] All 5 integration files created and compiling
- [ ] Services implement IFleetOrchestrator, IMonitoringService, ISecurityService
- [ ] DI setup centralized in MonadoDependencies
- [ ] Console Program updated with new orchestrator
- [ ] ViewModels subscribe to service bus events
- [ ] Database tables created (migrations run)
- [ ] Unit tests pass (80+ tests)
- [ ] Integration tests pass (end-to-end flows)
- [ ] No build warnings
- [ ] Logging configured correctly
- [ ] Error handling tested
- [ ] Graceful shutdown implemented

---

## NEXT PHASE: UI COMPONENTS

Once integration is complete:

1. Create design tokens XAML resource dictionary
2. Implement 18 core components (atoms, molecules, organisms)
3. Wire components to ViewModels
4. Implement responsive layouts
5. Add accessibility (WCAG AA)

---

## QUICK REFERENCE

| Component | File | Purpose |
|-----------|------|---------|
| ServiceBus | ServiceBus.cs | Event pub/sub |
| ServiceOrchestrator | ServiceOrchestrator.cs | Service initialization |
| ResilientServiceIntegration | ResilientServiceIntegration.cs | Retry + error handling |
| DependencyInjection | DependencyInjection.cs | Centralized DI setup |
| Program | Program.cs | Console entry point |

---

**Phase 10: Complete Integration Ready for Implementation** ✅
**Estimated completion: 8-10 hours**
