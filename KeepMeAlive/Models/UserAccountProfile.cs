namespace KeepMeAlive.Models;

/// <summary>Represents the account data shown in the account area.</summary>
public class UserAccountProfile
{
    /// <summary>Gets or sets the immutable authenticated account identifier.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Gets or sets the server-managed feature grants.</summary>
    public List<AccountEntitlement> Entitlements { get; set; } = [];
    /// <summary>Gets or sets the display name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address or a placeholder when not connected.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the environment or workspace label.</summary>
    public string WorkspaceName { get; set; } = "Personal account";

    /// <summary>Gets or sets the current authentication provider label.</summary>
    public string AuthProvider { get; set; } = "Neon";

    /// <summary>Gets or sets the current plan label.</summary>
    public string PlanName { get; set; } = "No subscription linked";

    /// <summary>Gets or sets the account status label.</summary>
    public string StatusLabel { get; set; } = "Connected";

    /// <summary>Gets or sets the last refresh timestamp in UTC.</summary>
    public DateTime LastSyncedAtUtc { get; set; }
}
