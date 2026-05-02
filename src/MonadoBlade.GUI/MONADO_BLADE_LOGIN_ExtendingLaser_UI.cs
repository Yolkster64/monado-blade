/// ============================================================================
/// MONADO BLADE - EXTENDING LASER BLADE LOGIN SCREEN
/// Premium login experience with extending blade, laser word effects, glow
/// ============================================================================

namespace MonadoBlade.GUI.LoginScreen
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    /// <summary>
    /// MONADO BLADE LOGIN SCREEN
    /// ════════════════════════════════════════════════════════════════════════
    /// 
    /// Premium authentication UI with:
    /// ✨ Extending laser blade (grows from center outward)
    /// 💫 Glowing laser word effects
    /// 🔐 Secure authentication fields
    /// 🎬 Smooth animations (5-10 seconds total)
    /// 🌈 Dynamic color changes (status feedback)
    /// 
    /// Phases:
    /// 1. Start (0s) - Blade center point appears
    /// 2. Extend (0-3s) - Blade extends from center outward with laser trail
    /// 3. Glow (3-5s) - Monado text appears with laser word glow
    /// 4. Login (5s+) - Full UI fades in, user can interact
    /// 
    /// Status Colors:
    /// - Ready: Cyan (#00C8FF)
    /// - Processing: Purple (#8B2AFF)
    /// - Error: Red (#FF0064)
    /// - Success: Green (#00FF96)
    /// </summary>
    public class MonadoBladeLoginScreen
    {
        // ════════════════════════════════════════════════════════════════════
        // PART 1: EXTENDING BLADE ANIMATION
        // ════════════════════════════════════════════════════════════════════

        public class ExtendingLaserBlade
        {
            /*
             * EXTENDING MONADO BLADE EFFECT
             * ════════════════════════════════════════════════════════════════
             * 
             * Visual:
             *     (3s)              (3-5s)           (5s+)
             *      •        →      ────────→        ────────
             *   center    laser trail    glow      full blade
             * 
             * Animation Details:
             * ─────────────────────────────────────────────────────────────
             * Phase 1 (0-0.5s): Blade core appears
             *   - Tiny point at center
             *   - Cyan glow (radius 10px)
             *   - 100 particle burst
             * 
             * Phase 2 (0.5-3s): Blade extends outward
             *   - Extends from center (X=400, Y=300) upward
             *   - Target length: 800px (to top of screen)
             *   - Laser trail: 200+ particles following line
             *   - Trail colors: Cyan → Electric Cyan → Violet
             *   - Trail glow: Intense (blur 20px)
             *   - Speed: Smooth acceleration (ease-out)
             * 
             * Phase 3 (3-5s): Blade glows and settles
             *   - Blade length: 800px (fixed)
             *   - Glow intensity: Pulsing (1.0x → 2.0x → 1.0x)
             *   - Pulse duration: 2 seconds
             *   - Particles: Spiral up/down blade
             *   - Hum sound (optional): Low frequency tone
             * 
             * Phase 4 (5s+): Ready for login
             *   - Blade stable at 800px
             *   - Subtle glow continues
             *   - Login UI fades in over 1 second
             *   - User can now interact
             */

            public class BladeProperties
            {
                public double CenterX { get; set; } = 400;        // Center of screen
                public double CenterY { get; set; } = 300;        // Middle height
                public double BladeLength { get; set; } = 0;      // Grows from 0 to 800px
                public double MaxBladeLength { get; set; } = 800; // Full extension
                public double BladeWidth { get; set; } = 40;      // Blade thickness
                public Color BladeColor { get; set; } = Color.FromRgb(0, 200, 255);     // Cyan
                public Color GlowColor { get; set; } = Color.FromRgb(0, 255, 200);      // Electric Cyan
                public double GlowIntensity { get; set; } = 1.0;  // Pulse intensity
                public double GlowRadius { get; set; } = 30;      // Blur radius
                public bool IsExtending { get; set; } = false;    // Animation state
                public bool IsGlowing { get; set; } = false;      // Glow state
            }

            public static void StartExtensionAnimation(BladeProperties blade)
            {
                /*
                 * TIMELINE:
                 * 
                 * 0-0.5s:  APPEARANCE
                 *          ├─ Blade core point appears
                 *          ├─ 100 particles burst outward
                 *          └─ Cyan glow (10px → 30px)
                 * 
                 * 0.5-3s:  EXTENSION
                 *          ├─ BladeLength: 0 → 800px (2.5s duration)
                 *          ├─ Easing: EaseOut (smooth deceleration)
                 *          ├─ 200+ trail particles follow blade edge
                 *          ├─ Trail colors: Cyan at start → Violet at end
                 *          ├─ Glow intensifies during extension
                 *          └─ Possible sound: Laser extension sound
                 * 
                 * 3-5s:    GLOW & SETTLE
                 *          ├─ Glow intensity: 1.0 → 2.0 → 1.0 (2s pulse)
                 *          ├─ Blade stable at 800px
                 *          ├─ Spiral particles on blade surface
                 *          └─ Hum sound (optional, continuous)
                 * 
                 * 5s+:     READY
                 *          ├─ Blade maintains stable glow
                 *          ├─ Text animation starts
                 *          └─ Login UI fades in
                 */

                // Phase 1: Appearance (0-0.5s)
                var appearanceAnimation = new DoubleAnimation
                {
                    From = 10,          // Glow starts at 10px
                    To = 30,            // Expands to 30px
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                // Phase 2: Extension (0.5-3s)
                var extensionAnimation = new DoubleAnimation
                {
                    From = 0,           // Starts at 0
                    To = 800,           // Extends to 800px
                    BeginTime = TimeSpan.FromMilliseconds(500),
                    Duration = TimeSpan.FromMilliseconds(2500),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                // Phase 3: Glow Pulse (3-5s)
                var glowPulseAnimation = new DoubleAnimation
                {
                    From = 1.0,         // Start at 1x
                    To = 2.0,           // Pulse to 2x
                    BeginTime = TimeSpan.FromMilliseconds(3000),
                    Duration = TimeSpan.FromMilliseconds(2000),
                    RepeatBehavior = new RepeatBehavior(1.0),  // One full pulse
                    AutoReverse = true  // Goes 1.0 → 2.0 → 1.0
                };
            }

            public class LaserTrailParticle
            {
                /*
                 * LASER TRAIL PARTICLES
                 * ────────────────────────────────────────────────────────
                 * 
                 * During extension (0.5-3s):
                 * - 200+ particles follow the blade edge
                 * - Start at blade center (Y=300)
                 * - Move upward following blade extension
                 * - Colors transition: Cyan → Electric Cyan → Violet
                 * - Size: 2-6px (random)
                 * - Lifetime: 1 second
                 * - Fade: Trails fade as they move
                 * - Glow: Each particle has glow effect (blur 10px)
                 * 
                 * Result: Visible laser trail as blade extends
                 */

                public double X { get; set; }           // Horizontal position
                public double Y { get; set; }           // Vertical position (moves up)
                public double TargetY { get; set; }     // Target Y on blade
                public double VelocityY { get; set; }   // Upward movement
                public int Size { get; set; }           // 2-6px
                public Color StartColor { get; set; } = Color.FromRgb(0, 200, 255);   // Cyan
                public Color EndColor { get; set; } = Color.FromRgb(138, 43, 226);    // Violet
                public double LifeTime { get; set; } = 1.0;
                public double ElapsedTime { get; set; } = 0;
                public double GlowRadius { get; set; } = 10;
            }

            public static List<LaserTrailParticle> GenerateTrailParticles(double bladeLength)
            {
                var particles = new List<LaserTrailParticle>();
                var random = new Random();

                // Generate 200+ particles along the blade extension
                for (int i = 0; i < 250; i++)
                {
                    particles.Add(new LaserTrailParticle
                    {
                        X = 400 + random.Next(-20, 20),     // Slight horizontal variance
                        Y = 300,                            // Start at center
                        TargetY = 300 - (bladeLength * (i / 250.0)), // Distributed along blade
                        VelocityY = -(300 + random.Next(100)),  // Move upward
                        Size = random.Next(2, 7),
                        LifeTime = 1.0 + (random.NextDouble() * 0.5)  // 1.0-1.5s lifetime
                    });
                }

                return particles;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // PART 2: LASER WORD EFFECTS
        // ════════════════════════════════════════════════════════════════════

        public class LaserWordEffect
        {
            /*
             * MONADO BLADE TEXT - LASER DRAWING EFFECT
             * ════════════════════════════════════════════════════════════════
             * 
             * Timeline: 3-5 seconds (after blade extends)
             * 
             * Effect: Each letter is "drawn" with laser glow
             * ────────────────────────────────────────────────────────────────
             * 
             * M O N A D O   B L A D E
             * ═ ═ ═ ═ ═ ═   ═ ═ ═ ═ ═
             * 
             * Each letter:
             * 1. Outlines draw (laser trail)
             * 2. Fills with glow
             * 3. Pulses with energy
             * 4. Stays glowing
             * 
             * Colors: Cyan (M,O,N) → Electric Cyan (A,D) → Violet (B,L,A,D,E)
             */

            public class LaserLetter
            {
                public char Character { get; set; }
                public double StartX { get; set; }
                public double StartY { get; set; }
                public double FontSize { get; set; } = 60;
                public string FontFamily { get; set; } = "Orbitron";
                public Color OutlineColor { get; set; } = Color.FromRgb(0, 200, 255);
                public Color FillColor { get; set; } = Color.FromRgb(0, 255, 200);
                public double GlowRadius { get; set; } = 15;
                public double GlowIntensity { get; set; } = 1.5;
                public double DrawProgress { get; set; } = 0;  // 0-1 (0=not started, 1=complete)
                public bool IsDrawn { get; set; } = false;
                public double DelayMs { get; set; }  // Staggered start times
            }

            public static List<LaserLetter> CreateMonadoBladeText()
            {
                var letters = new List<LaserLetter>();
                var text = "MONADO BLADE";
                double x = 150;
                var random = new Random();

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == ' ')
                    {
                        x += 40;  // Space between words
                        continue;
                    }

                    // Color progression: Cyan → Electric → Violet
                    Color color;
                    if (i < 6)
                        color = Color.FromRgb(0, 200, 255);      // Cyan (MONADO)
                    else if (i < 8)
                        color = Color.FromRgb(0, 255, 200);      // Electric (BL)
                    else
                        color = Color.FromRgb(138, 43, 226);     // Violet (ADE)

                    letters.Add(new LaserLetter
                    {
                        Character = text[i],
                        StartX = x,
                        StartY = 150,
                        OutlineColor = color,
                        FillColor = color,
                        DelayMs = (i * 150) + 3000  // Stagger 150ms apart, start at 3s
                    });

                    x += 50;  // Letter spacing
                }

                return letters;
            }

            public static void AnimateLaserLetters(List<LaserLetter> letters)
            {
                /*
                 * ANIMATION SEQUENCE:
                 * 
                 * For each letter (staggered):
                 * 1. Draw outline (300ms)
                 *    - StrokeDashOffset animation
                 *    - Cyan laser trail color
                 * 2. Fill glow (200ms)
                 *    - Opacity 0 → 1
                 *    - GlowRadius 5 → 20
                 * 3. Pulse (1000ms)
                 *    - GlowIntensity 1.0 → 2.0 → 1.0
                 *    - Repeat infinitely
                 * 
                 * Result: Laser-drawn text that glows
                 */

                foreach (var letter in letters)
                {
                    // Draw outline animation
                    var drawAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        BeginTime = TimeSpan.FromMilliseconds(letter.DelayMs),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };

                    // Glow fill animation (starts after draw)
                    var glowAnimation = new DoubleAnimation
                    {
                        From = 5,
                        To = 20,
                        BeginTime = TimeSpan.FromMilliseconds(letter.DelayMs + 300),
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    // Continuous pulse
                    var pulseAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 2.0,
                        BeginTime = TimeSpan.FromMilliseconds(letter.DelayMs + 500),
                        Duration = TimeSpan.FromMilliseconds(1000),
                        RepeatBehavior = RepeatBehavior.Forever,
                        AutoReverse = true
                    };
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // PART 3: LOGIN FIELDS & AUTHENTICATION
        // ════════════════════════════════════════════════════════════════════

        public class AuthenticationUI
        {
            /*
             * LOGIN SCREEN LAYOUT
             * ════════════════════════════════════════════════════════════════
             * 
             * (0-5s): Animation phase
             * ┌────────────────────────────────┐
             * │                                │
             * │        Extending Blade         │  (animation)
             * │        ────────────            │
             * │                                │
             * │      MONADO BLADE              │  (laser text)
             * │                                │
             * │   (fading in at 4s)            │
             * │                                │
             * └────────────────────────────────┘
             * 
             * (5s+): Interactive phase
             * ┌────────────────────────────────┐
             * │        ⚔ MONADO BLADE         │
             * │      SYSTEM AUTHENTICATION      │
             * │                                │
             * │  ☐ Username/Profile            │
             * │  ┌────────────────────────┐    │
             * │  │ admin@monado           │    │
             * │  └────────────────────────┘    │
             * │                                │
             * │  🔐 Password                   │
             * │  ┌────────────────────────┐    │
             * │  │ ••••••••••••••••••     │    │
             * │  └────────────────────────┘    │
             * │                                │
             * │  🔑 USB Key (Optional)         │
             * │  ┌────────────────────────┐    │
             * │  │ Insert USB Key...      │    │
             * │  └────────────────────────┘    │
             * │                                │
             * │  🟢 AUTHENTICATE               │
             * │                                │
             * │  Status: Ready (Cyan)          │
             * │                                │
             * └────────────────────────────────┘
             */

            public class LoginField
            {
                public string Label { get; set; }
                public string Placeholder { get; set; }
                public string Icon { get; set; }
                public bool IsPassword { get; set; } = false;
                public bool IsOptional { get; set; } = false;
                public string Value { get; set; } = "";
                public Color BorderColor { get; set; } = Color.FromRgb(0, 200, 255);  // Cyan
                public double BorderThickness { get; set; } = 2;
                public Color FocusColor { get; set; } = Color.FromRgb(0, 255, 200);   // Electric
                public string ValidationStatus { get; set; } = "empty";  // empty, valid, invalid
            }

            public class AuthenticationStatus
            {
                public enum Status
                {
                    Ready,          // Ready to authenticate (Cyan #00C8FF)
                    Processing,     // Checking credentials (Purple #8B2AFF)
                    ValidatingUSB,  // Checking USB key (Violet #A020F0)
                    Success,        // Authentication successful (Green #00FF96)
                    Error,          // Authentication failed (Red #FF0064)
                    Locked          // Too many attempts (Dark Red #990000)
                }

                public Status CurrentStatus { get; set; } = Status.Ready;
                public string Message { get; set; } = "Ready to authenticate";
                public Color StatusColor { get; set; } = Color.FromRgb(0, 200, 255);

                public static Color GetStatusColor(Status status) => status switch
                {
                    Status.Ready => Color.FromRgb(0, 200, 255),      // Cyan
                    Status.Processing => Color.FromRgb(138, 43, 226),  // Purple
                    Status.ValidatingUSB => Color.FromRgb(160, 32, 240),  // Violet
                    Status.Success => Color.FromRgb(0, 255, 150),    // Green
                    Status.Error => Color.FromRgb(255, 0, 100),      // Red
                    Status.Locked => Color.FromRgb(153, 0, 0),       // Dark Red
                    _ => Color.FromRgb(200, 200, 200)                // Gray
                };

                public static string GetStatusMessage(Status status) => status switch
                {
                    Status.Ready => "Ready to authenticate",
                    Status.Processing => "Verifying credentials...",
                    Status.ValidatingUSB => "Scanning USB key...",
                    Status.Success => "Authentication successful!",
                    Status.Error => "Invalid credentials. Try again.",
                    Status.Locked => "Account locked. Contact admin.",
                    _ => "Status unknown"
                };
            }

            public static List<LoginField> CreateLoginFields()
            {
                return new List<LoginField>
                {
                    new LoginField
                    {
                        Label = "Profile/Username",
                        Placeholder = "admin@monado",
                        Icon = "☐",
                        IsPassword = false,
                        IsOptional = false
                    },
                    new LoginField
                    {
                        Label = "Password",
                        Placeholder = "••••••••••••",
                        Icon = "🔐",
                        IsPassword = true,
                        IsOptional = false
                    },
                    new LoginField
                    {
                        Label = "USB Key (2FA)",
                        Placeholder = "Insert USB Key...",
                        Icon = "🔑",
                        IsPassword = false,
                        IsOptional = true
                    }
                };
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // PART 4: LOGIN SEQUENCE ORCHESTRATION
        // ════════════════════════════════════════════════════════════════════

        public class LoginSequenceController
        {
            /*
             * COMPLETE LOGIN ANIMATION TIMELINE
             * ════════════════════════════════════════════════════════════════
             * 
             * 0-0.5s:   BLADE APPEARANCE
             *           └─ Core point + burst (100 particles)
             * 
             * 0.5-3s:   BLADE EXTENSION
             *           └─ Laser extends upward (800px), trail particles
             * 
             * 3-5s:     TEXT ANIMATION
             *           ├─ "MONADO BLADE" laser-drawn (staggered letters)
             *           └─ Each letter glows with 1.5x intensity
             * 
             * 4-6s:     LOGIN UI FADE-IN
             *           ├─ Background fade in
             *           ├─ Input fields fade in
             *           ├─ Buttons fade in
             *           └─ Status text fades in
             * 
             * 6s+:      READY FOR INTERACTION
             *           ├─ Blade maintains stable glow
             *           ├─ Text continues subtle pulse
             *           ├─ User can click fields
             *           ├─ User can enter credentials
             *           └─ Status changes with input
             * 
             * On Authentication:
             *           ├─ Status changes (Processing → Validating → Success/Error)
             *           ├─ Color changes reflect status
             *           ├─ Particles react to status
             *           └─ On success → Transition to main dashboard
             */

            public void StartLoginAnimation()
            {
                // Phase 1: Blade appearance (0-0.5s)
                // Phase 2: Blade extension (0.5-3s)
                // Phase 3: Text animation (3-5s)
                // Phase 4: UI fade-in (4-6s)
            }

            public void HandleAuthentication(string username, string password, bool usbDetected)
            {
                // Validate credentials
                // If valid + USB detected → Success
                // If invalid → Error (with red flash)
                // If locked → Locked (dark red, disabled)
            }

            public void TransitionToDashboard()
            {
                // Blade + text fade out (1s)
                // Dashboard fades in
                // Simpler blade version appears in dashboard
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // ANIMATION HELPER CLASSES
    // ════════════════════════════════════════════════════════════════════

    public class BladeAnimationHelper
    {
        public static void ApplyGlowEffect(UIElement element, Color glowColor, double radius)
        {
            var blurEffect = new System.Windows.Media.Effects.BlurEffect
            {
                Radius = radius
            };
            element.Effect = blurEffect;
        }

        public static void PulseGlow(double from, double to, int durationMs)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };
        }

        public static Color InterpolateColor(Color from, Color to, double progress)
        {
            return Color.FromRgb(
                (byte)(from.R + (to.R - from.R) * progress),
                (byte)(from.G + (to.G - from.G) * progress),
                (byte)(from.B + (to.B - from.B) * progress)
            );
        }
    }
}
