using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using StatusUpdater.Messages;
using StatusUpdater.Services.Interfaces;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StatusUpdater.ViewModels;

public partial class MainViewModel : ObservableObject,
    IRecipient<ShowNotificationMessage>,
    IRecipient<KeepAliveStatusMessage>,
    IRecipient<CloseSettingsMessage>
{
    private readonly IMessenger _messenger;
    private readonly IThemeService _themeService;
    private TaskbarIcon? _trayIcon;
    private Window? _window;

    [ObservableProperty]
    private bool _isSettingsOpen;

    public DashboardViewModel DashboardViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public UpdateViewModel UpdateViewModel { get; }

    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        SettingsViewModel settingsViewModel,
        UpdateViewModel updateViewModel,
        IMessenger messenger,
        IThemeService themeService)
    {
        DashboardViewModel = dashboardViewModel;
        SettingsViewModel = settingsViewModel;
        UpdateViewModel = updateViewModel;
        _messenger = messenger;
        _themeService = themeService;

        _messenger.RegisterAll(this);
    }

    public void Initialize(Window window, TaskbarIcon trayIcon)
    {
        _window = window;
        _trayIcon = trayIcon;
    }

    [RelayCommand]
    private void ShowWindow()
    {
        if (_window == null) return;

        var workArea = SystemParameters.WorkArea;
        _window.Left = workArea.Right - _window.Width - 12;
        _window.Top = workArea.Bottom - _window.Height - 12;

        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
        _window.Focus();
    }

    [RelayCommand]
    private void HideWindow()
    {
        if (IsSettingsOpen)
            IsSettingsOpen = false;
        _window?.Hide();
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        if (IsSettingsOpen)
        {
            IsSettingsOpen = false;
        }
        else
        {
            SettingsViewModel.LoadFromSettings();
            IsSettingsOpen = true;
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        _trayIcon?.Dispose();
        Application.Current.Shutdown();
    }

    public void Receive(ShowNotificationMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _trayIcon?.ShowBalloonTip(message.Title, message.Body, message.Icon);
        });
    }

    public void Receive(CloseSettingsMessage message)
    {
        Application.Current.Dispatcher.Invoke(() => IsSettingsOpen = false);
    }

    public void Receive(KeepAliveStatusMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_trayIcon == null) return;
            _trayIcon.ToolTipText = message.IsRunning
                ? "Status Updater — Running"
                : "Status Updater — Stopped";

            var iconUri = message.IsRunning
                ? new Uri("pack://application:,,,/Resources/Icons/tray_active.ico")
                : new Uri("pack://application:,,,/Resources/Icons/tray_inactive.ico");

            _trayIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(iconUri);
        });
    }
}
