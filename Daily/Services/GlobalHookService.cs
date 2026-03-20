using System;
using System.Runtime.InteropServices;

namespace Daily.Services;

/// <summary>
/// Installs low-level global hooks for mouse clicks and keyboard presses
/// to count total interactions. Uses WH_MOUSE_LL and WH_KEYBOARD_LL.
/// </summary>
public sealed class GlobalHookService : IDisposable
{
    private IntPtr _mouseHook = IntPtr.Zero;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private HookProc? _mouseProc;
    private HookProc? _keyboardProc;
    private bool _disposed;

    private long _mouseClickCount;
    private long _keyboardPressCount;

    /// <summary>
    /// Offset added to the raw hook counts, used to restore values from a previous session.
    /// </summary>
    private long _mouseClickOffset;
    private long _keyboardPressOffset;

    public long MouseClickCount => _mouseClickCount + _mouseClickOffset;
    public long KeyboardPressCount => _keyboardPressCount + _keyboardPressOffset;

    /// <summary>
    /// Sets initial offset values so that counts resume from where a previous session left off.
    /// Must be called before <see cref="Install"/>.
    /// </summary>
    public void SetInitialCounts(long mouseClicks, long keyboardPresses)
    {
        _mouseClickOffset = mouseClicks;
        _keyboardPressOffset = keyboardPresses;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    public void Install()
    {
        if (_mouseHook != IntPtr.Zero || _keyboardHook != IntPtr.Zero)
            return;

        var hMod = GetModuleHandle(null);

        // Keep delegates alive to prevent GC collection
        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;

        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN)
            {
                System.Threading.Interlocked.Increment(ref _mouseClickCount);
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
            {
                System.Threading.Interlocked.Increment(ref _keyboardPressCount);
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    public void Uninstall()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
    }
}
