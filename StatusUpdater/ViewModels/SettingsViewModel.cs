using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StatusUpdater.Messages;
using StatusUpdater.Models;
using StatusUpdater.Services.Interfaces;

namespace StatusUpdater.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IStartupService _startupService;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private bool _startOnBoot;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private string _licenseKey = "";

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IStartupService startupService,
        IMessenger messenger)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _startupService = startupService;
        _messenger = messenger;
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Current;
        SelectedTheme = s.Theme;
        StartOnBoot = _startupService.IsStartupEnabled;
        ShowNotifications = s.ShowNotifications;
        LicenseKey = s.LicenseKey;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        // Live preview while settings are open
        _themeService.Apply(value);
    }

    [RelayCommand]
    private void SaveAndClose()
    {
        var settings = _settingsService.Current;
        settings.Theme = SelectedTheme;
        settings.ShowNotifications = ShowNotifications;
        settings.LicenseKey = LicenseKey;
        _settingsService.Save(settings);

        _startupService.SetStartup(StartOnBoot);
        _themeService.Apply(SelectedTheme);

        _messenger.Send(new ThemeChangedMessage(SelectedTheme));

        // Tell MainViewModel to close settings overlay
        _messenger.Send(new CloseSettingsMessage());
    }

    [RelayCommand]
    private void ActivateLicense()
    {
        // TODO: call ILicenseService.ValidateAsync(LicenseKey)
        // For now, store the key so it's ready when the subscription goes live
        var settings = _settingsService.Current;
        settings.LicenseKey = LicenseKey;
        _settingsService.Save(settings);
    }
}
