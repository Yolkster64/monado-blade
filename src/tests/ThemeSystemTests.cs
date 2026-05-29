using System;
using System.Collections.Generic;
using System.Windows.Media;
using Xunit;
using MonadoBlade.GUI.Themes;

namespace MonadoBlade.GUI.Tests
{
    public class ThemeSystemTests
    {
        [Fact]
        public void TimeAwareTheme_InitializeDefaultThemes()
        {
            var theme = new TimeAwareTheme();
            Assert.NotNull(theme.AllThemes);
            Assert.True(theme.AllThemes.Count >= 5);
        }

        [Fact]
        public void TimeAwareTheme_GetThemeForTime()
        {
            var theme = new TimeAwareTheme();
            var daytimeTheme = theme.GetThemeForTime(TimeSpan.FromHours(12));
            Assert.NotNull(daytimeTheme);
            Assert.Equal("Daytime", daytimeTheme.ThemeName);
        }

        [Fact]
        public void TimeAwareTheme_InterpolateColors()
        {
            var theme = new TimeAwareTheme();
            var colors = theme.GetInterpolatedColors(TimeSpan.FromHours(12));
            Assert.NotNull(colors.primary);
            Assert.NotNull(colors.secondary);
            Assert.NotNull(colors.background);
        }

        [Fact]
        public void TimeAwareTheme_SeasonalVariation()
        {
            var theme = new TimeAwareTheme();
            var currentTheme = theme.CurrentTheme;
            theme.ApplySeasonalVariation(currentTheme);
            Assert.True(currentTheme.Brightness >= 0.3 && currentTheme.Brightness <= 1.0);
        }

        [Fact]
        public void DynamicBackgroundController_GenerateCloudEffect()
        {
            var controller = new DynamicBackgroundController();
            var cloudEffect = controller.GenerateCloudEffect(
                800, 600,
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(0, 150, 200)
            );
            Assert.NotNull(cloudEffect);
        }

        [Fact]
        public void DynamicBackgroundController_CreateAnimatedGradient()
        {
            var controller = new DynamicBackgroundController();
            var gradient = controller.CreateAnimatedGradient(
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(10, 20, 40),
                new System.Windows.Duration(TimeSpan.FromSeconds(1))
            );
            Assert.NotNull(gradient);
            Assert.Equal(2, gradient.GradientStops.Count);
        }

        [Fact]
        public void DynamicBackgroundController_CreateRadialGradient()
        {
            var controller = new DynamicBackgroundController();
            var gradient = controller.CreateRadialGradient(
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(10, 20, 40)
            );
            Assert.NotNull(gradient);
        }

        [Fact]
        public void DynamicBackgroundController_GenerateParticleEffect()
        {
            var controller = new DynamicBackgroundController();
            var particles = controller.GenerateParticleEffect(
                50,
                Color.FromRgb(0, 217, 255),
                800, 600
            );
            Assert.NotNull(particles);
        }

        [Fact]
        public void GradientController_CreateHorizontalGradient()
        {
            var controller = new GradientController();
            var gradient = controller.CreateHorizontalGradient(
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(255, 184, 0)
            );
            Assert.NotNull(gradient);
            Assert.Equal(2, gradient.GradientStops.Count);
        }

        [Fact]
        public void GradientController_CreateVerticalGradient()
        {
            var controller = new GradientController();
            var gradient = controller.CreateVerticalGradient(
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(255, 184, 0)
            );
            Assert.NotNull(gradient);
            Assert.Equal(2, gradient.GradientStops.Count);
        }

        [Fact]
        public void GradientController_CreateDiagonalGradient()
        {
            var controller = new GradientController();
            var gradient = controller.CreateDiagonalGradient(
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(255, 184, 0),
                45
            );
            Assert.NotNull(gradient);
        }

        [Fact]
        public void GradientController_CreateRadialGradient()
        {
            var controller = new GradientController();
            var gradient = controller.CreateRadialGradient(
                Color.FromRgb(0, 217, 255),
                Color.FromRgb(10, 20, 40)
            );
            Assert.NotNull(gradient);
        }

        [Fact]
        public void SeasonalTheme_UpdateActiveOverlays()
        {
            var theme = new SeasonalTheme();
            theme.UpdateActiveOverlays();
            // Should not throw
        }

        [Fact]
        public void SeasonalTheme_ActivatePredefinedEvent()
        {
            var theme = new SeasonalTheme();
            var result = theme.ActivatePredefinedEvent("Christmas");
            Assert.True(result);
        }

        [Fact]
        public void SeasonalTheme_GetEventOverlay()
        {
            var theme = new SeasonalTheme();
            var overlay = theme.GetEventOverlay("Christmas");
            Assert.NotNull(overlay);
        }

        [Fact]
        public void SeasonalTheme_BlendOverlayColor()
        {
            var theme = new SeasonalTheme();
            var baseColor = Color.FromRgb(0, 217, 255);
            var overlayColor = Color.FromRgb(255, 0, 0);
            var blended = theme.BlendOverlayColor(baseColor, overlayColor, 0.5);
            Assert.NotNull(blended);
        }

        [Fact]
        public void ResponsiveTheme_Initialize()
        {
            var theme = new ResponsiveTheme();
            Assert.NotNull(theme.CurrentSettings);
            Assert.True(theme.CurrentSettings.Width > 0);
            Assert.True(theme.CurrentSettings.Height > 0);
        }

        [Fact]
        public void ResponsiveTheme_GetResponsiveFontSize()
        {
            var theme = new ResponsiveTheme();
            var smallFont = theme.GetResponsiveFontSize(FontSizeStyle.Small);
            var normalFont = theme.GetResponsiveFontSize(FontSizeStyle.Normal);
            var largeFont = theme.GetResponsiveFontSize(FontSizeStyle.Large);
            
            Assert.True(smallFont < normalFont);
            Assert.True(normalFont < largeFont);
        }

        [Fact]
        public void ResponsiveTheme_GetResponsiveMargin()
        {
            var theme = new ResponsiveTheme();
            var margin = theme.GetResponsiveMargin(1.0);
            Assert.NotNull(margin);
            Assert.True(margin.Left > 0);
        }

        [Fact]
        public void ResponsiveTheme_DPIScaleFactor()
        {
            var theme = new ResponsiveTheme();
            Assert.True(theme.DPIScaleFactor > 0);
            Assert.True(theme.DPIScaleFactor >= 0.5 && theme.DPIScaleFactor <= 5.0);
        }

        [Fact]
        public void TimeAwareTheme_UpdateTheme()
        {
            var theme = new TimeAwareTheme();
            theme.UpdateTheme();
            Assert.NotNull(theme.CurrentTheme);
        }

        [Fact]
        public void TimeAwareTheme_SetTransitionSmoothness()
        {
            var theme = new TimeAwareTheme();
            theme.SetTransitionSmoothness(TimeSpan.FromSeconds(2));
            // Should not throw
        }

        [Fact]
        public void DynamicBackgroundController_CreateMultiStopGradient()
        {
            var controller = new DynamicBackgroundController();
            var stops = new List<(Color, double)>
            {
                (Color.FromRgb(0, 217, 255), 0.0),
                (Color.FromRgb(255, 184, 0), 0.5),
                (Color.FromRgb(10, 20, 40), 1.0)
            };
            var gradient = controller.CreateMultiStopGradient(stops);
            Assert.NotNull(gradient);
            Assert.Equal(3, gradient.GradientStops.Count);
        }

        [Fact]
        public void GradientController_GPUAccelerationToggle()
        {
            var controller = new GradientController(true);
            Assert.True(controller.IsGPUAccelerationEnabled);
            
            controller.SetGPUAcceleration(false);
            Assert.False(controller.IsGPUAccelerationEnabled);
        }

        [Fact]
        public void SeasonalTheme_IsEventThemeActive()
        {
            var theme = new SeasonalTheme();
            Assert.False(theme.IsEventThemeActive);
        }

        [Fact]
        public void ResponsiveTheme_GetResponsiveCornerRadius()
        {
            var theme = new ResponsiveTheme();
            var radius = theme.GetResponsiveCornerRadius(1.0);
            Assert.NotNull(radius);
        }

        [Fact]
        public void TimeAwareTheme_AddCustomTheme()
        {
            var theme = new TimeAwareTheme();
            var initialCount = theme.AllThemes.Count;
            
            var customTheme = new TimeAwareTheme.TimeThemeDefinition
            {
                ThemeName = "CustomTest",
                StartTime = TimeSpan.FromHours(20),
                EndTime = TimeSpan.FromHours(21),
                PrimaryColor = Color.FromRgb(255, 0, 0),
                SecondaryColor = Color.FromRgb(0, 255, 0),
                BackgroundColor = Color.FromRgb(0, 0, 255),
                Brightness = 0.8,
                Saturation = 0.9
            };
            
            theme.AddCustomTheme(customTheme);
            Assert.Equal(initialCount + 1, theme.AllThemes.Count);
        }
    }
}
