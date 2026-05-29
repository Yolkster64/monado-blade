using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace HELIOS.Platform.SystemIntegration
{
    /// <summary>
    /// Manages global hotkeys and keyboard shortcuts for Monado Blade.
    /// Supports registration, conflict detection, and customization.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 1;
        private const uint MOD_CTRL = 2;
        private const uint MOD_SHIFT = 4;
        private const uint MOD_WIN = 8;

        private readonly Dictionary<string, RegisteredHotkey> _registeredHotkeys;
        private readonly Dictionary<string, HotkeyBinding> _hotkeyBindings;
        private IntPtr _windowHandle;
        private bool _disposed;

        public event EventHandler<HotkeyEventArgs> HotkeyPressed;
        public event EventHandler<HotkeyConflictEventArgs> HotkeyConflictDetected;

        public HotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _registeredHotkeys = new Dictionary<string, RegisteredHotkey>();
            _hotkeyBindings = new Dictionary<string, HotkeyBinding>();
            InitializeDefaultHotkeys();
        }

        /// <summary>
        /// Initializes default hotkey bindings.
        /// </summary>
        private void InitializeDefaultHotkeys()
        {
            var defaultBindings = new[]
            {
                new HotkeyBinding 
                { 
                    Name = "ShowMainWindow", 
                    DisplayName = "Show Main Window",
                    Key = Key.M,
                    Modifiers = ModifierKeys.Alt | ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Window
                },
                new HotkeyBinding 
                { 
                    Name = "VolumeUp", 
                    DisplayName = "Volume Up",
                    Key = Key.OemPlus,
                    Modifiers = ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Audio
                },
                new HotkeyBinding 
                { 
                    Name = "VolumeDown", 
                    DisplayName = "Volume Down",
                    Key = Key.OemMinus,
                    Modifiers = ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Audio
                },
                new HotkeyBinding 
                { 
                    Name = "ToggleMute", 
                    DisplayName = "Toggle Mute",
                    Key = Key.M,
                    Modifiers = ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Audio
                },
                new HotkeyBinding 
                { 
                    Name = "PerformanceMonitor", 
                    DisplayName = "Open Performance Monitor",
                    Key = Key.P,
                    Modifiers = ModifierKeys.Alt | ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Application
                },
                new HotkeyBinding 
                { 
                    Name = "Settings", 
                    DisplayName = "Open Settings",
                    Key = Key.S,
                    Modifiers = ModifierKeys.Alt | ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Application
                },
                new HotkeyBinding 
                { 
                    Name = "ScreenCapture", 
                    DisplayName = "Screen Capture",
                    Key = Key.PrintScreen,
                    Modifiers = ModifierKeys.Control,
                    Enabled = true,
                    IsCustomizable = true,
                    Category = HotkeyCategory.Application
                }
            };

            foreach (var binding in defaultBindings)
            {
                _hotkeyBindings[binding.Name] = binding;
            }

            Debug.WriteLine($"[HotkeyManager] Initialized {defaultBindings.Length} default hotkey bindings");
        }

        /// <summary>
        /// Registers all enabled hotkeys with the system.
        /// </summary>
        public void RegisterAllHotkeys()
        {
            foreach (var binding in _hotkeyBindings.Values.Where(b => b.Enabled))
            {
                RegisterHotkey(binding.Name, binding);
            }
        }

        /// <summary>
        /// Registers a single hotkey.
        /// </summary>
        public bool RegisterHotkey(string name, HotkeyBinding binding)
        {
            try
            {
                // Check for conflicts
                if (DetectConflict(binding))
                {
                    HotkeyConflictDetected?.Invoke(this, new HotkeyConflictEventArgs 
                    { 
                        HotkeyName = name, 
                        Binding = binding,
                        Message = $"Hotkey {binding.DisplayName} conflicts with another application"
                    });
                    return false;
                }

                uint modifiers = ConvertModifiers(binding.Modifiers);
                uint vk = (uint)KeyInterop.VirtualKeyFromKey(binding.Key);

                int hotkeyId = binding.Name.GetHashCode();

                if (RegisterHotKey(_windowHandle, hotkeyId, modifiers, vk))
                {
                    _registeredHotkeys[name] = new RegisteredHotkey 
                    { 
                        Id = hotkeyId, 
                        Binding = binding 
                    };

                    Debug.WriteLine($"[HotkeyManager] Registered hotkey: {binding.DisplayName} ({modifiers}+{vk})");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[HotkeyManager] Failed to register hotkey: {binding.DisplayName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeyManager] Exception registering hotkey {name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unregisters a hotkey.
        /// </summary>
        public bool UnregisterHotkey(string name)
        {
            if (_registeredHotkeys.TryGetValue(name, out var hotkey))
            {
                if (UnregisterHotKey(_windowHandle, hotkey.Id))
                {
                    _registeredHotkeys.Remove(name);
                    Debug.WriteLine($"[HotkeyManager] Unregistered hotkey: {name}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handles hotkey press events from the system.
        /// </summary>
        public void HandleHotkeyPress(int hotkeyId)
        {
            var hotkey = _registeredHotkeys.Values.FirstOrDefault(h => h.Id == hotkeyId);
            if (hotkey != null)
            {
                Debug.WriteLine($"[HotkeyManager] Hotkey pressed: {hotkey.Binding.DisplayName}");
                HotkeyPressed?.Invoke(this, new HotkeyEventArgs 
                { 
                    Name = GetHotkeyNameById(hotkeyId),
                    Binding = hotkey.Binding
                });
            }
        }

        /// <summary>
        /// Detects conflicts with existing hotkeys.
        /// </summary>
        private bool DetectConflict(HotkeyBinding newBinding)
        {
            // Check against registered hotkeys
            foreach (var existing in _registeredHotkeys.Values)
            {
                if (existing.Binding.Key == newBinding.Key && 
                    existing.Binding.Modifiers == newBinding.Modifiers)
                {
                    return true;
                }
            }

            // Note: Complete system-wide conflict detection would require 
            // enumerating all registered hotkeys, which requires additional APIs
            return false;
        }

        /// <summary>
        /// Gets all hotkey bindings.
        /// </summary>
        public IEnumerable<HotkeyBinding> GetAllHotkeys()
        {
            return _hotkeyBindings.Values;
        }

        /// <summary>
        /// Updates a hotkey binding and re-registers it.
        /// </summary>
        public bool UpdateHotkey(string name, HotkeyBinding newBinding)
        {
            if (!_hotkeyBindings.ContainsKey(name))
                return false;

            UnregisterHotkey(name);
            _hotkeyBindings[name] = newBinding;
            return RegisterHotkey(name, newBinding);
        }

        /// <summary>
        /// Gets hotkeys by category.
        /// </summary>
        public IEnumerable<HotkeyBinding> GetHotkeysByCategory(HotkeyCategory category)
        {
            return _hotkeyBindings.Values.Where(h => h.Category == category);
        }

        private uint ConvertModifiers(ModifierKeys modifiers)
        {
            uint result = 0;
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                result |= MOD_ALT;
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                result |= MOD_CTRL;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                result |= MOD_SHIFT;
            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                result |= MOD_WIN;
            return result;
        }

        private string GetHotkeyNameById(int id)
        {
            return _registeredHotkeys.FirstOrDefault(h => h.Value.Id == id).Key ?? "Unknown";
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                foreach (var hotkey in _registeredHotkeys.Values)
                {
                    UnregisterHotKey(_windowHandle, hotkey.Id);
                }

                _registeredHotkeys.Clear();
                _disposed = true;
                Debug.WriteLine("[HotkeyManager] All hotkeys unregistered and disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeyManager] Error during disposal: {ex.Message}");
            }
        }
    }

    public class HotkeyBinding
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public bool Enabled { get; set; }
        public bool IsCustomizable { get; set; }
        public HotkeyCategory Category { get; set; }

        public override string ToString()
        {
            var modifierStrs = new List<string>();
            if ((Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                modifierStrs.Add("Ctrl");
            if ((Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                modifierStrs.Add("Alt");
            if ((Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                modifierStrs.Add("Shift");
            if ((Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                modifierStrs.Add("Win");

            return $"{string.Join("+", modifierStrs)}+{Key}";
        }
    }

    public enum HotkeyCategory
    {
        Window,
        Audio,
        Application,
        System,
        Custom
    }

    public class HotkeyEventArgs : EventArgs
    {
        public string Name { get; set; }
        public HotkeyBinding Binding { get; set; }
    }

    public class HotkeyConflictEventArgs : EventArgs
    {
        public string HotkeyName { get; set; }
        public HotkeyBinding Binding { get; set; }
        public string Message { get; set; }
    }

    internal class RegisteredHotkey
    {
        public int Id { get; set; }
        public HotkeyBinding Binding { get; set; }
    }
}
