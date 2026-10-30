namespace KeepMeAlive.Models;

/// <summary>Describes access granted by the authenticated account service.</summary>
public sealed class LicenseInfo
{
    /// <summary>Gets the legacy locally saved key, which does not itself grant access.</summary>
    public string Key { get; init; } = string.Empty;
    /// <summary>Gets the server-provided access expiration, if any.</summary>
    public DateTime? ExpiresAt { get; init; }
    /// <summary>Gets the verified subscription state.</summary>
    public SubscriptionTier Tier { get; init; } = SubscriptionTier.Unlinked;
}
