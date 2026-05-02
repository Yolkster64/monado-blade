# MONADO BLADE - Component Library Guide

## Quick Start

### 1. Adding Components to Your View

```xaml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:MonadoBlade.UI.Components.Navigation"
        xmlns:theme="clr-namespace:MonadoBlade.UI.Themes">
    <Window.Resources>
        <ResourceDictionary Source="pack://application:,,,/MonadoBlade.UI;component/Themes/XenobladeTheme.xaml"/>
    </Window.Resources>
    
    <local:SidebarNav/>
</Window>
```

### 2. Binding to ViewModels

All components inherit from `StateVM` which implements `INotifyPropertyChanged`:

```csharp
public class MyViewModel : StateVM
{
    private string _title = "Welcome";
    
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
```

---

## Component Reference

### NAVIGATION COMPONENTS

#### SidebarNav
**Purpose**: Collapsible sidebar with navigation items

**Features**:
- Vertical navigation menu
- Active state highlighting (cyan)
- Icon + label support
- Collapsible on mobile

**Usage**:
```xaml
<local:SidebarNav Width="200"/>
```

**Properties**:
- `SelectedItem` (INotifyPropertyChanged): Current selection
- `Items` (ObservableCollection): Navigation items
- `IsCollapsed` (bool): Collapse state

**Accessibility**:
- Keyboard navigation: Arrow Up/Down to navigate
- Enter to select
- Escape to close on mobile

---

#### TopCommandBar
**Purpose**: Action bar with command buttons

**Features**:
- Horizontal layout
- Primary, Secondary, Danger button styles
- Responsive button wrapping
- Quick action access

**Usage**:
```xaml
<local:TopCommandBar Height="50"/>
```

**Properties**:
- `Commands` (ObservableCollection): List of commands
- `SelectedCommand` (ICommand): Command to execute

---

#### BreadcrumbNav
**Purpose**: Navigation trail showing current location

**Features**:
- Clickable breadcrumb items
- Responsive truncation
- Navigation history

**Usage**:
```xaml
<local:BreadcrumbNav/>
```

**Properties**:
- `Path` (List<BreadcrumbItem>): Navigation path
- `OnBreadcrumbClick` (ICommand): Click handler

---

### DATA DISPLAY COMPONENTS

#### DataGrid
**Purpose**: Virtualized table with sorting/filtering

**Features**:
- Column sorting (click header)
- Row filtering
- Selection (single/multi)
- Responsive column visibility
- Virtual scrolling (performance)

**Usage**:
```xaml
<local:DataGrid ItemsSource="{Binding Items}" RowHeight="36"/>
```

**Properties**:
- `ItemsSource` (IEnumerable): Grid data
- `SelectedItems` (IList): Selected rows
- `SelectedColumn` (string): Sort column
- `SortDirection` (ListSortDirection): Asc/Desc

**Accessibility**:
- Tab to navigate cells
- Arrow keys to move focus
- Space to select row
- Enter to edit cell

---

#### PropertyPanel
**Purpose**: Key-value pair display (read-only properties)

**Features**:
- Organized property display
- Section grouping
- Value formatting
- Copy-to-clipboard support

**Usage**:
```xaml
<local:PropertyPanel Properties="{Binding ObjectProperties}"/>
```

**Properties**:
- `Properties` (Dictionary<string, object>): Property pairs

---

#### Alert
**Purpose**: Notification message display

**Features**:
- 4 severity levels (Info, Success, Warning, Error)
- Icon display
- Dismissible
- Auto-hide (optional)

**Usage**:
```xaml
<local:Alert Type="Warning" Message="This is a warning message"/>
```

**Properties**:
- `Type` (AlertType): Info/Success/Warning/Error
- `Message` (string): Alert content
- `IsDismissible` (bool): Show close button
- `AutoHideSeconds` (int): Auto-dismiss delay (0 = manual)

**Color Reference**:
- Info: Cyan (#00D9FF)
- Success: Green (#00FF41)
- Warning: Amber (#FFB800)
- Error: Red (#FF0055)

---

#### Card
**Purpose**: Reusable container with consistent styling

**Features**:
- Elevated surface appearance
- Consistent padding/spacing
- Border and shadow effects
- Responsive sizing

**Usage**:
```xaml
<local:Card Title="Card Title" Content="{Binding CardContent}"/>
```

**Properties**:
- `Title` (string): Card header
- `Content` (object): Card body content
- `Footer` (object): Card footer
- `IsElevated` (bool): Apply shadow (default: true)

---

#### MetricBox
**Purpose**: KPI display with change indicator

**Features**:
- Large metric number
- Label and sublabel
- Trend indicator (↑/↓)
- Color-coded trend (green/red)

**Usage**:
```xaml
<local:MetricBox Value="9876" Label="Monthly Sales" Trend="+12%" TrendUp="true"/>
```

**Properties**:
- `Value` (object): Metric value
- `Label` (string): Metric name
- `Trend` (string): Change percentage
- `TrendUp` (bool): Direction (green if true, red if false)

---

### INPUT COMPONENTS

#### TextField
**Purpose**: Text input with validation support

**Features**:
- Label above input
- Placeholder support
- Error/helper text
- Character limit indicator
- Multi-line support

**Usage**:
```xaml
<local:TextField Label="Full Name" Placeholder="Enter your name" Value="{Binding Name}"/>
```

**Properties**:
- `Label` (string): Field label
- `Value` (string): Input value (binding)
- `Placeholder` (string): Placeholder text
- `IsError` (bool): Error state
- `ErrorMessage` (string): Error text
- `HelperText` (string): Helper message
- `MaxLength` (int): Character limit

**Accessibility**:
- Label automatically associated
- Error announced to screen readers
- Tab to focus
- Type to input

---

#### Button (3 Variants)
**Purpose**: Interactive button with multiple styles

**Features**:
- Primary (cyan) - Main actions
- Secondary (outline) - Alternative actions
- Danger (red) - Destructive actions
- Hover state feedback
- Disabled state

**Usage**:
```xaml
<!-- Primary -->
<local:Button Content="Save" Style="{DynamicResource ButtonPrimary}"/>

<!-- Secondary -->
<local:Button Content="Cancel" Style="{DynamicResource ButtonSecondary}"/>

<!-- Danger -->
<local:Button Content="Delete" Style="{DynamicResource ButtonDanger}"/>
```

**Properties**:
- `Content` (object): Button label
- `Command` (ICommand): Command to execute
- `IsEnabled` (bool): Enable/disable state
- `Click` (event): Click handler

**Sizing**:
- Height: 36px (standard)
- Padding: 8px vertical, 16px horizontal
- Min-width: auto

---

#### Toggle
**Purpose**: On/Off switch

**Features**:
- Checkbox-based implementation
- Xenoblade styling
- Label support
- Accessibility-friendly

**Usage**:
```xaml
<local:Toggle Label="Enable Feature" IsChecked="{Binding IsEnabled}"/>
```

**Properties**:
- `IsChecked` (bool): Toggle state (binding)
- `Label` (string): Label text
- `OnLabel` (string): Label when on
- `OffLabel` (string): Label when off

---

#### Dropdown
**Purpose**: Select menu with multiple options

**Features**:
- Scrollable list
- Search/filter (optional)
- Default selection
- Grouped options (optional)

**Usage**:
```xaml
<local:Dropdown ItemsSource="{Binding Options}" SelectedItem="{Binding Selected}"/>
```

**Properties**:
- `ItemsSource` (IEnumerable): Option list
- `SelectedItem` (object): Selected value
- `DisplayMemberPath` (string): Property to display
- `SelectedValuePath` (string): Property to return
- `IsFilterable` (bool): Show search box

---

#### SearchBox
**Purpose**: Specialized input for search queries

**Features**:
- Search icon display
- Clear button (auto)
- Debounced search (optional)
- Suggestion dropdown (optional)

**Usage**:
```xaml
<local:SearchBox Text="{Binding SearchQuery}" IsDebounced="true" DebounceMs="300"/>
```

**Properties**:
- `Text` (string): Search query
- `Placeholder` (string): Placeholder
- `IsDebounced` (bool): Debounce input
- `DebounceMs` (int): Debounce delay
- `OnSearch` (ICommand): Search command

---

### CONTAINER COMPONENTS

#### Modal
**Purpose**: Overlaid dialog for user interaction

**Features**:
- Backdrop overlay
- Centered positioning
- Header, body, footer sections
- Keyboard dismissal (Escape)
- Configurable size

**Usage**:
```xaml
<local:Modal IsOpen="{Binding IsModalOpen}" 
             Title="Confirm Action"
             Message="Are you sure?"
             OkCommand="{Binding ConfirmCommand}"
             CancelCommand="{Binding CancelCommand}"/>
```

**Properties**:
- `IsOpen` (bool): Show/hide modal
- `Title` (string): Modal title
- `Content` (object): Body content
- `Footer` (object): Footer content
- `OkCommand` (ICommand): OK button handler
- `CancelCommand` (ICommand): Cancel handler
- `Width` (double): Modal width
- `Height` (double): Modal height

**Keyboard Interaction**:
- Enter: Accept (if OkCommand)
- Escape: Cancel/Close

---

#### Tabs
**Purpose**: Tab-based navigation/content switching

**Features**:
- Multiple tabs
- Active tab highlighting (cyan)
- Content area per tab
- Responsive tab wrapping
- Keyboard navigation

**Usage**:
```xaml
<local:Tabs SelectedTabIndex="0">
    <local:TabItem Header="Tab 1" Content="{Binding Content1}"/>
    <local:TabItem Header="Tab 2" Content="{Binding Content2}"/>
</local:Tabs>
```

**Properties**:
- `Items` (ObservableCollection<TabItem>): Tab list
- `SelectedTabIndex` (int): Active tab index
- `SelectedTabItem` (TabItem): Active tab object

**Keyboard Navigation**:
- Arrow Left/Right to navigate tabs
- Tab to enter content area

---

#### Section
**Purpose**: Collapsible content container

**Features**:
- Expand/collapse toggle
- Header with arrow indicator
- Smooth animation
- Nested sections support

**Usage**:
```xaml
<local:Section Header="Advanced Settings" IsExpanded="false">
    <!-- Content here -->
</local:Section>
```

**Properties**:
- `Header` (string): Section title
- `Content` (object): Collapsible content
- `IsExpanded` (bool): Expanded state
- `CanToggle` (bool): Allow expand/collapse

**Animation**:
- Duration: 200ms (smooth)
- Easing: Ease-in-out

---

#### Toolbar
**Purpose**: Action toolbar with tool buttons

**Features**:
- Horizontal button layout
- Icon + tooltip support
- Separator support
- Responsive wrapping

**Usage**:
```xaml
<local:Toolbar>
    <local:ToolbarButton Icon="💾" Tooltip="Save" Command="{Binding SaveCommand}"/>
    <local:ToolbarButton Icon="↶" Tooltip="Undo" Command="{Binding UndoCommand}"/>
    <Separator/>
    <local:ToolbarButton Content="Export" Command="{Binding ExportCommand}"/>
</local:Toolbar>
```

**Properties**:
- `Items` (ObservableCollection): Toolbar items
- `RowHeight` (double): Toolbar height

---

#### StatusBar
**Purpose**: Footer status information display

**Features**:
- Left-aligned status
- Right-aligned metrics
- Status icon (✓/⚠/✗)
- Real-time updates

**Usage**:
```xaml
<local:StatusBar Status="Ready" StatusIcon="✓" 
                 RightContent="Items: 42 | Memory: 256MB"/>
```

**Properties**:
- `Status` (string): Status text
- `StatusIcon` (string): Icon (✓/⚠/✗)
- `RightContent` (object): Right-aligned content
- `IsLoading` (bool): Show loading state

---

## Theming & Customization

### Using Theme Resources

All components use design tokens from the theme dictionary:

```xaml
<TextBlock Foreground="{DynamicResource PrimaryBrush}" 
           FontSize="{DynamicResource FontSizeH1}"/>
```

### Available Theme Resources

**Colors**:
- `PrimaryBrush`, `SuccessBrush`, `ErrorBrush`, `WarningBrush`
- `BackgroundBrush`, `SurfaceBrush`, `TextBrush`, `BorderBrush`

**Typography**:
- `FontSizeH1`, `FontSizeH2`, `FontSizeH3`, `FontSizeBody`, `FontSizeSmall`
- `TextH1`, `TextH2`, `TextH3`, `TextBody`, `TextSmall` (styles)

**Spacing**:
- `SpacingXS`, `SpacingSM`, `SpacingMD`, `SpacingLG`, `SpacingXL`
- `PaddingButton`, `PaddingInput`, `PaddingCardSM`, `PaddingCardMD`

**Styles**:
- `ButtonPrimary`, `ButtonSecondary`, `ButtonDanger`
- `TextFieldStyle`, `CardStyle`

### Custom Theme Colors

```xaml
<!-- Override a color -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#FF00FF"/>

<!-- Or use in code -->
var brush = (SolidColorBrush)Application.Current.Resources["PrimaryBrush"];
brush.Color = Colors.Magenta;
```

---

## Responsive Design

### Breakpoints

```csharp
if (ResponsiveBreakpoints.IsMobile(window.ActualWidth))
{
    // Mobile layout
}
else if (ResponsiveBreakpoints.IsTablet(window.ActualWidth))
{
    // Tablet layout
}
else
{
    // Desktop layout
}
```

### Responsive Visibility

```xaml
<TextBlock local:ResponsiveVisibility.HideOnMobile="true" Text="Desktop-only content"/>
```

### Responsive Padding

```csharp
var breakpoint = ResponsiveBreakpoints.GetCurrentBreakpoint(window.ActualWidth);
var padding = ResponsiveSpacing.GetResponsivePadding(breakpoint);
```

---

## Accessibility Checklist

- ✓ All interactive elements keyboard accessible
- ✓ Focus indicators visible (2px cyan border)
- ✓ ARIA labels on all buttons/icons
- ✓ Color contrast WCAG AAA compliant
- ✓ Screen reader support built-in
- ✓ Semantic HTML/XAML structure
- ✓ Keyboard shortcuts documented
- ✓ Motion respects prefers-reduced-motion

---

## Performance Tips

1. **Virtual Scrolling**: DataGrid uses virtualization for large datasets
2. **Lazy Loading**: Load content on demand
3. **Debouncing**: SearchBox debounces input (300ms default)
4. **Resource Sharing**: Use theme resources instead of inline styles
5. **Animations**: Keep animations ≤300ms for responsive feel

---

## State Management Pattern

```csharp
// ViewModel
public class MyViewModel : AsyncStateVM
{
    public async void LoadData()
    {
        SetLoading();
        try
        {
            var data = await api.GetDataAsync();
            SetSuccess();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }
}

// XAML
<TextBlock Text="Loading..." Visibility="{Binding IsLoading, Converter=BoolToVisibilityConverter}"/>
<local:Alert Type="Error" Message="{Binding ErrorMessage}" 
             Visibility="{Binding LoadingState, Converter=StateToVisibilityConverter}"/>
```

---

## Week 2 Integration Points

Components are ready for:
- ✓ Service binding
- ✓ Real-time data updates (SignalR)
- ✓ Advanced visualizations
- ✓ Custom themes
- ✓ Extended functionality

All stubs are designed to accept business logic without structural changes.

---

## Support & Examples

See `/Examples` folder for:
- Dashboard layout example
- Form submission flow
- Real-time data grid
- Modal workflows

---

**Status**: Week 1 Foundation Complete ✓
Ready for Week 2 implementation phase.
