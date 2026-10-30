using Microsoft.Win32;
using KeepMeAlive.Services.Interfaces;
using System.ComponentModel;
using System.Windows;
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
        var controlsTheme = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("/MahApps.Metro;component/Styles/Themes/", StringComparison.Ordinal) == true);
        if (controlsTheme != null)
        {
            var index = dictionaries.IndexOf(controlsTheme);
            dictionaries[index] = new ResourceDictionary { Source = new Uri($"pack://application:,,,/MahApps.Metro;component/Styles/Themes/{resolved}.Blue.xaml") };
        }
        var existing = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.EndsWith("LightTheme.xaml", StringComparison.Ordinal) == true || d.Source?.OriginalString.EndsWith("DarkTheme.xaml", StringComparison.Ordinal) == true);
        if (existing != null) { dictionaries.Remove(existing); }
        dictionaries.Insert(0, CreateResources(resolved, highContrast));
        if (highContrast)
        {
            var controls = new ResourceDictionary { Source = new Uri("pack://application:,,,/KeepMeAlive;component/Resources/Themes/HighContrastControls.xaml") };
            controls["MahApps.Styles.ComboBoxItem"] = controls["HighContrast.ComboBoxItem"];
            controls["MahApps.Styles.CalendarDayButton"] = controls["HighContrast.CalendarDayButton"];
            controls["MahApps.Styles.CalendarButton"] = controls["HighContrast.CalendarButton"];
            controls["MahApps.Styles.Button.Calendar.Header"] = controls["HighContrast.CalendarHeader"];
            dictionaries.Add(controls);
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
