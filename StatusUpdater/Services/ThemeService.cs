using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using KeepMeAlive.Messages;
using KeepMeAlive.Services.Interfaces;
using System.Windows;

namespace KeepMeAlive.Services;

public class ThemeService : IThemeService
{
    public string CurrentTheme { get; private set; } = "Dark";

    public void Apply(string themeName)
    {
        var resolved = themeName == "System" ? DetectSystemTheme() : themeName;
        var uri = new Uri($"pack://application:,,,/Resources/Themes/{resolved}Theme.xaml");

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            var existing = dicts.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("Theme.xaml") == true &&
                !d.Source.OriginalString.Contains("Shared"));

            if (existing != null)
                dicts.Remove(existing);

            dicts.Insert(0, new ResourceDictionary { Source = uri });
        });

        CurrentTheme = resolved;
        WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(resolved));
    }

    private static string DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1 ? "Light" : "Dark";
        }
        catch
        {
            return "Dark";
        }
    }
}
