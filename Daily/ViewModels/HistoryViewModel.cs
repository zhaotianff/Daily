using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Daily.Models;
using Daily.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Daily.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly StatisticsService _statisticsService;
    private readonly ProgramCategoryService _categoryService;

    private const int MaxPieChartLabelLength = 20;
    private const int MaxBarChartLabelLength = 15;

    [ObservableProperty]
    private string _currentTheme = "Dark";

    partial void OnCurrentThemeChanged(string value) => RefreshCurrentCharts();

    /// <summary>All available category names, for binding to ComboBoxes.</summary>
    public IReadOnlyList<string> CategoryList => ProgramCategoryService.AllCategories;

    /// <summary>The currently-loaded day snapshot (used when saving category edits).</summary>
    private DailySnapshot? _currentSnapshot;

    /// <summary>
    /// Clears all history chart series and axes to stop LiveCharts rendering
    /// when the History page is removed from the visual tree.
    /// </summary>
    public void ClearHistoryCharts()
    {
        HistoryPieSeries = [];
        HistoryBarSeries = [];
        HistoryBarXAxes = [];
        HistoryBarYAxes = [];
    }

    public void RefreshCurrentCharts()
    {
        if (SelectedDay is null) return;
        var sorted = SelectedDay.AppUsages
            .OrderByDescending(a => a.TotalUsageSeconds)
            .ToList();
        RefreshHistoryCharts(sorted);
    }

    private static readonly SKColor[] Palette =
    [
        new(79, 141, 249),
        new(255, 145, 77),
        new(109, 212, 0),
        new(255, 199, 0),
        new(162, 93, 220),
        new(0, 197, 205),
        new(255, 87, 87),
        new(0, 175, 120),
        new(255, 150, 197),
        new(148, 103, 189)
    ];

    public ObservableCollection<DailySnapshot> AvailableDays { get; } = [];

    [ObservableProperty]
    private DailySnapshot? _selectedDay;

    [ObservableProperty]
    private DateTime? _selectedDate;

    [ObservableProperty]
    private DateTime _minDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _maxDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<AppUsageSnapshot> _selectedDayApps = [];

    [ObservableProperty]
    private string _selectedDayLabel = string.Empty;

    [ObservableProperty]
    private ISeries[] _historyPieSeries = [];

    [ObservableProperty]
    private ISeries[] _historyBarSeries = [];

    [ObservableProperty]
    private Axis[] _historyBarXAxes = [];

    [ObservableProperty]
    private Axis[] _historyBarYAxes = [];

    [ObservableProperty]
    private bool _hasChartData;

    public HistoryViewModel(StatisticsService statisticsService, ProgramCategoryService categoryService)
    {
        _statisticsService = statisticsService;
        _categoryService = categoryService;
        _selectedDayLabel = L.Get("History_SelectDayLabel", "Select a day to view details");
    }

    public void Refresh()
    {
        AvailableDays.Clear();
        var history = _statisticsService.LoadAllHistory();
        foreach (var day in history)
            AvailableDays.Add(day);

        if (AvailableDays.Count > 0)
        {
            MinDate = AvailableDays.Min(d => d.Date).Date;
            MaxDate = AvailableDays.Max(d => d.Date).Date;
            SelectedDate = AvailableDays[0].Date.Date;
        }
    }

    partial void OnSelectedDateChanged(DateTime? value)
    {
        if (value is null)
        {
            SelectedDay = null;
            return;
        }
        SelectedDay = AvailableDays.FirstOrDefault(d => d.Date.Date == value.Value.Date);
    }

    partial void OnSelectedDayChanged(DailySnapshot? value)
    {
        // Unsubscribe from previous snapshot's app usages
        if (_currentSnapshot?.AppUsages is not null)
            foreach (var snap in _currentSnapshot.AppUsages)
                snap.PropertyChanged -= OnAppSnapshotPropertyChanged;

        _currentSnapshot = value;

        if (value is null)
        {
            SelectedDayApps = [];
            SelectedDayLabel = L.Get("History_SelectDayLabel", "Select a day to view details");
            HasChartData = false;
            HistoryPieSeries = [];
            HistoryBarSeries = [];
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

        // Subscribe to category changes on each snapshot entry
        foreach (var snap in value.AppUsages)
            snap.PropertyChanged += OnAppSnapshotPropertyChanged;

        SelectedDayApps = new ObservableCollection<AppUsageSnapshot>(sorted);
        RefreshHistoryCharts(sorted);
    }

    private void OnAppSnapshotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppUsageSnapshot.Category)) return;
        if (sender is not AppUsageSnapshot snap || _currentSnapshot is null) return;

        // Save user override to category service
        _categoryService.SetUserCategory(snap.ProcessName, snap.Category);

        // Persist the updated snapshot to disk
        _statisticsService.SaveHistorySnapshot(_currentSnapshot);
    }

    private void RefreshHistoryCharts(List<AppUsageSnapshot> apps)
    {
        var top = apps.Take(10).ToList();

        if (top.Count == 0)
        {
            HasChartData = false;
            HistoryPieSeries = [];
            HistoryBarSeries = [];
            return;
        }

        HasChartData = true;

        HistoryPieSeries = top.Select((a, i) => (ISeries)new PieSeries<double>
        {
            Name = a.AppName.Length > MaxPieChartLabelLength ? a.AppName[..MaxPieChartLabelLength] + "…" : a.AppName,
            Values = [Math.Round(a.TotalUsageSeconds, 1)],
            Fill = new SolidColorPaint(Palette[i % Palette.Length]),
            DataLabelsSize = 12,
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
        }).ToArray();

        var values = top.Select(a => Math.Round(a.TotalUsageSeconds, 1)).ToArray();
        var labels = top
            .Select(a => a.AppName.Length > MaxBarChartLabelLength ? a.AppName[..MaxBarChartLabelLength] + "…" : a.AppName)
            .ToArray();

        HistoryBarSeries =
        [
            new RowSeries<double>
            {
                Name = L.Get("Chart_TotalTime", "Total Time"),
                Values = values,
                Fill = new SolidColorPaint(new SKColor(79, 141, 249)),
                DataLabelsSize = 11,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                DataLabelsFormatter = point =>
                {
                    var t = TimeSpan.FromSeconds(point.Coordinate.PrimaryValue);
                    if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
                    if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
                    return $"{(int)t.TotalSeconds}s";
                }
            }
        ];

        HistoryBarXAxes =
        [
            new Axis
            {
                Name = L.Get("Chart_TotalTimeAxis", "Total Time"),
                LabelsPaint = new SolidColorPaint(ChartThemeHelper.GetLabelColor(CurrentTheme)),
                TextSize = 11,
                Labeler = val =>
                {
                    if (val < 0) return string.Empty;
                    var t = TimeSpan.FromSeconds(val);
                    if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
                    if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
                    return $"{(int)t.TotalSeconds}s";
                }
            }
        ];

        HistoryBarYAxes =
        [
            new Axis
            {
                Labels = labels,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(ChartThemeHelper.GetLabelColor(CurrentTheme)),
            }
        ];
    }
}
