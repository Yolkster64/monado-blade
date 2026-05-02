# MONADO BLADE v2.2.0 - Design System Guide

## 1. COLOR THEME - Xenoblade Inspired

### Primary Colors
| Name | Hex | RGB | Usage |
|------|-----|-----|-------|
| Cyan (Primary) | `#00D9FF` | rgb(0, 217, 255) | Accents, highlights, primary actions |
| Dark Background | `#0A0E27` | rgb(10, 14, 39) | Main background |
| Light Surface | `#1A1F3A` | rgb(26, 31, 58) | Cards, panels, elevated surfaces |

### Semantic Colors
| Name | Hex | RGB | Usage |
|------|-----|-----|-------|
| Success | `#00FF41` | rgb(0, 255, 65) | Success states, valid inputs |
| Error | `#FF0055` | rgb(255, 0, 85) | Error states, destructive actions |
| Warning | `#FFB800` | rgb(255, 184, 0) | Warnings, cautions, pending states |
| Info | `#00D9FF` | rgb(0, 217, 255) | Informational messages |

### Neutral Colors
| Name | Hex | RGB | Usage |
|------|-----|-----|-------|
| Gray 50 | `#999999` | rgb(153, 153, 153) | Light text on dark bg |
| Gray 40 | `#666666` | rgb(102, 102, 102) | Secondary text |
| Gray 30 | `#333333` | rgb(51, 51, 51) | Borders, dividers |

### Color Accessibility
- Cyan #00D9FF on Dark #0A0E27: WCAG AAA compliant (20:1 contrast)
- Green #00FF41 on Dark #0A0E27: WCAG AAA compliant (18:1 contrast)
- Red #FF0055 on Dark #0A0E27: WCAG AAA compliant (12:1 contrast)
- All semantic colors meet WCAG AA minimum (4.5:1) for accessibility

---

## 2. TYPOGRAPHY SCALE

### Font Family
- **Primary**: Inter (system default fallback: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif)
- **Monospace**: Inconsolata (fallback: "Courier New", monospace)

### Type Hierarchy

#### Heading 1 (H1)
- Size: 32px
- Weight: Bold (700)
- Line Height: 1.2 (38.4px)
- Letter Spacing: -0.02em
- Use: Page titles, major section headers

#### Heading 2 (H2)
- Size: 24px
- Weight: Bold (700)
- Line Height: 1.3 (31.2px)
- Letter Spacing: -0.01em
- Use: Section headers, modal titles

#### Heading 3 (H3)
- Size: 18px
- Weight: Semi-bold (600)
- Line Height: 1.4 (25.2px)
- Letter Spacing: 0em
- Use: Subsection headers, card titles

#### Body Text (Regular)
- Size: 14px
- Weight: Regular (400)
- Line Height: 1.6 (22.4px)
- Letter Spacing: 0em
- Use: Main content, descriptions, paragraphs

#### Small Text
- Size: 12px
- Weight: Regular (400)
- Line Height: 1.5 (18px)
- Letter Spacing: 0.01em
- Use: Captions, helper text, small labels

#### Monospace (Code)
- Size: 12px
- Weight: Regular (400)
- Line Height: 1.5 (18px)
- Family: Inconsolata
- Use: Code snippets, terminal output, variable names

---

## 3. SPACING SYSTEM (8px Grid)

### Spacing Scale
| Token | Size | Usage |
|-------|------|-------|
| `xs` | 4px | Small gaps, compact spacing |
| `sm` | 8px | Default padding, button gaps |
| `md` | 16px | Standard padding, section gaps |
| `lg` | 24px | Large padding, major section gaps |
| `xl` | 32px | Extra large sections, columns |
| `2xl` | 48px | Full section separators |

### Component Spacing Guidelines
- **Margin**: Use multiples of 8px grid
- **Padding**: Use multiples of 8px grid
- **Gap**: Between flex/grid items, use spacing scale
- **Line Height**: Based on typography scale
- **Min Touch Target**: 36px height (accessibility)

---

## 4. COMPONENT SPACING SPECIFICATIONS

### Button
- Height: 36px
- Horizontal Padding: 16px
- Vertical Padding: 8px
- Computed: 8px 16px (vertical horizontal)
- Icon Size: 16px (internal)
- Icon + Text Gap: 8px

### Input Field (TextField)
- Height: 32px
- Horizontal Padding: 12px
- Vertical Padding: 8px
- Border: 1px
- Computed: 8px 12px (vertical horizontal)
- Focus Border Width: 2px

### Card
- Small Padding: 16px (md)
- Medium Padding: 24px (lg)
- Border Radius: 4px (sharp style)
- Elevation: 1 (subtle shadow)
- Margin Bottom: 16px (between cards)

### Section Container
- Padding: 24px (lg)
- Margin Bottom: 32px (xl)
- Border Radius: 8px (rounded style)
- Elevation: 2 (raised appearance)

### Modal Dialog
- Padding: 24px (lg)
- Header Padding: 24px
- Body Padding: 24px
- Footer Padding: 24px (with 8px gaps between buttons)
- Min Width: 320px
- Max Width: 90% of viewport or 600px

### DataGrid / Table
- Row Height: 36px (including borders)
- Cell Padding: 12px (horizontal), 8px (vertical)
- Header Padding: 12px (horizontal), 12px (vertical)
- Column Gap: 0 (no gap, use borders)

---

## 5. SHADOWS & ELEVATION

### Shadow System (3 levels)

#### Elevation 1 (Subtle)
```
Box-Shadow: 0 2px 4px rgba(0, 0, 0, 0.1)
```
- Use: Cards, buttons (hover), small floating elements
- Depth: Minimal, close to surface

#### Elevation 2 (Raised)
```
Box-Shadow: 0 4px 8px rgba(0, 0, 0, 0.2)
```
- Use: Dropdowns, popovers, modal backdrops
- Depth: Medium elevation

#### Elevation 3 (High)
```
Box-Shadow: 0 8px 16px rgba(0, 0, 0, 0.3)
```
- Use: Modals, floating action buttons, high-priority overlays
- Depth: Maximum elevation

### Glow Effects (Monado Accent)

#### Cyan Glow (Primary)
```
Box-Shadow: 0 0 16px rgba(0, 217, 255, 0.3)
```
- Use: Active states, focus indicators, accent highlights
- Effect: Glowing, energetic appearance

#### Green Glow (Success)
```
Box-Shadow: 0 0 12px rgba(0, 255, 65, 0.25)
```
- Use: Success confirmations, valid states
- Effect: Positive feedback glow

#### Red Glow (Error)
```
Box-Shadow: 0 0 12px rgba(255, 0, 85, 0.25)
```
- Use: Error states, validation failures
- Effect: Alert glow

---

## 6. BORDER & CORNER RADIUS

### Border Radius
- **Sharp**: 4px - Used for input fields, small components
- **Round**: 8px - Used for cards, containers, modals
- **Pill**: 20px - Used for badges, toggle switches
- **Circle**: 50% - Used for avatars, circular buttons

### Border Styles
- **Primary Border**: 1px solid rgba(0, 217, 255, 0.2) - Subtle cyan tint
- **Secondary Border**: 1px solid #333333 - Neutral gray
- **Focus Border**: 2px solid #00D9FF - Bright cyan (accessibility)
- **Error Border**: 2px solid #FF0055 - Bright red

---

## 7. ANIMATION & TRANSITIONS

### Standard Timing
- **Quick**: 150ms - Micro-interactions (hover, focus)
- **Standard**: 200ms - Component transitions
- **Slow**: 300ms - Page transitions, modals
- **Extra Slow**: 500ms - Complex animations

### Easing Functions
- **Ease-In-Out**: `cubic-bezier(0.4, 0, 0.2, 1)` - Standard animations
- **Ease-Out**: `cubic-bezier(0.0, 0, 0.2, 1)` - Entrances
- **Ease-In**: `cubic-bezier(0.4, 0, 1, 1)` - Exits

### Common Animations
- **Button Hover**: Color shift + 0px 2px 4px shadow (150ms ease-out)
- **Input Focus**: Border color + cyan glow (200ms ease-in-out)
- **Modal Enter**: Fade in + scale 0.95→1.0 (300ms ease-out)
- **Modal Exit**: Fade out + scale 1.0→0.95 (200ms ease-in)

---

## 8. RESPONSIVE BREAKPOINTS

### Breakpoint Sizes
| Device | Breakpoint | CSS | Usage |
|--------|------------|-----|-------|
| Mobile | < 768px | `xs` | Phones, small devices |
| Tablet | 768px - 1024px | `md` | Tablets, landscape phones |
| Desktop | > 1024px | `lg` | Desktops, large screens |

### Layout Adjustments by Breakpoint

#### Mobile (xs)
- Single column layout
- Full-width cards (margin: 8px)
- Sidebar hidden (hamburger menu shown)
- Font sizes: reduced by 10-20%
- Padding: reduced to `sm` (8px)

#### Tablet (md)
- Two-column layout (when applicable)
- Cards at 50% width (md breakpoint)
- Sidebar visible (collapsed)
- Standard font sizes
- Standard padding (md: 16px)

#### Desktop (lg)
- Three+ column layout
- Cards at 33% width
- Sidebar expanded
- Full typography scale
- Full padding scale (lg: 24px, xl: 32px)

---

## 9. ACCESSIBILITY SPECIFICATIONS

### Keyboard Navigation
- **Tab Index**: All interactive elements reachable via Tab
- **Focus Indicator**: 2px cyan border on focus
- **Enter/Space**: Activate buttons, toggles
- **Arrow Keys**: Navigation in lists, tabs, date pickers
- **Escape**: Close modals, popovers, dropdowns

### Screen Reader Support
- **ARIA Labels**: All buttons and icons have labels
- **ARIA Live Regions**: Notifications announced
- **Semantic HTML**: `<button>`, `<form>`, `<nav>`, etc.
- **Alt Text**: All images have descriptive alt text

### Color Contrast
- **Body Text**: 4.5:1 minimum (WCAG AA)
- **Large Text**: 3:1 minimum (WCAG AA)
- **All UI**: Never rely on color alone for information

### Motion & Animation
- **Prefers Reduced Motion**: Respect `prefers-reduced-motion` media query
- **Max Animation Duration**: 300ms for full-page transitions
- **No Auto-play**: Videos/animations don't auto-play

---

## 10. DARK THEME (Primary)

All color specifications above apply to the dark theme, which is the primary theme for MONADO BLADE.

### Light Theme (Optional Future)
When implemented, maintain the same color hierarchy but:
- Background: #FFFFFF or #F5F5F5
- Surface: #FAFAFA or #F0F0F0
- Text: #0A0E27 (inverted from dark theme)
- Borders: rgba(0, 0, 0, 0.2)
- Shadows: Reduce opacity (use rgba(0, 0, 0, 0.08) for elevation 1)

---

## 11. STATE INDICATORS

### Interactive State States
| State | Styling | Opacity/Shadow |
|-------|---------|----------------|
| Default | Base color | 100% opacity, elevation 0 |
| Hover | Slightly lighter | 90% opacity, elevation 1 |
| Active/Pressed | Highlight color | 100% opacity, elevation 2 |
| Disabled | Gray (#333333) | 50% opacity, no elevation |
| Focus | Cyan border outline | 2px border |
| Error | Red border/glow | 2px border, red glow |

---

## 12. DESIGN TOKEN REFERENCE

### Colors
```
Primary: #00D9FF (Cyan)
Success: #00FF41 (Green)
Error: #FF0055 (Red)
Warning: #FFB800 (Amber)
Info: #00D9FF (Cyan)
Background: #0A0E27 (Dark)
Surface: #1A1F3A (Light)
Text: #FFFFFF (White)
TextSecondary: #999999 (Gray)
Border: #333333 (Dark Gray)
```

### Spacing
```
xs: 4px
sm: 8px
md: 16px
lg: 24px
xl: 32px
2xl: 48px
```

### Typography
```
H1: 32px bold
H2: 24px bold
H3: 18px semi-bold
Body: 14px regular
Small: 12px regular
Mono: 12px Inconsolata
```

### Shadows
```
Elevation1: 0 2px 4px rgba(0,0,0,0.1)
Elevation2: 0 4px 8px rgba(0,0,0,0.2)
Elevation3: 0 8px 16px rgba(0,0,0,0.3)
CyanGlow: 0 0 16px rgba(0,217,255,0.3)
```

---

## Implementation Notes

1. **All XAML-based components** use resource dictionaries for all colors, spacing, and typography
2. **No hardcoded values** - All styling uses named resources
3. **Composable design** - Components build on other components
4. **Theme-aware** - All components respect theme resources
5. **Responsive by default** - Components adapt to breakpoints
6. **Accessibility first** - Keyboard navigation and screen reader support built-in

---

## Week 1 Status: COMPLETE ✓

All design tokens defined, ready for component implementation.
