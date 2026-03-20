using System.Windows;
using Daily.Services;
using Daily.ViewModels;
using Daily.Views;
using WpfApplication = System.Windows.Application;

namespace Daily;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    private StatisticsService? _statisticsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _statisticsService = new StatisticsService(Dispatcher);
        var dashboardVm = new DashboardViewModel(_statisticsService);
        var mainVm = new MainViewModel(_statisticsService);
        var historyVm = new HistoryViewModel(_statisticsService);

        var window = new MainWindow(mainVm, dashboardVm, historyVm);
        MainWindow = window;
        window.Show();

        _statisticsService.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _statisticsService?.Stop();
        _statisticsService?.Dispose();
        base.OnExit(e);
    }
}
