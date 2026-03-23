using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

public partial class DashboardViewModel : ObservableObject
{
    private readonly StatisticsService _statisticsService;
    private readonly ProgramCategoryService _categoryService;

    private const int MaxPieChartLabelLength = 20;
    private const int MaxBarChartLabelLength = 15;

    public DailyStatistics Statistics => _statisticsService.Statistics;

    /// <summary>All available category names, for binding to ComboBoxes.</summary>
    public IReadOnlyList<string> CategoryList => ProgramCategoryService.AllCategories;

    [ObservableProperty]
    private string _currentTheme = "Dark";

    partial void OnCurrentThemeChanged(string value) => RefreshCharts();

    // ── Chart Series ──────────────────────────────────────────────

    [ObservableProperty]
    private ISeries[] _pieSeries = [];

    [ObservableProperty]
    private ISeries[] _barSeries = [];

    [ObservableProperty]
    private Axis[] _barXAxes = [];

    [ObservableProperty]
    private Axis[] _barYAxes = [];

    /// <summary>App usages sorted by total usage time descending.</summary>
    [ObservableProperty]
    private ObservableCollection<AppUsageRecord> _sortedAppUsages = [];

    public DashboardViewModel(StatisticsService statisticsService, ProgramCategoryService categoryService)
    {
        _statisticsService = statisticsService;
        _categoryService = categoryService;

        // React to collection changes to refresh charts and sorted list
        Statistics.AppUsages.CollectionChanged += OnAppUsagesCollectionChanged;

        // Subscribe to existing records (if any were loaded from disk)
        foreach (var record in Statistics.AppUsages)
            record.PropertyChanged += OnRecordPropertyChanged;

        RefreshSortedApps();

        // Also refresh charts on a timer to pick up time updates
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += (_, _) => { RefreshSortedApps(); RefreshCharts(); };
        timer.Start();
    }

    private void OnAppUsagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (AppUsageRecord record in e.OldItems)
                record.PropertyChanged -= OnRecordPropertyChanged;

        if (e.NewItems is not null)
            foreach (AppUsageRecord record in e.NewItems)
                record.PropertyChanged += OnRecordPropertyChanged;

        RefreshSortedApps();
        RefreshCharts();
    }

    private void OnRecordPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppUsageRecord.Category) && sender is AppUsageRecord record)
            _categoryService.SetUserCategory(record.ProcessName, record.Category);

        if (e.PropertyName == nameof(AppUsageRecord.TotalUsageTime))
            RefreshSortedApps();
    }

    /// <summary>Rebuilds <see cref="SortedAppUsages"/> ordered by total usage time descending.</summary>
    public void RefreshSortedApps()
    {
        var sorted = Statistics.AppUsages
            .OrderByDescending(a => a.TotalUsageTime)
            .ToList();

        SortedAppUsages = new ObservableCollection<AppUsageRecord>(sorted);
    }

    public void RefreshCharts()
    {
        var top = Statistics.AppUsages
            .OrderByDescending(a => a.TotalUsageTime)
            .Take(10)
            .ToList();

        if (top.Count == 0)
        {
            PieSeries = [];
            BarSeries = [];
            return;
        }

        // Pie chart
        var palette = new SKColor[]
        {
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
        };

        PieSeries = top.Select((a, i) => (ISeries)new PieSeries<double>
        {
            Name = a.AppName.Length > MaxPieChartLabelLength ? a.AppName[..MaxPieChartLabelLength] + "…" : a.AppName,
            Values = new[] { Math.Round(a.TotalUsageTime.TotalSeconds, 1) },
            Fill = new SolidColorPaint(palette[i % palette.Length]),
            DataLabelsSize = 12,
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
        }).ToArray();

        // Bar chart (horizontal rows for better readability)
        var values = top.Select(a => Math.Round(a.TotalUsageTime.TotalSeconds, 1)).ToArray();
        var labels = top.Select(a => a.AppName.Length > MaxBarChartLabelLength ? a.AppName[..MaxBarChartLabelLength] + "…" : a.AppName).ToArray();

        BarSeries =
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

        // X-axis is the value axis for RowSeries
        BarXAxes =
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

        // Y-axis is the category axis for RowSeries
        BarYAxes =
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
