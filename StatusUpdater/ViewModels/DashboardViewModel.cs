using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StatusUpdater.Messages;
using StatusUpdater.Models;
using StatusUpdater.Services;
using StatusUpdater.Services.Interfaces;
using System.Windows.Threading;

namespace StatusUpdater.ViewModels;

public enum KeepAliveMethod { Keyboard, Mouse }

public partial class DashboardViewModel : ObservableObject
{
    private readonly IKeepAwakeService _keepAwake;
    private readonly IIdleMonitorService _idleMonitor;
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;

    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _idleTimer;
    private readonly Random _rnd = new();

    // ═══ Bound Properties ═══

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private KeepAliveMethod _selectedMethod = KeepAliveMethod.Keyboard;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private int _intervalSeconds = 60;

    [ObservableProperty]
    private bool _keepAwakeEnabled;

    [ObservableProperty]
    private int _virtualKeyCode = 126; // VK_F22

    [ObservableProperty]
    private int _mousePixelDelta = 2;

    [ObservableProperty]
    private bool _showAdvancedOptions;

    [ObservableProperty]
    private string _idleTimeDisplay = "Idle: —";

    // ═══ Constructor ═══

    public DashboardViewModel(
        IKeepAwakeService keepAwake,
        IIdleMonitorService idleMonitor,
        ISettingsService settingsService,
        IMessenger messenger)
    {
        _keepAwake = keepAwake;
        _idleMonitor = idleMonitor;
        _settingsService = settingsService;
        _messenger = messenger;

        LoadFromSettings();

        _idleMonitor.IdleUpdated += (_, _) => UpdateIdleDisplay();
        _idleMonitor.Start();

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleTimer.Tick += (_, _) => UpdateIdleDisplay();
    }

    // ═══ Commands ═══

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;

        if (KeepAwakeEnabled)
            _keepAwake.Enable();

        SaveToSettings();

        if (_settingsService.Current.ShowNotifications)
            _messenger.Send(new ShowNotificationMessage("Status Updater", "Keep-alive started."));

        _messenger.Send(new KeepAliveStatusMessage(true));
        _idleTimer.Start();

        var robustShift = new KeyboardStrategy_ScanCodeShift();
        var strategy = BuildStrategy();

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                robustShift.Pulse();
                strategy.Pulse();

                if (KeepAwakeEnabled) _keepAwake.Refresh();

                var jitter = _rnd.Next(-15, 16);
                var wait = Math.Max(20, IntervalSeconds + jitter);
                await Task.Delay(TimeSpan.FromSeconds(wait), _cts.Token);
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            if (KeepAwakeEnabled) _keepAwake.Disable();
            IsRunning = false;
            _idleTimer.Stop();
            _messenger.Send(new KeepAliveStatusMessage(false));

            if (_settingsService.Current.ShowNotifications)
                _messenger.Send(new ShowNotificationMessage("Status Updater", "Keep-alive stopped."));
        }
    }

    private bool CanStart() => !IsRunning && IntervalSeconds >= 20;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _cts?.Cancel();

    private bool CanStop() => IsRunning;

    [RelayCommand]
    private void ToggleAdvancedOptions() => ShowAdvancedOptions = !ShowAdvancedOptions;

    // ═══ Private helpers ═══

    private IKeepAliveStrategy BuildStrategy() => SelectedMethod switch
    {
        KeepAliveMethod.Mouse => new MouseStrategy(MousePixelDelta),
        _ => new KeyboardStrategy_VirtualKey((ushort)VirtualKeyCode)
    };

    private void UpdateIdleDisplay()
    {
        var secs = _idleMonitor.IdleSeconds;
        if (secs < 0) { IdleTimeDisplay = "Idle: —"; return; }

        var ts = TimeSpan.FromSeconds(secs);
        IdleTimeDisplay = ts.TotalHours >= 1
            ? $"Idle: {(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s"
            : $"Idle: {ts.Minutes}m {ts.Seconds:D2}s";
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.Current;
        if (Enum.TryParse<KeepAliveMethod>(s.KeepAliveMethod, out var method))
            SelectedMethod = method;
        IntervalSeconds = s.IntervalSeconds;
        KeepAwakeEnabled = s.KeepAwakeEnabled;
        VirtualKeyCode = s.VirtualKeyCode;
        MousePixelDelta = s.MousePixelDelta;
    }

    private void SaveToSettings()
    {
        var s = _settingsService.Current;
        s.KeepAliveMethod = SelectedMethod.ToString();
        s.IntervalSeconds = IntervalSeconds;
        s.KeepAwakeEnabled = KeepAwakeEnabled;
        s.VirtualKeyCode = VirtualKeyCode;
        s.MousePixelDelta = MousePixelDelta;
        _settingsService.Save(s);
    }
}
