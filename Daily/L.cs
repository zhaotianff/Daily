using System.Windows;

namespace Daily;

/// <summary>
/// Lightweight helper that retrieves a localized string from the WPF application resource
/// dictionary. Falls back to the provided default value when the application is not yet
/// running or the requested key does not exist.
/// </summary>
internal static class L
{
    public static string Get(string key, string fallback = "") =>
        System.Windows.Application.Current?.TryFindResource(key) as string ?? fallback;
}
