using System;
using System.Collections.ObjectModel;
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

    private const int MaxPieChartLabelLength = 20;
    private const int MaxBarChartLabelLength = 15;

    public DailyStatistics Statistics => _statisticsService.Statistics;

    [ObservableProperty]
    private string _currentTheme = "Dark";

    // ── Chart Series ──────────────────────────────────────────────

    [ObservableProperty]
    private ISeries[] _pieSeries = [];

    [ObservableProperty]
    private ISeries[] _barSeries = [];

    [ObservableProperty]
    private Axis[] _barXAxes = [];

    [ObservableProperty]
    private Axis[] _barYAxes = [];

    public DashboardViewModel(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;

        // React to collection changes to refresh charts
        Statistics.AppUsages.CollectionChanged += (_, _) => RefreshCharts();

        // Also refresh charts on a timer to pick up time updates
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += (_, _) => RefreshCharts();
        timer.Start();
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
            Name = a.AppName.Length > MaxPieChartLabelLength ? a.AppName[..MaxPieChartLabelLength] : a.AppName,
            Values = new[] { Math.Round(a.TotalUsageTime.TotalSeconds, 1) },
            Fill = new SolidColorPaint(palette[i % palette.Length]),
            DataLabelsSize = 12,
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
        }).ToArray();

        // Bar chart
        var values = top.Select(a => Math.Round(a.TotalUsageTime.TotalSeconds, 1)).ToArray();
        var labels = top.Select(a => a.AppName.Length > MaxBarChartLabelLength ? a.AppName[..MaxBarChartLabelLength] : a.AppName).ToArray();

        BarSeries =
        [
            new ColumnSeries<double>
            {
                Name = "Usage (seconds)",
                Values = values,
                Fill = new SolidColorPaint(new SKColor(79, 141, 249)),
                DataLabelsSize = 11,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            }
        ];

        BarXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = -30,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
            }
        ];

        BarYAxes =
        [
            new Axis
            {
                Name = "Seconds",
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 11,
            }
        ];
    }
}
