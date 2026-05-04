using KeepMeAlive.Helpers;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.Diagnostics;

namespace KeepMeAlive.Services;

/// <summary>
/// Schedules a system action (shutdown, sleep, restart) to execute at a specific local time
/// by using a <see cref="System.Threading.Timer"/>.
/// </summary>
/// <remarks>
/// The timer fires on a thread-pool thread. <see cref="Schedule"/> and <see cref="Cancel"/>
/// are protected by a lock so they are safe to call from any thread (including the UI thread).
/// Shutdown and restart are delegated to <c>shutdown.exe</c>; sleep uses the native Windows
/// suspend API so it performs sleep rather than hibernation.
/// </remarks>
public sealed class ScheduledActionService : IScheduledActionService, IDisposable
{
    private System.Threading.Timer? _timer;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public bool IsScheduled { get; private set; }

    /// <inheritdoc/>
    public DateTime? ScheduledAt { get; private set; }

    /// <inheritdoc/>
    public void Schedule(ScheduledAction action, DateTime executeAt)
    {
        if (executeAt <= DateTime.Now)
        {
            throw new ArgumentException("Scheduled time must be in the future.", nameof(executeAt));
        }

        lock (_lock)
        {
            CancelCore();

            var delay = executeAt - DateTime.Now;

            _timer = new System.Threading.Timer(
                _ => Execute(action),
                state: null,
                dueTime: delay,
                period: System.Threading.Timeout.InfiniteTimeSpan);

            IsScheduled = true;
            ScheduledAt = executeAt;
        }
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        lock (_lock)
        {
            CancelCore();
        }
    }

    /// <summary>Releases all resources used by the service and cancels any pending schedule.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            CancelCore();
        }
    }

    /// <summary>
    /// Cancels and disposes the timer. Must be called inside <c>_lock</c>.
    /// </summary>
    private void CancelCore()
    {
        _timer?.Dispose();
        _timer = null;
        IsScheduled = false;
        ScheduledAt = null;
    }

    /// <summary>Executes the specified system action.</summary>
    /// <param name="action">The action to execute.</param>
    private static void Execute(ScheduledAction action)
    {
        if (action == ScheduledAction.Sleep)
        {
            Interop.SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: false);
            return;
        }

        var arguments = action switch
        {
            ScheduledAction.Shutdown => "/s /t 0",
            ScheduledAction.Restart  => "/r /t 0",
            _                        => throw new ArgumentOutOfRangeException(nameof(action))
        };

        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }
}
