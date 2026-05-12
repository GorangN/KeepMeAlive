using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Helpers;
using KeepMeAlive.Messages;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.Globalization;

namespace KeepMeAlive.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IAppRuntimeModeService _runtimeModeService;
    private readonly IThemeService _themeService;
    private readonly IStartupService _startupService;
    private readonly ISecretStore _secretStore;
    private readonly IMessenger _messenger;
    private readonly IScheduledActionService _scheduledActionService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _selectedTheme = "Dark";

    [ObservableProperty]
    private bool _startOnBoot;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private bool _startMinimizedToTray;

    [ObservableProperty]
    private StorageMode _selectedStorageMode = StorageMode.ProfileAppData;

    [ObservableProperty]
    private bool _enableAutomaticUpdateChecks;

    [ObservableProperty]
    private bool _enableStartupAccountSync;

    [ObservableProperty]
    private string _storageLocationDisplay = string.Empty;

    [ObservableProperty]
    private string _storageModeDescription = string.Empty;

    [ObservableProperty]
    private string _startOnBootDescription = string.Empty;

    [ObservableProperty]
    private bool _scheduledActionEnabled;

    [ObservableProperty]
    private ScheduledAction _selectedScheduledAction = ScheduledAction.Shutdown;

    [ObservableProperty]
    private string _scheduledActionTime = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>Gets the list of available scheduled actions for binding to a ComboBox.</summary>
    public IReadOnlyList<ScheduledAction> AvailableScheduledActions { get; } =
        Enum.GetValues<ScheduledAction>();

    /// <summary>Gets the list of available storage modes for binding to a ComboBox.</summary>
    public IReadOnlyList<StorageMode> AvailableStorageModes { get; } =
        Enum.GetValues<StorageMode>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings persistence service.</param>
    /// <param name="runtimeModeService">The runtime mode and path service.</param>
    /// <param name="themeService">The theme application service.</param>
    /// <param name="startupService">The Windows startup registry service.</param>
    /// <param name="secretStore">The secure secret store.</param>
    /// <param name="messenger">The messenger for cross-ViewModel communication.</param>
    /// <param name="scheduledActionService">The scheduled system action service.</param>
    /// <param name="dialogService">The dialog service.</param>
    public SettingsViewModel(
        ISettingsService settingsService,
        IAppRuntimeModeService runtimeModeService,
        IThemeService themeService,
        IStartupService startupService,
        ISecretStore secretStore,
        IMessenger messenger,
        IScheduledActionService scheduledActionService,
        IDialogService dialogService)
    {
        _settingsService = settingsService;
        _runtimeModeService = runtimeModeService;
        _themeService = themeService;
        _startupService = startupService;
        _secretStore = secretStore;
        _messenger = messenger;
        _scheduledActionService = scheduledActionService;
        _dialogService = dialogService;

        // Load settings immediately since the main window is always open
        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Current;
        SelectedTheme = s.Theme;
        StartOnBoot = _startupService.IsStartupEnabled;
        ShowNotifications = s.ShowNotifications;
        StartMinimizedToTray = s.StartMinimizedToTray;
        SelectedStorageMode = _settingsService.CurrentStorageMode;
        EnableAutomaticUpdateChecks = s.EnableAutomaticUpdateChecks;
        EnableStartupAccountSync = s.EnableStartupAccountSync;
        ScheduledActionEnabled = s.ScheduledActionEnabled;
        SelectedScheduledAction = s.ScheduledActionType;
        ScheduledActionTime = s.ScheduledActionTime;
        UpdateStoragePresentation();
        HasUnsavedChanges = false;
    }

    partial void OnStartOnBootChanged(bool value) => HasUnsavedChanges = true;

    partial void OnShowNotificationsChanged(bool value) => HasUnsavedChanges = true;

    partial void OnStartMinimizedToTrayChanged(bool value) => HasUnsavedChanges = true;

    partial void OnSelectedStorageModeChanged(StorageMode value)
    {
        HasUnsavedChanges = true;
        UpdateStoragePresentation();
    }

    partial void OnEnableAutomaticUpdateChecksChanged(bool value) => HasUnsavedChanges = true;

    partial void OnEnableStartupAccountSyncChanged(bool value) => HasUnsavedChanges = true;

    partial void OnSelectedScheduledActionChanged(ScheduledAction value) => HasUnsavedChanges = true;

    partial void OnScheduledActionTimeChanged(string value) => HasUnsavedChanges = true;

    partial void OnSelectedThemeChanged(string value)
    {
        // Live preview as user changes the theme
        _themeService.Apply(value);
    }

    [RelayCommand]
    private void Save()
    {
        var originalStorageMode = _settingsService.CurrentStorageMode;
        var originalLicenseKey = _secretStore.GetLicenseKey(originalStorageMode);

        if (StartOnBoot
            && SelectedStorageMode == StorageMode.PortableLocal
            && !_startupService.IsStartupEnabled
            && !_dialogService.ShowConfirmation(
                "Enable Windows startup?",
                "Portable local storage keeps your data beside KeepMeAlive.exe, but Windows startup still writes HKCU\\Run. Continue?"))
        {
            StartOnBoot = false;
        }

        if (SelectedStorageMode != originalStorageMode)
        {
            if (_settingsService.MigrateStorage(SelectedStorageMode))
            {
                _secretStore.SaveLicenseKey(originalLicenseKey, SelectedStorageMode);
                _secretStore.DeleteLicenseKey(originalStorageMode);
            }
            else
            {
                SelectedStorageMode = _settingsService.CurrentStorageMode;
            }
        }

        var settings = _settingsService.Current;
        settings.Theme = SelectedTheme;
        settings.ShowNotifications = ShowNotifications;
        settings.StartMinimizedToTray = StartMinimizedToTray;
        settings.EnableAutomaticUpdateChecks = EnableAutomaticUpdateChecks;
        settings.EnableStartupAccountSync = EnableStartupAccountSync;
        settings.ScheduledActionEnabled = ScheduledActionEnabled;
        settings.ScheduledActionType = SelectedScheduledAction;
        settings.ScheduledActionTime = ScheduledActionTime;
        settings.StorageMode = _settingsService.CurrentStorageMode;
        _settingsService.Save(settings);

        _startupService.SetStartup(StartOnBoot, StartMinimizedToTray);
        _themeService.Apply(SelectedTheme);
        UpdateStoragePresentation();

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

        HasUnsavedChanges = false;
        _messenger.Send(new ThemeChangedMessage(SelectedTheme));
        _messenger.Send(new StorageChangedMessage(_settingsService.CurrentStorageMode));
    }

    partial void OnScheduledActionEnabledChanged(bool value)
    {
        HasUnsavedChanges = true;
        if (!value)
        {
            _scheduledActionService.Cancel();
        }
    }

    private void UpdateStoragePresentation()
    {
        StorageLocationDisplay = _runtimeModeService.GetDataDirectory(SelectedStorageMode);
        StorageModeDescription = SelectedStorageMode switch
        {
            StorageMode.PortableLocal => "Stores settings beside the executable in a local Data folder.",
            _ => "Stores settings under %APPDATA%\\KeepMeAlive for the current user profile."
        };

        StartOnBootDescription = SelectedStorageMode switch
        {
            StorageMode.PortableLocal => "Starts KeepMeAlive with Windows, but still writes HKCU\\Run as a registry footprint.",
            _ => "Start KeepMeAlive automatically when Windows boots."
        };
    }
}
