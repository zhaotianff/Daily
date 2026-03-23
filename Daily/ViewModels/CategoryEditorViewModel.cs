using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Daily.Models;
using Daily.Services;

namespace Daily.ViewModels;

public partial class CategoryEditorViewModel : ObservableObject
{
    private readonly ProgramCategoryService _categoryService;

    /// <summary>All available category names, for binding to ComboBoxes.</summary>
    public IReadOnlyList<string> CategoryList => ProgramCategoryService.AllCategories;

    /// <summary>Filtered list of entries displayed in the editor DataGrid.</summary>
    [ObservableProperty]
    private ObservableCollection<CategoryMappingEntry> _entries = [];

    /// <summary>All loaded entries (before filtering).</summary>
    private List<CategoryMappingEntry> _allEntries = [];

    [ObservableProperty]
    private CategoryMappingEntry? _selectedEntry;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _newProcessName = string.Empty;

    [ObservableProperty]
    private string _newCategory = ProgramCategoryService.CategoryOther;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool CanDeleteSelected => SelectedEntry?.IsUserOverride == true;

    public CategoryEditorViewModel(ProgramCategoryService categoryService)
    {
        _categoryService = categoryService;
        LoadEntries();
    }

    public void LoadEntries()
    {
        // Unsubscribe from old entries
        foreach (var e in _allEntries)
            e.PropertyChanged -= OnEntryPropertyChanged;

        _allEntries = _categoryService.GetAllMappings().ToList();

        // Subscribe to changes
        foreach (var e in _allEntries)
            e.PropertyChanged += OnEntryPropertyChanged;

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedEntryChanged(CategoryMappingEntry? value) =>
        OnPropertyChanged(nameof(CanDeleteSelected));

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allEntries
            : _allEntries.Where(e =>
                e.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Entries = new ObservableCollection<CategoryMappingEntry>(filtered);
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not CategoryMappingEntry entry) return;

        if (e.PropertyName == nameof(CategoryMappingEntry.Category))
        {
            // Save the user override
            _categoryService.SetUserCategory(entry.ProcessName, entry.Category);
            entry.MarkAsUserOverride();
            OnPropertyChanged(nameof(CanDeleteSelected));
            StatusMessage = L.Get("CategoryEditor_Saved", "Saved.");
        }
    }

    [RelayCommand]
    private void AddEntry()
    {
        var proc = NewProcessName.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(proc)) return;

        _categoryService.SetUserCategory(proc, NewCategory);

        // Replace or add in the full list
        var existing = _allEntries.FirstOrDefault(e =>
            e.ProcessName.Equals(proc, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.PropertyChanged -= OnEntryPropertyChanged;
            _allEntries.Remove(existing);
        }

        var newEntry = new CategoryMappingEntry(proc, NewCategory, isUserOverride: true);
        newEntry.PropertyChanged += OnEntryPropertyChanged;
        _allEntries.Add(newEntry);
        _allEntries = _allEntries.OrderBy(e => e.ProcessName, StringComparer.OrdinalIgnoreCase).ToList();

        NewProcessName = string.Empty;
        ApplyFilter();
        SelectedEntry = Entries.FirstOrDefault(e =>
            e.ProcessName.Equals(proc, StringComparison.OrdinalIgnoreCase));

        StatusMessage = L.Get("CategoryEditor_Added", "Entry added.");
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedEntry is null || !SelectedEntry.IsUserOverride) return;

        var proc = SelectedEntry.ProcessName;
        _categoryService.RemoveUserCategory(proc);

        SelectedEntry.PropertyChanged -= OnEntryPropertyChanged;
        _allEntries.RemoveAll(e => e.ProcessName.Equals(proc, StringComparison.OrdinalIgnoreCase));

        // Re-add the built-in entry if it exists
        LoadEntries();
        StatusMessage = L.Get("CategoryEditor_Deleted", "User override removed.");
    }
}
