using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>
/// Stores and retrieves sensitive application data using a storage-mode-specific secure backend.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Gets the stored license key for the current storage mode.
    /// </summary>
    /// <returns>The stored license key, or an empty string when none is stored.</returns>
    string GetLicenseKey();

    /// <summary>
    /// Gets the stored license key for a specific storage mode.
    /// </summary>
    /// <param name="storageMode">The storage mode to read from.</param>
    /// <returns>The stored license key, or an empty string when none is stored.</returns>
    string GetLicenseKey(StorageMode storageMode);

    /// <summary>
    /// Saves or clears the license key for the current storage mode.
    /// </summary>
    /// <param name="licenseKey">The license key to persist, or an empty value to clear it.</param>
    void SaveLicenseKey(string? licenseKey);

    /// <summary>
    /// Saves or clears the license key for a specific storage mode.
    /// </summary>
    /// <param name="licenseKey">The license key to persist, or an empty value to clear it.</param>
    /// <param name="storageMode">The storage mode to write to.</param>
    void SaveLicenseKey(string? licenseKey, StorageMode storageMode);

    /// <summary>
    /// Deletes the stored license key for a specific storage mode.
    /// </summary>
    /// <param name="storageMode">The storage mode to clear.</param>
    void DeleteLicenseKey(StorageMode storageMode);
}
