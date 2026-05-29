using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// Advanced kanji animation controller implementing color-coded glowing effects,
    /// interaction sounds, and Xenoblade Chronicles-inspired visual polish.
    /// Provides per-kanji customized animations with smooth easing and particle integration.
    /// </summary>
    public class KanjiAnimationController
    {
        // Animation state
        private Dictionary<string, KanjiAnimationState> _kanjiStates;
        private AudioController _audioController;
        private BladeAnimationController _bladeController;

        // Animation configuration
        private const double HOVER_SCALE_TARGET = 1.15;
        private const double HOVER_GLOW_INCREASE = 0.3;
        private const int HOVER_ANIMATION_MS = 200;
        private const int ACTIVATION_ANIMATION_MS = 300;

        // Event system
        public event Action<string, double> OnKanjiScaleChanged;
        public event Action<string, double> OnKanjiGlowChanged;
        public event Action<string, Color> OnKanjiColorChanged;
        public event Action<string> OnKanjiActivated;
        public event Action<string> OnKanjiDeactivated;

        public KanjiAnimationController(AudioController audioController = null, BladeAnimationController bladeController = null)
        {
            _audioController = audioController;
            _bladeController = bladeController;
            InitializeKanjiStates();
        }

        /// <summary>
        /// Initialize animation states for all kanji types.
        /// </summary>
        private void InitializeKanjiStates()
        {
            _kanjiStates = new Dictionary<string, KanjiAnimationState>
            {
                { KanjiConstants.KANJI_POWER, CreateState(KanjiConstants.KANJI_POWER, BladeConstants.COLOR_MAGENTA) },
                { KanjiConstants.KANJI_SWORD, CreateState(KanjiConstants.KANJI_SWORD, BladeConstants.COLOR_GOLD) },
                { KanjiConstants.KANJI_LIGHT, CreateState(KanjiConstants.KANJI_LIGHT, BladeConstants.COLOR_LIGHT_BLUE) },
                { KanjiConstants.KANJI_FLOW, CreateState(KanjiConstants.KANJI_FLOW, BladeConstants.COLOR_GREEN) },
                { KanjiConstants.KANJI_SOUL, CreateState(KanjiConstants.KANJI_SOUL, BladeConstants.COLOR_PINK) },
                { KanjiConstants.KANJI_MACHINE, CreateState(KanjiConstants.KANJI_MACHINE, BladeConstants.COLOR_CYAN) }
            };
        }

        /// <summary>
        /// Create kanji animation state with proper initialization.
        /// </summary>
        private KanjiAnimationState CreateState(string kanjiId, Color baseColor)
        {
            return new KanjiAnimationState
            {
                KanjiId = kanjiId,
                CurrentScale = 1.0,
                CurrentGlow = 0.2,
                BaseColor = baseColor,
                CurrentColor = baseColor,
                IsHovered = false,
                IsActive = false
            };
        }

        /// <summary>
        /// Execute hover animation for kanji - scale increase and glow boost.
        /// Triggered on mouse enter for visual feedback.
        /// </summary>
        public void PlayHoverAnimation(string kanjiId)
        {
            if (!_kanjiStates.TryGetValue(kanjiId, out var state))
                return;

            state.IsHovered = true;

            // Scale animation: 1.0 → 1.15
            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = HOVER_SCALE_TARGET,
                Duration = new Duration(TimeSpan.FromMilliseconds(HOVER_ANIMATION_MS)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Glow animation: current → current + increase
            double targetGlow = Math.Min(state.CurrentGlow + HOVER_GLOW_INCREASE, 0.8);
            var glowAnimation = new DoubleAnimation
            {
                From = state.CurrentGlow,
                To = targetGlow,
                Duration = new Duration(TimeSpan.FromMilliseconds(HOVER_ANIMATION_MS)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            scaleAnimation.Completed += (s, e) =>
            {
                state.CurrentScale = HOVER_SCALE_TARGET;
                OnKanjiScaleChanged?.Invoke(kanjiId, HOVER_SCALE_TARGET);
            };

            glowAnimation.Completed += (s, e) =>
            {
                state.CurrentGlow = targetGlow;
                OnKanjiGlowChanged?.Invoke(kanjiId, targetGlow);
            };

            state.CurrentScale = 1.0;
            state.CurrentGlow = Math.Max(state.CurrentGlow - 0.05, 0.2);
        }

        /// <summary>
        /// Execute unhover animation for kanji - return to idle state.
        /// Triggered on mouse leave.
        /// </summary>
        public void PlayUnhoverAnimation(string kanjiId)
        {
            if (!_kanjiStates.TryGetValue(kanjiId, out var state))
                return;

            state.IsHovered = false;

            // Scale animation: 1.15 → 1.0
            var scaleAnimation = new DoubleAnimation
            {
                From = state.CurrentScale,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(HOVER_ANIMATION_MS)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Glow animation: current → 0.2 (idle)
            var glowAnimation = new DoubleAnimation
            {
                From = state.CurrentGlow,
                To = 0.2,
                Duration = new Duration(TimeSpan.FromMilliseconds(HOVER_ANIMATION_MS)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            scaleAnimation.Completed += (s, e) =>
            {
                state.CurrentScale = 1.0;
                OnKanjiScaleChanged?.Invoke(kanjiId, 1.0);
            };

            glowAnimation.Completed += (s, e) =>
            {
                state.CurrentGlow = 0.2;
                OnKanjiGlowChanged?.Invoke(kanjiId, 0.2);
            };

            state.CurrentScale = Math.Max(state.CurrentScale - 0.05, 1.0);
            state.CurrentGlow = Math.Max(state.CurrentGlow - 0.1, 0.2);
        }

        /// <summary>
        /// Execute activation animation for kanji - intense color shift and glow spike.
        /// Triggered on click or keyboard activation.
        /// </summary>
        public void PlayActivationAnimation(string kanjiId)
        {
            if (!_kanjiStates.TryGetValue(kanjiId, out var state))
                return;

            state.IsActive = true;

            // Color shift animation: base → white (intensity boost)
            var colorAnimation = new ColorAnimation
            {
                From = state.BaseColor,
                To = Colors.White,
                Duration = new Duration(TimeSpan.FromMilliseconds(ACTIVATION_ANIMATION_MS / 2)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                AutoReverse = true
            };

            // Glow spike animation: current → 1.0 (max)
            var glowAnimation = new DoubleAnimation
            {
                From = state.CurrentGlow,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(ACTIVATION_ANIMATION_MS / 2)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                AutoReverse = true
            };

            // Scale pulse: 1.15 → 1.3 → 1.15
            var scaleAnimation = new DoubleAnimation
            {
                From = state.CurrentScale,
                To = 1.3,
                Duration = new Duration(TimeSpan.FromMilliseconds(ACTIVATION_ANIMATION_MS / 2)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                AutoReverse = true
            };

            colorAnimation.Completed += (s, e) =>
            {
                state.CurrentColor = state.BaseColor;
                OnKanjiColorChanged?.Invoke(kanjiId, state.BaseColor);
                OnKanjiActivated?.Invoke(kanjiId);
            };

            glowAnimation.Completed += (s, e) =>
            {
                state.CurrentGlow = 0.4;
                OnKanjiGlowChanged?.Invoke(kanjiId, 0.4);
            };

            scaleAnimation.Completed += (s, e) =>
            {
                state.CurrentScale = 1.0;
                OnKanjiScaleChanged?.Invoke(kanjiId, 1.0);
                state.IsActive = false;
                OnKanjiDeactivated?.Invoke(kanjiId);
            };

            // Play kanji-specific sound
            PlayKanjiSound(kanjiId);

            // Update blade color to kanji color
            if (_bladeController != null)
            {
                _bladeController.PlayColorTransition(state.BaseColor, 200);
                _bladeController.PlayActivationPulse();
            }

            state.CurrentColor = state.BaseColor;
            state.CurrentScale = Math.Max(state.CurrentScale - 0.05, 1.0);
            state.CurrentGlow = Math.Min(state.CurrentGlow + 0.2, 0.8);
        }

        /// <summary>
        /// Play kanji-specific interaction sound.
        /// Each kanji has a unique frequency corresponding to its nature.
        /// </summary>
        private void PlayKanjiSound(string kanjiId)
        {
            if (_audioController == null)
                return;

            int frequency = kanjiId switch
            {
                KanjiConstants.KANJI_POWER => BladeConstants.FREQ_POWER,
                KanjiConstants.KANJI_SWORD => BladeConstants.FREQ_SWORD,
                KanjiConstants.KANJI_LIGHT => BladeConstants.FREQ_LIGHT,
                KanjiConstants.KANJI_FLOW => BladeConstants.FREQ_FLOW,
                KanjiConstants.KANJI_SOUL => BladeConstants.FREQ_SOUL,
                KanjiConstants.KANJI_MACHINE => BladeConstants.FREQ_MACHINE,
                _ => 440 // Default to A4
            };

            // Play tone at specific frequency
            _audioController.PlayTone(frequency, 200); // 200ms duration
        }

        /// <summary>
        /// Execute glow effect based on proximity to blade or user interaction.
        /// Increases glow when hovering near blade center.
        /// </summary>
        public void UpdateProximityGlow(string kanjiId, double proximityFactor)
        {
            if (!_kanjiStates.TryGetValue(kanjiId, out var state))
                return;

            // proximityFactor: 0.0 (far) to 1.0 (close)
            proximityFactor = Math.Clamp(proximityFactor, 0, 1);
            
            double targetGlow = 0.2 + (proximityFactor * 0.6); // 0.2 to 0.8 range
            
            state.CurrentGlow = targetGlow;
            OnKanjiGlowChanged?.Invoke(kanjiId, targetGlow);
        }

        /// <summary>
        /// Get all kanji animation states for debugging/monitoring.
        /// </summary>
        public IReadOnlyDictionary<string, KanjiAnimationState> GetAllStates()
        {
            return _kanjiStates;
        }

        /// <summary>
        /// Get specific kanji animation state.
        /// </summary>
        public KanjiAnimationState GetState(string kanjiId)
        {
            _kanjiStates.TryGetValue(kanjiId, out var state);
            return state;
        }

        /// <summary>
        /// Reset all kanji to idle state.
        /// </summary>
        public void ResetAllToIdle()
        {
            foreach (var kvp in _kanjiStates)
            {
                var state = kvp.Value;
                state.CurrentScale = 1.0;
                state.CurrentGlow = 0.2;
                state.CurrentColor = state.BaseColor;
                state.IsHovered = false;
                state.IsActive = false;

                OnKanjiScaleChanged?.Invoke(kvp.Key, 1.0);
                OnKanjiGlowChanged?.Invoke(kvp.Key, 0.2);
                OnKanjiColorChanged?.Invoke(kvp.Key, state.BaseColor);
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            _audioController = null;
            _bladeController = null;
            _kanjiStates?.Clear();
        }
    }

    /// <summary>
    /// Kanji animation state snapshot for individual kanji tracking.
    /// </summary>
    public class KanjiAnimationState
    {
        public string KanjiId { get; set; }
        public double CurrentScale { get; set; }
        public double CurrentGlow { get; set; }
        public Color BaseColor { get; set; }
        public Color CurrentColor { get; set; }
        public bool IsHovered { get; set; }
        public bool IsActive { get; set; }

        public override string ToString()
        {
            return $"KanjiState[{KanjiId}, Scale={CurrentScale:F2}, Glow={CurrentGlow:F2}, Color={CurrentColor}, Hovered={IsHovered}, Active={IsActive}]";
        }
    }
}
