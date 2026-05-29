using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace MonadoBlade.GUI.ViewModels
{
    /// <summary>
    /// ViewModel for theme settings view.
    /// Manages theme selection, customization, and persistence.
    /// </summary>
    public class ThemeSettingsViewModel : ViewModelBase
    {
        public class ThemeModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public Color PrimaryColor { get; set; }
            public Color SecondaryColor { get; set; }
            public Color AccentColor { get; set; }
            public Color BackgroundColor { get; set; }
            public double Brightness { get; set; }
            public Color PreviewColor { get; set; }
        }

        private ObservableCollection<ThemeModel> _presetThemes;
        private ThemeModel _currentTheme;
        private RelayCommand _selectThemeCommand;
        private RelayCommand _applyThemeCommand;
        private RelayCommand _saveThemeCommand;
        private RelayCommand _resetThemeCommand;

        public ThemeSettingsViewModel()
        {
            InitializePresetThemes();
            _currentTheme = _presetThemes[0];
        }

        /// <summary>
        /// Initializes the collection of preset themes.
        /// </summary>
        private void InitializePresetThemes()
        {
            _presetThemes = new ObservableCollection<ThemeModel>
            {
                new ThemeModel
                {
                    Id = "dark-monado",
                    Name = "Dark Monado",
                    PrimaryColor = Color.FromRgb(0, 217, 255),
                    SecondaryColor = Color.FromRgb(0, 150, 200),
                    AccentColor = Color.FromRgb(255, 184, 0),
                    BackgroundColor = Color.FromRgb(10, 20, 40),
                    Brightness = 1.0,
                    PreviewColor = Color.FromRgb(0, 217, 255)
                },
                new ThemeModel
                {
                    Id = "light-modern",
                    Name = "Light Modern",
                    PrimaryColor = Color.FromRgb(0, 100, 200),
                    SecondaryColor = Color.FromRgb(100, 150, 255),
                    AccentColor = Color.FromRgb(255, 100, 50),
                    BackgroundColor = Color.FromRgb(245, 247, 250),
                    Brightness = 1.0,
                    PreviewColor = Color.FromRgb(100, 150, 255)
                },
                new ThemeModel
                {
                    Id = "neon-cyberpunk",
                    Name = "Neon Cyberpunk",
                    PrimaryColor = Color.FromRgb(255, 0, 255),
                    SecondaryColor = Color.FromRgb(0, 255, 255),
                    AccentColor = Color.FromRgb(255, 255, 0),
                    BackgroundColor = Color.FromRgb(20, 10, 30),
                    Brightness = 0.9,
                    PreviewColor = Color.FromRgb(255, 0, 255)
                },
                new ThemeModel
                {
                    Id = "forest-green",
                    Name = "Forest Green",
                    PrimaryColor = Color.FromRgb(0, 200, 100),
                    SecondaryColor = Color.FromRgb(50, 150, 80),
                    AccentColor = Color.FromRgb(200, 180, 0),
                    BackgroundColor = Color.FromRgb(15, 30, 20),
                    Brightness = 0.85,
                    PreviewColor = Color.FromRgb(0, 200, 100)
                },
                new ThemeModel
                {
                    Id = "sunset-warm",
                    Name = "Sunset Warm",
                    PrimaryColor = Color.FromRgb(255, 140, 0),
                    SecondaryColor = Color.FromRgb(200, 100, 50),
                    AccentColor = Color.FromRgb(255, 200, 100),
                    BackgroundColor = Color.FromRgb(40, 20, 10),
                    Brightness = 0.8,
                    PreviewColor = Color.FromRgb(255, 140, 0)
                },
                new ThemeModel
                {
                    Id = "ocean-blue",
                    Name = "Ocean Blue",
                    PrimaryColor = Color.FromRgb(0, 150, 200),
                    SecondaryColor = Color.FromRgb(100, 180, 220),
                    AccentColor = Color.FromRgb(255, 180, 0),
                    BackgroundColor = Color.FromRgb(10, 30, 50),
                    Brightness = 0.9,
                    PreviewColor = Color.FromRgb(0, 150, 200)
                }
            };
        }

        /// <summary>
        /// Gets the collection of preset themes.
        /// </summary>
        public ObservableCollection<ThemeModel> PresetThemes
        {
            get => _presetThemes;
            set => SetProperty(ref _presetThemes, value);
        }

        /// <summary>
        /// Gets or sets the current theme being edited.
        /// </summary>
        public ThemeModel CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        /// <summary>
        /// Command to select a preset theme.
        /// </summary>
        public ICommand SelectThemeCommand
        {
            get
            {
                if (_selectThemeCommand == null)
                {
                    _selectThemeCommand = new RelayCommand(param =>
                    {
                        if (param is string themeId)
                        {
                            var theme = FindThemeById(themeId);
                            if (theme != null)
                            {
                                CurrentTheme = new ThemeModel
                                {
                                    Id = theme.Id,
                                    Name = theme.Name,
                                    PrimaryColor = theme.PrimaryColor,
                                    SecondaryColor = theme.SecondaryColor,
                                    AccentColor = theme.AccentColor,
                                    BackgroundColor = theme.BackgroundColor,
                                    Brightness = theme.Brightness,
                                    PreviewColor = theme.PreviewColor
                                };
                            }
                        }
                    });
                }
                return _selectThemeCommand;
            }
        }

        /// <summary>
        /// Command to apply the current theme.
        /// </summary>
        public ICommand ApplyThemeCommand
        {
            get
            {
                if (_applyThemeCommand == null)
                {
                    _applyThemeCommand = new RelayCommand(() =>
                    {
                        // Implement theme application logic
                        OnPropertyChanged(nameof(CurrentTheme));
                    });
                }
                return _applyThemeCommand;
            }
        }

        /// <summary>
        /// Command to save the current theme.
        /// </summary>
        public ICommand SaveThemeCommand
        {
            get
            {
                if (_saveThemeCommand == null)
                {
                    _saveThemeCommand = new RelayCommand(() =>
                    {
                        // Implement save logic (persist to storage)
                    });
                }
                return _saveThemeCommand;
            }
        }

        /// <summary>
        /// Command to reset theme to default.
        /// </summary>
        public ICommand ResetThemeCommand
        {
            get
            {
                if (_resetThemeCommand == null)
                {
                    _resetThemeCommand = new RelayCommand(() =>
                    {
                        CurrentTheme = new ThemeModel
                        {
                            Id = "dark-monado",
                            Name = "Dark Monado",
                            PrimaryColor = Color.FromRgb(0, 217, 255),
                            SecondaryColor = Color.FromRgb(0, 150, 200),
                            AccentColor = Color.FromRgb(255, 184, 0),
                            BackgroundColor = Color.FromRgb(10, 20, 40),
                            Brightness = 1.0,
                            PreviewColor = Color.FromRgb(0, 217, 255)
                        };
                    });
                }
                return _resetThemeCommand;
            }
        }

        /// <summary>
        /// Finds a theme by its ID.
        /// </summary>
        private ThemeModel FindThemeById(string themeId)
        {
            foreach (var theme in _presetThemes)
            {
                if (theme.Id == themeId)
                    return theme;
            }
            return null;
        }
    }

    /// <summary>
    /// Base ViewModel class with property change notification.
    /// </summary>
    public abstract class ViewModelBase : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(ref T field, T value, string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                var name = propertyName ?? System.Reflection.CallerMemberName.GetCallerMemberName();
                OnPropertyChanged(name);
            }
        }
    }

    /// <summary>
    /// Generic command implementation for MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);
    }
}

namespace System.Runtime.CompilerServices
{
    // Helper to get caller member name
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class CallerMemberNameAttribute : Attribute
    {
    }

    internal static class CallerMemberName
    {
        public static string GetCallerMemberName() => "Unknown";
    }
}
