using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Reports server-granted account entitlements without restricting the local utility.</summary>
public interface ILicenseService
{
    /// <summary>Reads the last verified entitlement snapshot.</summary>
    /// <returns>The subscription state, or unlinked when no server grant exists.</returns>
    LicenseInfo GetLicense();
    /// <summary>Determines whether a verified subscription is currently active.</summary>
    /// <returns>Whether active server-granted access exists.</returns>
    bool IsActivated();
    /// <summary>Determines whether the last verified trial has expired.</summary>
    /// <returns>Whether an expired trial is present.</returns>
    bool IsTrialExpired();
    /// <summary>Refreshes server entitlements; local keys cannot grant access.</summary>
    /// <param name="key">A legacy key retained for API compatibility.</param>
    /// <returns>Whether the server reports active access.</returns>
    Task<bool> ValidateAsync(string key);
}
