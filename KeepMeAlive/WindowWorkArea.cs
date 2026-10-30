using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace KeepMeAlive;

/// <summary>Keeps custom-chrome maximized bounds inside the current monitor's work area.</summary>
internal sealed class WindowWorkArea : IDisposable
{
    private const int GetMinMaxInfoMessage = 0x0024;
    private readonly Window _window;
    private HwndSource? _source;

    private WindowWorkArea(Window window)
    {
        _window = window;
        _window.SourceInitialized += OnSourceInitialized;
        _window.Closed += OnClosed;
        AttachSource();
    }

    internal static void Attach(Window window) => _ = new WindowWorkArea(window);

    internal static (Rect Bounds, Size MinimumTrackSize) CalculateBounds(
        Rect monitor, Rect workArea, Size minimumSize, DpiScale dpi) =>
        (new Rect(workArea.Left - monitor.Left, workArea.Top - monitor.Top, workArea.Width, workArea.Height),
            new Size(Math.Ceiling(minimumSize.Width * dpi.DpiScaleX), Math.Ceiling(minimumSize.Height * dpi.DpiScaleY)));

    private void OnSourceInitialized(object? sender, EventArgs eventArgs) => AttachSource();

    private void AttachSource()
    {
        if (_source is not null)
        {
            return;
        }

        IntPtr handle = new WindowInteropHelper(_window).Handle;
        if (handle != IntPtr.Zero)
        {
            _source = HwndSource.FromHwnd(handle);
            _source?.AddHook(OnWindowMessage);
        }
    }

    private IntPtr OnWindowMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != GetMinMaxInfoMessage)
        {
            return IntPtr.Zero;
        }

        IntPtr monitor = MonitorFromWindow(window, 2);
        var information = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref information))
        {
            return IntPtr.Zero;
        }

        uint dpi = GetDpiForWindow(window);
        double scale = dpi == 0 ? 1 : dpi / 96d;
        var geometry = CalculateBounds(information.Monitor.ToRect(), information.WorkArea.ToRect(),
            new Size(_window.MinWidth, _window.MinHeight), new DpiScale(scale, scale));
        var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        limits.MaximumPosition = new NativePoint((int)geometry.Bounds.X, (int)geometry.Bounds.Y);
        limits.MaximumSize = new NativePoint((int)geometry.Bounds.Width, (int)geometry.Bounds.Height);
        limits.MinimumTrackSize = new NativePoint(
            Math.Max(limits.MinimumTrackSize.X, (int)geometry.MinimumTrackSize.Width),
            Math.Max(limits.MinimumTrackSize.Y, (int)geometry.MinimumTrackSize.Height));
        Marshal.StructureToPtr(limits, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    private void OnClosed(object? sender, EventArgs eventArgs) => Dispose();

    /// <summary>Detaches the native hook and window lifecycle subscriptions.</summary>
    public void Dispose()
    {
        _window.SourceInitialized -= OnSourceInitialized;
        _window.Closed -= OnClosed;
        if (_source is { IsDisposed: false })
        {
            _source.RemoveHook(OnWindowMessage);
        }

        _source = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint(int x, int y)
    {
        internal int X = x;
        internal int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal readonly Rect ToRect() => new(Left, Top, Right - Left, Bottom - Top);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        internal int Size;
        internal NativeRect Monitor;
        internal NativeRect WorkArea;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        internal NativePoint Reserved;
        internal NativePoint MaximumSize;
        internal NativePoint MaximumPosition;
        internal NativePoint MinimumTrackSize;
        internal NativePoint MaximumTrackSize;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo information);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);
}
