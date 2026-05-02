# PHASE 10: SYSTEM ARCHITECTURE VISUAL GUIDE

**Format**: ASCII diagrams + reference tables
**Purpose**: Quick visual understanding of entire integration
**Use Case**: Whiteboard reference, debugging, planning

---

## 📐 COMPLETE SYSTEM ARCHITECTURE

```
╔════════════════════════════════════════════════════════════════════════════╗
║                   MONADO BLADE v2.0 INTEGRATION ARCHITECTURE               ║
╚════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│ LAYER 0: DEPENDENCY INJECTION (Foundation)                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                         ServiceCollection                                    │
│                              ↓                                               │
│                    AddMonadoBladeServices()                                  │
│                              ↓                                               │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐             │
│  ↓              ↓              ↓              ↓              ↓             │
│ Logger      ServiceBus    Orchestrator   Resilience    DbContext           │
│  (Serilog)  (Pub/Sub)     (Sequencer)    (Retry)       (EF Core)          │
└─────────────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ LAYER 1: SERVICES (Business Logic)                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐ │
│  │ HermesFleetOrchest   │  │ HermesMonitoring     │  │ HermesSecurity   │ │
│  ├──────────────────────┤  ├──────────────────────┤  ├──────────────────┤ │
│  │ • RegisterAgent      │  │ • CollectMetrics     │  │ • VerifyTPM      │ │
│  │ • StartBoot          │  │ • GetMetricHistory   │  │ • VerifyBitLock  │ │
│  │ • OptimizeProcess    │  │ • AnalyzeLearning    │  │ • ApplyHardening │ │
│  │ • GetBootProgress    │  │ • GetAverageCPU      │  │ • QuarantineThreat
│  └──────────────────────┘  └──────────────────────┘  └──────────────────┘ │
│           ↓ Publish Events           ↓ Publish Events      ↓ Publish Events│
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐ │
│  │ AgentRegisteredEvent │  │ MetricsCollectedEvt  │  │ ThreatDetectedEv │ │
│  │ BootProgressChangedEv│  │ LearningCompleteEv   │  │ SecurityEvent    │ │
│  └──────────────────────┘  └──────────────────────┘  └──────────────────┘ │
│                              ServiceBus (Central Hub)                       │
│                                    ↑ ↓                                      │
└─────────────────────────────────────┼─────────────────────────────────────┘
                                      │
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ LAYER 2: VIEWMODELS (State Management & Binding)                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐ │
│  │ DashboardViewModel   │  │ FleetViewModel       │  │ SettingsViewModel│ │
│  ├──────────────────────┤  ├──────────────────────┤  ├──────────────────┤ │
│  │ StateVM Base         │  │ StateVM Base         │  │ StateVM Base     │ │
│  │                      │  │                      │  │                  │ │
│  │ Properties:          │  │ Properties:          │  │ Properties:      │ │
│  │ • IsLoading          │  │ • IsLoading          │  │ • IsLoading      │ │
│  │ • CurrentCpuUsage    │  │ • Agents (ObsColl)   │  │ • SecurityScore  │ │
│  │ • MetricsHistory     │  │ • BootProgress       │  │ • Policies       │ │
│  │ • Error              │  │ • IsBootRunning      │  │ • Vulnerabilities│ │
│  │                      │  │ • Error              │  │ • Error          │ │
│  │ Subscribe to:        │  │ Subscribe to:        │  │ Subscribe to:    │ │
│  │ • MetricsCollectedEv │  │ • AgentRegisteredEv  │  │ • SecurityEventEv│ │
│  └──────────────────────┘  └──────────────────────┘  └──────────────────┘ │
│           ↓ XAML Binding            ↓ XAML Binding      ↓ XAML Binding    │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ LAYER 3: UI (Presentation & Commands)                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐ │
│  │ DashboardPage        │  │ FleetPage            │  │ SettingsPage     │ │
│  ├──────────────────────┤  ├──────────────────────┤  ├──────────────────┤ │
│  │ <MetricsCard>        │  │ <DataGrid>           │  │ <ToggleSwitch>   │ │
│  │ <Chart>              │  │ <ProgressBar>        │  │ <TextBox>        │ │
│  │ <Button Refresh>     │  │ <Button AddAgent>    │  │ <Button Harden>  │ │
│  │ <TextBlock Status>   │  │ <Button Boot>        │  │ <Slider>         │ │
│  └──────────────────────┘  └──────────────────────┘  └──────────────────┘ │
│           ↑ Bind to                 ↑ Bind to            ↑ Bind to        │
│      DashboardViewModel        FleetViewModel      SettingsViewModel     │
└─────────────────────────────────────────────────────────────────────────────┘
                                      ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ LAYER 4: PERSISTENCE (Data Storage)                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│             HermesDbContext (Entity Framework Core)                          │
│                              ↓                                               │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐             │
│  ↓              ↓              ↓              ↓              ↓             │
│ Metrics     Security        Fleet         Learning       Audit             │
│ Snapshots   Events          Status        Patterns       Logs              │
│ (30s)       (every op)       (heartbeat)   (batch)       (all ops)         │
│                                                                              │
│             SQL Server / PostgreSQL / SQLite                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 DATA FLOW DIAGRAM

```
USER ACTION (Click Button)
       ↓
COMMAND BINDING (DataContext=ViewModel)
       ↓
VIEWMODEL METHOD (e.g., LoadMetricsAsync)
       ↓
EXECUTE ASYNC WRAPPER (IsLoading = true)
       ↓
SERVICE CALL (e.g., CollectSystemMetricsAsync)
       ↓
COLLECTION LOGIC (e.g., Read system CPU/Memory)
       ↓
DATABASE PERSISTENCE (Via IUnitOfWork)
       ├─→ Create Entity (SystemMetricsSnapshot)
       ├─→ Add to DbSet
       └─→ SaveChangesAsync()
       ↓
EVENT PUBLICATION (ServiceBus.PublishAsync)
       ├─→ MetricsCollectedEvent
       ├─→ All subscribers notified
       └─→ Other services react
       ↓
RETURN TO VIEWMODEL
       ├─→ Map result to properties
       ├─→ Update IsLoading = false
       ├─→ Update Error (if any)
       └─→ Trigger INotifyPropertyChanged
       ↓
UI BINDING UPDATES (XAML Auto-Update)
       ├─→ TextBlock.Text = new value
       ├─→ MetricsCard re-renders
       ├─→ LoadingIndicator disappears
       └─→ Success message shows
       ↓
USER SEES RESULT
```

---

## 📊 SERVICE INTERACTION MATRIX

```
                 Fleet    Monitor  Security   Bus     DB
Fleet            -        publish  publish   uses    uses
Monitor        subscribe    -       publish   uses    uses
Security       publish   subscribe    -       uses    uses
ServiceBus     central hub for all event-driven communication
Database       receives all persistence from services
```

---

## 🎯 CONTROL FLOW DURING STARTUP

```
PROGRAM START
    ↓
CREATE SERVICE COLLECTION
    ↓
CALL AddMonadoBladeServices()
    ↓
REGISTER ALL DEPENDENCIES
    ├─ ServiceBus (singleton)
    ├─ HermesFleetOrchestrator (singleton)
    ├─ HermesMonitoringService (singleton)
    ├─ HermesSecurityService (singleton)
    ├─ IServiceOrchestrator (singleton)
    ├─ IUnitOfWork (scoped)
    └─ DbContext (scoped)
    ↓
BUILD SERVICE PROVIDER
    ↓
GET ORCHESTRATOR FROM CONTAINER
    ↓
CALL orchestrator.InitializeAsync()
    ↓
PHASE 1: SECURITY
    ├─ VerifyTPMAsync()
    ├─ VerifyBitLockerAsync()
    └─ Set _health.SecurityReady = true
    ↓
PHASE 2: FLEET (Depends on Phase 1)
    ├─ StartBootSequenceAsync()
    ├─ Register self-hearbeat
    └─ Set _health.FleetReady = true
    ↓
PHASE 3: MONITORING (Depends on Phase 1 + 2)
    ├─ CollectSystemMetricsAsync()
    ├─ Publish MetricsCollectedEvent
    └─ Set _health.MonitoringReady = true
    ↓
SUBSCRIBE TO EVENTS
    ├─ Services subscribe to each other's events
    ├─ ViewModels subscribe to service events
    └─ UI components subscribe to ViewModel changes
    ↓
HEALTH REPORT
    ├─ Status = "Ready"
    ├─ All services = ready
    └─ System operational
    ↓
RETURN CONTROL TO MAIN PROGRAM
    ↓
DISPLAY INTERACTIVE MENU
```

---

## 🔐 ERROR HANDLING FLOW

```
SERVICE CALL INITIATED
    ↓
RESILIENT WRAPPER ATTEMPTS
    ├─ Attempt 1: Execute
    │  ├─ Success → Return result
    │  └─ Timeout/Network → Attempt 2
    ├─ Attempt 2: Wait 500ms, retry
    │  ├─ Success → Return result
    │  └─ Timeout/Network → Attempt 3
    ├─ Attempt 3: Wait 1000ms, retry
    │  ├─ Success → Return result
    │  └─ Error → FAIL
    └─ After 3 failures → Throw exception
    ↓
VIEWMODEL CATCHES EXCEPTION
    ├─ Set Error property
    ├─ Set IsLoading = false
    └─ Trigger INotifyPropertyChanged
    ↓
UI ERROR BOUNDARY DISPLAYS
    ├─ Show error message
    ├─ Show retry button
    └─ Hide success content
    ↓
USER CLICKS RETRY
    └─ Flow starts again (Attempt 1)
```

---

## 🌐 EVENT PROPAGATION EXAMPLE

```
SCENARIO: User clicks "Refresh Metrics"

Step 1: UI Layer
    DashboardPage_RefreshButton_Click()
        ↓ Command binding
        
Step 2: ViewModel Layer
    DashboardViewModel.LoadMetricsAsync()
        ├─ IsLoading = true
        ├─ Call service
        └─ (UI updates via binding)
        ↓
        
Step 3: Service Layer
    HermesMonitoringService.CollectSystemMetricsAsync()
        ├─ Read system metrics (CPU, Memory, GPU)
        ├─ Create entity
        ├─ Add to repository
        ├─ Save to database
        ├─ Publish event: MetricsCollectedEvent
        └─ Return metrics object
        ↓
        
Step 4: Event Distribution
    ServiceBus.PublishAsync(MetricsCollectedEvent)
        ├─ HermesFleetOrchestrator subscribes
        │  └─ Notified of new metrics
        ├─ HermesSecurityService subscribes
        │  └─ Checks for anomalies
        ├─ DashboardViewModel subscribes
        │  └─ Updates display
        └─ Other subscribers...
        ↓
        
Step 5: ViewModel Updates
    OnMetricsUpdated(MetricsCollectedEvent evt)
        ├─ CurrentCpuUsage = evt.CpuUsage
        ├─ CurrentMemoryUsage = evt.MemoryUsage
        ├─ IsLoading = false
        └─ Trigger INotifyPropertyChanged
        ↓
        
Step 6: UI Re-renders
    XAML Binding Auto-Updates
        ├─ <TextBlock Text="{Binding CurrentCpuUsage}" />
        ├─ MetricsCard re-calculates display
        ├─ LoadingIndicator disappears
        └─ Chart updates with new data
        ↓
        
RESULT: User sees fresh metrics on screen
```

---

## 📈 SCALABILITY ARCHITECTURE

```
SINGLE MACHINE (Current)
┌─────────────────────────┐
│  Monado Blade Console   │
├─────────────────────────┤
│  Services (3)           │
│  ViewModels (3)         │
│  Database (1)           │
└─────────────────────────┘

MULTI-MACHINE (Phase 4-5)
┌────────────┐
│ Controller │  (Central ServiceBus + Orchestrator)
├────────────┤
│ Fleet      │  (Distributed services)
│ Monitoring │
│ Security   │
└────────────┘
     ↓ (Network)
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Agent 1  │  │ Agent 2  │  │ Agent N  │
├──────────┤  ├──────────┤  ├──────────┤
│Services  │  │Services  │  │Services  │
│LocalDB   │  │LocalDB   │  │LocalDB   │
└──────────┘  └──────────┘  └──────────┘
     ↓ (Heartbeat)
┌─────────────────────────────┐
│ Central Database            │
│ (Metrics aggregation)       │
└─────────────────────────────┘
```

---

## 🎓 LAYER RESPONSIBILITIES

| Layer | Responsibility | Example |
|-------|----------------|---------|
| **DI** | Provide dependencies | Create ServiceBus instance |
| **Services** | Business logic | Collect metrics from system |
| **ServiceBus** | Event communication | Publish MetricsCollected event |
| **ViewModels** | State management | Hold CurrentCpuUsage property |
| **UI** | User interaction | Display metrics, handle click |
| **Database** | Persistence | Save metrics snapshot to DB |

---

## ✅ INTEGRATION CHECKLIST

```
FOUNDATION LAYER
☐ ServiceBus created and working
☐ ServiceOrchestrator sequencing correctly
☐ DependencyInjection centralized
☐ Resilience wrapper configured
☐ Database context created

SERVICE LAYER
☐ IFleetOrchestrator implemented
☐ IMonitoringService implemented
☐ ISecurityService implemented
☐ Events published correctly
☐ Services subscribe to each other's events

VIEWMODEL LAYER
☐ DashboardViewModel receives events
☐ FleetViewModel receives events
☐ SettingsViewModel receives events
☐ Properties update on event received
☐ INotifyPropertyChanged fires

UI LAYER
☐ XAML bindings target ViewModel properties
☐ Button commands call ViewModel methods
☐ LoadingIndicator shows during async
☐ Error displays show exceptions
☐ UI updates when properties change

DATABASE LAYER
☐ EF Core DbContext works
☐ Migrations run successfully
☐ Services persist data
☐ Historical queries return results
☐ Database transactions atomic

INTEGRATION TESTS
☐ Service initialization order correct
☐ Events propagate to all subscribers
☐ ViewModel receives event data
☐ UI updates reflect ViewModel changes
☐ Database persists all data
☐ Error handling tested
☐ Recovery from failures works
```

---

**Phase 10: Complete System Architecture** ✅
**Visual guides, diagrams, and reference materials for entire integration**
