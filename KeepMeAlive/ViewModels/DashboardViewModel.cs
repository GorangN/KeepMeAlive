using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Messages;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.ViewModels;

/// <summary>Exposes validated session controls and actual engine state to the dashboard.</summary>
public partial class DashboardViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IKeepAliveEngine _engine;
    private readonly IIdleMonitorService _idleMonitor;
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly TimeProvider _clock;
    private readonly SynchronizationContext? _uiContext;
    private CancellationTokenSource? _cancellation;
    private Task? _runTask;
    private bool _loading;
    private bool _disposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private KeepAliveMethod _selectedMethod;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private int _intervalSeconds = 60;

    [ObservableProperty]
    private bool _keepAwakeEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private int _virtualKeyCode = 126;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private int _mousePixelDelta = 2;

    [ObservableProperty]
    private bool _showAdvancedOptions;

    [ObservableProperty]
    private string _idleTimeDisplay = "Last input: unavailable";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _nextPulseDisplay = "Start a session to begin";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>Creates dashboard controls with testable engine and persistence dependencies.</summary>
    /// <param name="engine">The session engine.</param>
    /// <param name="idleMonitor">The system last-input monitor.</param>
    /// <param name="settingsService">The local settings store.</param>
    /// <param name="messenger">The shell status messenger.</param>
    /// <param name="clock">The countdown clock.</param>
    public DashboardViewModel(IKeepAliveEngine engine, IIdleMonitorService idleMonitor,
        ISettingsService settingsService, IMessenger messenger, TimeProvider clock)
    {
        _engine = engine;
        _idleMonitor = idleMonitor;
        _settingsService = settingsService;
        _messenger = messenger;
        _clock = clock;
        _uiContext = SynchronizationContext.Current;
        DiscardChanges();
        _engine.StatusChanged += OnEngineStatusChanged;
        _idleMonitor.IdleUpdated += OnIdleUpdated;
        _idleMonitor.Start();
    }

    /// <summary>Validates and durably saves the current input preferences.</summary>
    /// <returns>True only after the settings store accepts the update.</returns>
    public bool ApplySettings()
    {
        if (!string.IsNullOrEmpty(InputError))
        {
            return false;
        }

        try
        {
            CreateOptions().Validate();
            var settings = _settingsService.Current;
            settings.KeepAliveMethod = SelectedMethod.ToString();
            settings.IntervalSeconds = IntervalSeconds;
            settings.KeepAwakeEnabled = KeepAwakeEnabled;
            settings.VirtualKeyCode = VirtualKeyCode;
            settings.MousePixelDelta = MousePixelDelta;
            _settingsService.Save(settings);
            HasUnsavedChanges = false;
            ErrorMessage = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception is ArgumentException
                ? "Choose an interval of 20–86400 seconds, a key from F13–F24, and a mouse distance of 1–5 pixels."
                : "Preferences could not be saved. Check that the selected storage folder is writable.";
            return false;
        }
    }

    /// <summary>Restores the last saved input preferences and clears the draft state.</summary>
    public void DiscardChanges()
    {
        _loading = true;
        var settings = _settingsService.Current;
        SelectedMethod = Enum.TryParse<KeepAliveMethod>(settings.KeepAliveMethod, out var method)
            && Enum.IsDefined(method) ? method : KeepAliveMethod.Keyboard;
        IntervalSeconds = settings.IntervalSeconds;
        KeepAwakeEnabled = settings.KeepAwakeEnabled;
        VirtualKeyCode = settings.VirtualKeyCode;
        MousePixelDelta = settings.MousePixelDelta;
        ResetInputText();
        _loading = false;
        HasUnsavedChanges = false;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (!ApplySettings())
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        IsRunning = true;
        ErrorMessage = string.Empty;
        _runTask = _engine.RunAsync(CreateOptions(), cancellation.Token);
        try
        {
            await _runTask.ConfigureAwait(false);
        }
        finally
        {
            cancellation.Dispose();
            if (ReferenceEquals(_cancellation, cancellation))
            {
                _cancellation = null;
            }
        }
    }

    private bool CanStart() => !IsRunning && string.IsNullOrEmpty(InputError);

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _cancellation?.Cancel();

    private bool CanStop() => IsRunning;

    [RelayCommand]
    private void ToggleAdvancedOptions() => ShowAdvancedOptions = !ShowAdvancedOptions;

    partial void OnSelectedMethodChanged(KeepAliveMethod value) => MarkDirty();
    partial void OnIntervalSecondsChanged(int value)
    {
        if (!_editingInputText) { IntervalSecondsText = value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        MarkDirty();
    }
    partial void OnVirtualKeyCodeChanged(int value)
    {
        if (!_editingInputText) { VirtualKeyCodeText = value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        MarkDirty();
    }
    partial void OnMousePixelDeltaChanged(int value)
    {
        if (!_editingInputText) { MousePixelDeltaText = value.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        MarkDirty();
    }

    partial void OnKeepAwakeEnabledChanged(bool value)
    {
        MarkDirty();
        if (IsRunning)
        {
            _engine.SetKeepAwake(value);
        }
    }

    private void MarkDirty()
    {
        if (!_loading)
        {
            HasUnsavedChanges = true;
        }
    }

    private KeepAliveOptions CreateOptions() =>
        new(SelectedMethod, IntervalSeconds, KeepAwakeEnabled, VirtualKeyCode, MousePixelDelta);

    private void OnEngineStatusChanged(object? sender, KeepAliveStatus state)
    {
        Dispatch(() =>
        {
            bool wasRunning = IsRunning;
            IsRunning = state.IsRunning;
            IsPaused = state.IsPaused;
            ErrorMessage = state.Error ?? string.Empty;
            StatusText = state.Error is not null ? "Needs attention" : state.IsPaused ? "Paused while locked" : state.IsRunning ? "Running" : "Ready";
            NextPulseDisplay = state.NextPulseAt.HasValue
                ? $"Next input in {Math.Max(0, (int)Math.Ceiling((state.NextPulseAt.Value - _clock.GetUtcNow()).TotalSeconds))}s"
                : state.IsPaused ? "Resumes when your session is available" : "Start a session to begin";
            _messenger.Send(new KeepAliveStatusMessage(state.IsRunning));
            if (wasRunning && !state.IsRunning && _settingsService.Current.ShowNotifications)
            {
                _messenger.Send(new ShowNotificationMessage("KeepMeAlive", state.Error ?? "Keep-alive stopped."));
            }
        });
    }

    private void OnIdleUpdated(object? sender, EventArgs arguments)
    {
        Dispatch(() =>
        {
            int seconds = _idleMonitor.IdleSeconds;
            if (seconds < 0)
            {
                IdleTimeDisplay = "Last input: unavailable";
                return;
            }

            var elapsed = TimeSpan.FromSeconds(seconds);
            IdleTimeDisplay = elapsed.TotalHours >= 1
                ? $"Last input: {(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m ago"
                : $"Last input: {elapsed.Minutes}m {elapsed.Seconds:D2}s ago";
        });
    }

    private void Dispatch(Action action)
    {
        if (_disposed)
        {
            return;
        }

        if (_uiContext is not null && SynchronizationContext.Current != _uiContext)
        {
            _uiContext.Post(_ =>
            {
                if (!_disposed)
                {
                    action();
                }
            }, null);
        }
        else
        {
            action();
        }
    }

    /// <summary>Cancels the session and stops monitoring before disposing the application.</summary>
    /// <returns>A task completed after the engine releases its power request.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _cancellation?.Cancel();
        if (_runTask is not null)
        {
            await _runTask.ConfigureAwait(false);
        }

        _engine.StatusChanged -= OnEngineStatusChanged;
        _idleMonitor.IdleUpdated -= OnIdleUpdated;
        _idleMonitor.Stop();
    }
}
