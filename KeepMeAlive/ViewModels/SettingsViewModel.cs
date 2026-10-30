using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Messages;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.Globalization;

namespace KeepMeAlive.ViewModels;

/// <summary>Edits a detached preference draft and applies changes only after validation and persistence.</summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private const string PersistedTimeFormat = "dd:MM:yyyy:HH:mm:ss";
    private readonly ISettingsService _settingsService;
    private readonly IAppRuntimeModeService _runtimeModeService;
    private readonly IThemeService _themeService;
    private readonly IStartupService _startupService;
    private readonly IMessenger _messenger;
    private readonly IScheduledActionService _scheduledActionService;
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;
    private bool _isLoading;

    [ObservableProperty] private string _selectedTheme = "Light";
    [ObservableProperty] private bool _startOnBoot;
    [ObservableProperty] private bool _showNotifications = true;
    [ObservableProperty] private bool _startMinimizedToTray;
    [ObservableProperty] private StorageMode _selectedStorageMode = StorageMode.ProfileAppData;
    [ObservableProperty] private bool _enableAutomaticUpdateChecks;
    [ObservableProperty] private bool _enableStartupAccountSync;
    [ObservableProperty] private string _storageLocationDisplay = string.Empty;
    [ObservableProperty] private string _storageModeDescription = string.Empty;
    [ObservableProperty] private string _startOnBootDescription = string.Empty;
    [ObservableProperty] private bool _scheduledActionEnabled;
    [ObservableProperty] private ScheduledAction _selectedScheduledAction = ScheduledAction.Shutdown;
    [ObservableProperty] private string _scheduledActionTime = string.Empty;
    [ObservableProperty] private DateTime? _scheduledDate;
    [ObservableProperty] private string _scheduledTime = "17:00";
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _scheduleStatus = "No action scheduled.";

    /// <summary>Gets the supported one-time system actions.</summary>
    public IReadOnlyList<ScheduledAction> AvailableScheduledActions { get; } = Enum.GetValues<ScheduledAction>();
    /// <summary>Gets the supported local storage locations.</summary>
    public IReadOnlyList<StorageMode> AvailableStorageModes { get; } = Enum.GetValues<StorageMode>();

    /// <summary>Creates the preference editor with its application service boundaries.</summary>
    /// <param name="settingsService">Persists committed settings.</param>
    /// <param name="runtimeModeService">Resolves the selected storage directory.</param>
    /// <param name="themeService">Applies committed or previewed themes.</param>
    /// <param name="startupService">Updates Windows startup registration.</param>
    /// <param name="messenger">Publishes committed preference changes.</param>
    /// <param name="scheduledActionService">Arms or cancels committed scheduled actions.</param>
    public SettingsViewModel(ISettingsService settingsService, IAppRuntimeModeService runtimeModeService,
        IThemeService themeService, IStartupService startupService, IMessenger messenger,
        IScheduledActionService scheduledActionService)
    {
        _settingsService = settingsService;
        _runtimeModeService = runtimeModeService;
        _themeService = themeService;
        _startupService = startupService;
        _messenger = messenger;
        _scheduledActionService = scheduledActionService;
        _scheduledActionService.StatusChanged += OnScheduleStatusChanged;
        LoadFromSettings();
        UpdateScheduleStatus();
    }

    /// <summary>Restores the committed preferences and appearance, discarding the current draft.</summary>
    public void LoadFromSettings()
    {
        _isLoading = true;
        try
        {
            var settings = _settingsService.Current;
            SelectedTheme = settings.Theme;
            StartOnBoot = settings.StartOnBoot;
            ShowNotifications = settings.ShowNotifications;
            StartMinimizedToTray = settings.StartMinimizedToTray;
            SelectedStorageMode = _settingsService.CurrentStorageMode;
            EnableAutomaticUpdateChecks = settings.EnableAutomaticUpdateChecks;
            EnableStartupAccountSync = settings.EnableStartupAccountSync;
            ScheduledActionEnabled = settings.ScheduledActionEnabled;
            SelectedScheduledAction = settings.ScheduledActionType;
            ScheduledActionTime = settings.ScheduledActionTime;
            var scheduled = DateTime.TryParseExact(settings.ScheduledActionTime, PersistedTimeFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : DateTime.Today;
            ScheduledDate = scheduled.Date;
            ScheduledTime = parsed == default ? "17:00" : scheduled.ToString("HH:mm", CultureInfo.InvariantCulture);
            UpdateStoragePresentation();
            _themeService.Apply(SelectedTheme);
            HasUnsavedChanges = false;
            ErrorMessage = _settingsService.LastError ?? string.Empty;
        }
        finally { _isLoading = false; }
    }

    /// <summary>Validates and commits the preference draft; failures leave the draft available for correction.</summary>
    /// <returns>Whether the settings and their system effects were successfully applied.</returns>
    public bool TrySave()
    {
        ErrorMessage = string.Empty;
        DateTime? fireAt;
        try { fireAt = ValidateSchedule(); }
        catch (ArgumentException exception) { ErrorMessage = exception.Message; return false; }
        var original = _settingsService.Current;
        var originalMode = _settingsService.CurrentStorageMode;
        var candidate = _settingsService.Current;
        candidate.Theme = SelectedTheme;
        candidate.StartOnBoot = StartOnBoot;
        candidate.ShowNotifications = ShowNotifications;
        candidate.StartMinimizedToTray = StartMinimizedToTray;
        candidate.EnableAutomaticUpdateChecks = EnableAutomaticUpdateChecks;
        candidate.EnableStartupAccountSync = EnableStartupAccountSync;
        candidate.ScheduledActionEnabled = ScheduledActionEnabled;
        candidate.ScheduledActionType = SelectedScheduledAction;
        candidate.ScheduledActionTime = fireAt?.ToString(PersistedTimeFormat, CultureInfo.InvariantCulture) ?? ScheduledActionTime;
        try
        {
            _settingsService.Save(candidate);
            if (!_settingsService.MigrateStorage(SelectedStorageMode))
            {
                throw new System.IO.IOException(_settingsService.LastError ?? "Storage could not be moved.");
            }
            _startupService.SetStartup(StartOnBoot, StartMinimizedToTray);
            if (fireAt.HasValue) { _scheduledActionService.Schedule(SelectedScheduledAction, fireAt.Value); }
            else { _scheduledActionService.Cancel(); }
            ScheduledActionTime = candidate.ScheduledActionTime;
            _themeService.Apply(SelectedTheme);
            UpdateStoragePresentation();
            HasUnsavedChanges = false;
            _messenger.Send(new ThemeChangedMessage(SelectedTheme));
            _messenger.Send(new StorageChangedMessage(_settingsService.CurrentStorageMode));
            return true;
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or
            System.ComponentModel.Win32Exception or System.Security.Cryptography.CryptographicException or ArgumentException or System.Security.SecurityException)
        {
            ErrorMessage = "Your changes could not be applied. " + exception.Message;
            try
            {
                if (_settingsService.CurrentStorageMode != originalMode && !_settingsService.MigrateStorage(originalMode))
                {
                    ErrorMessage += " The data remains in " + _settingsService.CurrentSettingsDirectory + ".";
                }
                _settingsService.Save(original);
                _startupService.SetStartup(original.StartOnBoot, original.StartMinimizedToTray);
                _scheduledActionService.Restore();
            }
            catch (Exception rollbackException) when (rollbackException is System.IO.IOException or UnauthorizedAccessException or
                System.ComponentModel.Win32Exception or ArgumentException or System.Security.SecurityException)
            {
                ErrorMessage += " Recovery could not finish. Restart the app before changing preferences again.";
            }
            HasUnsavedChanges = true;
            return false;
        }
    }

    [RelayCommand] private void Save() => TrySave();
    [RelayCommand] private void Discard() => LoadFromSettings();

    /// <summary>Stops observing scheduler changes when the application host shuts down.</summary>
    public void Dispose() => _scheduledActionService.StatusChanged -= OnScheduleStatusChanged;

    private void OnScheduleStatusChanged(object? sender, EventArgs eventArgs)
    {
        if (_synchronizationContext is null) { UpdateScheduleStatus(); }
        else { _synchronizationContext.Post(_ => UpdateScheduleStatus(), null); }
    }

    private void UpdateScheduleStatus()
    {
        ScheduleStatus = _scheduledActionService.ScheduledAt is { } time
            ? $"{_settingsService.Current.ScheduledActionType} scheduled for {time:g}."
            : _scheduledActionService.LastError ?? "No action scheduled.";
        if (!HasUnsavedChanges)
        {
            _isLoading = true;
            ScheduledActionEnabled = _settingsService.Current.ScheduledActionEnabled;
            _isLoading = false;
        }
        if (!string.IsNullOrWhiteSpace(_scheduledActionService.LastError)) { ErrorMessage = _scheduledActionService.LastError; }
    }

    private DateTime? ValidateSchedule()
    {
        if (!ScheduledActionEnabled) { return null; }
        if (!ScheduledDate.HasValue || !TimeSpan.TryParseExact(ScheduledTime, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
        {
            throw new ArgumentException("Choose a date and enter a time as HH:mm.");
        }
        var local = DateTime.SpecifyKind(ScheduledDate.Value.Date + time, DateTimeKind.Unspecified);
        if (DateTime.TryParseExact(_settingsService.Current.ScheduledActionTime, PersistedTimeFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var persisted) &&
            persisted.Date == local.Date && persisted.Hour == local.Hour && persisted.Minute == local.Minute)
        {
            local = persisted;
        }
        if (local <= DateTime.Now) { throw new ArgumentException("Choose a scheduled time in the future."); }
        if (TimeZoneInfo.Local.IsInvalidTime(local) || TimeZoneInfo.Local.IsAmbiguousTime(local))
        {
            throw new ArgumentException("This time occurs during a daylight-saving clock change. Choose another time.");
        }
        return local;
    }

    partial void OnSelectedThemeChanged(string value) { MarkDirty(); _themeService.Apply(value); }
    partial void OnStartOnBootChanged(bool value) => MarkDirty();
    partial void OnShowNotificationsChanged(bool value) => MarkDirty();
    partial void OnStartMinimizedToTrayChanged(bool value) => MarkDirty();
    partial void OnEnableAutomaticUpdateChecksChanged(bool value) => MarkDirty();
    partial void OnEnableStartupAccountSyncChanged(bool value) => MarkDirty();
    partial void OnScheduledActionEnabledChanged(bool value) => MarkDirty();
    partial void OnSelectedScheduledActionChanged(ScheduledAction value) => MarkDirty();
    partial void OnScheduledActionTimeChanged(string value) => MarkDirty();
    partial void OnScheduledDateChanged(DateTime? value) => MarkDirty();
    partial void OnScheduledTimeChanged(string value) => MarkDirty();
    partial void OnSelectedStorageModeChanged(StorageMode value) { MarkDirty(); UpdateStoragePresentation(); }
    private void MarkDirty() { if (!_isLoading) { HasUnsavedChanges = true; } }
    private void UpdateStoragePresentation()
    {
        StorageLocationDisplay = _runtimeModeService.GetDataDirectory(SelectedStorageMode);
        StorageModeDescription = SelectedStorageMode == StorageMode.PortableLocal
            ? "Settings stay beside the app. Protected sign-in is tied to this Windows user."
            : "Settings stay in your Windows profile. Protected sign-in is tied to this Windows user.";
        StartOnBootDescription = "Launch KeepMeAlive when you sign in to Windows.";
    }
}
