using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StatusUpdater.Helpers;
using StatusUpdater.Messages;
using StatusUpdater.Models;
using StatusUpdater.Services.Interfaces;
using System.Globalization;

namespace StatusUpdater.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IStartupService _startupService;
    private readonly IMessenger _messenger;
    private readonly IScheduledActionService _scheduledActionService;

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private bool _startOnBoot;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private string _licenseKey = "";

    [ObservableProperty]
    private bool _scheduledActionEnabled;

    [ObservableProperty]
    private ScheduledAction _selectedScheduledAction = ScheduledAction.Shutdown;

    [ObservableProperty]
    private string _scheduledActionTime = string.Empty;

    /// <summary>Gets the list of available scheduled actions for binding to a ComboBox.</summary>
    public IReadOnlyList<ScheduledAction> AvailableScheduledActions { get; } =
        Enum.GetValues<ScheduledAction>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings persistence service.</param>
    /// <param name="themeService">The theme application service.</param>
    /// <param name="startupService">The Windows startup registry service.</param>
    /// <param name="messenger">The messenger for cross-ViewModel communication.</param>
    /// <param name="scheduledActionService">The scheduled system action service.</param>
    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IStartupService startupService,
        IMessenger messenger,
        IScheduledActionService scheduledActionService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _startupService = startupService;
        _messenger = messenger;
        _scheduledActionService = scheduledActionService;
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Current;
        SelectedTheme = s.Theme;
        StartOnBoot = _startupService.IsStartupEnabled;
        ShowNotifications = s.ShowNotifications;
        LicenseKey = s.LicenseKey;
        ScheduledActionEnabled = s.ScheduledActionEnabled;
        SelectedScheduledAction = s.ScheduledActionType;
        ScheduledActionTime = s.ScheduledActionTime;
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
        settings.ScheduledActionEnabled = ScheduledActionEnabled;
        settings.ScheduledActionType = SelectedScheduledAction;
        settings.ScheduledActionTime = ScheduledActionTime;
        _settingsService.Save(settings);

        _startupService.SetStartup(StartOnBoot);
        _themeService.Apply(SelectedTheme);

        if (ScheduledActionEnabled
            && DateTime.TryParseExact(
                   ScheduledActionTime,
                   DateTimeFormatValidationRule.Format,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out var fireAt)
            && fireAt > DateTime.Now)
        {
            _scheduledActionService.Schedule(SelectedScheduledAction, fireAt);
        }
        else
        {
            _scheduledActionService.Cancel();
        }

        _messenger.Send(new ThemeChangedMessage(SelectedTheme));

        // Tell MainViewModel to close settings overlay
        _messenger.Send(new CloseSettingsMessage());
    }

    partial void OnScheduledActionEnabledChanged(bool value)
    {
        if (!value)
        {
            _scheduledActionService.Cancel();
        }
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
