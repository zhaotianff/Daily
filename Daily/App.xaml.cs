using System;
using System.Globalization;
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
    private readonly ProgramCategoryService _categoryService = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Override the default English strings with Chinese if the system UI language is Chinese
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            var zhDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Resources/Strings.zh.xaml")
            };
            Resources.MergedDictionaries.Add(zhDict);
        }

        _statisticsService = new StatisticsService(Dispatcher, _categoryService);
        var dashboardVm = new DashboardViewModel(_statisticsService, _categoryService);
        var mainVm = new MainViewModel(_statisticsService);
        var historyVm = new HistoryViewModel(_statisticsService, _categoryService);
        var categoryEditorVm = new CategoryEditorViewModel(_categoryService);

        var window = new MainWindow(mainVm, dashboardVm, historyVm, categoryEditorVm);
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
