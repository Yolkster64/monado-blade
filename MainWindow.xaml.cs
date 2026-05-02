using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MonadoBlade;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(IServiceProvider serviceProvider, ILogger<MainWindow> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        InitializeComponent();

        _logger.LogInformation("🖥️ MainWindow initialized");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("🖥️ MainWindow loaded - Monado Blade GUI ready");
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _logger.LogInformation("🖥️ MainWindow closing - Shutting down");
    }
}
