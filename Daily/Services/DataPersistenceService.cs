using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Daily.Models;

namespace Daily.Services;

/// <summary>
/// Handles loading and saving of daily statistics snapshots to
/// %APPDATA%\Daily\stats_YYYY-MM-DD.json.
/// </summary>
public sealed class DataPersistenceService
{
    private static readonly string DataDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Daily");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public DataPersistenceService()
    {
        Directory.CreateDirectory(DataDirectory);
    }

    /// <summary>
    /// Saves a snapshot of today's statistics to disk.
    /// </summary>
    public void SaveToday(DailyStatistics statistics)
    {
        var snapshot = ToSnapshot(statistics);
        SaveSnapshot(snapshot);
    }

    /// <summary>
    /// Loads today's persisted snapshot (if any) and merges it into <paramref name="statistics"/>.
    /// Call this once on startup before starting the tracker.
    /// </summary>
    public void LoadToday(DailyStatistics statistics)
    {
        var snapshot = LoadSnapshot(DateTime.Today);
        if (snapshot is null) return;

        statistics.TotalMouseClicks = snapshot.TotalMouseClicks;
        statistics.TotalKeyboardPresses = snapshot.TotalKeyboardPresses;

        foreach (var appSnap in snapshot.AppUsages)
        {
            var record = new AppUsageRecord
            {
                ProcessName = appSnap.ProcessName,
                AppName = appSnap.AppName,
                ExecutablePath = appSnap.ExecutablePath,
                TotalUsageTime = TimeSpan.FromSeconds(appSnap.TotalUsageSeconds),
                LastActiveTime = appSnap.LastActiveTime,
                Category = appSnap.Category,
            };
            statistics.AppUsages.Add(record);
        }
    }

    /// <summary>
    /// Returns all available historical snapshots (excluding today), sorted newest-first.
    /// </summary>
    public IReadOnlyList<DailySnapshot> LoadHistory()
    {
        if (!Directory.Exists(DataDirectory))
            return [];

        var result = new List<DailySnapshot>();
        foreach (var file in Directory.GetFiles(DataDirectory, "stats_*.json"))
        {
            var snapshot = TryLoadFile(file);
            if (snapshot is not null && snapshot.Date.Date != DateTime.Today)
                result.Add(snapshot);
        }

        result.Sort((a, b) => b.Date.CompareTo(a.Date));
        return result;
    }

    /// <summary>
    /// Returns all available snapshots including today, sorted newest-first.
    /// </summary>
    public IReadOnlyList<DailySnapshot> LoadAllHistory()
    {
        if (!Directory.Exists(DataDirectory))
            return [];

        var result = new List<DailySnapshot>();
        foreach (var file in Directory.GetFiles(DataDirectory, "stats_*.json"))
        {
            var snapshot = TryLoadFile(file);
            if (snapshot is not null)
                result.Add(snapshot);
        }

        result.Sort((a, b) => b.Date.CompareTo(a.Date));
        return result;
    }

    private static DailySnapshot ToSnapshot(DailyStatistics statistics)
    {
        return new DailySnapshot
        {
            Date = statistics.Date,
            TotalMouseClicks = statistics.TotalMouseClicks,
            TotalKeyboardPresses = statistics.TotalKeyboardPresses,
            AppUsages = statistics.AppUsages.Select(a => new AppUsageSnapshot
            {
                ProcessName = a.ProcessName,
                AppName = a.AppName,
                ExecutablePath = a.ExecutablePath,
                TotalUsageSeconds = a.TotalUsageTime.TotalSeconds,
                LastActiveTime = a.LastActiveTime,
                Category = a.Category,
            }).ToList(),
        };
    }

    public void SaveSnapshot(DailySnapshot snapshot)
    {
        var filePath = GetFilePath(snapshot.Date);
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    private DailySnapshot? LoadSnapshot(DateTime date)
    {
        var filePath = GetFilePath(date);
        return TryLoadFile(filePath);
    }

    private static DailySnapshot? TryLoadFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<DailySnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string GetFilePath(DateTime date) =>
        Path.Combine(DataDirectory, $"stats_{date:yyyy-MM-dd}.json");
}
