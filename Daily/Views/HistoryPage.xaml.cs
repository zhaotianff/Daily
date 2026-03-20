using System.Windows.Controls;
using Daily.ViewModels;

namespace Daily.Views;

/// <summary>
/// Interaction logic for HistoryPage.xaml
/// </summary>
public partial class HistoryPage : Page
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Refresh the history list whenever the page is shown
        Loaded += (_, _) => viewModel.Refresh();
    }
}
