using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Linq;

namespace MonadoBlade.GUI.Themes
{
    /// <summary>
    /// Controls animated gradient effects including multi-layer gradients,
    /// directional and radial transitions with configurable timing.
    /// </summary>
    public class GradientController
    {
        private readonly List<GradientAnimation> _activeAnimations;
        private bool _enableGPUAcceleration;

        public class GradientAnimation
        {
            public string AnimationId { get; set; }
            public Storyboard Storyboard { get; set; }
            public DateTime StartTime { get; set; }
            public TimeSpan Duration { get; set; }
            public GradientBrush TargetBrush { get; set; }
            public bool IsPlaying { get; set; }
        }

        public GradientController(bool enableGPUAcceleration = true)
        {
            _activeAnimations = new List<GradientAnimation>();
            _enableGPUAcceleration = enableGPUAcceleration;
        }

        /// <summary>
        /// Creates a horizontal linear gradient with specified colors.
        /// </summary>
        public LinearGradientBrush CreateHorizontalGradient(params Color[] colors)
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint = new Point(1, 0.5);

            if (_enableGPUAcceleration)
            {
                brush.Freeze();
            }

            for (int i = 0; i < colors.Length; i++)
            {
                var offset = (double)i / (colors.Length - 1);
                brush.GradientStops.Add(new GradientStop(colors[i], offset));
            }

            return brush;
        }

        /// <summary>
        /// Creates a vertical linear gradient with specified colors.
        /// </summary>
        public LinearGradientBrush CreateVerticalGradient(params Color[] colors)
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0.5, 0);
            brush.EndPoint = new Point(0.5, 1);

            if (_enableGPUAcceleration)
            {
                brush.Freeze();
            }

            for (int i = 0; i < colors.Length; i++)
            {
                var offset = (double)i / (colors.Length - 1);
                brush.GradientStops.Add(new GradientStop(colors[i], offset));
            }

            return brush;
        }

        /// <summary>
        /// Creates a radial gradient for spotlight/vignette effects.
        /// </summary>
        public RadialGradientBrush CreateRadialGradient(Color center, Color outer, double radius = 0.5)
        {
            var brush = new RadialGradientBrush();
            brush.Center = new Point(0.5, 0.5);
            brush.RadiusX = radius;
            brush.RadiusY = radius;
            brush.GradientStops.Add(new GradientStop(center, 0.0));
            brush.GradientStops.Add(new GradientStop(outer, 1.0));

            if (_enableGPUAcceleration)
            {
                brush.Freeze();
            }

            return brush;
        }

        /// <summary>
        /// Creates a diagonal gradient at specified angle.
        /// </summary>
        public LinearGradientBrush CreateDiagonalGradient(Color startColor, Color endColor, double angleInDegrees = 45)
        {
            var angleInRadians = angleInDegrees * Math.PI / 180.0;
            var brush = new LinearGradientBrush();
            
            // Calculate start and end points based on angle
            var cos = Math.Cos(angleInRadians);
            var sin = Math.Sin(angleInRadians);

            brush.StartPoint = new Point(0.5 - cos / 2, 0.5 - sin / 2);
            brush.EndPoint = new Point(0.5 + cos / 2, 0.5 + sin / 2);

            brush.GradientStops.Add(new GradientStop(startColor, 0.0));
            brush.GradientStops.Add(new GradientStop(endColor, 1.0));

            if (_enableGPUAcceleration)
            {
                brush.Freeze();
            }

            return brush;
        }

        /// <summary>
        /// Animates a smooth transition between two gradient states.
        /// </summary>
        public string AnimateGradientTransition(
            LinearGradientBrush brush,
            Color[] targetColors,
            TimeSpan duration,
            string animationId = null,
            EventHandler completionCallback = null)
        {
            animationId = animationId ?? $"gradient_{Guid.NewGuid()}";

            var storyboard = new Storyboard();
            storyboard.Duration = duration;

            // Create animations for each gradient stop
            var stopCount = brush.GradientStops.Count;
            var targetStopCount = targetColors.Length;
            var stopCountToAnimate = Math.Min(stopCount, targetStopCount);

            for (int i = 0; i < stopCountToAnimate; i++)
            {
                var colorAnimation = new ColorAnimation(targetColors[i], duration);
                colorAnimation.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };

                Storyboard.SetTarget(colorAnimation, brush.GradientStops[i]);
                Storyboard.SetTargetProperty(colorAnimation, new PropertyPath(GradientStop.ColorProperty));

                storyboard.Children.Add(colorAnimation);
            }

            if (completionCallback != null)
            {
                storyboard.Completed += completionCallback;
            }

            var animation = new GradientAnimation
            {
                AnimationId = animationId,
                Storyboard = storyboard,
                StartTime = DateTime.Now,
                Duration = duration,
                TargetBrush = brush,
                IsPlaying = true
            };

            _activeAnimations.Add(animation);
            storyboard.Begin();

            return animationId;
        }

        /// <summary>
        /// Creates a pulsing gradient effect that cycles between colors.
        /// </summary>
        public string CreatePulsingGradient(
            LinearGradientBrush brush,
            Color color1,
            Color color2,
            TimeSpan cycleDuration,
            int cycleCount = -1)
        {
            var animationId = $"pulsing_{Guid.NewGuid()}";
            var storyboard = new Storyboard();

            if (brush.GradientStops.Count < 2)
            {
                brush.GradientStops.Add(new GradientStop(color1, 0.0));
                brush.GradientStops.Add(new GradientStop(color2, 1.0));
            }

            var halfDuration = TimeSpan.FromMilliseconds(cycleDuration.TotalMilliseconds / 2);

            // First half: color1 -> color2
            var animation1 = new ColorAnimation(color2, halfDuration);
            animation1.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            Storyboard.SetTarget(animation1, brush.GradientStops[0]);
            Storyboard.SetTargetProperty(animation1, new PropertyPath(GradientStop.ColorProperty));
            storyboard.Children.Add(animation1);

            // Second half: color2 -> color1
            var animation2 = new ColorAnimation(color1, halfDuration);
            animation2.BeginTime = halfDuration;
            animation2.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            Storyboard.SetTarget(animation2, brush.GradientStops[0]);
            Storyboard.SetTargetProperty(animation2, new PropertyPath(GradientStop.ColorProperty));
            storyboard.Children.Add(animation2);

            storyboard.Duration = cycleDuration;
            if (cycleCount > 0)
                storyboard.RepeatBehavior = new RepeatBehavior(cycleCount);
            else if (cycleCount < 0)
                storyboard.RepeatBehavior = RepeatBehavior.Forever;

            var animation = new GradientAnimation
            {
                AnimationId = animationId,
                Storyboard = storyboard,
                StartTime = DateTime.Now,
                Duration = cycleDuration,
                TargetBrush = brush,
                IsPlaying = true
            };

            _activeAnimations.Add(animation);
            storyboard.Begin();

            return animationId;
        }

        /// <summary>
        /// Creates a rotating gradient effect (useful for loading indicators).
        /// </summary>
        public string CreateRotatingGradient(
            LinearGradientBrush brush,
            TimeSpan rotationDuration,
            int rotations = 1)
        {
            var animationId = $"rotating_{Guid.NewGuid()}";
            var totalDuration = TimeSpan.FromMilliseconds(rotationDuration.TotalMilliseconds * rotations);

            var storyboard = new Storyboard();
            storyboard.Duration = totalDuration;

            // Animate the gradient start and end points in a circle
            var startXAnim = new DoubleAnimation(0, 1, rotationDuration);
            startXAnim.EasingFunction = new LinearEase();
            startXAnim.RepeatBehavior = new RepeatBehavior(rotations);

            Storyboard.SetTarget(startXAnim, brush);
            Storyboard.SetTargetProperty(startXAnim, new PropertyPath(LinearGradientBrush.StartPointProperty));
            storyboard.Children.Add(startXAnim);

            var animation = new GradientAnimation
            {
                AnimationId = animationId,
                Storyboard = storyboard,
                StartTime = DateTime.Now,
                Duration = totalDuration,
                TargetBrush = brush,
                IsPlaying = true
            };

            _activeAnimations.Add(animation);
            storyboard.Begin();

            return animationId;
        }

        /// <summary>
        /// Creates a multi-layer gradient effect with multiple gradient brushes.
        /// </summary>
        public DrawingBrush CreateMultiLayerGradient(
            params LinearGradientBrush[] layers)
        {
            var drawing = new DrawingBrush();
            var drawingGroup = new DrawingGroup();

            foreach (var layer in layers)
            {
                var geometry = new RectangleGeometry(new Rect(0, 0, 1, 1));
                var geometryDrawing = new GeometryDrawing(layer, null, geometry);
                drawingGroup.Children.Add(geometryDrawing);
            }

            drawing.Drawing = drawingGroup;
            return drawing;
        }

        /// <summary>
        /// Stops a specific gradient animation by ID.
        /// </summary>
        public bool StopAnimation(string animationId)
        {
            var animation = _activeAnimations.FirstOrDefault(a => a.AnimationId == animationId);
            if (animation != null)
            {
                animation.Storyboard.Stop();
                animation.IsPlaying = false;
                _activeAnimations.Remove(animation);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stops all active animations.
        /// </summary>
        public void StopAllAnimations()
        {
            foreach (var animation in _activeAnimations.ToList())
            {
                animation.Storyboard.Stop();
                animation.IsPlaying = false;
            }
            _activeAnimations.Clear();
        }

        /// <summary>
        /// Pauses a specific gradient animation.
        /// </summary>
        public bool PauseAnimation(string animationId)
        {
            var animation = _activeAnimations.FirstOrDefault(a => a.AnimationId == animationId);
            if (animation != null)
            {
                animation.Storyboard.Pause();
                animation.IsPlaying = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resumes a paused gradient animation.
        /// </summary>
        public bool ResumeAnimation(string animationId)
        {
            var animation = _activeAnimations.FirstOrDefault(a => a.AnimationId == animationId);
            if (animation != null)
            {
                animation.Storyboard.Resume();
                animation.IsPlaying = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the number of active animations.
        /// </summary>
        public int ActiveAnimationCount => _activeAnimations.Count;

        /// <summary>
        /// Enables or disables GPU acceleration.
        /// </summary>
        public void SetGPUAcceleration(bool enabled)
        {
            _enableGPUAcceleration = enabled;
        }

        /// <summary>
        /// Gets GPU acceleration status.
        /// </summary>
        public bool IsGPUAccelerationEnabled => _enableGPUAcceleration;

        /// <summary>
        /// Creates a gradient offset animation for sliding effects.
        /// </summary>
        public string CreateGradientOffsetAnimation(
            LinearGradientBrush brush,
            double offsetAmount,
            TimeSpan duration,
            int repeatCount = -1)
        {
            var animationId = $"offset_{Guid.NewGuid()}";
            var storyboard = new Storyboard();

            var offsetAnim = new DoubleAnimation(0, offsetAmount, duration);
            offsetAnim.EasingFunction = new LinearEase();
            if (repeatCount > 0)
                offsetAnim.RepeatBehavior = new RepeatBehavior(repeatCount);
            else if (repeatCount < 0)
                offsetAnim.RepeatBehavior = RepeatBehavior.Forever;

            Storyboard.SetTarget(offsetAnim, brush);
            Storyboard.SetTargetProperty(offsetAnim, new PropertyPath(LinearGradientBrush.EndPointProperty));
            storyboard.Children.Add(offsetAnim);

            var animation = new GradientAnimation
            {
                AnimationId = animationId,
                Storyboard = storyboard,
                StartTime = DateTime.Now,
                Duration = duration,
                TargetBrush = brush,
                IsPlaying = true
            };

            _activeAnimations.Add(animation);
            storyboard.Begin();

            return animationId;
        }
    }
}
