# MONADO BLADE - Responsive Design Guidelines

## Responsive Breakpoints

### Mobile (< 768px)
- **Typical Devices**: iPhone, Android phones, small tablets
- **Portrait Orientation**: 320px - 767px
- **Landscape Orientation**: 480px - 767px
- **DPI**: 1x - 3x (high DPI phones)

### Tablet (768px - 1024px)
- **Typical Devices**: iPad, Android tablets, landscape phones
- **Portrait Orientation**: 768px - 1023px
- **Landscape Orientation**: 768px - 1024px
- **DPI**: 1x - 2x

### Desktop (> 1024px)
- **Typical Devices**: Desktops, laptops, large monitors
- **Minimum Width**: 1024px
- **Common Sizes**: 1920x1080, 1440x900, 2560x1440
- **DPI**: 1x - 2x

---

## Layout Patterns by Breakpoint

### Mobile (< 768px)

#### Typography Adjustments
```
H1: 28px → 32px (reduced from desktop)
H2: 20px → 24px
H3: 16px → 18px
Body: 12px → 14px
Small: 10px → 12px

Line heights reduced by 5-10% for compact display
```

#### Spacing Adjustments
```
Margins: Reduced from lg (24px) to sm (8px)
Padding: Reduced from lg (24px) to md (16px)
Gaps: Reduced from md (16px) to sm (8px)
```

#### Component Behavior
- **SidebarNav**: Hidden (toggle with hamburger menu)
- **TopCommandBar**: Reorganized vertically (dropdown menu)
- **DataGrid**: Horizontal scroll or card view (convert rows to cards)
- **Card**: Full width (margin: 8px)
- **Button**: Full width or stacked vertically
- **Modal**: Full screen or 90% width with padding
- **Tabs**: Vertical or scrollable horizontal
- **MetricBox**: Single column (full width)

#### Grid Layout
```xaml
<!-- 1-column layout on mobile -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="*"/>
</Grid>

<!-- Use visibility to hide desktop elements -->
<TextBlock local:ResponsiveVisibility.HideOnMobile="true" Text="Desktop only"/>
```

#### Hamburger Menu
```xaml
<!-- Show hamburger on mobile, sidebar on desktop -->
<Button Content="☰" Visibility="{Binding IsDesktop, Converter=BoolToVisibilityConverter}"/>
<local:SidebarNav Visibility="{Binding IsMobile, Converter=InverseBoolToVisibilityConverter}"/>
```

---

### Tablet (768px - 1024px)

#### Typography
```
H1: 32px (unchanged)
H2: 24px (unchanged)
H3: 18px (unchanged)
Body: 14px (unchanged)

Line heights: Standard scale
```

#### Spacing
```
Margins: Standard lg (24px)
Padding: Standard md (16px)
Gaps: Standard md (16px)
```

#### Layout Changes
- **Sidebar**: Collapsed or visible (depends on device orientation)
- **Grid**: 2-column layout
- **Cards**: 2 per row (50% width)
- **MetricBox**: 2 per row

#### Grid Layout
```xaml
<!-- 2-column layout on tablet -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>
```

---

### Desktop (> 1024px)

#### Typography
```
H1: 32px (full size)
H2: 24px (full size)
H3: 18px (full size)
Body: 14px (full size)
Small: 12px (full size)

All line heights: Standard scale
```

#### Spacing
```
Margins: Full scale (lg: 24px, xl: 32px, 2xl: 48px)
Padding: Full scale
Gaps: Full scale
```

#### Layout Features
- **SidebarNav**: Always visible, expandable
- **TopCommandBar**: Full horizontal layout
- **DataGrid**: Full virtualized table
- **Cards**: 3+ columns possible
- **Modal**: Centered, fixed width (600px)
- **Sidebar**: Expanded by default

#### Grid Layout
```xaml
<!-- 3+ column layout on desktop -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>
```

---

## Implementation Patterns

### Responsive Value Converter

```csharp
public class ResponsiveValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is double windowWidth)
        {
            var breakpoint = ResponsiveBreakpoints.GetCurrentBreakpoint(windowWidth);
            
            return breakpoint switch
            {
                Breakpoint.Mobile => values[1],    // Mobile value
                Breakpoint.Tablet => values[2],    // Tablet value
                Breakpoint.Desktop => values[3],   // Desktop value
                _ => values[3]
            };
        }
        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### Usage in XAML

```xaml
<!-- Responsive font size -->
<TextBlock Text="Responsive Title">
    <TextBlock.FontSize>
        <MultiBinding Converter="{StaticResource ResponsiveValueConverter}">
            <Binding RelativeSource="{RelativeSource AncestorType=Window}" Path="ActualWidth"/>
            <binding>28</binding>  <!-- Mobile -->
            <binding>28</binding>  <!-- Tablet -->
            <binding>32</binding>  <!-- Desktop -->
        </MultiBinding>
    </TextBlock.FontSize>
</TextBlock>
```

### Responsive Container Component

```xaml
<!-- Custom responsive container -->
<Grid Width="{Binding ActualWidth, RelativeSource={RelativeSource AncestorType=Window}}">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{Binding ColumnWidth, Converter=ResponsiveColumnWidthConverter}"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="{Binding ColumnWidth, Converter=ResponsiveColumnWidthConverter}"/>
    </Grid.ColumnDefinitions>
</Grid>
```

---

## Component-Specific Responsive Behavior

### SidebarNav
```
Mobile:   Hidden (show via hamburger)
Tablet:   Collapsed (icon-only)
Desktop:  Expanded (icon + label)
```

### DataGrid
```
Mobile:   Card view (each row as card)
Tablet:   Scrollable grid (some columns hidden)
Desktop:  Full grid (all columns visible)
```

### Buttons
```
Mobile:   Full width or 2-column grid
Tablet:   Auto width (inline)
Desktop:  Auto width (inline)
```

### MetricBox
```
Mobile:   1 per row (full width)
Tablet:   2 per row (50%)
Desktop:  3-4 per row (25-33%)
```

### Modal
```
Mobile:   Full screen (min 8px margin)
Tablet:   90% width, centered
Desktop:  600px width, centered
```

### Toolbar
```
Mobile:   Vertical stack or hidden (show in menu)
Tablet:   Horizontal, wrappable
Desktop:  Horizontal, fixed
```

---

## Testing Responsive Layouts

### Desktop Browser DevTools
1. Open DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M)
3. Test at breakpoints:
   - 375px (Mobile)
   - 768px (Tablet)
   - 1024px (Desktop)
   - 1920px (Large desktop)

### Physical Devices
- **Mobile**: iPhone SE, iPhone 14 Pro, Android
- **Tablet**: iPad (9.7"), iPad Pro (12.9")
- **Desktop**: 1920x1080, 2560x1440

### Test Scenarios
- [ ] Window resize (drag from desktop)
- [ ] Orientation change (mobile rotation)
- [ ] Zoom levels (100%, 110%, 150%, 200%)
- [ ] Font scaling (browser text size)
- [ ] Touch vs. mouse interaction
- [ ] Slow network (test image loading)
- [ ] High DPI (2x, 3x pixel density)

---

## Responsive Images

### Image Sizing
```xaml
<!-- Responsive image -->
<Image Source="logo.png" 
       Width="200"
       Height="Auto"
       MaxWidth="{Binding AvailableWidth}"/>
```

### DPI Considerations
```
Mobile (1x):   Use standard resolution
Tablet (2x):   Use 2x resolution (@2x suffix)
Desktop (2x):  Use 2x resolution (@2x suffix)
```

---

## Responsive Typography

### Font Size Scaling

```csharp
public static double GetResponsiveFontSize(double baseSize, Breakpoint breakpoint)
{
    return breakpoint switch
    {
        Breakpoint.Mobile => baseSize * 0.85,    // 15% reduction
        Breakpoint.Tablet => baseSize * 0.95,    // 5% reduction
        Breakpoint.Desktop => baseSize,          // Full size
        _ => baseSize
    };
}
```

### Example Usage
```csharp
var fontSize = GetResponsiveFontSize(32, breakpoint);  // H1

// Mobile:  27.2px
// Tablet:  30.4px
// Desktop: 32px
```

---

## Responsive Touch Targets

### Minimum Sizes
- **Mobile**: 44x44px (touch)
- **Tablet**: 40x40px (touch)
- **Desktop**: 36x36px (mouse)

### Implementation
```xaml
<Button Height="44" MinWidth="44" Content="Touch Button"/>

<!-- Desktop: Can use smaller sizes -->
<Button Height="32" MinWidth="32" Content="Mouse Button"/>
```

---

## Performance Optimization

### Mobile Optimization
- Reduce animation duration (200ms → 150ms)
- Hide non-essential elements
- Optimize images (progressive JPEG)
- Lazy-load content below fold
- Limit column counts in DataGrid

### Load Time Targets
- Mobile: < 3s first paint
- Tablet: < 2s first paint
- Desktop: < 1.5s first paint

### Implementation
```csharp
// Lazy load content
if (ResponsiveBreakpoints.IsDesktop(width))
{
    LoadDetailedContent();
}
else if (ResponsiveBreakpoints.IsTablet(width))
{
    LoadSimplifiedContent();
}
```

---

## Responsive State Management

```csharp
public class ResponsiveViewModel : StateVM
{
    private double _windowWidth;
    private Breakpoint _currentBreakpoint;

    public ResponsiveViewModel()
    {
        SystemEvents.DisplaySettingsChanged += (s, e) => 
        {
            UpdateBreakpoint(Application.Current.MainWindow.ActualWidth);
        };
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (SetProperty(ref _windowWidth, value))
            {
                UpdateBreakpoint(value);
            }
        }
    }

    public Breakpoint CurrentBreakpoint
    {
        get => _currentBreakpoint;
        set => SetProperty(ref _currentBreakpoint, value);
    }

    private void UpdateBreakpoint(double width)
    {
        CurrentBreakpoint = ResponsiveBreakpoints.GetCurrentBreakpoint(width);
    }
}
```

---

## Testing Checklist

### Mobile (< 768px)
- [ ] All content readable without horizontal scroll
- [ ] Touch targets ≥ 44px
- [ ] Buttons don't overflow
- [ ] Text wraps properly
- [ ] Images scaled appropriately
- [ ] Sidebar hidden (hamburger menu visible)

### Tablet (768px - 1024px)
- [ ] 2-column layout renders correctly
- [ ] Touch targets ≥ 40px
- [ ] Sidebar collapsed or visible
- [ ] DataGrid shows 2-3 columns
- [ ] Spacing adjusted correctly

### Desktop (> 1024px)
- [ ] Full layout displays
- [ ] All columns visible
- [ ] Spacing maximized
- [ ] Sidebar expanded
- [ ] 3+ column layouts work

---

## Week 1 Status: COMPLETE ✓

All components are responsive-ready.
Breakpoint utilities tested and documented.
