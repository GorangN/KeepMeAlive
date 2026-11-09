using Microsoft.Win32;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Wpf.Ui.Controls;

namespace KeepMeAlive;

/// <summary>Applies the supported Windows backdrop without changing app theme or window commands.</summary>
internal static class WindowMaterial
{
    internal static void Attach(Window window)
    {
        window.SourceInitialized += OnSourceInitialized;
    }

    private static void OnSourceInitialized(object? sender, EventArgs args)
    {
        if (sender is Window window)
        {
            window.SourceInitialized -= OnSourceInitialized;
            Apply(window, SystemParameters.HighContrast);
        }
    }

    internal static void Apply(Window window, bool highContrast)
    {
        if (new WindowInteropHelper(window).Handle == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) { return; }
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        bool transparency = key?.GetValue("EnableTransparency") is not int enabled || enabled != 0;
        if (highContrast || !transparency)
        {
            WindowBackdrop.RemoveBackdrop(window);
            window.SetResourceReference(Window.BackgroundProperty, "BackgroundBrush");
            window.Resources.Remove("ChromeBrush");
            SetGlass(window, false);
        }
        else
        {
            SetGlass(window, true);
            WindowBackdrop.RemoveBackground(window);
            if (WindowBackdrop.ApplyBackdrop(new WindowInteropHelper(window).Handle, WindowBackdropType.Mica))
            {
                window.Resources["ChromeBrush"] = Brushes.Transparent;
            }
            else
            {
                window.SetResourceReference(Window.BackgroundProperty, "BackgroundBrush");
                window.Resources.Remove("ChromeBrush");
                SetGlass(window, false);
            }
        }
    }

    private static void SetGlass(Window window, bool enabled)
    {
        if (WindowChrome.GetWindowChrome(window) is WindowChrome chrome)
        {
            var updated = (WindowChrome)chrome.CloneCurrentValue();
            updated.GlassFrameThickness = new Thickness(enabled ? -1 : 0);
            updated.UseAeroCaptionButtons = false;
            WindowChrome.SetWindowChrome(window, updated);
        }
    }
}
