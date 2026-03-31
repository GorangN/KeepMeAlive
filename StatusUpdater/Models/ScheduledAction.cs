namespace StatusUpdater.Models;

/// <summary>Defines the system action to execute at the scheduled time.</summary>
public enum ScheduledAction
{
    /// <summary>Powers off the machine.</summary>
    Shutdown,

    /// <summary>Suspends the machine to sleep/hibernate.</summary>
    Sleep,

    /// <summary>Reboots the machine.</summary>
    Restart
}
