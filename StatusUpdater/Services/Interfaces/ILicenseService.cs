using StatusUpdater.Models;

namespace StatusUpdater.Services.Interfaces;

public interface ILicenseService
{
    LicenseInfo GetLicense();
    bool IsActivated();
    bool IsTrialExpired();
    Task<bool> ValidateAsync(string key);
}
