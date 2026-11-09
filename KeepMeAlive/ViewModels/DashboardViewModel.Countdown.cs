using KeepMeAlive.Models;

namespace KeepMeAlive.ViewModels;

public partial class DashboardViewModel
{
    private DateTimeOffset? _pulseDeadline;
    private double _pulseCycleSeconds;
    private string _pulseCountdownDisplay = "00:01:00";
    private double _pulseProgress;
    private bool _hasPulseCountdown;

    /// <summary>Gets the remaining pulse time, the configured stopped interval, or a paused placeholder.</summary>
    public string PulseCountdownDisplay
    {
        get => _pulseCountdownDisplay;
        private set => SetProperty(ref _pulseCountdownDisplay, value);
    }

    /// <summary>Gets the fraction remaining in the observed engine cycle, bounded between zero and one.</summary>
    public double PulseProgress
    {
        get => _pulseProgress;
        private set => SetProperty(ref _pulseProgress, value);
    }

    /// <summary>Gets whether the ring represents a running engine deadline rather than a neutral preview.</summary>
    public bool HasPulseCountdown
    {
        get => _hasPulseCountdown;
        private set => SetProperty(ref _hasPulseCountdown, value);
    }

    private void UpdatePulseCountdown(KeepAliveStatus state)
    {
        HasPulseCountdown = state.IsRunning && !state.IsPaused && state.Error is null && state.NextPulseAt.HasValue;
        if (!HasPulseCountdown)
        {
            PulseProgress = 0;
            PulseCountdownDisplay = state.IsPaused && state.Error is null
                ? "--:--:--"
                : FormatCountdown(Math.Max(0, IntervalSeconds));
            if (!state.IsRunning || state.Error is not null)
            {
                _pulseDeadline = null;
                _pulseCycleSeconds = 0;
            }
            return;
        }

        var deadline = state.NextPulseAt!.Value;
        var remainingSeconds = Math.Max(0, (deadline - _clock.GetUtcNow()).TotalSeconds);
        if (_pulseDeadline != deadline)
        {
            _pulseDeadline = deadline;
            _pulseCycleSeconds = remainingSeconds;
        }

        PulseCountdownDisplay = FormatCountdown((long)Math.Ceiling(remainingSeconds));
        PulseProgress = _pulseCycleSeconds > 0 ? Math.Clamp(remainingSeconds / _pulseCycleSeconds, 0, 1) : 0;
    }

    private static string FormatCountdown(long seconds) =>
        FormattableString.Invariant($"{seconds / 3600:D2}:{seconds / 60 % 60:D2}:{seconds % 60:D2}");
}
