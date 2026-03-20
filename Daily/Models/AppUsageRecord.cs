using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Daily.Models;

public partial class AppUsageRecord : ObservableObject
{
    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _executablePath = string.Empty;

    [ObservableProperty]
    private TimeSpan _totalUsageTime;

    [ObservableProperty]
    private DateTime _lastActiveTime;

    public string FormattedTime =>
        TotalUsageTime.TotalHours >= 1
            ? $"{(int)TotalUsageTime.TotalHours}h {TotalUsageTime.Minutes}m {TotalUsageTime.Seconds}s"
            : TotalUsageTime.TotalMinutes >= 1
                ? $"{TotalUsageTime.Minutes}m {TotalUsageTime.Seconds}s"
                : $"{TotalUsageTime.Seconds}s";

    partial void OnTotalUsageTimeChanged(TimeSpan value) =>
        OnPropertyChanged(nameof(FormattedTime));
}
