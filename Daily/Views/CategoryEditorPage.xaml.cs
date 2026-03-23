using System.Windows.Controls;
using Daily.ViewModels;

namespace Daily.Views;

/// <summary>
/// Interaction logic for CategoryEditorPage.xaml
/// </summary>
public partial class CategoryEditorPage : Page
{
    public CategoryEditorPage(CategoryEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Reload entries whenever the page is shown
        Loaded += (_, _) => viewModel.LoadEntries();
    }
}
