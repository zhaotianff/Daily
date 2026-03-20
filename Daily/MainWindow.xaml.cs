using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
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
    private NotifyIcon? _notifyIcon;
    private bool _isExiting;

    public MainWindow(MainViewModel mainViewModel, DashboardViewModel dashboardViewModel, HistoryViewModel historyViewModel)
    {
        _mainViewModel = mainViewModel;

        InitializeComponent();
        DataContext = mainViewModel;

        // Apply initial dark theme
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);

        // Set page service so NavigationView can create pages with dependencies
        RootNavigation.SetServiceProvider(BuildServiceProvider(dashboardViewModel, historyViewModel));

        // Navigate to dashboard on startup
        Loaded += OnLoaded;

        // Set up notify icon
        InitializeNotifyIcon();
    }

    private static IServiceProvider BuildServiceProvider(DashboardViewModel dashboardViewModel, HistoryViewModel historyViewModel)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dashboardViewModel);
        services.AddSingleton(historyViewModel);
        services.AddTransient<DashboardPage>();
        services.AddTransient<ChartsPage>();
        services.AddTransient<HistoryPage>();
        return services.BuildServiceProvider();
    }

    private void InitializeNotifyIcon()
    {
        // Use a built-in system icon as fallback since no custom icon resource is embedded
        var icon = SystemIcons.Application;

        var contextMenu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem(L.Get("Menu_ShowHide", "Show / Hide"));
        showItem.Click += (_, _) => ToggleWindowVisibility();
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem(L.Get("Menu_Exit", "Exit"));
        exitItem.Click += (_, _) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = L.Get("NotifyIcon_Text", "Daily – Software Usage Statistics"),
            Visible = true,
            ContextMenuStrip = contextMenu,
        };

        _notifyIcon.DoubleClick += (_, _) => ToggleWindowVisibility();
    }

    private void ToggleWindowVisibility()
    {
        if (IsVisible && WindowState != WindowState.Minimized)
        {
            Hide();
        }
        else
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        System.Windows.Application.Current.Shutdown();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RootNavigation.Navigate(typeof(DashboardPage));
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Hide to tray instead of closing
            e.Cancel = true;
            Hide();
        }
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
