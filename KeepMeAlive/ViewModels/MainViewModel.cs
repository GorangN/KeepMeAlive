using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Messages;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.ViewModels;

/// <summary>Coordinates navigation and shell commands through presentation interfaces.</summary>
public partial class MainViewModel : ObservableObject, IRecipient<ShowNotificationMessage>, IRecipient<KeepAliveStatusMessage>
{
    private readonly IDialogService _dialogs;
    private readonly IShellService _shell;
    private bool _isRunning;

    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.Dashboard;
    /// <summary>Gets the keep-alive controls.</summary>
    public DashboardViewModel DashboardViewModel { get; }
    /// <summary>Gets the preferences and schedule editor.</summary>
    public SettingsViewModel SettingsViewModel { get; }
    /// <summary>Gets the connected account controls.</summary>
    public AccountViewModel AccountViewModel { get; }
    /// <summary>Gets the release checker.</summary>
    public UpdateViewModel UpdateViewModel { get; }

    /// <summary>Creates the application shell coordinator.</summary>
    /// <param name="dashboardViewModel">The dashboard.</param>
    /// <param name="settingsViewModel">The preferences.</param>
    /// <param name="accountViewModel">The account.</param>
    /// <param name="updateViewModel">The release checker.</param>
    /// <param name="messenger">The application messenger.</param>
    /// <param name="dialogService">The dialog adapter.</param>
    /// <param name="shell">The shell adapter.</param>
    public MainViewModel(DashboardViewModel dashboardViewModel, SettingsViewModel settingsViewModel,
        AccountViewModel accountViewModel, UpdateViewModel updateViewModel, IMessenger messenger,
        IDialogService dialogService, IShellService shell)
    {
        DashboardViewModel = dashboardViewModel;
        SettingsViewModel = settingsViewModel;
        AccountViewModel = accountViewModel;
        UpdateViewModel = updateViewModel;
        _dialogs = dialogService;
        _shell = shell;
        messenger.RegisterAll(this);
    }

    [RelayCommand]
    private void Navigate(NavigationPage page)
    {
        if (page == CurrentPage) { return; }
        if (SettingsViewModel.HasUnsavedChanges || DashboardViewModel.HasUnsavedChanges)
        {
            switch (_dialogs.ShowUnsavedChangesDialog())
            {
                case UnsavedChangesChoice.Cancel: return;
                case UnsavedChangesChoice.Save:
                    if (!SettingsViewModel.TrySave() || !DashboardViewModel.ApplySettings()) { return; }
                    break;
                case UnsavedChangesChoice.Discard:
                    SettingsViewModel.LoadFromSettings();
                    DashboardViewModel.DiscardChanges();
                    break;
            }
        }
        CurrentPage = page;
    }

    [RelayCommand] private void ShowWindow() => _shell.Show();
    [RelayCommand] private void HideWindow() => _shell.Hide();
    [RelayCommand] private void MinimizeWindow() => _shell.Minimize();
    [RelayCommand] private void ToggleMaximize() => _shell.ToggleMaximize();
    [RelayCommand(CanExecute = nameof(CanTrayStart))] private void TrayStart() => DashboardViewModel.StartCommand.Execute(null);
    private bool CanTrayStart() => !_isRunning;
    [RelayCommand(CanExecute = nameof(CanTrayStop))] private void TrayStop() => DashboardViewModel.StopCommand.Execute(null);
    private bool CanTrayStop() => _isRunning;
    [RelayCommand] private void ExitApplication() => _shell.Exit();
    /// <inheritdoc/>
    public void Receive(ShowNotificationMessage message) => _shell.Notify(message.Title, message.Body);
    /// <inheritdoc/>
    public void Receive(KeepAliveStatusMessage message)
    {
        _isRunning = message.IsRunning;
        TrayStartCommand.NotifyCanExecuteChanged();
        TrayStopCommand.NotifyCanExecuteChanged();
        _shell.SetRunning(message.IsRunning);
    }
}
