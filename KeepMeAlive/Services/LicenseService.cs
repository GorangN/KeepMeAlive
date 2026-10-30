using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Reports entitlements fetched from the authenticated backend.</summary>
public sealed class LicenseService(IAccountService accountService) : ILicenseService
{
    /// <inheritdoc/>
    public LicenseInfo GetLicense()
    {
        var entitlement = accountService.Current?.Entitlements.FirstOrDefault();
        return entitlement is null ? new LicenseInfo() : new LicenseInfo
        {
            ExpiresAt = entitlement.ExpiresAt?.UtcDateTime,
            Tier = entitlement.ExpiresAt is { } expiry && expiry <= DateTimeOffset.UtcNow
                ? SubscriptionTier.Expired : SubscriptionTier.Active,
        };
    }
    /// <inheritdoc/>
    public bool IsActivated() => GetLicense().Tier == SubscriptionTier.Active;
    /// <inheritdoc/>
    public bool IsTrialExpired() => GetLicense().Tier == SubscriptionTier.Expired;
    /// <inheritdoc/>
    public async Task<bool> ValidateAsync(string key)
    {
        await accountService.GetCurrentAsync().ConfigureAwait(false);
        return IsActivated();
    }
}
