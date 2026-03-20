using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Daily.Models;

namespace Daily.Services;

/// <summary>
/// Central service that manages usage statistics.
/// Coordinates between the foreground window tracker and global input hooks.
/// Persists data across sessions via <see cref="DataPersistenceService"/>.
/// </summary>
public sealed class StatisticsService : IDisposable
{
    private readonly ForegroundWindowTracker _windowTracker = new();
    private readonly GlobalHookService _hookService = new();
    private readonly DataPersistenceService _persistence = new();
    private readonly DispatcherTimer _uiUpdateTimer;
    private readonly DispatcherTimer _autoSaveTimer;
    private readonly Dispatcher _dispatcher;

    private string _currentProcessName = string.Empty;
    private string _currentAppName = string.Empty;
    private string _currentExecPath = string.Empty;
    private DateTime _currentAppStartTime = DateTime.Now;
    private bool _disposed;

    public DailyStatistics Statistics { get; } = new();

    public StatisticsService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        // Update UI every 2 seconds to reduce CPU usage from frequent UI redraws
        _uiUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _uiUpdateTimer.Tick += OnUiUpdate;

        // Auto-save every 5 minutes
        _autoSaveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _autoSaveTimer.Tick += (_, _) => _persistence.SaveToday(Statistics);

        _windowTracker.AppChanged += OnAppChanged;

        // Load persisted data for today on startup
        _persistence.LoadToday(Statistics);

        // Restore input count offsets so today's hooks add on top of persisted totals
        _hookService.SetInitialCounts(Statistics.TotalMouseClicks, Statistics.TotalKeyboardPresses);
    }

    public void Start()
    {
        _hookService.Install();
        _windowTracker.Start();
        _uiUpdateTimer.Start();
        _autoSaveTimer.Start();
    }

    public void Stop()
    {
        _uiUpdateTimer.Stop();
        _autoSaveTimer.Stop();
        _hookService.Uninstall();
        // Update final time for the current app
        FlushCurrentApp();
        // Persist data before stopping
        _persistence.SaveToday(Statistics);
    }

    /// <summary>
    /// Loads all persisted historical snapshots (including today).
    /// </summary>
    public IReadOnlyList<DailySnapshot> LoadAllHistory() =>
        _persistence.LoadAllHistory();

    private void OnAppChanged(string processName, string appName, string execPath)
    {
        // Flush time for the previous app
        FlushCurrentApp();

        // Start tracking the new app
        _currentProcessName = processName;
        _currentAppName = appName;
        _currentExecPath = execPath;
        _currentAppStartTime = DateTime.Now;

        // Ensure the new app exists in the collection (on UI thread)
        _dispatcher.BeginInvoke(() => EnsureRecord(processName, appName, execPath));
    }

    private void FlushCurrentApp()
    {
        if (string.IsNullOrEmpty(_currentProcessName))
            return;

        var elapsed = DateTime.Now - _currentAppStartTime;
        if (elapsed <= TimeSpan.Zero)
            return;

        _currentAppStartTime = DateTime.Now;

        var pn = _currentProcessName;
        var an = _currentAppName;
        var ep = _currentExecPath;

        _dispatcher.BeginInvoke(() =>
        {
            var record = EnsureRecord(pn, an, ep);
            record.TotalUsageTime += elapsed;
            record.LastActiveTime = DateTime.Now;
        });
    }

    private AppUsageRecord EnsureRecord(string processName, string appName, string execPath)
    {
        var record = Statistics.AppUsages.FirstOrDefault(r => r.ProcessName == processName);
        if (record is null)
        {
            record = new AppUsageRecord
            {
                ProcessName = processName,
                AppName = appName,
                ExecutablePath = execPath,
                LastActiveTime = DateTime.Now,
                Category = ProgramCategoryService.GetCategory(processName, execPath),
            };
            Statistics.AppUsages.Add(record);
        }
        return record;
    }

    private void OnUiUpdate(object? sender, EventArgs e)
    {
        // Update current app's time in real time
        if (!string.IsNullOrEmpty(_currentProcessName))
        {
            var elapsed = DateTime.Now - _currentAppStartTime;
            if (elapsed > TimeSpan.Zero)
            {
                _currentAppStartTime = DateTime.Now;
                var record = Statistics.AppUsages.FirstOrDefault(r => r.ProcessName == _currentProcessName);
                if (record is not null)
                    record.TotalUsageTime += elapsed;
            }
        }

        // Update input counts
        Statistics.TotalMouseClicks = _hookService.MouseClickCount;
        Statistics.TotalKeyboardPresses = _hookService.KeyboardPressCount;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _windowTracker.Dispose();
        _hookService.Dispose();
    }
}

