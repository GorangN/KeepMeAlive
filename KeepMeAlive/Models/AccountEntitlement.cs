namespace KeepMeAlive.Models;

/// <summary>Represents one access grant returned by the account API.</summary>
public sealed class AccountEntitlement
{
    /// <summary>Gets or sets the granted feature identifier.</summary>
    public string Feature { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional server-defined expiration.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
