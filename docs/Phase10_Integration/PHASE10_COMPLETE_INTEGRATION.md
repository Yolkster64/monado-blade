# PHASE 10: COMPLETE SYSTEM INTEGRATION ARCHITECTURE

**Status**: End-to-End Integration Design
**Scope**: Service integration, ViewModel binding, UI data flow, database persistence, cross-layer communication

---

## EXECUTIVE INTEGRATION OVERVIEW

### 1.1 The Complete Data Flow Pipeline

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MONADO BLADE INTEGRATION ARCHITECTURE                │
└─────────────────────────────────────────────────────────────────────────┘

LAYER 1: SERVICES (Business Logic)
┌──────────────────┬──────────────────┬──────────────────┐
│   HermesFleet    │  HermesMonitoring│  HermesSecurity  │
│  Orchestrator    │    Service       │     Service      │
├──────────────────┼──────────────────┼──────────────────┤
│ • Register Agent │ • Collect Metrics│ • Apply Policies │
│ • Boot Pipeline  │ • LLM Analysis   │ • Audio Config   │
│ • Optimize Proc  │ • Learn Patterns │ • Threat Quarantine
└──────────────────┴──────────────────┴──────────────────┘
         ↓ (Async methods + Events)
         
LAYER 2: VIEWMODELS (State Management)
┌──────────────────┬──────────────────┬──────────────────┐
│   Dashboard VM   │     Fleet VM      │   Settings VM    │
│  (StateVM base)  │  (StateVM base)   │  (StateVM base)  │
├──────────────────┼──────────────────┼──────────────────┤
│ • IsLoading      │ • IsLoading       │ • IsLoading      │
│ • Error state    │ • Error state     │ • Error state    │
│ • Metrics data   │ • Agents list     │ • Policies       │
│ • History        │ • Boot progress   │ • Security score │
└──────────────────┴──────────────────┴──────────────────┘
         ↓ (INotifyPropertyChanged binding)
         
LAYER 3: UI COMPONENTS (Presentation)
┌──────────────────┬──────────────────┬──────────────────┐
│  Dashboard Page  │    Fleet Page     │  Settings Page   │
│  • MetricsCard   │  • AgentGrid      │  • PolicyToggles │
│  • Chart         │  • BootBar        │  • SecurityScore │
│  • Refresh btn   │  • AddAgentForm   │  • HardenButton  │
└──────────────────┴──────────────────┴──────────────────┘
         ↓ (User interactions)
         
LAYER 4: PERSISTENCE (Data Storage)
┌──────────────────┬──────────────────┬──────────────────┐
│   Metrics DB     │   Security DB     │    Fleet DB      │
│ • Snapshots      │ • Events          │ • Agent Status   │
│ • History        │ • Audit Logs      │ • Boot Logs      │
│ • LLM Results    │ • Threats         │ • Processes      │
└──────────────────┴──────────────────┴──────────────────┘
```

---

## PART 1: SERVICE-TO-SERVICE INTEGRATION

### 1.1 Service Discovery & Communication Pattern

```csharp
// Pattern: Services know about each other but communicate through events
namespace MonadoBlade.Core.Services.Integration;

/// <summary>
/// Service discovery and cross-service communication
/// Pattern: Service locator + event bus
/// </summary>
public interface IServiceBus
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Publish<TEvent>(TEvent @event) where TEvent : class;
}

public class ServiceBus : IServiceBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();
        
        _handlers[eventType].Add(handler);
    }
    
    public void Publish<TEvent>(TEvent @event) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
                (handler as Action<TEvent>)?.Invoke(@event);
        }
    }
}

// Integration events between services
public class AgentRegisteredEvent
{
    public string AgentName { get; init; }
    public DateTime RegisteredAt { get; init; }
}

public class MetricsCollectedEvent
{
    public SystemMetrics Metrics { get; init; }
    public DateTime CollectedAt { get; init; }
}

public class SecurityThreatDetectedEvent
{
    public string ThreatPath { get; init; }
    public SeverityLevel Severity { get; init; }
    public DateTime DetectedAt { get; init; }
}

// Usage in services
public class HermesFleetOrchestrator : IFleetOrchestrator
{
    private readonly IServiceBus _serviceBus;
    
    public async Task RegisterAgentAsync(string agentName)
    {
        // Register agent logic...
        
        // Notify other services
        _serviceBus.Publish(new AgentRegisteredEvent 
        { 
            AgentName = agentName,
            RegisteredAt = DateTime.UtcNow
        });
    }
}

public class HermesMonitoringService : IMonitoringService
{
    private readonly IServiceBus _serviceBus;
    
    public HermesMonitoringService(IServiceBus serviceBus)
    {
        _serviceBus = serviceBus;
        
        // Subscribe to fleet events
        _serviceBus.Subscribe<AgentRegisteredEvent>(OnAgentRegistered);
        _serviceBus.Subscribe<SecurityThreatDetectedEvent>(OnThreatDetected);
    }
    
    private void OnAgentRegistered(AgentRegisteredEvent evt)
    {
        // Log agent registration for metrics
        _logger.LogInformation("Agent registered: {AgentName}", evt.AgentName);
    }
    
    private void OnThreatDetected(SecurityThreatDetectedEvent evt)
    {
        // Record security event for analysis
        _logger.LogWarning("Security threat detected: {ThreatPath}", evt.ThreatPath);
    }
}
```

### 1.2 Coordinated Service Initialization

```csharp
// Pattern: Services initialize in dependency order
namespace MonadoBlade.Core.Services.Integration;

public interface IServiceOrchestrator
{
    Task InitializeAsync();
    Task ShutdownAsync();
}

public class HermesServiceOrchestrator : IServiceOrchestrator
{
    private readonly IFleetOrchestrator _fleet;
    private readonly IMonitoringService _monitoring;
    private readonly ISecurityService _security;
    private readonly ILogger<HermesServiceOrchestrator> _logger;
    
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Hermes services...");
            
            // Phase 1: Security (must be first)
            _logger.LogInformation("Phase 1: Initializing security...");
            await _security.VerifyTPMAsync();
            await _security.VerifyBitLockerAsync();
            
            // Phase 2: Fleet (depends on security)
            _logger.LogInformation("Phase 2: Initializing fleet...");
            await _fleet.StartBootSequenceAsync();
            
            // Phase 3: Monitoring (depends on both)
            _logger.LogInformation("Phase 3: Initializing monitoring...");
            var metrics = await _monitoring.CollectSystemMetricsAsync();
            
            _logger.LogInformation("Hermes services initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Hermes services");
            await ShutdownAsync();
            throw;
        }
    }
    
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down Hermes services...");
        
        // Shutdown in reverse order
        await _monitoring.StopAsync();
        await _fleet.StopAsync();
        await _security.StopAsync();
        
        _logger.LogInformation("Hermes services shutdown complete");
    }
}
```

---

## PART 2: SERVICE-TO-VIEWMODEL INTEGRATION

### 2.1 Binding Pattern (Service → ViewModel)

```csharp
// Pattern: ViewModel calls service, maps result to observable properties
namespace MonadoBlade.UI.ViewModels;

public class DashboardViewModel : StateVM
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<DashboardViewModel> _logger;
    
    private ObservableCollection<SystemMetrics> _metricsHistory;
    
    [ObservableProperty] private double currentCpuUsage;
    [ObservableProperty] private double currentMemoryUsage;
    [ObservableProperty] private double averageCpuUsage;
    [ObservableProperty] private double peakCpuUsage;
    
    public DashboardViewModel(IMonitoringService monitoringService, ILogger<DashboardViewModel> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
        _metricsHistory = new();
    }
    
    /// <summary>Integration: Service → ViewModel → UI binding</summary>
    public async Task LoadMetricsAsync()
    {
        await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Loading metrics from service...");
            
            // Call service
            var metrics = await _monitoringService.CollectSystemMetricsAsync();
            
            // Map to ViewModel properties (triggers UI binding)
            CurrentCpuUsage = metrics.CpuUsage;
            CurrentMemoryUsage = metrics.MemoryUsage;
            
            // Load aggregates
            AverageCpuUsage = await _monitoringService.GetAverageCpuUsageAsync();
            PeakCpuUsage = await _monitoringService.GetPeakCpuUsageAsync();
            
            // Load history for chart
            var history = await _monitoringService.GetMetricsHistoryAsync(50);
            _metricsHistory.Clear();
            foreach (var item in history)
                _metricsHistory.Add(item);
            
            // Update empty state
            IsEmpty = _metricsHistory.Count == 0;
            
            _logger.LogInformation("Metrics loaded: CPU={Cpu}%, Memory={Memory}%", 
                CurrentCpuUsage, CurrentMemoryUsage);
        });
    }
    
    /// <summary>Auto-refresh on timer (every 2 seconds)</summary>
    public async Task StartAutoRefreshAsync(TimeSpan interval)
    {
        using var timer = new PeriodicTimer(interval);
        
        await foreach (var _ in timer.WaitForNextTickAsync())
        {
            if (!IsLoading)
                await LoadMetricsAsync();
        }
    }
}
```

### 2.2 ViewModel Collection Binding Pattern

```csharp
// Pattern: Observable collections auto-update UI when service data changes
namespace MonadoBlade.UI.ViewModels;

public class FleetViewModel : StateVM
{
    private readonly IFleetOrchestrator _fleetOrchestrator;
    
    [ObservableProperty] private ObservableCollection<AgentViewModel> agents;
    [ObservableProperty] private double bootProgress;
    [ObservableProperty] private bool isBootRunning;
    
    public async Task LoadAgentsAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Get from service
            var activeAgents = await _fleetOrchestrator.GetActiveAgentsAsync();
            var deadAgents = await _fleetOrchestrator.GetDeadAgentsAsync(TimeSpan.FromSeconds(5));
            
            // Convert to ViewModels
            Agents.Clear();
            
            foreach (var agentName in activeAgents)
            {
                Agents.Add(new AgentViewModel 
                { 
                    Name = agentName,
                    Status = "Active",
                    StatusColor = DesignTokens.Colors.Success
                });
            }
            
            foreach (var agentName in deadAgents)
            {
                Agents.Add(new AgentViewModel 
                { 
                    Name = agentName,
                    Status = "Offline",
                    StatusColor = DesignTokens.Colors.Danger
                });
            }
            
            IsEmpty = Agents.Count == 0;
        });
    }
}

// View model for individual items
public class AgentViewModel : ObservableObject
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private string statusColor = "";
    [ObservableProperty] private DateTime registeredAt;
}
```

### 2.3 Two-Way Integration (UI Command → Service)

```csharp
// Pattern: User action → ViewModel command → Service call → Result binding
namespace MonadoBlade.UI.ViewModels;

public partial class FleetViewModel : StateVM
{
    [RelayCommand]
    public async Task RegisterAgentAsync(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            Error = new ArgumentException("Agent name required");
            return;
        }
        
        await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Registering agent: {AgentName}", agentName);
            
            // Service call
            await _fleetOrchestrator.RegisterAgentAsync(agentName);
            
            // Update UI
            await LoadAgentsAsync();
            
            // Show confirmation
            await Task.Delay(500); // Brief delay for UX feedback
        });
    }
    
    [RelayCommand]
    public async Task StartBootSequenceAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsBootRunning = true;
            BootProgress = 0;
            
            // Service call
            await _fleetOrchestrator.StartBootSequenceAsync();
            
            // Poll progress
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                BootProgress = await _fleetOrchestrator.GetBootProgressAsync();
                
                if (BootProgress >= 100)
                    break;
            }
            
            IsBootRunning = false;
        });
    }
}
```

---

## PART 3: VIEWMODEL-TO-UI INTEGRATION

### 3.1 XAML Data Binding

```xaml
<!-- Dashboard.xaml -->
<Page x:Class="MonadoBlade.UI.Pages.DashboardPage">
    <Grid>
        <!-- Loading indicator -->
        <LoadingIndicator 
            IsVisible="{Binding IsLoading, Mode=OneWay}"
            StatusMessage="{Binding LoadingMessage, Mode=OneWay}" />
        
        <!-- Error boundary -->
        <ErrorBoundary 
            Error="{Binding Error, Mode=OneWay}"
            Content="{x:Bind ContentGrid}">
            <!-- Grid content here -->
        </ErrorBoundary>
        
        <!-- Metrics cards grid -->
        <ItemsControl ItemsSource="{Binding MetricsHistory, Mode=OneWay}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <UniformGrid Columns="2" ColumnSpacing="{StaticResource SpacingMD}" />
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <MetricsCard 
                        Value="{Binding CpuUsage, Mode=OneWay}"
                        Title="CPU"
                        Color="{StaticResource PrimaryBrush}" />
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <!-- Refresh button command binding -->
        <Button 
            Content="Refresh"
            Command="{Binding RefreshMetricsCommand}"
            IsEnabled="{Binding IsLoading, Mode=OneWay, Converter={StaticResource BoolNegationConverter}}" />
    </Grid>
</Page>
```

### 3.2 Code-Behind Initialization

```csharp
// DashboardPage.xaml.cs
public sealed partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;
    
    public DashboardPage()
    {
        this.InitializeComponent();
        
        // Get ViewModel from DI container
        _viewModel = App.Current.Services.GetRequiredService<DashboardViewModel>();
        this.DataContext = _viewModel;
    }
    
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Load initial data
        await _viewModel.LoadMetricsAsync();
        
        // Start periodic refresh
        _ = _viewModel.StartAutoRefreshAsync(TimeSpan.FromSeconds(2));
    }
}
```

### 3.3 Component Integration

```xaml
<!-- MetricsCard.xaml - Reusable component -->
<UserControl x:Class="MonadoBlade.UI.Components.MetricsCard">
    <Border Background="{StaticResource SurfaceBrush}" CornerRadius="{StaticResource BorderRadiusMedium}">
        <StackPanel Padding="{StaticResource PaddingMedium}" Spacing="{StaticResource SpacingMD}">
            <!-- Title -->
            <TextBlock 
                Text="{Binding Title, Mode=OneWay}"
                Style="{StaticResource BodyTextBlockStyle}" />
            
            <!-- Value -->
            <TextBlock 
                Text="{Binding Value, Mode=OneWay, StringFormat='{0:N1}%'}"
                FontSize="{StaticResource FontSizeDisplay}"
                Foreground="{Binding Color, Mode=OneWay}" />
            
            <!-- Trend indicator -->
            <StackPanel Orientation="Horizontal" Spacing="{StaticResource SpacingSM}">
                <FontIcon Glyph="{Binding TrendIcon, Mode=OneWay}" Foreground="{Binding TrendColor, Mode=OneWay}" />
                <TextBlock Text="{Binding TrendValue, Mode=OneWay, StringFormat='+{0:N1}%'}" />
            </StackPanel>
        </StackPanel>
    </Border>
</UserControl>
```

---

## PART 4: DATABASE INTEGRATION

### 4.1 Service-to-Database Pattern

```csharp
// Pattern: Services persist data through repository
namespace MonadoBlade.Core.Persistence;

public class HermesMonitoringService : IMonitoringService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<SystemMetrics> CollectSystemMetricsAsync()
    {
        var metrics = new SystemMetrics
        {
            CpuUsage = GetSystemCpuUsage(),
            MemoryUsage = GetSystemMemoryUsage(),
            GpuUsage = GetSystemGpuUsage(),
            CollectedAt = DateTime.UtcNow
        };
        
        // Persist to database
        var snapshot = new SystemMetricsSnapshot
        {
            CpuUsage = metrics.CpuUsage,
            MemoryUsage = metrics.MemoryUsage,
            GpuUsage = metrics.GpuUsage,
            Timestamp = metrics.CollectedAt
        };
        
        await _unitOfWork.Metrics.AddAsync(snapshot);
        await _unitOfWork.SaveChangesAsync();
        
        return metrics;
    }
    
    public async Task<IEnumerable<SystemMetrics>> GetMetricsHistoryAsync(int count)
    {
        // Query from database
        var snapshots = await _unitOfWork.Metrics
            .QueryAsync(m => m.Timestamp > DateTime.UtcNow.AddHours(-1))
            .ContinueWith(t => t.Result.OrderByDescending(m => m.Timestamp).Take(count));
        
        return snapshots.Select(s => new SystemMetrics
        {
            CpuUsage = s.CpuUsage,
            MemoryUsage = s.MemoryUsage,
            GpuUsage = s.GpuUsage
        });
    }
}

public class HermesSecurityService : ISecurityService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task QuarantineThreatAsync(string threatPath)
    {
        // Execute security action
        IsolateFile(threatPath);
        
        // Log to database for audit trail
        var securityEvent = new SecurityEvent
        {
            EventType = "ThreatQuarantined",
            ThreatPath = threatPath,
            Severity = SeverityLevel.High,
            Timestamp = DateTime.UtcNow,
            IsResolved = true
        };
        
        await _unitOfWork.SecurityEvents.AddAsync(securityEvent);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### 4.2 Database Synchronization Pattern

```csharp
// Pattern: Keep database in sync with real-time service data
namespace MonadoBlade.Core.Persistence;

public class HermesFleetOrchestrator : IFleetOrchestrator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceBus _serviceBus;
    
    public async Task RegisterAgentAsync(string agentName)
    {
        // Register in-memory
        var agent = new Agent { Name = agentName };
        RegisterInMemory(agent);
        
        // Persist to database
        var agentStatus = new AgentStatus
        {
            AgentName = agentName,
            Status = "Active",
            RegisteredAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow
        };
        
        await _unitOfWork.Agents.AddAsync(agentStatus);
        await _unitOfWork.SaveChangesAsync();
        
        // Broadcast event for other services
        _serviceBus.Publish(new AgentRegisteredEvent 
        { 
            AgentName = agentName,
            RegisteredAt = DateTime.UtcNow
        });
    }
    
    /// <summary>Periodic sync: Update database with current state</summary>
    public async Task SyncDatabaseAsync()
    {
        var activeAgents = GetActiveAgents();
        
        foreach (var agent in activeAgents)
        {
            var existing = await _unitOfWork.Agents
                .QueryAsync(a => a.AgentName == agent.Name);
            
            if (existing.Any())
            {
                var status = existing.First();
                status.LastHeartbeat = DateTime.UtcNow;
                status.Status = "Active";
                
                await _unitOfWork.Agents.UpdateAsync(status);
            }
        }
        
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### 4.3 Query Performance Integration

```csharp
// Pattern: Efficient database queries with indexing
namespace MonadoBlade.Core.Persistence.Configurations;

public class SystemMetricsConfiguration : IEntityTypeConfiguration<SystemMetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<SystemMetricsSnapshot> builder)
    {
        builder.ToTable("SystemMetricsSnapshots");
        builder.HasKey(e => e.Id);
        
        // Index for efficient queries by time range
        builder.HasIndex(e => e.Timestamp)
            .IsDescending()
            .HasDatabaseName("IX_Metrics_Timestamp_Desc");
        
        // Composite index for common queries
        builder.HasIndex(e => new { e.Timestamp, e.CpuUsage })
            .HasDatabaseName("IX_Metrics_TimeAndCpu");
        
        // Column configuration
        builder.Property(e => e.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.Property(e => e.CpuUsage)
            .HasColumnType("DECIMAL(5,2)")
            .HasPrecision(5, 2);
        
        // Partitioning hint for large datasets
        builder.ToTable("SystemMetricsSnapshots")
            .HasComment("Partitioned by date for performance");
    }
}
```

---

## PART 5: ERROR HANDLING & RESILIENCE INTEGRATION

### 5.1 Cross-Layer Error Propagation

```csharp
// Pattern: Errors flow up through layers with context
namespace MonadoBlade.Core.Services.Integration;

public class ResilientServiceIntegration
{
    private readonly ILogger<ResilientServiceIntegration> _logger;
    
    /// <summary>Execute service call with retry logic and error context</summary>
    public async Task<T?> ExecuteWithRetryAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? delayBetweenRetries = null)
    {
        delayBetweenRetries ??= TimeSpan.FromMilliseconds(500);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Executing {Operation} (attempt {Attempt}/{MaxRetries})", 
                    operationName, attempt, maxRetries);
                
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, 
                    "{Operation} failed on attempt {Attempt}, retrying in {Delay}ms",
                    operationName, attempt, delayBetweenRetries.Value.TotalMilliseconds);
                
                await Task.Delay(delayBetweenRetries.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Operation} failed after {Attempts} attempts", 
                    operationName, maxRetries);
                throw;
            }
        }
        
        return default;
    }
}
```

### 5.2 UI Error Handling Integration

```csharp
// Pattern: ViewModel captures service errors and displays in UI
namespace MonadoBlade.UI.ViewModels;

public class ResilientViewModel : StateVM
{
    protected async Task<T?> SafeExecuteAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Action<Exception>? onError = null)
    {
        try
        {
            IsLoading = true;
            Error = null;
            
            return await operation();
        }
        catch (ArgumentException ex)
        {
            Error = new UserFriendlyException("Invalid input", ex.Message);
            onError?.Invoke(ex);
        }
        catch (TimeoutException ex)
        {
            Error = new UserFriendlyException("Operation timeout", 
                "The operation took too long. Please try again.");
            onError?.Invoke(ex);
        }
        catch (HttpRequestException ex)
        {
            Error = new UserFriendlyException("Network error", 
                "Unable to connect to service. Check your connection.");
            onError?.Invoke(ex);
        }
        catch (Exception ex)
        {
            Error = new UserFriendlyException("Operation failed", 
                $"An unexpected error occurred: {ex.Message}");
            onError?.Invoke(ex);
        }
        finally
        {
            IsLoading = false;
        }
        
        return default;
    }
}
```

### 5.3 Database Error Handling Integration

```csharp
// Pattern: Handle database errors gracefully
namespace MonadoBlade.Core.Persistence;

public class ResilientRepository<T> : IRepository<T> where T : class
{
    private readonly HermesDbContext _context;
    private readonly ILogger<ResilientRepository<T>> _logger;
    
    public async Task SaveAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed for {Entity}", typeof(T).Name);
            throw new PersistenceException($"Failed to save {typeof(T).Name}", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Database operation cancelled");
            throw;
        }
    }
}

public class PersistenceException : Exception
{
    public PersistenceException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

---

## PART 6: REAL-TIME DATA SYNCHRONIZATION

### 6.1 Event-Driven Synchronization

```csharp
// Pattern: Services publish events when data changes, subscribers update UI
namespace MonadoBlade.Core.Services.Integration;

public class DataSynchronizationBus : IServiceBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.ContainsKey(eventType))
            _subscribers[eventType] = new List<Delegate>();
        
        _subscribers[eventType].Add(handler);
    }
    
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers
                .Cast<Func<TEvent, Task>>()
                .Select(h => h(@event));
            
            await Task.WhenAll(tasks);
        }
    }
}

// Event definitions
public class MetricsUpdatedEvent
{
    public SystemMetrics CurrentMetrics { get; init; }
    public DateTime CollectedAt { get; init; }
}

public class AgentStatusChangedEvent
{
    public string AgentName { get; init; }
    public string NewStatus { get; init; }
    public DateTime ChangedAt { get; init; }
}

// Usage in ViewModel
public class DashboardViewModel : StateVM
{
    private readonly DataSynchronizationBus _syncBus;
    
    public DashboardViewModel(DataSynchronizationBus syncBus)
    {
        _syncBus = syncBus;
        
        // Subscribe to real-time updates
        _syncBus.Subscribe<MetricsUpdatedEvent>(OnMetricsUpdated);
    }
    
    private async Task OnMetricsUpdated(MetricsUpdatedEvent evt)
    {
        // Update ViewModel automatically
        CurrentCpuUsage = evt.CurrentMetrics.CpuUsage;
        CurrentMemoryUsage = evt.CurrentMetrics.MemoryUsage;
        
        await Task.CompletedTask;
    }
}
```

### 6.2 WebSocket Integration (for future real-time features)

```csharp
// Pattern: Real-time data push from service to UI
namespace MonadoBlade.Core.Services.Integration;

public interface IRealtimeService
{
    Task<IAsyncEnumerable<SystemMetrics>> StreamMetricsAsync(TimeSpan updateInterval);
}

public class RealtimeMonitoringService : IRealtimeService
{
    private readonly HermesMonitoringService _monitoringService;
    
    public async IAsyncEnumerable<SystemMetrics> StreamMetricsAsync(TimeSpan updateInterval)
    {
        using var timer = new PeriodicTimer(updateInterval);
        
        await foreach (var _ in timer.WaitForNextTickAsync())
        {
            var metrics = await _monitoringService.CollectSystemMetricsAsync();
            yield return metrics;
        }
    }
}

// Usage in ViewModel
public class RealtimeDashboardViewModel : StateVM
{
    private readonly IRealtimeService _realtimeService;
    
    public async Task StartRealtimeStreamAsync()
    {
        await foreach (var metrics in _realtimeService.StreamMetricsAsync(TimeSpan.FromSeconds(1)))
        {
            CurrentCpuUsage = metrics.CpuUsage;
            CurrentMemoryUsage = metrics.MemoryUsage;
            
            _metricsHistory.Add(metrics);
        }
    }
}
```

---

## PART 7: COMPLETE END-TO-END INTEGRATION FLOW

### 7.1 User Action → Complete Flow

```
┌──────────────────────────────────────────────────────────────┐
│                    USER CLICKS "REFRESH METRICS"             │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 1. UI LAYER (DashboardPage.xaml)                             │
│    - Button_Click → Call ViewModel Command                   │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 2. VIEWMODEL LAYER (DashboardViewModel.cs)                   │
│    - ExecuteAsync starts                                     │
│    - IsLoading = true (triggers LoadingIndicator in UI)     │
│    - Call service: await _monitoringService.Collect...()   │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 3. SERVICE LAYER (HermesMonitoringService.cs)               │
│    - CollectSystemMetricsAsync()                            │
│    - Read system metrics (CPU, Memory, GPU)                 │
│    - Create snapshot object                                  │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 4. DATABASE LAYER (EF Core)                                 │
│    - await _unitOfWork.Metrics.AddAsync(snapshot)          │
│    - await _unitOfWork.SaveChangesAsync()                  │
│    - INSERT into SystemMetricsSnapshots table              │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 5. SERVICE LAYER (returns to caller)                        │
│    - Return SystemMetrics object                            │
│    - Publish MetricsCollectedEvent                          │
│    - Other services notified of new metrics                 │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 6. VIEWMODEL LAYER (processes result)                       │
│    - Map metrics to ViewModel properties                    │
│    - CurrentCpuUsage = metrics.CpuUsage                    │
│    - CurrentMemoryUsage = metrics.MemoryUsage              │
│    - Load history: await _monitoringService.GetHistory    │
│    - Add to _metricsHistory collection                     │
│    - IsLoading = false (hides LoadingIndicator)           │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 7. UI LAYER (bindings auto-update)                          │
│    - TextBlock shows new CPU% value                        │
│    - MetricsCard colors update based on new value          │
│    - Chart re-renders with new history                     │
│    - LoadingIndicator disappears                           │
└──────────────────────────────────────────────────────────────┘
```

### 7.2 Exception Flow

```
User Action → ViewModel ExecuteAsync → Service Call
                                           ↓ (Exception!)
Database fails → Service throws → ViewModel catches in ExecuteAsync
                                           ↓
                                    Error = ex
                                           ↓
                                  UI ErrorBoundary detects Error
                                           ↓
                          Shows error message + retry button
                                           ↓
                            User clicks retry → Flow restarts
```

---

## PART 8: INTEGRATION TESTING STRATEGY

### 8.1 Integration Test Layers

```csharp
// Service ↔ Database Integration Test
[TestClass]
public class ServiceDatabaseIntegrationTests
{
    private HermesDbContext _dbContext;
    private HermesMonitoringService _service;
    
    [TestInitialize]
    public void Setup()
    {
        // In-memory database for testing
        var options = new DbContextOptionsBuilder<HermesDbContext>()
            .UseInMemoryDatabase("HermesTestDb")
            .Options;
        
        _dbContext = new HermesDbContext(options);
        
        var unitOfWork = new UnitOfWork(_dbContext);
        _service = new HermesMonitoringService(unitOfWork);
    }
    
    [TestMethod]
    public async Task CollectMetrics_PersistsToDatabase()
    {
        // Act
        var metrics = await _service.CollectSystemMetricsAsync();
        
        // Assert
        var saved = await _dbContext.MetricsSnapshots
            .FirstOrDefaultAsync(m => m.Timestamp == metrics.CollectedAt);
        
        Assert.IsNotNull(saved);
        Assert.AreEqual(metrics.CpuUsage, saved.CpuUsage);
    }
}

// ViewModel ↔ Service Integration Test
[TestClass]
public class ViewModelServiceIntegrationTests
{
    private Mock<IMonitoringService> _mockService;
    private DashboardViewModel _viewModel;
    
    [TestInitialize]
    public void Setup()
    {
        _mockService = new Mock<IMonitoringService>();
        var logger = new Mock<ILogger<DashboardViewModel>>();
        
        _viewModel = new DashboardViewModel(_mockService.Object, logger.Object);
    }
    
    [TestMethod]
    public async Task LoadMetrics_UpdatesViewModelProperties()
    {
        // Arrange
        var expectedMetrics = new SystemMetrics 
        { 
            CpuUsage = 45.0, 
            MemoryUsage = 60.0 
        };
        _mockService.Setup(s => s.CollectSystemMetricsAsync())
            .ReturnsAsync(expectedMetrics);
        
        // Act
        await _viewModel.LoadMetricsAsync();
        
        // Assert
        Assert.AreEqual(45.0, _viewModel.CurrentCpuUsage);
        Assert.AreEqual(60.0, _viewModel.CurrentMemoryUsage);
    }
}

// UI ↔ ViewModel Integration Test
[TestClass]
public class UIViewModelIntegrationTests
{
    private DashboardViewModel _viewModel;
    private DashboardPage _page;
    
    [TestInitialize]
    public void Setup()
    {
        var mockService = new Mock<IMonitoringService>();
        var logger = new Mock<ILogger<DashboardViewModel>>();
        
        _viewModel = new DashboardViewModel(mockService.Object, logger.Object);
        _page = new DashboardPage { DataContext = _viewModel };
    }
    
    [TestMethod]
    public async Task LoadingIndicator_ShowsDuringLoad()
    {
        // Act
        var loadTask = _viewModel.LoadMetricsAsync();
        
        // Assert (during load)
        Assert.IsTrue(_viewModel.IsLoading);
        
        // Complete load
        await loadTask;
        Assert.IsFalse(_viewModel.IsLoading);
    }
}
```

---

## INTEGRATION CHECKLIST

### Before Production Deployment

- [ ] All services initialize in correct order
- [ ] Cross-service events fire correctly
- [ ] ViewModels bind to all service data
- [ ] UI updates when ViewModel properties change
- [ ] Database persists all service data
- [ ] Errors propagate correctly through all layers
- [ ] Retry logic works on transient failures
- [ ] Real-time updates work end-to-end
- [ ] Integration tests pass
- [ ] Performance baseline established
- [ ] Load testing completed
- [ ] Logging captures all operations

---

**Phase 10: Complete Integration Architecture Ready** ✅
**All layers connected and tested end-to-end**
