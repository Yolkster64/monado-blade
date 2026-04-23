# PHASE 10: PUBLIC REPOSITORIES & INDUSTRY BEST PRACTICES INTEGRATION

**Focus**: Learning from proven open-source patterns to optimize Monado Blade architecture

---

## PART 1: PUBLIC REPOSITORY REFERENCE ARCHITECTURES

### 1.1 Microsoft MVVM Toolkit Pattern (Recommended)

**Repository**: `microsoft/WindowsCommunityToolkit` (nuget: Microsoft.Toolkit.Mvvm)

**Why It Matters**:
- Official Microsoft MVVM implementation
- Source generators for zero-boilerplate properties
- Used in production by major enterprises
- Active maintenance and modern C# 12 support

**Pattern to Adopt** (vs current manual StateVM):

```csharp
// ❌ CURRENT: Manual implementation (100+ LOC)
public abstract class StateVM : INotifyPropertyChanged
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;
        
        storage = value;
        OnPropertyChanged(name);
        return true;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// ✅ RECOMMENDED: MVVM Toolkit (5 LOC with source generators)
using Microsoft.Toolkit.Mvvm.ComponentModel;

public abstract class StateVM : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private Exception? error;
    [ObservableProperty] private bool isEmpty;
}
```

**Benefit**: 95% boilerplate reduction, automatic source generation, better performance

**Implementation for Monado Blade**:
```xml
<!-- MonadoBlade.Core.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.Toolkit.Mvvm" Version="8.2.2" />
</ItemGroup>
```

---

### 1.2 Microsoft UI (WinUI) Design System Pattern

**Repository**: `microsoft/microsoft-ui-xaml`

**Design Token Structure** (from WinUI):

```csharp
// WinUI pattern we should adopt:
namespace WinUI.Tokens
{
    // Semantic tokens (not just colors)
    public static class ColorTokens
    {
        // Functional colors (not just named colors)
        public static class Surface
        {
            public const string Primary = "..." ;      // Main surface
            public const string Secondary = "...";     // Alt surface
            public const string Tertiary = "...";      // Accent
        }
        
        // State variants built-in
        public static class ButtonPrimary
        {
            public const string Default = "#00D9FF";
            public const string Hovered = "#00A8CC";
            public const string Pressed = "#007FA3";
            public const string Disabled = "#404040";
        }
    }
}
```

**Apply to Monado Blade**:

```csharp
// src/MonadoBlade.Core/DesignSystem/SemanticTokens.cs
namespace MonadoBlade.DesignSystem
{
    public static class SemanticTokens
    {
        // Component-specific color states
        public static class Button
        {
            public static class Primary
            {
                public const string Default = "#00D9FF";
                public const string Hovered = "#00A8CC";
                public const string Pressed = "#007FA3";
                public const string Disabled = "#404040";
            }
            
            public static class Success
            {
                public const string Default = "#00FF41";
                public const string Hovered = "#00CC33";
                public const string Pressed = "#009922";
                public const string Disabled = "#404040";
            }
        }
        
        public static class Input
        {
            public const string Background = "#1A1A1A";
            public const string Border = "#333333";
            public const string BorderFocused = "#00D9FF";
            public const string BorderError = "#FF0055";
            public const string Text = "#FFFFFF";
        }
        
        public static class Card
        {
            public const string Background = "#1F1F1F";
            public const string Border = "#333333";
            public const string Hovered = "#2A2A2A";
        }
    }
}
```

---

### 1.3 NuGet Package Consolidation Pattern

**Reference**: Major open-source projects (react/vue libraries, .NET frameworks)

**Problem**: Scattered NuGet dependencies across projects

**Pattern from Popular Repos**:

```csharp
// ❌ ANTI-PATTERN: Duplicate dependencies
// MonadoBlade.Core.csproj
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
</ItemGroup>

// MonadoBlade.Console.csproj (DUPLICATES CORE)
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
</ItemGroup>

// ✅ PATTERN: Centralized in base library
// MonadoBlade.Core.csproj (SHARED)
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Toolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Serilog" Version="4.0.0" />
</ItemGroup>

// MonadoBlade.Console.csproj (ONLY references Core, no direct DI dependency)
<ItemGroup>
    <ProjectReference Include="..\MonadoBlade.Core\MonadoBlade.Core.csproj" />
    <!-- Inherits all dependencies from Core -->
</ItemGroup>
```

---

## PART 2: COMPONENT LIBRARY PATTERNS FROM PUBLIC REPOS

### 2.1 Fluent UI React Pattern (Applicable to XAML)

**Repository**: `microsoft/fluentui` (18,000+ stars)

**Component Hierarchy Structure**:

```
components/
├── atoms/                          # Smallest building blocks
│   ├── Badge/
│   ├── Icon/
│   ├── Spinner/
│   └── StatusIndicator/
├── molecules/                      # Combine atoms
│   ├── ButtonGroup/
│   ├── TextField/
│   ├── FormField/
│   └── Card/
├── organisms/                      # Complex components
│   ├── Form/
│   ├── DataTable/
│   ├── Modal/
│   └── Navigation/
├── layouts/                        # Page layouts
│   ├── SidebarLayout/
│   ├── HeaderLayout/
│   └── TwoColumnLayout/
└── theme/                          # Design tokens
    ├── tokens.json
    ├── light-theme.json
    └── dark-theme.json
```

**Apply to Monado Blade** (Current Structure):

```
components/
├── Atoms/
│   ├── StatusBadge.xaml           ✅ (Need to build)
│   ├── LoadingSpinner.xaml        ✅ (Need to build)
│   ├── Icon.xaml                  ❌ (Missing)
│   └── Tooltip.xaml               ❌ (Missing)
├── Molecules/
│   ├── FormField.xaml             ❌ (Missing)
│   ├── MetricsCard.xaml           ✅ (Need to build)
│   ├── ProgressBar.xaml           ✅ (Need to build)
│   └── TextField.xaml             ❌ (Missing)
├── Organisms/
│   ├── DataGrid.xaml              ❌ (Missing - CRITICAL)
│   ├── Modal.xaml                 ❌ (Missing)
│   ├── NotificationCenter.xaml    ✅ (Need to build)
│   └── ErrorBoundary.xaml         ✅ (Need to build)
├── Layouts/
│   ├── TopBarLayout.xaml          ❌ (Missing)
│   ├── SidebarLayout.xaml         ❌ (Missing)
│   ├── DashboardLayout.xaml       ❌ (Missing)
│   └── FleetLayout.xaml           ❌ (Missing)
└── DesignSystem/
    ├── DesignTokens.cs            ✅ (Need to create)
    ├── SemanticTokens.cs          ✅ (Need to create)
    ├── Tokens.xaml                ✅ (Need to create)
    └── Themes/
        ├── Dark.xaml              ✅ (Need to create)
        └── Light.xaml             ✅ (Need to create)
```

---

### 2.2 Material-UI (MUI) Spacing & Sizing System

**Repository**: `mui/material-ui` (90,000+ stars)

**Pattern**: 8px grid with semantic names

```csharp
// ❌ MAGIC NUMBERS (What we have now)
Padding = "16,8,16,8"
Margin = "24"
Width = "200"

// ✅ SEMANTIC SIZING (MUI pattern)
public static class Spacing
{
    // Base unit multiples
    public const int Unit = 8;
    
    // Named sizes
    public static class Layout
    {
        public const int SidebarWidth = Unit * 30;     // 240px
        public const int TopBarHeight = Unit * 7;      // 56px
        public const int CardMaxWidth = Unit * 60;     // 480px
    }
    
    // Component-specific
    public static class Button
    {
        public const int PaddingX = Unit * 2;          // 16px
        public const int PaddingY = Unit;              // 8px
        public const int MinHeight = Unit * 5;         // 40px
    }
    
    public static class Form
    {
        public const int FieldGap = Unit * 2;          // 16px
        public const int LabelMarginBottom = Unit;     // 8px
        public const int HelperTextMarginTop = Unit;   // 8px
    }
}
```

---

### 2.3 Storybook Documentation Pattern

**Repository**: `storybookjs/storybook` (85,000+ stars)

**Pattern**: Component documentation with usage examples

**Implement for Monado Blade**:

```csharp
// src/MonadoBlade.UI/Components/StatusBadge.xaml.cs
namespace MonadoBlade.UI.Components;

/// <summary>
/// StatusBadge - Visual indicator for status states
/// 
/// STORYBOOK STORIES:
/// 
/// Story 1: Status=Active (GREEN)
///   Usage: <StatusBadge Status="Active" />
///   
/// Story 2: Status=Inactive (GRAY)
///   Usage: <StatusBadge Status="Inactive" />
///   
/// Story 3: Status=Error (RED)
///   Usage: <StatusBadge Status="Error" />
///   
/// Story 4: Large variant
///   Usage: <StatusBadge Status="Active" Size="Large" />
/// 
/// Design Tokens Used:
///   - Colors.Success / Colors.Danger / Colors.Neutral
///   - Spacing.SM for padding
///   - Typography.Caption for font size
///   - BorderRadius.Small for corner radius
/// </summary>
public sealed partial class StatusBadge : UserControl
{
    // Component implementation
}
```

---

## PART 3: DEPENDENCY INJECTION CONSOLIDATION

### 3.1 Pattern from Microsoft.Extensions.DependencyInjection

**Reference**: Official Microsoft DI container used by ASP.NET Core

**Current State**: Scattered DI registration

**Recommended Pattern**:

```csharp
// ✅ CONSOLIDATED: src/MonadoBlade.Core/DependencyInjection.cs
namespace MonadoBlade.Core;

public static class MonadoBladeDependencies
{
    /// <summary>
    /// Register all Monado Blade services in a single place
    /// Pattern from: Microsoft.Extensions.DependencyInjection
    /// </summary>
    public static IServiceCollection AddMonadoBladeServices(
        this IServiceCollection services,
        MonadoBlazeOptions? options = null)
    {
        options ??= new MonadoBlazeOptions();
        
        // Logging infrastructure
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.FromLoggingLevel(options.LogLevel)
                .WriteTo.Console()
                .WriteTo.File("logs/hermes-.txt", rollingInterval: RollingInterval.Day)
                .Enrich.FromLogContext()
                .CreateLogger());
        });
        
        // Consolidated services (singletons)
        services.AddSingleton<IFleetOrchestrator, HermesFleetOrchestrator>();
        services.AddSingleton<IMonitoringService, HermesMonitoringService>();
        services.AddSingleton<ISecurityService, HermesSecurityService>();
        
        // Database (if enabled)
        if (options.EnableDatabase)
        {
            services.AddDbContext<HermesDbContext>(opt =>
                opt.UseSqlServer(options.ConnectionString));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
        
        return services;
    }
}

public class MonadoBlazeOptions
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public bool EnableDatabase { get; set; } = true;
    public string? ConnectionString { get; set; }
}

// Usage in Console:
var services = new ServiceCollection()
    .AddMonadoBladeServices(new MonadoBlazeOptions 
    { 
        ConnectionString = "Server=localhost;Database=HermesDB"
    })
    .BuildServiceProvider();
```

---

## PART 4: ENTITY FRAMEWORK CORE CONSOLIDATION

### 4.1 Pattern from EF Core Best Practices

**Reference**: `dotnet/EntityFrameworkCore` official patterns

**Database Architecture**:

```csharp
// ✅ Unified DbContext (Single source of truth)
namespace MonadoBlade.Core.Persistence;

public class HermesDbContext : DbContext
{
    public HermesDbContext(DbContextOptions<HermesDbContext> options) 
        : base(options) { }
    
    // Monitoring data
    public DbSet<SystemMetricsSnapshot> MetricsSnapshots { get; set; }
    public DbSet<LearningPattern> LearningPatterns { get; set; }
    
    // Security data
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Fleet data
    public DbSet<AgentStatus> AgentStatuses { get; set; }
    public DbSet<BootSequenceLog> BootSequenceLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure aggregates
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HermesDbContext).Assembly);
        
        // Apply global query filters for soft deletes
        modelBuilder.Entity<SecurityEvent>().HasQueryFilter(e => !e.IsDeleted);
    }
}

// Configuration files (Aggregate roots)
// From EF Core pattern: Entity Configurations

// src/MonadoBlade.Core/Persistence/Configurations/SystemMetricsConfiguration.cs
public class SystemMetricsConfiguration : IEntityTypeConfiguration<SystemMetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<SystemMetricsSnapshot> builder)
    {
        builder.ToTable("SystemMetricsSnapshots");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.Property(e => e.CpuUsage)
            .HasColumnType("DECIMAL(5,2)");
        
        builder.HasIndex(e => e.Timestamp)
            .IsDescending();
    }
}
```

**Repository Pattern** (from EF best practices):

```csharp
// Generic repository
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveAsync();
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly HermesDbContext _context;
    
    public Repository(HermesDbContext context) => _context = context;
    
    public async Task<T?> GetByIdAsync(int id)
        => await _context.Set<T>().FindAsync(id);
    
    public async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
        => await _context.Set<T>().Where(predicate).ToListAsync();
    
    public async Task SaveAsync()
        => await _context.SaveChangesAsync();
}

// Unit of Work pattern
public interface IUnitOfWork
{
    IRepository<SystemMetricsSnapshot> Metrics { get; }
    IRepository<SecurityEvent> SecurityEvents { get; }
    IRepository<AgentStatus> Agents { get; }
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly HermesDbContext _context;
    
    private IRepository<SystemMetricsSnapshot>? _metricsRepository;
    private IRepository<SecurityEvent>? _securityEventsRepository;
    private IRepository<AgentStatus>? _agentsRepository;
    
    public IRepository<SystemMetricsSnapshot> Metrics 
        => _metricsRepository ??= new Repository<SystemMetricsSnapshot>(_context);
    
    public IRepository<SecurityEvent> SecurityEvents 
        => _securityEventsRepository ??= new Repository<SecurityEvent>(_context);
    
    public IRepository<AgentStatus> Agents 
        => _agentsRepository ??= new Repository<AgentStatus>(_context);
    
    public async Task<int> SaveChangesAsync() 
        => await _context.SaveChangesAsync();
}
```

**Migration Pattern**:

```bash
# Create initial migration
dotnet ef migrations add InitialCreate \
  --project src/MonadoBlade.Core \
  --startup-project src/MonadoBlade.Console

# Apply migration
dotnet ef database update \
  --project src/MonadoBlade.Core \
  --startup-project src/MonadoBlade.Console
```

---

## PART 5: PUBLIC REPOSITORY RECOMMENDATIONS SUMMARY

### 5.1 Libraries to Adopt from Public Repos

| Library | Purpose | Link | Status |
|---------|---------|------|--------|
| **Microsoft.Toolkit.Mvvm** | MVVM implementation | github.com/CommunityToolkit/MVVM-Toolkit | 🔴 ADD |
| **Microsoft.EntityFrameworkCore** | ORM/Database | github.com/dotnet/EntityFrameworkCore | 🔴 ADD |
| **Serilog** | Logging | github.com/serilog/serilog | ✅ HAVE |
| **Microsoft.Extensions.DependencyInjection** | DI Container | github.com/dotnet/runtime | ✅ HAVE |
| **xUnit** | Testing | github.com/xunit/xunit | 🟡 (Not implemented) |
| **Moq** | Mocking | github.com/moq/moq | 🟡 (For testing) |

### 5.2 Architectural Patterns to Adopt

```csharp
// From public repos - patterns proven at scale

// ✅ 1. Semantic Tokens (from WinUI)
DesignTokens.SemanticTokens.Button.Primary.Hovered

// ✅ 2. Centralized DI (from ASP.NET Core)
services.AddMonadoBladeServices()

// ✅ 3. Repository + Unit of Work (from Entity Framework)
unitOfWork.Metrics.QueryAsync(...)

// ✅ 4. MVVM with Source Generators (from MVVM Toolkit)
[ObservableProperty] private bool isLoading;

// ✅ 5. Component Storybook (from Fluent UI)
/// <summary>Component story with usage examples</summary>
```

---

## PART 6: QUICK IMPLEMENTATION ROADMAP

### Week 1: Consolidation
- [ ] Upgrade StateVM to MVVM Toolkit (Microsoft pattern)
- [ ] Centralize DI setup (ASP.NET Core pattern)
- [ ] Create DesignTokens.cs (WinUI pattern)
- [ ] Delete 8 old services

### Week 2: Components
- [ ] Implement atom components (Badge, Spinner, Icon)
- [ ] Implement molecule components (FormField, Card, TextField)
- [ ] Create component Storybook documentation

### Week 3: Database
- [ ] Add EF Core DbContext (Entity Framework pattern)
- [ ] Implement Repository + Unit of Work
- [ ] Create migrations and seed data

---

**Phase 10 Optimization Complete** ✅
**Leveraging industry best practices from 200,000+ community stars**
