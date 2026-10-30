using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Schedules, restores, and cancels a deferred system action.</summary>
public interface IScheduledActionService
{
    /// <summary>Gets whether an action is currently armed.</summary>
    bool IsScheduled { get; }

    /// <summary>Gets the local execution time, or null when no action is armed.</summary>
    DateTime? ScheduledAt { get; }

    /// <summary>Gets the last scheduling failure or missed-action explanation.</summary>
    string? LastError { get; }

    /// <summary>Occurs when an action is armed, cancelled, completed, or rejected.</summary>
    event EventHandler? StatusChanged;

    /// <summary>Arms one future action, replacing any previous action.</summary>
    /// <param name="action">The system action to execute.</param>
    /// <param name="executeAt">The local execution time.</param>
    /// <exception cref="ArgumentException">The time is past, invalid, or ambiguous.</exception>
    /// <exception cref="ObjectDisposedException">The scheduler has been disposed.</exception>
    void Schedule(ScheduledAction action, DateTime executeAt);

    /// <summary>Cancels a queued action before it begins execution.</summary>
    void Cancel();

    /// <summary>Restores a future persisted action and safely discards missed actions.</summary>
    void Restore();
}
