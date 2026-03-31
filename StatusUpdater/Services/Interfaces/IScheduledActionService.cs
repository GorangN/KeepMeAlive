using StatusUpdater.Models;

namespace StatusUpdater.Services.Interfaces;

/// <summary>Provides scheduling and cancellation of a deferred system action.</summary>
public interface IScheduledActionService
{
    /// <summary>Gets a value indicating whether a system action is currently scheduled.</summary>
    bool IsScheduled { get; }

    /// <summary>
    /// Gets the local time at which the scheduled action will fire,
    /// or <see langword="null"/> if none is scheduled.
    /// </summary>
    DateTime? ScheduledAt { get; }

    /// <summary>
    /// Arms the service to execute <paramref name="action"/> at <paramref name="executeAt"/>.
    /// Any previously armed schedule is cancelled before the new one is set.
    /// </summary>
    /// <param name="action">The system action to execute.</param>
    /// <param name="executeAt">The local date/time at which to execute the action.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="executeAt"/> is not in the future.
    /// </exception>
    void Schedule(ScheduledAction action, DateTime executeAt);

    /// <summary>Cancels any currently armed schedule without executing the action.</summary>
    void Cancel();
}
