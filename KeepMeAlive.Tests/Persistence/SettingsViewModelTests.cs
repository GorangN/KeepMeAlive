using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.ViewModels;
using System.IO;

namespace KeepMeAlive.Tests.Persistence;

/// <summary>Checks draft isolation and failure recovery across preference side effects.</summary>
public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "KeepMeAlive-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Checks that disabling a schedule in an uncommitted draft cannot cancel the live action.</summary>
    [Fact]
    public void DraftScheduleChangesDoNotAffectRuntimeUntilSave()
    {
        var (viewModel, settings, schedule, _) = Create();
        viewModel.ScheduledActionEnabled = true;
        viewModel.ScheduledDate = DateTime.Today.AddDays(1);
        Assert.Equal(0, schedule.CancelCount);
        Assert.False(settings.Current.ScheduledActionEnabled);
        viewModel.DiscardCommand.Execute(null);
        Assert.False(viewModel.ScheduledActionEnabled);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal(0, schedule.CancelCount);
    }

    /// <summary>Checks that discarding a theme preview restores the persisted appearance.</summary>
    [Fact]
    public void DiscardRestoresThemeAndClearsDirtyState()
    {
        var (viewModel, _, _, theme) = Create();
        viewModel.SelectedTheme = "Dark";
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("Dark", theme.CurrentTheme);
        viewModel.DiscardCommand.Execute(null);
        Assert.Equal("Light", theme.CurrentTheme);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    /// <summary>Checks that malformed schedule input does not commit unrelated draft changes.</summary>
    [Fact]
    public void InvalidSchedulePreventsAllPreferenceEffects()
    {
        var (viewModel, settings, schedule, _) = Create();
        viewModel.ShowNotifications = false;
        viewModel.ScheduledActionEnabled = true;
        viewModel.ScheduledTime = "nonsense";
        Assert.False(viewModel.TrySave());
        Assert.True(settings.Current.ShowNotifications);
        Assert.Equal(0, schedule.ScheduleCount);
        Assert.Equal(0, schedule.CancelCount);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.NotEmpty(viewModel.ErrorMessage);
    }

    /// <summary>Checks that a registry failure restores settings and leaves the draft available.</summary>
    [Fact]
    public void StartupFailureRollsBackCommittedSettings()
    {
        var runtime = new TestRuntime(_directory);
        var settings = new SettingsService(runtime, new TestSecrets());
        settings.Save(new AppSettings());
        var schedule = new TestSchedule();
        var viewModel = new SettingsViewModel(settings, runtime, new TestTheme(), new TestStartup { FailEnable = true },
            new WeakReferenceMessenger(), schedule);
        viewModel.StartOnBoot = true;
        viewModel.ShowNotifications = false;
        Assert.False(viewModel.TrySave());
        Assert.False(settings.Current.StartOnBoot);
        Assert.True(settings.Current.ShowNotifications);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal(1, schedule.RestoreCount);
    }

    /// <summary>Checks that saving unrelated preferences preserves seconds in an existing legacy schedule.</summary>
    [Fact]
    public void SavingPreferencesPreservesLegacyScheduleSeconds()
    {
        var (viewModel, settings, _, _) = Create();
        var original = settings.Current;
        original.ScheduledActionEnabled = true;
        original.ScheduledActionTime = DateTime.Today.AddDays(1).AddHours(17).AddSeconds(37)
            .ToString("dd:MM:yyyy:HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        settings.Save(original);
        viewModel.LoadFromSettings();
        viewModel.ShowNotifications = false;
        Assert.True(viewModel.TrySave());
        Assert.Equal(original.ScheduledActionTime, settings.Current.ScheduledActionTime);
    }

    /// <summary>Deletes only the temporary data created by this test instance.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory)) { Directory.Delete(_directory, recursive: true); }
    }

    private (SettingsViewModel ViewModel, SettingsService Settings, TestSchedule Schedule, TestTheme Theme) Create()
    {
        var runtime = new TestRuntime(_directory);
        var settings = new SettingsService(runtime, new TestSecrets());
        var schedule = new TestSchedule();
        var theme = new TestTheme();
        var viewModel = new SettingsViewModel(settings, runtime, theme, new TestStartup(), new WeakReferenceMessenger(), schedule);
        return (viewModel, settings, schedule, theme);
    }

    private sealed class TestRuntime(string directory) : IAppRuntimeModeService
    {
        public string ApplicationDirectory => directory;
        public bool IsPortableDistribution => false;
        public StorageMode DefaultStorageMode => StorageMode.ProfileAppData;
        public StorageMode GetConfiguredStorageMode() => StorageMode.ProfileAppData;
        public void SetConfiguredStorageMode(StorageMode storageMode) { }
        public string GetDataDirectory(StorageMode storageMode) => Path.Combine(directory, storageMode.ToString());
    }
    private sealed class TestSecrets : ISecretStore
    {
        public string GetLicenseKey() => string.Empty;
        public string GetLicenseKey(StorageMode storageMode) => string.Empty;
        public void SaveLicenseKey(string? licenseKey) { }
        public void SaveLicenseKey(string? licenseKey, StorageMode storageMode) { }
        public void DeleteLicenseKey(StorageMode storageMode) { }
        public string GetSession() => string.Empty;
        public void SaveSession(string? session) { }
        public void DeleteSession() { }
        public void CopyTo(StorageMode sourceMode, StorageMode targetMode) { }
        public void DeleteAll(StorageMode storageMode) { }
    }
    private sealed class TestTheme : IThemeService
    {
        public string CurrentTheme { get; private set; } = "Light";
        public void Apply(string themeName) => CurrentTheme = themeName;
    }
    private sealed class TestStartup : IStartupService
    {
        public bool FailEnable { get; set; }
        public bool IsStartupEnabled { get; private set; }
        public void SetStartup(bool enable, bool minimizedToTray)
        {
            if (enable && FailEnable) { throw new IOException("Simulated registry failure."); }
            IsStartupEnabled = enable;
        }
    }
    private sealed class TestSchedule : IScheduledActionService
    {
        public int ScheduleCount { get; private set; }
        public int CancelCount { get; private set; }
        public int RestoreCount { get; private set; }
        public bool IsScheduled => ScheduleCount > CancelCount;
        public DateTime? ScheduledAt => null;
        public string? LastError => null;
        public event EventHandler? StatusChanged { add { } remove { } }
        public void Schedule(ScheduledAction action, DateTime executeAt) => ScheduleCount++;
        public void Cancel() => CancelCount++;
        public void Restore() => RestoreCount++;
    }
}
