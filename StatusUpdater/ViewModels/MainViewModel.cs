using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using KeepMeAlive.Messages;
using KeepMeAlive.Models;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KeepMeAlive.ViewModels;

/// <summary>
/// Shell ViewModel — manages the main window lifecycle, tray icon state,
/// sidebar navigation, and exposes Start/Stop relay commands for the tray context menu.
/// </summary>
public partial class MainViewModel : ObservableObject,
    IRecipient<ShowNotificationMessage>,
    IRecipient<KeepAliveStatusMessage>
{
    private readonly IMessenger _messenger;
    private TaskbarIcon? _trayIcon;
    private Window? _window;
    private bool _isRunning;

    /// <summary>Gets or sets the currently active navigation page.</summary>
    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.Dashboard;

    /// <summary>Gets the dashboard ViewModel (keep-alive controls).</summary>
    public DashboardViewModel DashboardViewModel { get; }

    /// <summary>Gets the settings ViewModel (preferences, license, scheduled actions).</summary>
    public SettingsViewModel SettingsViewModel { get; }

    /// <summary>Gets the update ViewModel (version check).</summary>
    public UpdateViewModel UpdateViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        SettingsViewModel settingsViewModel,
        UpdateViewModel updateViewModel,
        IMessenger messenger)
    {
        DashboardViewModel = dashboardViewModel;
        SettingsViewModel = settingsViewModel;
        UpdateViewModel = updateViewModel;
        _messenger = messenger;

        _messenger.RegisterAll(this);
    }

    /// <summary>Wires the main window and tray icon to this ViewModel.</summary>
    /// <param name="window">The main application window.</param>
    /// <param name="trayIcon">The system tray icon.</param>
    public void Initialize(Window window, TaskbarIcon trayIcon)
    {
        _window = window;
        _trayIcon = trayIcon;
    }

    /// <summary>Navigates to the specified sidebar page.</summary>
    /// <param name="page">The target navigation page.</param>
    [RelayCommand]
    private void Navigate(NavigationPage page)
    {
        CurrentPage = page;
    }

    /// <summary>Shows the main window centered on the primary screen.</summary>
    [RelayCommand]
    private void ShowWindow()
    {
        if (_window == null) { return; }

        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
        _window.Focus();
    }

    /// <summary>Hides the main window (minimizes to tray).</summary>
    [RelayCommand]
    private void HideWindow()
    {
        _window?.Hide();
    }

    /// <summary>Starts the keep-alive loop via the tray context menu.</summary>
    [RelayCommand(CanExecute = nameof(CanTrayStart))]
    private void TrayStart() => DashboardViewModel.StartCommand.Execute(null);

    private bool CanTrayStart() => !_isRunning;

    /// <summary>Stops the keep-alive loop via the tray context menu.</summary>
    [RelayCommand(CanExecute = nameof(CanTrayStop))]
    private void TrayStop() => DashboardViewModel.StopCommand.Execute(null);

    private bool CanTrayStop() => _isRunning;

    /// <summary>Exits the application cleanly.</summary>
    [RelayCommand]
    private void ExitApplication()
    {
        _trayIcon?.Dispose();
        Application.Current.Shutdown();
    }

    /// <inheritdoc/>
    public void Receive(ShowNotificationMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _trayIcon?.ShowBalloonTip(message.Title, message.Body, message.Icon);
        });
    }

    /// <inheritdoc/>
    public void Receive(KeepAliveStatusMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _isRunning = message.IsRunning;

            TrayStartCommand.NotifyCanExecuteChanged();
            TrayStopCommand.NotifyCanExecuteChanged();

            if (_trayIcon == null) { return; }

            _trayIcon.ToolTipText = message.IsRunning
                ? "KeepMeAlive — Running"
                : "KeepMeAlive — Stopped";

            var iconUri = message.IsRunning
                ? new Uri("pack://application:,,,/Resources/Icons/tray_active.ico")
                : new Uri("pack://application:,,,/Resources/Icons/tray_inactive.ico");

            _trayIcon.IconSource = new BitmapImage(iconUri);
        });
    }
}
