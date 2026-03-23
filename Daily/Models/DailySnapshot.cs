using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Daily.Models;

/// <summary>
/// Serializable snapshot of one day's statistics, used for JSON persistence.
/// </summary>
public class DailySnapshot
{
    public DateTime Date { get; set; }
    public long TotalMouseClicks { get; set; }
    public long TotalKeyboardPresses { get; set; }
    public List<AppUsageSnapshot> AppUsages { get; set; } = [];
}

/// <summary>
/// Serializable snapshot of a single app's usage within a day.
/// Category is observable so the History page can track edits.
/// </summary>
public partial class AppUsageSnapshot : ObservableObject
{
    public string ProcessName { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public double TotalUsageSeconds { get; set; }
    public DateTime LastActiveTime { get; set; }

    [ObservableProperty]
    private string _category = string.Empty;

    public string FormattedTime
    {
        get
        {
            var t = TimeSpan.FromSeconds(TotalUsageSeconds);
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s";
            if (t.TotalMinutes >= 1)
                return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }
    }
}
