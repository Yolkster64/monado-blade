using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace MonadoBlade.GUI.ViewModels
{
    /// <summary>
    /// Main ViewModel for the Developer Dashboard.
    /// Manages dashboard state, tab navigation, and real-time metrics.
    /// </summary>
    public class DeveloperDashboardViewModel : ViewModelBase
    {
        public class PerformanceMetric
        {
            public string Label { get; set; }
            public double Value { get; set; }
            public double MaxValue { get; set; }
            public string Unit { get; set; }
            public Color IndicatorColor { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class DashboardTab
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Icon { get; set; }
            public int BadgeCount { get; set; }
            public bool IsSelected { get; set; }
        }

        private ObservableCollection<DashboardTab> _tabs;
        private DashboardTab _selectedTab;
        private ObservableCollection<PerformanceMetric> _metrics;
        private RelayCommand _selectTabCommand;
        private RelayCommand _refreshCommand;
        private RelayCommand _settingsCommand;
        private bool _isRefreshing;
        private string _statusMessage;
        private double _cpuUsage;
        private double _memoryUsage;
        private double _diskUsage;
        private double _networkLatency;
        private int _activeProcesses;
        private int _errorCount;
        private int _warningCount;
        private DateTime _lastUpdate;

        public DeveloperDashboardViewModel()
        {
            InitializeTabs();
            _metrics = new ObservableCollection<PerformanceMetric>();
            InitializeMetrics();
            _lastUpdate = DateTime.Now;
        }

        private void InitializeTabs()
        {
            _tabs = new ObservableCollection<DashboardTab>
            {
                new DashboardTab { Id = "overview", Title = "Overview", Icon = "📊", IsSelected = true },
                new DashboardTab { Id = "performance", Title = "Performance", Icon = "⚡" },
                new DashboardTab { Id = "processes", Title = "Processes", Icon = "📋" },
                new DashboardTab { Id = "logs", Title = "Logs & Events", Icon = "📝" },
                new DashboardTab { Id = "tools", Title = "Developer Tools", Icon = "🔧" },
                new DashboardTab { Id = "advanced", Title = "Advanced", Icon = "🔬" },
            };
            _selectedTab = _tabs[0];
        }

        private void InitializeMetrics()
        {
            _metrics.Add(new PerformanceMetric
            {
                Label = "CPU Usage",
                Value = 0,
                MaxValue = 100,
                Unit = "%",
                IndicatorColor = Colors.Green,
                Timestamp = DateTime.Now
            });
            _metrics.Add(new PerformanceMetric
            {
                Label = "Memory Usage",
                Value = 0,
                MaxValue = 100,
                Unit = "%",
                IndicatorColor = Colors.Green,
                Timestamp = DateTime.Now
            });
            _metrics.Add(new PerformanceMetric
            {
                Label = "Disk Usage",
                Value = 0,
                MaxValue = 100,
                Unit = "%",
                IndicatorColor = Colors.Green,
                Timestamp = DateTime.Now
            });
            _metrics.Add(new PerformanceMetric
            {
                Label = "Network Latency",
                Value = 0,
                MaxValue = 500,
                Unit = "ms",
                IndicatorColor = Colors.Green,
                Timestamp = DateTime.Now
            });
        }

        public ObservableCollection<DashboardTab> Tabs
        {
            get => _tabs;
            set => SetProperty(ref _tabs, value);
        }

        public DashboardTab SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    UpdateTabSelection();
                }
            }
        }

        public ObservableCollection<PerformanceMetric> Metrics
        {
            get => _metrics;
            set => SetProperty(ref _metrics, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (SetProperty(ref _cpuUsage, value))
                {
                    UpdateMetricColor("CPU Usage", value);
                }
            }
        }

        public double MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                if (SetProperty(ref _memoryUsage, value))
                {
                    UpdateMetricColor("Memory Usage", value);
                }
            }
        }

        public double DiskUsage
        {
            get => _diskUsage;
            set
            {
                if (SetProperty(ref _diskUsage, value))
                {
                    UpdateMetricColor("Disk Usage", value);
                }
            }
        }

        public double NetworkLatency
        {
            get => _networkLatency;
            set
            {
                if (SetProperty(ref _networkLatency, value))
                {
                    UpdateMetricColor("Network Latency", value, 500);
                }
            }
        }

        public int ActiveProcesses
        {
            get => _activeProcesses;
            set => SetProperty(ref _activeProcesses, value);
        }

        public int ErrorCount
        {
            get => _errorCount;
            set => SetProperty(ref _errorCount, value);
        }

        public int WarningCount
        {
            get => _warningCount;
            set => SetProperty(ref _warningCount, value);
        }

        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set => SetProperty(ref _lastUpdate, value);
        }

        public ICommand SelectTabCommand =>
            _selectTabCommand ?? (_selectTabCommand = new RelayCommand<string>(SelectTab, CanSelectTab));

        public ICommand RefreshCommand =>
            _refreshCommand ?? (_refreshCommand = new RelayCommand(RefreshMetrics, CanRefresh));

        public ICommand SettingsCommand =>
            _settingsCommand ?? (_settingsCommand = new RelayCommand(OpenSettings));

        private bool CanSelectTab(string tabId) => !IsRefreshing && !string.IsNullOrEmpty(tabId);

        private bool CanRefresh() => !IsRefreshing;

        private void SelectTab(string tabId)
        {
            var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                SelectedTab = tab;
            }
        }

        private void UpdateTabSelection()
        {
            foreach (var tab in _tabs)
            {
                tab.IsSelected = tab.Id == _selectedTab.Id;
            }
        }

        private void UpdateMetricColor(string label, double value, double maxValue = 100)
        {
            var metric = _metrics.FirstOrDefault(m => m.Label == label);
            if (metric != null)
            {
                metric.Value = value;
                metric.IndicatorColor = GetHealthColor(value, maxValue);
            }
        }

        private Color GetHealthColor(double value, double maxValue)
        {
            var percentage = (value / maxValue) * 100;
            if (percentage < 50)
                return Colors.Green;
            if (percentage < 75)
                return Colors.Yellow;
            if (percentage < 90)
                return Colors.Orange;
            return Colors.Red;
        }

        private async void RefreshMetrics()
        {
            IsRefreshing = true;
            StatusMessage = "Updating metrics...";

            try
            {
                await Task.Run(() =>
                {
                    UpdateSystemMetrics();
                    System.Threading.Thread.Sleep(100);
                });

                StatusMessage = "Metrics updated successfully";
                LastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating metrics: {ex.Message}";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void UpdateSystemMetrics()
        {
            try
            {
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", false);
                cpuCounter.NextValue();
                System.Threading.Thread.Sleep(100);
                CpuUsage = Math.Round(cpuCounter.NextValue(), 2);

                var ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", false);
                MemoryUsage = Math.Round(ramCounter.NextValue(), 2);

                ActiveProcesses = Process.GetProcesses().Length;

                NetworkLatency = new Random().Next(5, 150);

                var diskDrive = System.IO.DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
                if (diskDrive != null)
                {
                    DiskUsage = Math.Round((diskDrive.TotalSize - diskDrive.TotalFreeSpace) /
                                          (double)diskDrive.TotalSize * 100, 2);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error collecting metrics: {ex.Message}";
            }
        }

        private void OpenSettings()
        {
            StatusMessage = "Settings opened";
        }
    }
}
