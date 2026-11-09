using Microsoft.Win32;
using KeepMeAlive.Services.Interfaces;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
namespace KeepMeAlive.Services;
/// <summary>Follows live system appearance with a solid high-contrast fallback.</summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private string _preference = "Light";
    /// <inheritdoc/>
    public string CurrentTheme { get; private set; } = "Light";
    /// <summary>Subscribes to system appearance changes.</summary>
    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnPreferenceChanged;
        SystemParameters.StaticPropertyChanged += OnSystemPropertyChanged;
    }
    /// <inheritdoc/>
    public void Apply(string themeName)
    {
        _preference = themeName is "Dark" or "System" ? themeName : "Light";
        var resolved = _preference == "System" ? DetectSystemTheme() : _preference;
        System.Windows.Application.Current.Dispatcher.Invoke(() => ApplyResources(resolved, SystemParameters.HighContrast));
        CurrentTheme = resolved;
    }
    internal static void ApplyResources(string resolved, bool highContrast)
    {
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var highContrastControls = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.EndsWith("HighContrastControls.xaml", StringComparison.Ordinal) == true);
        if (highContrastControls != null) { dictionaries.Remove(highContrastControls); }
        ApplicationThemeManager.Apply(resolved == "Dark" ? ApplicationTheme.Dark : ApplicationTheme.Light, WindowBackdropType.None, false);
        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.EndsWith("LightTheme.xaml", StringComparison.Ordinal) == true || d.Source?.OriginalString.EndsWith("DarkTheme.xaml", StringComparison.Ordinal) == true);
        if (existing != null) { dictionaries.Remove(existing); }
        var palette = CreateResources(resolved, highContrast);
        dictionaries.Add(palette);
        var accent = ((SolidColorBrush)palette["PrimaryBrush"]).Color;
        ApplicationAccentColorManager.Apply(accent, accent, accent, accent);
        if (highContrast)
        {
            var controls = new ResourceDictionary { Source = new Uri("pack://application:,,,/KeepMeAlive;component/Resources/Themes/HighContrastControls.xaml") };
            dictionaries.Add(controls);
        }
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            WindowMaterial.Apply(window, highContrast);
        }
    }
    internal static ResourceDictionary CreateResources(string resolved, bool highContrast)
    {
        var theme = new ResourceDictionary { Source = new Uri($"pack://application:,,,/KeepMeAlive;component/Resources/Themes/{resolved}Theme.xaml") };
        if (highContrast)
        {
            foreach (var key in theme.Keys.Cast<object>().OfType<string>().Where(k => k.EndsWith("Brush", StringComparison.Ordinal)).ToArray())
            {
                theme[key] = key.Contains("Primary", StringComparison.Ordinal) || key is "AccentBrush" or "RunningBrush" ? SystemColors.HighlightBrush :
                    key.StartsWith("On", StringComparison.Ordinal) || key is "ForegroundBrush" or "SubtleBrush" or "StoppedBrush" or "ErrorBrush" or "DangerBrush" ? SystemColors.WindowTextBrush : SystemColors.WindowBrush;
            }
            theme["OnPrimaryBrush"] = SystemColors.HighlightTextBrush;
            theme["OutlineBrush"] = SystemColors.WindowTextBrush;
            theme["OutlineVariantBrush"] = SystemColors.WindowTextBrush;
            theme["ToggleOffBrush"] = SystemColors.WindowTextBrush;
            theme["ToggleThumbBrush"] = SystemColors.WindowBrush;
            theme["SelectedBackgroundBrush"] = SystemColors.HighlightBrush;
            theme["SelectedForegroundBrush"] = SystemColors.HighlightTextBrush;
            theme["ControlHoverBrush"] = SystemColors.HighlightBrush;
            theme["NavItemActiveBgBrush"] = SystemColors.WindowBrush;
            theme["RingTrackBrush"] = SystemColors.GrayTextBrush;
        }
        return theme;
    }
    private void OnPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Apply(_preference);
    private void OnSystemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast)) { Apply(_preference); }
    }
    private static string DetectSystemTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0 ? "Dark" : "Light";
    }
    /// <summary>Releases system subscriptions on shutdown.</summary>
    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
        SystemParameters.StaticPropertyChanged -= OnSystemPropertyChanged;
    }
}
