# PHASE 10: INTEGRATION QUICK REFERENCE CARD

**Print This** | **Keep Handy** | **Reference While Coding**

---

## 🎯 INTEGRATION AT A GLANCE

```
USER ACTION
    ↓
UI COMMAND
    ↓
VIEWMODEL METHOD
    ↓
SERVICE CALL
    ↓
DATABASE PERSISTENCE
    ↓
EVENT PUBLICATION
    ↓
VIEWMODEL SUBSCRIPTION
    ↓
UI PROPERTY UPDATE
    ↓
USER SEES RESULT
```

---

## 📋 THE 6 FILES TO CREATE/UPDATE

| # | File | Purpose | LOC | Effort |
|---|------|---------|-----|--------|
| 1 | ServiceBus.cs | Event pub/sub | 400 | 1h |
| 2 | ServiceOrchestrator.cs | Init sequence | 200 | 0.5h |
| 3 | ResilientServiceIntegration.cs | Retry logic | 150 | 0.5h |
| 4 | DependencyInjection.cs | Centralized DI | 180 | 0.5h |
| 5 | ServiceBase.cs | Service base class | 100 | 0.5h |
| 6 | Program.cs | Console entry | 150 | 1h |

---

## 🔗 INTEGRATION WIRING DIAGRAM

```
SERVICE LAYER
├─ Orchestrator
│  ├─ Fleet
│  ├─ Monitoring
│  └─ Security
└─ ServiceBus (events)
    ├─ MetricsCollectedEvent
    ├─ AgentRegisteredEvent
    ├─ SecurityThreatDetectedEvent
    └─ BootProgressChangedEvent
         ↓
VIEWMODEL LAYER
├─ DashboardViewModel (subscribes)
├─ FleetViewModel (subscribes)
└─ SettingsViewModel (subscribes)
     ↓
UI LAYER
├─ DashboardPage (binds)
├─ FleetPage (binds)
└─ SettingsPage (binds)
     ↓
DATABASE LAYER
├─ MetricsSnapshots
├─ SecurityEvents
└─ AgentStatuses
```

---

## 💻 CODE SNIPPETS

### Snippet 1: Publish Event
```csharp
// In service
await _serviceBus.PublishAsync(new MetricsCollectedEvent 
{ 
    CpuUsage = 45.0,
    MemoryUsage = 60.0
});
```

### Snippet 2: Subscribe to Event
```csharp
// In ViewModel constructor
_serviceBus.Subscribe<MetricsCollectedEvent>(evt =>
{
    CurrentCpuUsage = evt.CpuUsage;
});
```

### Snippet 3: Call Service
```csharp
// In ViewModel
var metrics = await _monitoringService.CollectSystemMetricsAsync();
CurrentCpuUsage = metrics.CpuUsage;
```

### Snippet 4: Persist to Database
```csharp
// In service
await _unitOfWork.Metrics.AddAsync(snapshot);
await _unitOfWork.SaveChangesAsync();
```

### Snippet 5: DI Setup
```csharp
// In Program.cs
var services = new ServiceCollection()
    .AddMonadoBladeServices()
    .BuildServiceProvider();
```

### Snippet 6: ViewModel Binding
```xaml
<!-- In XAML -->
<TextBlock Text="{Binding CurrentCpuUsage}" />
<Button Command="{Binding RefreshCommand}" />
```

---

## 🧪 TESTING CHECKLIST

- [ ] ServiceBus.Publish() works
- [ ] ServiceBus.Subscribe() receives events
- [ ] Services initialize in order
- [ ] ViewModels receive service data
- [ ] UI updates when ViewModel changes
- [ ] Database saves on service call
- [ ] Retry works on timeout
- [ ] Circuit breaker trips on failures
- [ ] Logging captures all operations
- [ ] Graceful shutdown works

---

## ⚠️ COMMON MISTAKES TO AVOID

| ❌ Wrong | ✅ Right |
|---------|----------|
| Service calls other service directly | Use ServiceBus.Publish() |
| DI setup in multiple files | Centralize in DependencyInjection.cs |
| ViewModel doesn't unsubscribe | Implement IDisposable in ViewModel |
| Hard-coded connection string | Use appsettings.json |
| No retry on database errors | Wrap with ResilientServiceIntegration |
| Logging only on errors | Log at every state change |
| No interfaces on services | Create IFleetOrchestrator, etc. |
| Database calls without using IUnitOfWork | Always use repository pattern |

---

## 📊 SUCCESS METRICS

**Code Quality**
- Build: 0 errors
- Warnings: <20
- Test coverage: >80%

**Performance**
- Init time: <2 seconds
- Service call: <500ms
- Event pub/sub: <50ms

**Reliability**
- Uptime: 99.9%
- Retry success: >95%
- Error recovery: automatic

---

## 🚨 TROUBLESHOOTING

| Problem | Solution |
|---------|----------|
| Service not initializing | Check ServiceOrchestrator phase order |
| Events not received | Verify subscription in ViewModel __init |
| Database errors | Check connection string + migrations |
| UI not updating | Check INotifyPropertyChanged binding |
| Build fails | Check namespaces + project references |
| Tests failing | Mock all dependencies in test |

---

## 📝 DOCUMENTATION MAP

| Need | Find In |
|------|---------|
| Architecture | COMPLETE_INTEGRATION.md |
| Code examples | INTEGRATION_CODE.md |
| Implementation steps | INTEGRATION_QUICKSTART.md |
| Best practices | PUBLIC_REPOS_REFERENCE.md |
| Full index | INTEGRATION_INDEX.md |
| Quick ref | This card |

---

## 🎯 IMPLEMENTATION ORDER

```
1. Create ServiceBus.cs
   ↓
2. Create ServiceOrchestrator.cs
   ↓
3. Create ResilientServiceIntegration.cs
   ↓
4. Update DependencyInjection.cs
   ↓
5. Add interfaces to services
   ↓
6. Update Program.cs
   ↓
7. Update ViewModels (add subscriptions)
   ↓
8. Build & test
   ↓
9. Commit to GitHub
```

---

## 💡 KEY PATTERNS

### Pattern 1: Event-Driven
```
Service A publishes → ServiceBus → Service B subscribes
```

### Pattern 2: Dependency Injection
```
ServiceCollection.AddMonadoBladeServices() → IServiceProvider → Services get deps
```

### Pattern 3: MVVM Binding
```
ViewModel.Property changes → INotifyPropertyChanged → UI auto-updates
```

### Pattern 4: Repository
```
ViewModel calls Service → Service calls Repository → Repository queries DbContext
```

### Pattern 5: Resilience
```
Service call → ResilientServiceIntegration → Retry logic → Circuit breaker
```

---

## 🎬 READY TO IMPLEMENT?

**✅ ALL DOCUMENTATION COMPLETE**
- 5 comprehensive guides (100 KB)
- 6 code files ready (1,180 LOC)
- 10 integration points defined
- Step-by-step checklist ready

**⏱️ ESTIMATED TIME**
- Setup: 1.5 hours
- Coding: 4-5 hours
- Testing: 2 hours
- Total: 8-10 hours

**🚀 START NOW**
→ Read PHASE10_INTEGRATION_QUICKSTART.md
→ Follow Step 1-6
→ Copy code from INTEGRATION_CODE.md
→ Build & test
→ Commit & push

---

**PHASE 10: INTEGRATION QUICK REFERENCE** ✅
**Print this, keep it handy, reference while coding**
