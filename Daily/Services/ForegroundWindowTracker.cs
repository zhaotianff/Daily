using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Daily.Services;

/// <summary>
/// Tracks the currently active foreground window using low-overhead Win32 polling.
/// Uses a background timer that polls every second to minimize CPU usage.
/// </summary>
public sealed class ForegroundWindowTracker : IDisposable
{
    private Timer? _timer;
    private string _lastProcessName = string.Empty;
    private string _lastAppName = string.Empty;
    private string _lastExecPath = string.Empty;
    private bool _disposed;

    public event Action<string, string, string>? AppChanged;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("psapi.dll")]
    private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public void Start()
    {
        // Poll every 1000ms to keep CPU usage low
        _timer = new Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void OnTick(object? state)
    {
        try
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return;

            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == 0)
                return;

            string processName = string.Empty;
            string execPath = string.Empty;
            string windowTitle = string.Empty;

            // Get window title
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            windowTitle = sb.ToString();

            try
            {
                using var process = Process.GetProcessById((int)pid);
                processName = process.ProcessName;
                try { execPath = process.MainModule?.FileName ?? string.Empty; }
                catch { execPath = string.Empty; }
            }
            catch
            {
                return;
            }

            string appName = string.IsNullOrEmpty(windowTitle) ? processName : windowTitle;

            if (processName != _lastProcessName)
            {
                _lastProcessName = processName;
                _lastAppName = appName;
                _lastExecPath = execPath;
                AppChanged?.Invoke(processName, appName, execPath);
            }
        }
        catch
        {
            // Swallow all exceptions to avoid crashing the timer thread
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }
}
