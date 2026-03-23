using CommunityToolkit.Mvvm.ComponentModel;

namespace Daily.Models;

/// <summary>
/// Represents a single process-name → category mapping entry,
/// used in the Category Editor UI.
/// </summary>
public partial class CategoryMappingEntry : ObservableObject
{
    [ObservableProperty]
    private string _processName;

    [ObservableProperty]
    private string _category;

    [ObservableProperty]
    private bool _isUserOverride;

    public CategoryMappingEntry(string processName, string category, bool isUserOverride)
    {
        ProcessName = processName;
        Category = category;
        IsUserOverride = isUserOverride;
    }

    /// <summary>Promotes this entry to a user override (e.g., after the category is edited).</summary>
    public void MarkAsUserOverride() => IsUserOverride = true;
}
