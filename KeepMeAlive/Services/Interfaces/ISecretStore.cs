using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Persists license and authentication material using Windows protected storage.</summary>
public interface ISecretStore
{
    /// <summary>Reads the saved legacy license key in the active storage mode.</summary>
    /// <returns>The saved key, or an empty string when absent.</returns>
    string GetLicenseKey();
    /// <summary>Reads a legacy license key in the specified storage mode.</summary>
    /// <param name="storageMode">The source storage mode.</param>
    /// <returns>The saved key, or an empty string when absent.</returns>
    string GetLicenseKey(StorageMode storageMode);
    /// <summary>Saves or clears the active legacy license key.</summary>
    /// <param name="licenseKey">The key to save, or an empty value to clear it.</param>
    void SaveLicenseKey(string? licenseKey);
    /// <summary>Saves or clears the legacy license key in a specified mode.</summary>
    /// <param name="licenseKey">The key to save, or an empty value to clear it.</param>
    /// <param name="storageMode">The destination storage mode.</param>
    void SaveLicenseKey(string? licenseKey, StorageMode storageMode);
    /// <summary>Deletes the saved legacy license key.</summary>
    /// <param name="storageMode">The storage mode to clear.</param>
    void DeleteLicenseKey(StorageMode storageMode);
    /// <summary>Reads protected authentication session material.</summary>
    /// <returns>The serialized session, or an empty string when absent.</returns>
    string GetSession();
    /// <summary>Atomically replaces or clears protected authentication session material.</summary>
    /// <param name="session">The serialized session, or an empty value to clear it.</param>
    void SaveSession(string? session);
    /// <summary>Deletes the active protected session and matching migrated session copies while preserving unrelated sessions and legacy license recovery data.</summary>
    void DeleteSession();
    /// <summary>Copies all secrets and verifies their contents before allowing migration to commit.</summary>
    /// <param name="sourceMode">The original storage mode.</param>
    /// <param name="targetMode">The destination storage mode.</param>
    void CopyTo(StorageMode sourceMode, StorageMode targetMode);
    /// <summary>Deletes all application secrets from a specified storage mode.</summary>
    /// <param name="storageMode">The mode to clear.</param>
    void DeleteAll(StorageMode storageMode);
}
