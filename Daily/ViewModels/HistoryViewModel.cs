using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Daily.Models;
using Daily.Services;

namespace Daily.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly StatisticsService _statisticsService;

    public ObservableCollection<DailySnapshot> AvailableDays { get; } = [];

    [ObservableProperty]
    private DailySnapshot? _selectedDay;

    [ObservableProperty]
    private ObservableCollection<AppUsageSnapshot> _selectedDayApps = [];

    [ObservableProperty]
    private string _selectedDayLabel = "Select a day to view details";

    public HistoryViewModel(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public void Refresh()
    {
        AvailableDays.Clear();
        var history = _statisticsService.LoadAllHistory();
        foreach (var day in history)
            AvailableDays.Add(day);

        if (AvailableDays.Count > 0)
            SelectedDay = AvailableDays[0];
    }

    partial void OnSelectedDayChanged(DailySnapshot? value)
    {
        if (value is null)
        {
            SelectedDayApps = [];
            SelectedDayLabel = "Select a day to view details";
            return;
        }

        SelectedDayLabel = $"{value.Date:yyyy-MM-dd}  ·  {value.AppUsages.Count} apps  ·  " +
                           $"{value.TotalMouseClicks:N0} clicks  ·  {value.TotalKeyboardPresses:N0} key presses";

        var sorted = value.AppUsages
            .OrderByDescending(a => a.TotalUsageSeconds)
            .ToList();

        SelectedDayApps = new ObservableCollection<AppUsageSnapshot>(sorted);
    }
}
