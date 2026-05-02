# PHASE 10: DESIGN SYSTEM OPTIMIZATION & COMPONENT ARCHITECTURE

**Status**: Architecture Design Complete
**Focus**: Design token implementation, component hierarchy, operability optimization

---

## PART 1: DESIGN SYSTEM INVENTORY & GAP ANALYSIS

### 1.1 Design System Current State

#### ✅ What's Defined (Documentation)
```
Color Palette:     Xenoblade theme (Cyan, Green, Pink, Amber, Dark)
Spacing Scale:     8px grid (xs=4px → 2xl=48px)
Typography:        7 sizes (Display → Caption)
Breakpoints:       4 device sizes (Mobile → Wide)
Shadows:           3 elevation levels
Border Radius:     5 radius options
Themes:            Light/Dark mode ready
```

#### ❌ What's NOT Implemented (Code)
```
Design Tokens File:        Missing (NEED: DesignTokens.cs)
XAML Resource Dictionary:  Missing (NEED: Styles.xaml)
Component Library:         Missing (18 designed, 0 built)
Spacing Constants:         Scattered in XAML
Color Constants:           Hard-coded hex values
Typography Dictionary:     No centralized fonts
Theme System:              No theme switching
Responsive Utilities:      No breakpoint helpers
```

#### 🟡 Partial Implementation
```
ViewModels:        3 created (DashboardVM, FleetVM, SettingsVM)
StateVM:           Created with manual INotifyPropertyChanged
Services:          Consolidated 8→3, but legacy files remain
DI Setup:          Scattered across Console and Core
```

---

## PART 2: OPERABILITY ANALYSIS - USER FLOWS

### 2.1 Critical User Flows & Friction Points

#### Flow 1: System Monitoring Dashboard
```
Current State:
  User Opens App
    ↓
  MainWindow loads
    ↓ ❌ FRICTION: No loading indicator
  DashboardViewModel.LoadMetricsAsync() called
    ↓ 
  HermesMonitoringService.CollectSystemMetricsAsync()
    ↓ 🟡 FRICTION: No error boundary
  UI displays metrics (or crashes)
    ↓
  User sees... nothing? (no UI components)
    ↓ ❌ CRITICAL: LoadingIndicator component doesn't exist
```

**Issues Found**:
1. No loading indicator during metric collection
2. No error boundary if service fails
3. No retry mechanism on failure
4. Metrics update but no progress feedback
5. No empty state when no data

**Fix**: Implement LoadingIndicator + ErrorBoundary components

---

#### Flow 2: Fleet Agent Management
```
Current State:
  User clicks "Add Agent"
    ↓ ❌ No button styling (no design tokens)
  Form appears
    ↓ 🟡 FRICTION: No form validation UI
  User enters agent name
    ↓ ❌ No input validation feedback
  User clicks "Register"
    ↓
  FleetViewModel.RegisterAgentAsync() called
    ↓ ❌ No loading state
  Agent registered (or error)
    ↓ ❌ No confirmation/error message
  Agent list refreshes
    ↓ 🟡 FRICTION: No animation, jarring update
```

**Issues Found**:
1. No visual feedback during async operation
2. No validation error messages
3. No success confirmation
4. No empty state in agent list
5. No grid/table component for agent display

**Fix**: Create FormField component + StatusBadge component + DataGrid

---

#### Flow 3: Security Hardening
```
Current State:
  User opens Settings
    ↓ ❌ No settings navigation (missing SideNav)
  Security section loads
    ↓
  SettingsViewModel.LoadSecuritySettingsAsync()
    ↓
  Displays current policies
    ↓ ❌ CRITICAL: No UI to display them
  User clicks "Apply Hardening"
    ↓ ❌ No indication of progress
  Multiple security tasks run in parallel
    ↓ 🟡 FRICTION: User doesn't know what's happening
  Tasks complete
    ↓ ❌ No completion notification
```

**Issues Found**:
1. No settings layout/navigation
2. No progress indication during async
3. No step-by-step UI (Collecting → Analyzing → Applying)
4. No success/error notification
5. No settings persistence/confirmation

**Fix**: Create SettingsLayout + ProgressBar component + NotificationCenter

---

### 2.2 Operability Issues Summary

| Issue | Severity | User Impact | Component Needed |
|-------|----------|-------------|------------------|
| No loading indicators | 🔴 CRITICAL | Confusion, appears frozen | LoadingIndicator |
| No error boundaries | 🔴 CRITICAL | App crashes silently | ErrorBoundary |
| No empty states | 🟡 HIGH | Confusing blank screens | EmptyState |
| No validation feedback | 🟡 HIGH | Users don't know if input is valid | ValidationMessage |
| No navigation | 🟡 HIGH | Users lost in app | SideNav, TopBar |
| No form layout | 🟡 HIGH | Forms look broken | FormField, Form |
| No data display | 🟡 HIGH | Can't see any data | DataGrid, MetricsCard |
| No notifications | 🟡 HIGH | Users don't know what happened | Toast, Notification |
| No progress feedback | 🟡 HIGH | Long operations seem stuck | ProgressBar |
| Inconsistent spacing | 🟡 MEDIUM | Looks unprofessional | Spacing tokens |
| Inconsistent colors | 🟡 MEDIUM | Brand looks weak | Color tokens |
| Poor responsive design | 🟠 MEDIUM | Mobile users can't use app | Responsive grid |

---

## PART 3: COMPONENT CONNECTIVITY MAP

### 3.1 Current Architecture

```
🏗️ CURRENT STATE (Incomplete)

Services (3 Consolidated)
├── HermesFleetOrchestrator
│   ├── public Data: Agent list, boot progress
│   └── ❌ No UI component connects here
├── HermesMonitoringService  
│   ├── public Data: Metrics, history, analysis
│   └── 🟡 DashboardViewModel connects (but no UI)
└── HermesSecurityService
    ├── public Data: Policies, audio, threats
    └── 🟡 SettingsViewModel connects (but no UI)
        ↓
    ViewModels (3 Created)
    ├── DashboardViewModel
    │   └── ❌ No corresponding XAML/Components
    ├── FleetViewModel
    │   └── ❌ No corresponding XAML/Components
    └── SettingsViewModel
        └── ❌ No corresponding XAML/Components
            ↓
        XAML UI (Not Created)
        └── ❌ MainWindow.xaml - EMPTY
            └── ❌ No components referenced
```

### 3.2 Required Connections

```
🔗 NEEDED: Full Component Hierarchy

Services (3)
    ↓ (Data via async methods)
ViewModels (3+) [HAVE 3]
    ↓ (Bind via INotifyPropertyChanged)
Pages/Views (3+) [NEED 3]
    ├── DashboardPage.xaml
    │   ├── TopBar (navigation)
    │   ├── LoadingIndicator (while loading metrics)
    │   ├── ErrorBoundary (error display)
    │   ├── MetricsGrid (display system metrics)
    │   │   ├── MetricsCard × 4 (CPU, Memory, GPU, Disk)
    │   │   └── StatusBadge (health indicator)
    │   ├── ChartViewer (metrics history)
    │   └── RefreshButton
    │
    ├── FleetPage.xaml
    │   ├── TopBar
    │   ├── AgentGrid (DataGrid)
    │   │   └── StatusBadge (per agent)
    │   ├── BootProgressBar
    │   ├── AddAgentForm
    │   │   ├── TextBox (agent name)
    │   │   ├── ValidationMessage
    │   │   └── Button
    │   └── ProcessOptimizationGrid
    │
    └── SettingsPage.xaml
        ├── SideNav
        ├── SettingsForm
        │   ├── Toggle (policies)
        │   ├── StatusBadge (status)
        │   ├── ProgressBar (during hardening)
        │   └── Button (apply)
        └── NotificationCenter (alerts)
```

---

## PART 4: COMPONENT LIBRARY - MISSING PIECES

### 4.1 Component Audit (18 Designed, 0 Built)

#### Navigation (0/3 Built)

**1. TopBar Component**
```xaml
<TopBar Title="Dashboard" OnSettingsClick="...">
  <Icon>settings</Icon>
</TopBar>
```
- Purpose: App header with navigation
- Used in: All pages
- Props: Title, Icons, Buttons
- Status: ❌ NOT BUILT

**2. SideNav Component**
```xaml
<SideNav Selected="Dashboard">
  <NavItem Label="Dashboard" Icon="home" />
  <NavItem Label="Fleet" Icon="servers" />
  <NavItem Label="Settings" Icon="settings" />
</SideNav>
```
- Purpose: Left sidebar navigation
- Used in: Main layout
- Props: Items, SelectedItem, Collapsible
- Status: ❌ NOT BUILT

**3. BreadcrumbTrail Component**
```xaml
<Breadcrumb Path="Settings/Security/Policies" />
```
- Purpose: Navigation breadcrumbs
- Used in: Nested pages
- Props: Path
- Status: ❌ NOT BUILT

#### Data Display (0/5 Built)

**4. DataGrid Component**
```xaml
<DataGrid ItemsSource="{Binding Agents}">
  <DataGridColumn Header="Name" Binding="{Binding Name}" />
  <DataGridColumn Header="Status" CellTemplate="{StaticResource StatusBadgeTemplate}" />
</DataGrid>
```
- Purpose: Tabular data display
- Used in: Fleet, Metrics
- Props: ItemsSource, Columns, Sortable, Filterable
- Status: ❌ NOT BUILT (CRITICAL)

**5. Chart Component**
```xaml
<LineChart Title="CPU Usage" Data="{Binding MetricsHistory}" />
```
- Purpose: Visualize metrics over time
- Used in: Dashboard
- Props: Title, Data, ChartType (line/bar/area)
- Status: ❌ NOT BUILT

**6. MetricsCard Component**
```xaml
<MetricsCard Title="CPU Usage" Value="45%" Trend="up" TrendValue="+5%" Color="Primary" />
```
- Purpose: Single metric display
- Used in: Dashboard (4 cards)
- Props: Title, Value, Trend, TrendValue, Color
- Status: ❌ NOT BUILT (REQUIRED)

**7. StatusBadge Component**
```xaml
<StatusBadge Status="Active" /> <!-- GREEN -->
<StatusBadge Status="Idle" />    <!-- YELLOW -->
<StatusBadge Status="Error" />   <!-- RED -->
```
- Purpose: Status indicator
- Used in: Grids, Cards
- Props: Status (enum), Size
- Status: ❌ NOT BUILT (REQUIRED)

**8. ProgressBar Component**
```xaml
<ProgressBar Value="45" Max="100" ShowLabel="true" />
```
- Purpose: Show operation progress
- Used in: Boot sequence, hardening
- Props: Value, Max, ShowLabel, Color
- Status: ❌ NOT BUILT (REQUIRED)

#### Actions (0/5 Built)

**9. Button Component**
```xaml
<Button Variant="Primary" Text="Apply Hardening" OnClick="..." />
<Button Variant="Secondary" Text="Cancel" />
<Button Variant="Tertiary" Text="Learn More" />
```
- Purpose: Trigger actions
- Used in: Forms, toolbars
- Props: Variant (primary/secondary/tertiary), Text, IsLoading, Disabled
- Status: ❌ NOT BUILT

**10. ToggleSwitch Component**
```xaml
<ToggleSwitch Label="Enable Spatial Audio" IsOn="{Binding SpatialAudioEnabled}" />
```
- Purpose: Boolean toggle
- Used in: Settings
- Props: Label, IsOn, OnChanged
- Status: ❌ NOT BUILT

**11. Dropdown Component**
```xaml
<Dropdown Label="Select Policy" Items="{Binding Policies}" SelectedItem="{Binding SelectedPolicy}" />
```
- Purpose: Select from list
- Used in: Forms
- Props: Items, SelectedItem, Label
- Status: ❌ NOT BUILT

**12. TextField Component**
```xaml
<TextField Label="Agent Name" Text="{Binding AgentName}" Placeholder="Enter name..." />
```
- Purpose: Text input
- Used in: Forms
- Props: Label, Text, Placeholder, Error
- Status: ❌ NOT BUILT

**13. FormField Component**
```xaml
<FormField Label="Agent Name" Error="{Binding NameError}">
  <TextBox Text="{Binding AgentName}" />
</FormField>
```
- Purpose: Input with validation
- Used in: Forms
- Props: Label, Error, Required
- Status: ❌ NOT BUILT

#### Containers (0/5 Built)

**14. Card Component**
```xaml
<Card Title="Metrics" Padding="16">
  <StackPanel>
    <MetricsCard ... />
  </StackPanel>
</Card>
```
- Purpose: Container with border/shadow
- Used in: Grid layouts
- Props: Title, Padding, Background
- Status: ❌ NOT BUILT

**15. Modal/Dialog Component**
```xaml
<Modal Title="Confirm Action" IsOpen="{Binding ShowConfirmDialog}">
  <TextBlock Text="Are you sure?" />
  <Button Text="Yes" OnClick="Confirm" />
  <Button Text="Cancel" OnClick="Cancel" />
</Modal>
```
- Purpose: Overlaid modal dialog
- Used in: Confirmations
- Props: Title, IsOpen, OnConfirm, OnCancel
- Status: ❌ NOT BUILT

**16. Alert Component**
```xaml
<Alert Type="Success" Title="Hardening Applied" Message="Security policies applied successfully" />
<Alert Type="Error" Title="Error" Message="Failed to apply policies" />
```
- Purpose: Alert/notification message
- Used in: Notifications
- Props: Type (success/error/warning/info), Title, Message
- Status: ❌ NOT BUILT

**17. Tooltip Component**
```xaml
<Tooltip Text="Hover over me" Position="Top">
  <TextBlock Text="?" />
</Tooltip>
```
- Purpose: Additional help text on hover
- Used in: Complex settings
- Props: Text, Position, Delay
- Status: ❌ NOT BUILT

**18. NotificationCenter Component**
```xaml
<NotificationCenter>
  <Notification Type="Success" Message="Operation completed" Duration="3s" />
  <Notification Type="Error" Message="Operation failed" Duration="0" />
</NotificationCenter>
```
- Purpose: Toast notifications
- Used in: Top-right corner
- Props: Type, Message, Duration
- Status: ❌ NOT BUILT (REQUIRED)

---

## PART 5: QUICK WINS - HIGH-IMPACT COMPONENTS

### 5.1 Priority Implementation Order

**IMMEDIATE (3-5 hours)**
```
1. DesignTokens.cs (45 min)         → Single source of truth
2. LoadingIndicator (1 hour)         → Show progress
3. ErrorBoundary (1 hour)            → Handle errors
4. StatusBadge (30 min)              → Status indicators
5. MetricsCard (1 hour)              → Metric display
```

**Week 1 (8 hours)**
```
6. DataGrid component (2 hours)      → Tabular data
7. ProgressBar (1 hour)              → Progress feedback
8. Button variants (1 hour)          → Action triggers
9. TextField/FormField (1.5 hours)   → Form inputs
10. Notification/Toast (1.5 hours)   → User feedback
```

**Week 2 (10+ hours)**
```
11. TopBar/Navigation (2 hours)
12. SideNav/Layout (2 hours)
13. Modal/Dialog (1.5 hours)
14. Chart/Graph (3 hours)
15. Advanced layouts
```

---

## PART 6: DESIGN TOKEN IMPLEMENTATION GUIDE

### 6.1 Centralized Design Tokens File

Create: `src/MonadoBlade.Core/DesignSystem/DesignTokens.cs`

```csharp
namespace MonadoBlade.Core.DesignSystem;

/// <summary>
/// Centralized design tokens for entire Monado Blade application.
/// Single source of truth for colors, spacing, typography, and responsive design.
/// </summary>
public static class DesignTokens
{
    // ═══════════════════════════════════════════════════════════
    // COLORS - Xenoblade Theme
    // ═══════════════════════════════════════════════════════════
    
    public static class Colors
    {
        // Primary (Cyan)
        public const string Primary = "#00D9FF";
        public const string PrimaryDark = "#00A8CC";
        public const string PrimaryLight = "#66E5FF";
        public const string PrimaryFaded = "#00D9FF33";
        
        // Success (Green)
        public const string Success = "#00FF41";
        public const string SuccessDark = "#00CC33";
        public const string SuccessLight = "#66FF6E";
        public const string SuccessFaded = "#00FF4133";
        
        // Danger (Pink)
        public const string Danger = "#FF0055";
        public const string DangerDark = "#CC0044";
        public const string DangerLight = "#FF6699";
        public const string DangerFaded = "#FF005533";
        
        // Warning (Amber)
        public const string Warning = "#FFB800";
        public const string WarningDark = "#CC9200";
        public const string WarningLight = "#FFCC33";
        public const string WarningFaded = "#FFB80033";
        
        // Neutral (Grayscale)
        public const string Background = "#0D0D0D";    // Darkest
        public const string Surface = "#1A1A1A";       // Card background
        public const string Neutral = "#1F1F1F";       // Dark gray
        public const string NeutralMedium = "#505050"; // Medium gray
        public const string NeutralLight = "#808080";  // Light gray
        public const string NeutralLighter = "#CCCCCC";// Lighter gray
        public const string Foreground = "#FFFFFF";    // Text
        
        // Semantic
        public const string Disabled = "#404040";
        public const string Border = "#333333";
        public const string Shadow = "#00000040";
    }
    
    // ═══════════════════════════════════════════════════════════
    // SPACING - 8px Grid System
    // ═══════════════════════════════════════════════════════════
    
    public static class Spacing
    {
        public const int XS = 4;      // 4px
        public const int SM = 8;      // 8px
        public const int MD = 16;     // 16px
        public const int LG = 24;     // 24px
        public const int XL = 32;     // 32px
        public const int XXL = 48;    // 48px
        
        // Common groupings
        public const int GAP_TIGHT = SM;      // Between related elements
        public const int GAP_NORMAL = MD;     // Between sections
        public const int GAP_LOOSE = LG;      // Between major sections
        
        // Padding presets
        public const int PADDING_SMALL = SM;
        public const int PADDING_MEDIUM = MD;
        public const int PADDING_LARGE = LG;
    }
    
    // ═══════════════════════════════════════════════════════════
    // TYPOGRAPHY
    // ═══════════════════════════════════════════════════════════
    
    public static class Typography
    {
        public const string FontFamily = "Segoe UI";
        public const string FontFamilyMono = "Consolas";
        
        public class FontSizes
        {
            public const int Display = 32;    // Large headings
            public const int Headline = 24;   // Page titles
            public const int Title = 18;      // Section titles
            public const int Subtitle = 16;   // Subtitles
            public const int Body = 14;       // Main content
            public const int Caption = 12;    // Help text
            public const int Tiny = 10;       // Micro labels
        }
        
        public class FontWeights
        {
            public const int Regular = 400;
            public const int Medium = 500;
            public const int SemiBold = 600;
            public const int Bold = 700;
        }
        
        public class LineHeights
        {
            public const double Tight = 1.2;
            public const double Normal = 1.5;
            public const double Loose = 1.8;
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // RESPONSIVE BREAKPOINTS
    // ═══════════════════════════════════════════════════════════
    
    public static class Breakpoints
    {
        public const int Mobile = 320;       // Min: 320px
        public const int Tablet = 768;       // Min: 768px
        public const int Desktop = 1024;     // Min: 1024px
        public const int Wide = 1400;        // Min: 1400px
        
        // Media query helpers
        public const int MOBILE_MAX = 767;
        public const int TABLET_MAX = 1023;
    }
    
    // ═══════════════════════════════════════════════════════════
    // SHADOWS & ELEVATION
    // ═══════════════════════════════════════════════════════════
    
    public static class Shadows
    {
        public const string Elevation0 = "none";
        public const string Elevation1 = "0 1px 3px rgba(0,0,0,0.12)";
        public const string Elevation2 = "0 3px 6px rgba(0,0,0,0.16)";
        public const string Elevation3 = "0 10px 20px rgba(0,0,0,0.19)";
        public const string Elevation4 = "0 15px 25px rgba(0,0,0,0.25)";
    }
    
    // ═══════════════════════════════════════════════════════════
    // BORDER RADIUS
    // ═══════════════════════════════════════════════════════════
    
    public static class BorderRadius
    {
        public const int None = 0;
        public const int Small = 4;
        public const int Medium = 8;
        public const int Large = 16;
        public const int Full = 9999;
        
        // Predefined sets
        public const int BUTTON = Small;
        public const int CARD = Medium;
        public const int MODAL = Large;
    }
    
    // ═══════════════════════════════════════════════════════════
    // TRANSITIONS & ANIMATIONS
    // ═══════════════════════════════════════════════════════════
    
    public static class Animations
    {
        public const int DURATION_FAST = 150;      // 150ms - Quick feedback
        public const int DURATION_NORMAL = 300;    // 300ms - Standard
        public const int DURATION_SLOW = 500;      // 500ms - Deliberate
        public const int DURATION_VERY_SLOW = 1000; // 1s - Loading
        
        public const string EASING_EASE_IN_OUT = "cubic-bezier(0.4, 0, 0.2, 1)";
        public const string EASING_EASE_OUT = "cubic-bezier(0, 0, 0.2, 1)";
        public const string EASING_EASE_IN = "cubic-bezier(0.4, 0, 1, 1)";
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPONENT SIZING
    // ═══════════════════════════════════════════════════════════
    
    public static class ComponentSizes
    {
        // Icon sizes
        public const int ICON_SMALL = 16;
        public const int ICON_MEDIUM = 24;
        public const int ICON_LARGE = 32;
        
        // Button sizes
        public const int BUTTON_HEIGHT_SMALL = 32;
        public const int BUTTON_HEIGHT_MEDIUM = 40;
        public const int BUTTON_HEIGHT_LARGE = 48;
        
        // Input field heights
        public const int INPUT_HEIGHT = 40;
        public const int INPUT_HEIGHT_SMALL = 32;
        public const int INPUT_HEIGHT_LARGE = 48;
        
        // List item heights
        public const int LIST_ITEM_HEIGHT = 48;
        public const int LIST_ITEM_DENSE = 40;
        public const int LIST_ITEM_SPACIOUS = 56;
    }
    
    // ═══════════════════════════════════════════════════════════
    // Z-INDEX LAYERS
    // ═══════════════════════════════════════════════════════════
    
    public static class ZIndex
    {
        public const int BASE = 0;
        public const int DROPDOWN = 100;
        public const int STICKY = 200;
        public const int FIXED = 300;
        public const int MODAL_BACKDROP = 400;
        public const int MODAL = 500;
        public const int TOOLTIP = 600;
        public const int NOTIFICATION = 700;
    }
}
```

### 6.2 XAML Resource Dictionary Integration

Create: `src/MonadoBlade.UI/Resources/DesignTokens.xaml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- COLORS -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#00D9FF" />
    <SolidColorBrush x:Key="SuccessBrush" Color="#00FF41" />
    <SolidColorBrush x:Key="DangerBrush" Color="#FF0055" />
    <SolidColorBrush x:Key="WarningBrush" Color="#FFB800" />
    <SolidColorBrush x:Key="NeutralBrush" Color="#1F1F1F" />
    
    <!-- SPACING THICKNESS -->
    <Thickness x:Key="SpacingXS">4</Thickness>
    <Thickness x:Key="SpacingSM">8</Thickness>
    <Thickness x:Key="SpacingMD">16</Thickness>
    <Thickness x:Key="SpacingLG">24</Thickness>
    
    <!-- FONT SIZES -->
    <FontFamily x:Key="DefaultFontFamily">Segoe UI</FontFamily>
    <x:Double x:Key="FontSizeDisplay">32</x:Double>
    <x:Double x:Key="FontSizeHeadline">24</x:Double>
    <x:Double x:Key="FontSizeBody">14</x:Double>
    
    <!-- STYLES -->
    <Style x:Key="TitleTextBlockStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
        <Setter Property="FontSize" Value="{StaticResource FontSizeHeadline}" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="Foreground" Value="White" />
    </Style>
    
    <Style x:Key="BodyTextBlockStyle" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{StaticResource DefaultFontFamily}" />
        <Setter Property="FontSize" Value="{StaticResource FontSizeBody}" />
        <Setter Property="Foreground" Value="White" />
    </Style>
    
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}" />
        <Setter Property="Foreground" Value="Black" />
        <Setter Property="Padding" Value="16,8" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="CornerRadius" Value="4" />
    </Style>
</ResourceDictionary>
```

---

## PART 7: QUICK WINS - DETAILED IMPLEMENTATION

### Win #1: DesignTokens.cs
**Effort**: 45 minutes | **Impact**: 100% design consistency

✅ Location: `src/MonadoBlade.Core/DesignSystem/DesignTokens.cs`
✅ Usage: `var color = DesignTokens.Colors.Primary;`
✅ Benefit: Single source of truth, no magic numbers

---

### Win #2: LoadingIndicator Component
**Effort**: 1 hour | **Impact**: Clear progress feedback

Create: `src/MonadoBlade.UI/Components/LoadingIndicator.xaml`

```xaml
<UserControl x:Class="MonadoBlade.UI.Components.LoadingIndicator">
    <Grid Background="#0000" Opacity="0.3" Visibility="{Binding IsVisible, Mode=OneWay}">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            <ProgressRing IsActive="True" Foreground="#00D9FF" />
            <TextBlock Text="{Binding StatusMessage}" Foreground="White" Margin="0,16,0,0" />
        </StackPanel>
    </Grid>
</UserControl>
```

---

### Win #3: ErrorBoundary Component
**Effort**: 1 hour | **Impact**: Graceful error handling

Shows error with retry button when service fails.

---

### Win #4: StatusBadge Component
**Effort**: 30 min | **Impact**: Quick visual status

```xaml
<UserControl x:Class="MonadoBlade.UI.Components.StatusBadge">
    <Border Background="{Binding StatusColor}" CornerRadius="4" Padding="8,4">
        <TextBlock Text="{Binding StatusText}" Foreground="White" FontSize="12" />
    </Border>
</UserControl>
```

---

### Win #5: MetricsCard Component
**Effort**: 1 hour | **Impact**: Professional metrics display

Displays: Title, Value, Trend with color indicator.

---

## FINAL SUMMARY

### What's Complete ✅
- Service consolidation (8→3)
- UI ViewModels (3 wired)
- StateVM foundation
- Architecture analysis

### What's Missing ❌ (Phase 10 Priority)
- Design tokens file
- 18 UI components
- XAML layouts
- Database layer

### Recommended Path Forward
1. **This week**: Implement 5 quick wins (DesignTokens + 4 components)
2. **Next week**: Build remaining components + database
3. **Week 3**: Full integration testing

**Estimated Effort**: 30 hours total
**Expected Outcome**: Production-ready design system + UI framework
