using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.Globalization;
using System.IO;

namespace KeepMeAlive.Services;

/// <summary>Serializes schedule replacement/execution and refuses stale or missed actions.</summary>
public sealed class ScheduledActionService : IScheduledActionService, IDisposable
{
    private readonly object _gate = new();
    private readonly ISystemActionExecutor _executor;
    private readonly ISettingsService _settings;
    private readonly TimeProvider _clock;
    private readonly TimeZoneInfo _timeZone;
    private ITimer? _timer;
    private long _generation;
    private DateTimeOffset _dueAt;
    private ScheduledAction _action;
    private bool _disposed;

    /// <summary>Creates a scheduler with isolated system actions and persistence.</summary>
    /// <param name="executor">The native action executor.</param>
    /// <param name="settings">The persisted schedule settings.</param>
    /// <param name="clock">The clock and timer source.</param>
    /// <param name="timeZone">The local schedule time zone; defaults to the operating-system zone.</param>
    public ScheduledActionService(ISystemActionExecutor executor, ISettingsService settings, TimeProvider clock, TimeZoneInfo? timeZone = null)
    {
        _executor = executor;
        _settings = settings;
        _clock = clock;
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    /// <inheritdoc/>
    public bool IsScheduled { get; private set; }

    /// <inheritdoc/>
    public DateTime? ScheduledAt { get; private set; }

    /// <inheritdoc/>
    public string? LastError { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? StatusChanged;

    /// <inheritdoc/>
    public void Schedule(ScheduledAction action, DateTime executeAt)
    {
        DateTimeOffset dueAt = ResolveExecutionTime(executeAt);
        if (dueAt <= _clock.GetUtcNow() || !Enum.IsDefined(action))
        {
            throw new ArgumentException("Choose a supported action and a future execution time.", nameof(executeAt));
        }

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            CancelCore();
            _action = action;
            _dueAt = dueAt;
            ScheduledAt = TimeZoneInfo.ConvertTime(dueAt, _timeZone).DateTime;
            IsScheduled = true;
            LastError = null;
            long generation = _generation;
            _timer = _clock.CreateTimer(_ => OnTimer(generation), null, GetNextDelay(), Timeout.InfiniteTimeSpan);
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        lock (_gate)
        {
            CancelCore();
            LastError = null;
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Restore()
    {
        Cancel();
        var settings = _settings.Current;
        if (!settings.ScheduledActionEnabled)
        {
            return;
        }

        if (DateTime.TryParseExact(settings.ScheduledActionTime, "dd:MM:yyyy:HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var executeAt))
        {
            try
            {
                Schedule(settings.ScheduledActionType, executeAt);
                return;
            }
            catch (ArgumentException)
            {
            }
        }

        lock (_gate)
        {
            CancelCore();
            LastError = "The saved action was missed or its time is invalid. It has been disarmed.";
            TryPersistDisarmed();
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Cancels pending callbacks and releases timer resources.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            CancelCore();
        }
    }

    private void OnTimer(long generation)
    {
        lock (_gate)
        {
            if (_disposed || !IsScheduled || generation != _generation)
            {
                return;
            }

            if (_clock.GetUtcNow() < _dueAt)
            {
                _timer?.Change(GetNextDelay(), Timeout.InfiniteTimeSpan);
                return;
            }

            bool missed = _clock.GetUtcNow() - _dueAt > TimeSpan.FromSeconds(30);
            ScheduledAction action = _action;
            CancelCore();
            if (!TryPersistDisarmed())
            {
                LastError = "The action was cancelled because its completion state could not be saved.";
            }
            else if (missed)
            {
                LastError = "The scheduled time was missed while the computer was unavailable. The action was skipped.";
            }
            else
            {
                try
                {
                    _executor.Execute(action);
                    LastError = null;
                }
                catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
                {
                    LastError = "Windows could not complete the scheduled action. It has been disarmed.";
                }
            }
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool TryPersistDisarmed()
    {
        try
        {
            var settings = _settings.Current;
            settings.ScheduledActionEnabled = false;
            _settings.Save(settings);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return false;
        }
    }

    private TimeSpan GetNextDelay()
    {
        TimeSpan remaining = _dueAt - _clock.GetUtcNow();
        return remaining <= TimeSpan.Zero ? TimeSpan.Zero
            : remaining > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : remaining;
    }

    private void CancelCore()
    {
        _generation++;
        _timer?.Dispose();
        _timer = null;
        IsScheduled = false;
        ScheduledAt = null;
    }

    private DateTimeOffset ResolveExecutionTime(DateTime time)
    {
        if (time.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(time);
        }

        DateTime localTime = DateTime.SpecifyKind(time, DateTimeKind.Unspecified);
        if (_timeZone.IsInvalidTime(localTime) || _timeZone.IsAmbiguousTime(localTime))
        {
            throw new ArgumentException("Choose a time outside the daylight-saving clock transition.", nameof(time));
        }

        return new DateTimeOffset(localTime, _timeZone.GetUtcOffset(localTime)).ToUniversalTime();
    }
}
