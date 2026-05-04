namespace KeepMeAlive.Models;

/// <summary>Represents the account data shown in the account area.</summary>
public class UserAccountProfile
{
    /// <summary>Gets or sets the display name shown in the UI.</summary>
    public string DisplayName { get; set; } = "Local User";

    /// <summary>Gets or sets the email address or a placeholder when not connected.</summary>
    public string Email { get; set; } = "Available after Supabase sign-in";

    /// <summary>Gets or sets the environment or workspace label.</summary>
    public string WorkspaceName { get; set; } = "Preview workspace";

    /// <summary>Gets or sets the current authentication provider label.</summary>
    public string AuthProvider { get; set; } = "Local preview";

    /// <summary>Gets or sets the current plan label.</summary>
    public string PlanName { get; set; } = "Starter Preview";

    /// <summary>Gets or sets the account status label.</summary>
    public string StatusLabel { get; set; } = "Not connected";

    /// <summary>Gets or sets the last refresh timestamp in UTC.</summary>
    public DateTime LastSyncedAtUtc { get; set; } = DateTime.UtcNow;
}
