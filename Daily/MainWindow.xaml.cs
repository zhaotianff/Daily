using System.Windows;
using Daily.ViewModels;
using Daily.Views;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Daily;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _mainViewModel;

    public MainWindow(MainViewModel mainViewModel, DashboardViewModel dashboardViewModel)
    {
        _mainViewModel = mainViewModel;

        InitializeComponent();
        DataContext = mainViewModel;

        // Apply initial dark theme
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);

        // Set page service so NavigationView can create pages with dependencies
        RootNavigation.SetServiceProvider(BuildServiceProvider(dashboardViewModel));

        // Navigate to dashboard on startup
        Loaded += OnLoaded;
    }

    private static IServiceProvider BuildServiceProvider(DashboardViewModel dashboardViewModel)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dashboardViewModel);
        services.AddTransient<DashboardPage>();
        services.AddTransient<ChartsPage>();
        return services.BuildServiceProvider();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RootNavigation.Navigate(typeof(DashboardPage));
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.ToggleThemeCommand.Execute(null);

        ApplicationThemeManager.Apply(
            _mainViewModel.IsDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light,
            WindowBackdropType.Mica);

        // Update button icon
        if (ThemeToggleButton.Content is SymbolIcon icon)
            icon.Symbol = _mainViewModel.IsDarkTheme ? SymbolRegular.WeatherMoon24 : SymbolRegular.WeatherSunny24;
    }
}
