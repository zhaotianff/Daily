using System.Windows.Controls;
using Daily.ViewModels;

namespace Daily.Views;

/// <summary>
/// Interaction logic for ChartsPage.xaml
/// </summary>
public partial class ChartsPage : Page
{
    public ChartsPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Refresh charts whenever the page becomes visible
        Loaded += (_, _) => viewModel.RefreshCharts();
    }
}
