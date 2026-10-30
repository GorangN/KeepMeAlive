namespace KeepMeAlive.Models;

/// <summary>Identifies the state of a server-managed subscription.</summary>
public enum SubscriptionTier
{
    /// <summary>No subscription is linked to the account.</summary>
    Unlinked,
    /// <summary>A server-granted trial is active.</summary>
    Trial,
    /// <summary>A server-granted subscription is active.</summary>
    Active,
    /// <summary>A previously granted subscription has expired.</summary>
    Expired,
}
