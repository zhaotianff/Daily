using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SystemTimer = System.Threading.Timer;

namespace Daily.Services;

/// <summary>
/// Tracks the currently active foreground window using low-overhead Win32 polling.
/// Uses a background timer that polls every second to minimize CPU usage.
/// Ignores system processes and the Daily application itself.
/// </summary>
public sealed class ForegroundWindowTracker : IDisposable
{
    private SystemTimer? _timer;
    private string _lastProcessName = string.Empty;
    private string _lastAppName = string.Empty;
    private string _lastExecPath = string.Empty;
    private bool _disposed;

    /// <summary>
    /// Process name of the Daily application itself (lower-cased), so it can be excluded.
    /// </summary>
    private static readonly string SelfProcessName =
        Process.GetCurrentProcess().ProcessName.ToLowerInvariant();

    /// <summary>
    /// Windows system/shell processes that should not be tracked.
    /// </summary>
    private static readonly HashSet<string> SystemProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        // Windows shell & desktop
        "explorer",
        "shellexperiencehost",
        "startmenuexperiencehost",
        "searchhost",
        "searchapp",
        "searchui",
        "cortana",
        "lockapp",
        "logonui",
        "winlogon",
        "wininit",
        "csrss",
        "smss",
        "services",
        "lsass",
        "svchost",
        "dwm",                      // Desktop Window Manager
        "conhost",                  // Console Window Host
        "runtimebroker",            // Windows Runtime Broker
        "applicationframehost",     // UWP app host
        "systemsettings",           // Windows Settings
        "textinputhost",            // On-screen keyboard / Input Method
        "tabtip",                   // Touch Keyboard
        "ctfmon",                   // CTF Monitor (language bar)
        "taskhostw",                // Task Host Window
        "sihost",                   // Shell Infrastructure Host
        "fontdrvhost",
        "audiodg",
        "spoolsv",
        "unsecapp",
        "wmiprvse",
        "wmpnetwk",
        "sdclt",                    // Windows Backup
        "msiexec",                  // Windows Installer
        "dllhost",
        "backgroundtaskhost",
        "useroobebroker",
        "securityhealthsystray",
        "securityhealthservice",
        "smartscreen",              // Windows SmartScreen
        "msmpeng",                  // Windows Defender
        "nissrv",                   // Network Inspection Service
        "msseces",                  // Microsoft Security Essentials
        "antimalwareserviceexe",
        "mrt",                      // Malicious Software Removal Tool
        "sppsvc",                   // Software Protection
        "wuauclt",                  // Windows Update
        "musnotification",          // Windows Update notification
        "uhssvc",                   // Update Health Service
        "compattelrunner",
        "taskmgr",                  // Task Manager
        "regedit",                  // Registry Editor
        "mmc",                      // Microsoft Management Console
        "eventvwr",                 // Event Viewer
        "compmgmt",
        "diskmgmt",
        "devmgmt",
        "perfmon",
        "resmon",                   // Resource Monitor
        "msconfig",                 // System Configuration
        "winver",
        "osk",                      // On-Screen Keyboard
        "magnify",                  // Magnifier
        "narrator",                 // Narrator
        "utilman",                  // Utility Manager
        "displayswitch",
        "consent",                  // UAC consent dialog
        "credui",                   // Credential UI
        "lsaiso",
        "physxloader",
        "igfxem",                   // Intel graphics tray
        "igfxtray",
        "igfxhk",
        "nvdisplay.container",
        "nvvsvc",
        "nvtmru",
        "rundll32",
        "regsvr32",
        "verclsid",
        "wermgr",                   // Windows Error Reporting
        "werhost",
        "werfault",
    };

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
        _timer = new SystemTimer(OnTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void OnTick(object? state)
    {
        try
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                // No foreground window (e.g. lock screen, desktop with no window focused)
                if (_lastProcessName.Length > 0)
                {
                    _lastProcessName = string.Empty;
                    _lastAppName = string.Empty;
                    _lastExecPath = string.Empty;
                    AppChanged?.Invoke(string.Empty, string.Empty, string.Empty);
                }
                return;
            }

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

            // Ignore system processes and the Daily app itself
            if (SystemProcesses.Contains(processName) ||
                string.Equals(processName, SelfProcessName, StringComparison.OrdinalIgnoreCase))
            {
                // Signal idle so callers stop counting time for the previously active app
                if (_lastProcessName.Length > 0)
                {
                    _lastProcessName = string.Empty;
                    _lastAppName = string.Empty;
                    _lastExecPath = string.Empty;
                    AppChanged?.Invoke(string.Empty, string.Empty, string.Empty);
                }
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

