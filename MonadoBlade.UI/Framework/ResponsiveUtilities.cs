using System;
using System.Windows;

namespace MonadoBlade.UI.Framework
{
    /// <summary>
    /// Responsive breakpoint utilities for adaptive layouts.
    /// </summary>
    public static class ResponsiveBreakpoints
    {
        // Breakpoint sizes
        public const double MobileMax = 768;    // < 768px
        public const double TabletMin = 768;    // >= 768px
        public const double TabletMax = 1024;   // < 1024px
        public const double DesktopMin = 1024;  // >= 1024px

        /// <summary>
        /// Get the current breakpoint based on window width.
        /// </summary>
        public static Breakpoint GetCurrentBreakpoint(double windowWidth)
        {
            if (windowWidth < MobileMax)
                return Breakpoint.Mobile;
            if (windowWidth < DesktopMin)
                return Breakpoint.Tablet;
            return Breakpoint.Desktop;
        }

        /// <summary>
        /// Check if current breakpoint is mobile.
        /// </summary>
        public static bool IsMobile(double windowWidth) => windowWidth < MobileMax;

        /// <summary>
        /// Check if current breakpoint is tablet.
        /// </summary>
        public static bool IsTablet(double windowWidth) => 
            windowWidth >= TabletMin && windowWidth < DesktopMin;

        /// <summary>
        /// Check if current breakpoint is desktop.
        /// </summary>
        public static bool IsDesktop(double windowWidth) => windowWidth >= DesktopMin;
    }

    /// <summary>
    /// Represents responsive breakpoints.
    /// </summary>
    public enum Breakpoint
    {
        Mobile,   // < 768px
        Tablet,   // 768px - 1024px
        Desktop   // >= 1024px
    }

    /// <summary>
    /// Helper class for responsive padding/margin adjustments.
    /// </summary>
    public static class ResponsiveSpacing
    {
        /// <summary>
        /// Get responsive padding based on breakpoint.
        /// </summary>
        public static Thickness GetResponsivePadding(Breakpoint breakpoint)
        {
            return breakpoint switch
            {
                Breakpoint.Mobile => new Thickness(8),    // sm
                Breakpoint.Tablet => new Thickness(16),   // md
                Breakpoint.Desktop => new Thickness(24),  // lg
                _ => new Thickness(16)
            };
        }

        /// <summary>
        /// Get responsive font size based on breakpoint.
        /// </summary>
        public static double GetResponsiveFontSize(double baseSize, Breakpoint breakpoint)
        {
            return breakpoint switch
            {
                Breakpoint.Mobile => baseSize * 0.85,
                Breakpoint.Tablet => baseSize * 0.95,
                Breakpoint.Desktop => baseSize,
                _ => baseSize
            };
        }

        /// <summary>
        /// Get responsive width for grid columns.
        /// </summary>
        public static double GetResponsiveColumnWidth(Breakpoint breakpoint, int totalColumns)
        {
            var (cols, spacing) = breakpoint switch
            {
                Breakpoint.Mobile => (1, 8),
                Breakpoint.Tablet => (2, 16),
                Breakpoint.Desktop => (totalColumns, 24),
                _ => (totalColumns, 16)
            };

            return (100.0 - (spacing * (cols - 1))) / cols;
        }
    }

    /// <summary>
    /// Attached behaviors for responsive visibility.
    /// </summary>
    public static class ResponsiveVisibility
    {
        public static bool GetHideOnMobile(DependencyObject obj)
        {
            return (bool)obj.GetValue(HideOnMobileProperty);
        }

        public static void SetHideOnMobile(DependencyObject obj, bool value)
        {
            obj.SetValue(HideOnMobileProperty, value);
        }

        public static readonly DependencyProperty HideOnMobileProperty =
            DependencyProperty.RegisterAttached(
                "HideOnMobile",
                typeof(bool),
                typeof(ResponsiveVisibility),
                new PropertyMetadata(false, OnHideOnMobileChanged));

        private static void OnHideOnMobileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Implementation would check current window width and set Visibility
            if (d is FrameworkElement element && e.NewValue is bool hideOnMobile)
            {
                element.Visibility = hideOnMobile ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}
