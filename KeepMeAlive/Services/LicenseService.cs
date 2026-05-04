using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>
/// Subscription stub — all features unlocked.
/// When the paid subscription launches: single plan, free trial month.
/// No free tier. Replace this implementation with real license validation.
/// </summary>
public class LicenseService : ILicenseService
{
    public LicenseInfo GetLicense() => new()
    {
        Key = "",
        ExpiresAt = null,
        Tier = SubscriptionTier.Trial   // TODO: validate against licensing API
    };

    public bool IsActivated() => false; // TODO: real license key validation

    public bool IsTrialExpired() => false; // TODO: check ExpiresAt against DateTime.UtcNow

    public Task<bool> ValidateAsync(string key)
    {
        // TODO: call licensing API endpoint
        return Task.FromResult(false);
    }
}
