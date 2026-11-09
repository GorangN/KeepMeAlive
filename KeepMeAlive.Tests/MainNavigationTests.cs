using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.Tests.Accounts;
using KeepMeAlive.ViewModels;

namespace KeepMeAlive.Tests;

/// <summary>Verifies that the Settings destination uses the same draft guard as every existing page.</summary>
public sealed class MainNavigationTests
{
    /// <summary>Checks that cancellation retains the page and both preference drafts.</summary>
    [Theory]
    [InlineData(NavigationPage.Settings, NavigationPage.Dashboard)]
    [InlineData(NavigationPage.Dashboard, NavigationPage.Settings)]
    public async Task CancelKeepsTheCurrentPageAndDrafts(NavigationPage origin, NavigationPage destination)
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(origin);
        fixture.EditDrafts();
        fixture.Dialogs.Choice = UnsavedChangesChoice.Cancel;
        fixture.Main.NavigateCommand.Execute(destination);
        Assert.Equal(origin, fixture.Main.CurrentPage);
        Assert.True(fixture.Settings.HasUnsavedChanges);
        Assert.True(fixture.Dashboard.HasUnsavedChanges);
        Assert.Equal(60, fixture.Store.Current.IntervalSeconds);
        Assert.Equal("Light", fixture.Store.Current.Theme);
    }

    /// <summary>Checks that discarding reloads both editors before navigation.</summary>
    [Theory]
    [InlineData(NavigationPage.Settings, NavigationPage.Dashboard)]
    [InlineData(NavigationPage.Dashboard, NavigationPage.Settings)]
    public async Task DiscardRestoresBothDraftsAndNavigates(NavigationPage origin, NavigationPage destination)
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(origin);
        fixture.EditDrafts();
        fixture.Dialogs.Choice = UnsavedChangesChoice.Discard;
        fixture.Main.NavigateCommand.Execute(destination);
        Assert.Equal(destination, fixture.Main.CurrentPage);
        Assert.Equal("Light", fixture.Settings.SelectedTheme);
        Assert.Equal("60", fixture.Dashboard.IntervalSecondsText);
        Assert.False(fixture.Settings.HasUnsavedChanges);
        Assert.False(fixture.Dashboard.HasUnsavedChanges);
    }

    /// <summary>Checks that successful saves persist both drafts and navigate to the requested page.</summary>
    [Theory]
    [InlineData(NavigationPage.Settings, NavigationPage.Dashboard)]
    [InlineData(NavigationPage.Dashboard, NavigationPage.Settings)]
    public async Task SaveCommitsBothDraftsAndNavigates(NavigationPage origin, NavigationPage destination)
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(origin);
        fixture.EditDrafts();
        fixture.Dialogs.Choice = UnsavedChangesChoice.Save;
        fixture.Main.NavigateCommand.Execute(destination);
        Assert.Equal(destination, fixture.Main.CurrentPage);
        Assert.Equal("Dark", fixture.Store.Current.Theme);
        Assert.Equal(90, fixture.Store.Current.IntervalSeconds);
        Assert.False(fixture.Settings.HasUnsavedChanges);
        Assert.False(fixture.Dashboard.HasUnsavedChanges);
    }

    /// <summary>Checks that invalid schedule/input drafts or unavailable persistence prevent navigation.</summary>
    [Theory]
    [InlineData("schedule")]
    [InlineData("input")]
    [InlineData("storage")]
    public async Task SaveFailureStaysOnSettingsAndRetainsTheFailingDraft(string failure)
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(NavigationPage.Settings);
        fixture.EditDrafts();
        fixture.Dialogs.Choice = UnsavedChangesChoice.Save;
        if (failure == "schedule")
        {
            fixture.Settings.ScheduledActionEnabled = true;
            fixture.Settings.ScheduledTime = "invalid";
        }
        if (failure == "input")
        {
            fixture.Dashboard.IntervalSecondsText = "invalid";
        }
        fixture.Store.FailSave = failure == "storage";
        fixture.Main.NavigateCommand.Execute(NavigationPage.Account);
        Assert.Equal(NavigationPage.Settings, fixture.Main.CurrentPage);
        if (failure == "input")
        {
            Assert.True(fixture.Dashboard.HasUnsavedChanges);
            Assert.NotEmpty(fixture.Dashboard.InputError);
        }
        else
        {
            Assert.True(fixture.Settings.HasUnsavedChanges);
            Assert.NotEmpty(fixture.Settings.ErrorMessage);
        }
    }

    /// <summary>Checks that selecting an already active page leaves the draft untouched without prompting.</summary>
    [Fact]
    public async Task SelectingTheActiveSettingsPageDoesNotPromptOrDiscard()
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(NavigationPage.Settings);
        fixture.EditDrafts();
        fixture.Main.NavigateCommand.Execute(NavigationPage.Settings);
        Assert.Equal(0, fixture.Dialogs.Prompts);
        Assert.True(fixture.Settings.HasUnsavedChanges);
    }

    /// <summary>Checks that nested command delivery cannot stack dialogs or replace the original destination.</summary>
    /// <param name="choice">The decision returned by the single active prompt.</param>
    [Theory]
    [InlineData(UnsavedChangesChoice.Cancel)]
    [InlineData(UnsavedChangesChoice.Save)]
    [InlineData(UnsavedChangesChoice.Discard)]
    public async Task ReentrantNavigationKeepsOnePromptAndAllowsLaterNavigation(UnsavedChangesChoice choice)
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(NavigationPage.Settings);
        fixture.EditDrafts();
        fixture.Dialogs.Choice = choice;
        fixture.Dialogs.OnPrompt = () => fixture.Main.NavigateCommand.Execute(NavigationPage.Account);

        fixture.Main.NavigateCommand.Execute(NavigationPage.Dashboard);

        Assert.Equal(1, fixture.Dialogs.Prompts);
        Assert.Equal(choice == UnsavedChangesChoice.Cancel ? NavigationPage.Settings : NavigationPage.Dashboard, fixture.Main.CurrentPage);
        fixture.Dialogs.Choice = UnsavedChangesChoice.Discard;
        fixture.Main.NavigateCommand.Execute(NavigationPage.Advanced);
        Assert.Equal(NavigationPage.Advanced, fixture.Main.CurrentPage);
        Assert.Equal(choice == UnsavedChangesChoice.Cancel ? 2 : 1, fixture.Dialogs.Prompts);
    }

    /// <summary>Checks that save failures release the navigation guard for a corrected retry.</summary>
    /// <param name="failure">The validation or persistence failure applied to the first navigation.</param>
    [Theory]
    [InlineData("schedule")]
    [InlineData("input")]
    [InlineData("storage")]
    public async Task FailedSaveReleasesReentrancyGuardForCorrection(string failure)
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(NavigationPage.Settings);
        fixture.EditDrafts();
        fixture.Dialogs.Choice = UnsavedChangesChoice.Save;
        fixture.Dialogs.OnPrompt = () => fixture.Main.NavigateCommand.Execute(NavigationPage.Account);
        if (failure == "schedule")
        {
            fixture.Settings.ScheduledActionEnabled = true;
            fixture.Settings.ScheduledTime = "invalid";
        }
        if (failure == "input")
        {
            fixture.Dashboard.IntervalSecondsText = "invalid";
        }
        fixture.Store.FailSave = failure == "storage";

        fixture.Main.NavigateCommand.Execute(NavigationPage.Dashboard);

        Assert.Equal(NavigationPage.Settings, fixture.Main.CurrentPage);
        Assert.Equal(1, fixture.Dialogs.Prompts);
        fixture.Store.FailSave = false;
        fixture.Settings.ScheduledActionEnabled = false;
        fixture.Dashboard.IntervalSecondsText = "90";
        fixture.Main.NavigateCommand.Execute(NavigationPage.Dashboard);
        Assert.Equal(NavigationPage.Dashboard, fixture.Main.CurrentPage);
        Assert.Equal(2, fixture.Dialogs.Prompts);
    }

    /// <summary>Checks that a failed modal host does not permanently disable subsequent navigation.</summary>
    [Fact]
    public async Task PromptExceptionReleasesReentrancyGuard()
    {
        await using var fixture = new Fixture();
        fixture.Main.NavigateCommand.Execute(NavigationPage.Settings);
        fixture.EditDrafts();
        fixture.Dialogs.OnPrompt = () => throw new InvalidOperationException("Modal host unavailable");
        Assert.Throws<InvalidOperationException>(() => fixture.Main.NavigateCommand.Execute(NavigationPage.Dashboard));
        fixture.Dialogs.Choice = UnsavedChangesChoice.Discard;
        fixture.Main.NavigateCommand.Execute(NavigationPage.Dashboard);
        Assert.Equal(NavigationPage.Dashboard, fixture.Main.CurrentPage);
        Assert.Equal(2, fixture.Dialogs.Prompts);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal Store Store { get; } = new();
        internal Dialogs Dialogs { get; } = new();
        internal SettingsViewModel Settings { get; }
        internal DashboardViewModel Dashboard { get; }
        internal MainViewModel Main { get; }
        private readonly AccountViewModel _account = new(new TestAccountService(), new TestAuthenticationService());

        internal Fixture()
        {
            var messenger = new WeakReferenceMessenger();
            var environment = new EnvironmentStub();
            Settings = new SettingsViewModel(Store, environment, environment, environment, messenger, environment);
            Dashboard = new DashboardViewModel(environment, environment, Store, messenger, TimeProvider.System);
            Main = new MainViewModel(Dashboard, Settings, _account, new UpdateViewModel(environment, environment), messenger, Dialogs, environment);
        }

        internal void EditDrafts()
        {
            Settings.SelectedTheme = "Dark";
            Dashboard.IntervalSecondsText = "90";
        }

        public async ValueTask DisposeAsync()
        {
            Settings.Dispose();
            await Dashboard.DisposeAsync();
            await _account.DisposeAsync();
        }
    }

    private sealed class Store : ISettingsService
    {
        private AppSettings _current = new();
        internal bool FailSave { get; set; }
        public AppSettings Current => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(_current))!;
        public StorageMode CurrentStorageMode => StorageMode.ProfileAppData;
        public string CurrentSettingsDirectory => string.Empty;
        public string CurrentSettingsPath => string.Empty;
        public string? LastError => null;
        public AppSettings Load() => Current;
        public void Save(AppSettings settings)
        {
            if (FailSave)
            {
                throw new IOException("Storage unavailable");
            }
            _current = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
        }
        public bool MigrateStorage(StorageMode targetStorageMode) => true;
    }

    private sealed class Dialogs : IDialogService
    {
        internal UnsavedChangesChoice Choice { get; set; } = UnsavedChangesChoice.Cancel;
        internal int Prompts { get; private set; }
        internal Action? OnPrompt { get; set; }
        public UnsavedChangesChoice ShowUnsavedChangesDialog()
        {
            Prompts++;
            var callback = OnPrompt;
            OnPrompt = null;
            callback?.Invoke();
            return Choice;
        }
        public bool ShowConfirmation(string title, string message) => false;
    }

    private sealed class EnvironmentStub : IAppRuntimeModeService, IThemeService, IStartupService,
        IScheduledActionService, IKeepAliveEngine, IIdleMonitorService, IShellService, IUpdateService, IBrowserLauncher
    {
        public string ApplicationDirectory => Path.GetTempPath();
        public bool IsPortableDistribution => false;
        public StorageMode DefaultStorageMode => StorageMode.ProfileAppData;
        public StorageMode GetConfiguredStorageMode() => StorageMode.ProfileAppData;
        public void SetConfiguredStorageMode(StorageMode storageMode) { }
        public string GetDataDirectory(StorageMode storageMode) => Path.GetTempPath();
        public string CurrentTheme { get; private set; } = "Light";
        public void Apply(string themeName) => CurrentTheme = themeName;
        public bool IsStartupEnabled => false;
        public void SetStartup(bool enable, bool minimizedToTray) { }
        public bool IsScheduled => false;
        public DateTime? ScheduledAt => null;
        public string? LastError => null;
        event EventHandler? IScheduledActionService.StatusChanged { add { } remove { } }
        public void Schedule(ScheduledAction action, DateTime executeAt) { }
        public void Cancel() { }
        public void Restore() { }
        event EventHandler<KeepAliveStatus>? IKeepAliveEngine.StatusChanged { add { } remove { } }
        public Task RunAsync(KeepAliveOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
        public void SetKeepAwake(bool enabled) { }
        public int IdleSeconds => 0;
        public event EventHandler? IdleUpdated { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Show() { }
        public void Hide() { }
        public void Minimize() { }
        public void ToggleMaximize() { }
        public void Exit() { }
        public void SetRunning(bool running) { }
        public void Notify(string title, string body) { }
        public string LatestVersion => string.Empty;
        public string ReleaseUrl => string.Empty;
        public Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Open(Uri uri) { }
    }
}
