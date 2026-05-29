using System;
using System.Windows;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Manages responsive theme behavior across different resolutions,
    /// DPI scaling, font sizes, and device types.
    /// </summary>
    public class ResponsiveTheme
    {
        public enum DeviceType
        {
            DesktopSmall,    // < 1080p
            DesktopStandard, // 1080p - 1440p
            DesktopLarge,    // 1440p - 4K
            Laptop,          // Typical laptop resolution
            Tablet,
            Mobile
        }

        public class ResponsiveSettings
        {
            public DeviceType Device { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double DPI { get; set; }
            public double ScaleFactor { get; set; }
            public double BaseFontSize { get; set; }
            public double ButtonHeight { get; set; }
            public double ButtonWidth { get; set; }
            public double Spacing { get; set; }
            public double BorderRadius { get; set; }
            public bool IsPortrait { get; set; }
        }

        private ResponsiveSettings _currentSettings;
        private double _baselineScreenWidth;
        private double _baselineScreenHeight;
        private double _baselineDPI;

        public event EventHandler<ResponsiveSettingsChangedEventArgs> SettingsChanged;

        public ResponsiveTheme()
        {
            _baselineScreenWidth = 1920;
            _baselineScreenHeight = 1080;
            _baselineDPI = 96; // Windows standard DPI

            Initialize();
        }

        /// <summary>
        /// Initializes responsive theme based on current system settings.
        /// </summary>
        private void Initialize()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var dpi = GetSystemDPI();

            _currentSettings = new ResponsiveSettings
            {
                Width = screenWidth,
                Height = screenHeight,
                DPI = dpi,
                IsPortrait = screenHeight > screenWidth
            };

            DetermineDPI();
            DetermineDeviceType();
            CalculateResponsiveMetrics();
        }

        /// <summary>
        /// Determines the current system DPI.
        /// </summary>
        private double GetSystemDPI()
        {
            try
            {
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
                return graphics.DpiX;
            }
            catch
            {
                return 96; // Default DPI
            }
        }

        /// <summary>
        /// Calculates the DPI scale factor.
        /// </summary>
        private void DetermineDPI()
        {
            _currentSettings.ScaleFactor = _currentSettings.DPI / _baselineDPI;
        }

        /// <summary>
        /// Determines the device type based on screen resolution.
        /// </summary>
        private void DetermineDeviceType()
        {
            var width = _currentSettings.Width;
            var height = _currentSettings.Height;
            var minDimension = Math.Min(width, height);
            var maxDimension = Math.Max(width, height);

            if (maxDimension <= 1024 && minDimension <= 600)
            {
                _currentSettings.Device = DeviceType.Mobile;
            }
            else if (maxDimension <= 1280 && minDimension <= 800)
            {
                _currentSettings.Device = DeviceType.Tablet;
            }
            else if (maxDimension <= 1366)
            {
                _currentSettings.Device = DeviceType.Laptop;
            }
            else if (maxDimension <= 1920)
            {
                _currentSettings.Device = DeviceType.DesktopSmall;
            }
            else if (maxDimension <= 2560)
            {
                _currentSettings.Device = DeviceType.DesktopStandard;
            }
            else
            {
                _currentSettings.Device = DeviceType.DesktopLarge;
            }
        }

        /// <summary>
        /// Calculates responsive metrics based on screen size and DPI.
        /// </summary>
        private void CalculateResponsiveMetrics()
        {
            // Base font size with DPI scaling
            _currentSettings.BaseFontSize = CalculateBaseFontSize();

            // Button sizing
            _currentSettings.ButtonHeight = 40 * _currentSettings.ScaleFactor;
            _currentSettings.ButtonWidth = Math.Min(_currentSettings.Width * 0.15, 200 * _currentSettings.ScaleFactor);

            // Spacing (padding, margins, gaps)
            _currentSettings.Spacing = 8 * _currentSettings.ScaleFactor;

            // Border radius for modern design
            _currentSettings.BorderRadius = 4 * _currentSettings.ScaleFactor;
        }

        /// <summary>
        /// Calculates the base font size responsive to screen resolution.
        /// </summary>
        private double CalculateBaseFontSize()
        {
            var widthScaleFactor = _currentSettings.Width / _baselineScreenWidth;
            var heightScaleFactor = _currentSettings.Height / _baselineScreenHeight;
            var minScaleFactor = Math.Min(widthScaleFactor, heightScaleFactor);

            // Base font size of 14 with responsive scaling
            var fontSize = 14.0 * minScaleFactor * _currentSettings.ScaleFactor;

            // Clamp font size between reasonable bounds
            return Math.Max(10, Math.Min(18, fontSize));
        }

        /// <summary>
        /// Gets responsive font size based on style.
        /// </summary>
        public double GetResponsiveFontSize(FontSizeStyle style)
        {
            var baseSize = _currentSettings.BaseFontSize;
            return style switch
            {
                FontSizeStyle.Small => baseSize * 0.85,
                FontSizeStyle.Normal => baseSize,
                FontSizeStyle.Large => baseSize * 1.2,
                FontSizeStyle.ExtraLarge => baseSize * 1.5,
                FontSizeStyle.Title => baseSize * 2.0,
                _ => baseSize
            };
        }

        /// <summary>
        /// Gets responsive dimension based on device type.
        /// </summary>
        public double GetResponsiveDimension(double baselineValue)
        {
            return baselineValue * _currentSettings.ScaleFactor;
        }

        /// <summary>
        /// Gets responsive margin based on device type.
        /// </summary>
        public Thickness GetResponsiveMargin(double all = 1)
        {
            var margin = _currentSettings.Spacing * all;
            return new Thickness(margin);
        }

        /// <summary>
        /// Gets responsive margin with different values for each side.
        /// </summary>
        public Thickness GetResponsiveMargin(double horizontal = 1, double vertical = 1)
        {
            var h = _currentSettings.Spacing * horizontal;
            var v = _currentSettings.Spacing * vertical;
            return new Thickness(h, v, h, v);
        }

        /// <summary>
        /// Gets responsive padding.
        /// </summary>
        public Thickness GetResponsivePadding(double all = 1)
        {
            return GetResponsiveMargin(all);
        }

        /// <summary>
        /// Gets responsive padding with different values.
        /// </summary>
        public Thickness GetResponsivePadding(double horizontal = 1, double vertical = 1)
        {
            return GetResponsiveMargin(horizontal, vertical);
        }

        /// <summary>
        /// Gets responsive grid spacing.
        /// </summary>
        public double GetResponsiveGridSpacing()
        {
            return _currentSettings.Spacing * 2;
        }

        /// <summary>
        /// Gets responsive button height.
        /// </summary>
        public double GetResponsiveButtonHeight()
        {
            return _currentSettings.ButtonHeight;
        }

        /// <summary>
        /// Gets responsive button width.
        /// </summary>
        public double GetResponsiveButtonWidth()
        {
            return _currentSettings.ButtonWidth;
        }

        /// <summary>
        /// Gets responsive corner radius for modern design.
        /// </summary>
        public CornerRadius GetResponsiveCornerRadius(double multiplier = 1)
        {
            var radius = _currentSettings.BorderRadius * multiplier;
            return new CornerRadius(radius);
        }

        /// <summary>
        /// Gets the current responsive settings.
        /// </summary>
        public ResponsiveSettings CurrentSettings => _currentSettings;

        /// <summary>
        /// Gets the current device type.
        /// </summary>
        public DeviceType CurrentDeviceType => _currentSettings.Device;

        /// <summary>
        /// Gets the DPI scale factor.
        /// </summary>
        public double DPIScaleFactor => _currentSettings.ScaleFactor;

        /// <summary>
        /// Checks if the current device is a mobile device.
        /// </summary>
        public bool IsMobileDevice => _currentSettings.Device == DeviceType.Mobile || _currentSettings.Device == DeviceType.Tablet;

        /// <summary>
        /// Checks if the current device is in portrait orientation.
        /// </summary>
        public bool IsPortraitOrientation => _currentSettings.IsPortrait;

        /// <summary>
        /// Refreshes responsive settings in response to resolution changes.
        /// </summary>
        public void RefreshSettings()
        {
            var oldSettings = _currentSettings;
            Initialize();

            if (oldSettings.Device != _currentSettings.Device ||
                Math.Abs(oldSettings.DPI - _currentSettings.DPI) > 0.1)
            {
                OnSettingsChanged(new ResponsiveSettingsChangedEventArgs
                {
                    PreviousDeviceType = oldSettings.Device,
                    CurrentDeviceType = _currentSettings.Device,
                    PreviousDPI = oldSettings.DPI,
                    CurrentDPI = _currentSettings.DPI,
                    PreviousResolution = new Size(oldSettings.Width, oldSettings.Height),
                    CurrentResolution = new Size(_currentSettings.Width, _currentSettings.Height)
                });
            }
        }

        /// <summary>
        /// Manually sets a custom baseline for responsive calculations.
        /// </summary>
        public void SetCustomBaseline(double width, double height, double dpi = 96)
        {
            _baselineScreenWidth = width;
            _baselineScreenHeight = height;
            _baselineDPI = dpi;
            Initialize();
        }

        protected virtual void OnSettingsChanged(ResponsiveSettingsChangedEventArgs e)
        {
            SettingsChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Font size style enumeration for responsive typography.
    /// </summary>
    public enum FontSizeStyle
    {
        Small,
        Normal,
        Large,
        ExtraLarge,
        Title
    }

    /// <summary>
    /// Event args for responsive settings changes.
    /// </summary>
    public class ResponsiveSettingsChangedEventArgs : EventArgs
    {
        public ResponsiveTheme.DeviceType PreviousDeviceType { get; set; }
        public ResponsiveTheme.DeviceType CurrentDeviceType { get; set; }
        public double PreviousDPI { get; set; }
        public double CurrentDPI { get; set; }
        public Size PreviousResolution { get; set; }
        public Size CurrentResolution { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
