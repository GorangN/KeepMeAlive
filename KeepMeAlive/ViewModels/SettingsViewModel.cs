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
    private bool _startMinimizedToTray;

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

        // Load settings immediately since the main window is always open
        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Current;
        SelectedTheme = "Light"; // Light mode only
        StartOnBoot = _startupService.IsStartupEnabled;
        ShowNotifications = s.ShowNotifications;
        StartMinimizedToTray = s.StartMinimizedToTray;
        ScheduledActionEnabled = s.ScheduledActionEnabled;
        SelectedScheduledAction = s.ScheduledActionType;
        ScheduledActionTime = s.ScheduledActionTime;
        HasUnsavedChanges = false;
    }

    partial void OnStartOnBootChanged(bool value) => HasUnsavedChanges = true;

    partial void OnShowNotificationsChanged(bool value) => HasUnsavedChanges = true;

    partial void OnStartMinimizedToTrayChanged(bool value) => HasUnsavedChanges = true;

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
        var settings = _settingsService.Current;
        settings.Theme = SelectedTheme;
        settings.ShowNotifications = ShowNotifications;
        settings.StartMinimizedToTray = StartMinimizedToTray;
        settings.ScheduledActionEnabled = ScheduledActionEnabled;
        settings.ScheduledActionType = SelectedScheduledAction;
        settings.ScheduledActionTime = ScheduledActionTime;
        _settingsService.Save(settings);
        HasUnsavedChanges = false;

        _startupService.SetStartup(StartOnBoot, StartMinimizedToTray);
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
    }

    partial void OnScheduledActionEnabledChanged(bool value)
    {
        HasUnsavedChanges = true;
        if (!value)
        {
            _scheduledActionService.Cancel();
        }
    }
}
