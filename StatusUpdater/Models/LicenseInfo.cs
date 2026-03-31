namespace StatusUpdater.Models;

public enum SubscriptionTier { Trial, Active, Expired }

public class LicenseInfo
{
    public string Key { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Trial;
}
