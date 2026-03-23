using SkiaSharp;

namespace Daily.ViewModels;

/// <summary>
/// Provides theme-aware colours for chart axes and labels.
/// </summary>
internal static class ChartThemeHelper
{
    /// <summary>
    /// Returns an axis label colour that contrasts well against the current theme background.
    /// </summary>
    public static SKColor GetLabelColor(string theme) => theme == "Dark"
        ? new SKColor(200, 200, 200)   // light gray for dark background
        : new SKColor(60, 60, 60);     // dark gray for light background
}
