using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Daily.Models;

public partial class DailyStatistics : ObservableObject
{
    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private long _totalMouseClicks;

    [ObservableProperty]
    private long _totalKeyboardPresses;

    public ObservableCollection<AppUsageRecord> AppUsages { get; } = [];
}
