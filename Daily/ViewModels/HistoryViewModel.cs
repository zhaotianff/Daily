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
    private string _selectedDayLabel = string.Empty;

    public HistoryViewModel(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _selectedDayLabel = L.Get("History_SelectDayLabel", "Select a day to view details");
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
            SelectedDayLabel = L.Get("History_SelectDayLabel", "Select a day to view details");
            return;
        }

        var fmt = L.Get("History_LabelFormat",
            "{0:yyyy-MM-dd}  ·  {1} apps  ·  {2:N0} clicks  ·  {3:N0} key presses");
        SelectedDayLabel = string.Format(fmt,
            value.Date,
            value.AppUsages.Count,
            value.TotalMouseClicks,
            value.TotalKeyboardPresses);

        var sorted = value.AppUsages
            .OrderByDescending(a => a.TotalUsageSeconds)
            .ToList();

        SelectedDayApps = new ObservableCollection<AppUsageSnapshot>(sorted);
    }
}
