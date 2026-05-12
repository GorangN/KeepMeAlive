using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>
/// Resolves runtime-specific paths and the configured storage mode for the current application instance.
/// </summary>
public interface IAppRuntimeModeService
{
    /// <summary>
    /// Gets the directory that contains the running application binaries.
    /// </summary>
    string ApplicationDirectory { get; }

    /// <summary>
    /// Gets a value indicating whether the current distribution was published as a portable package.
    /// </summary>
    bool IsPortableDistribution { get; }

    /// <summary>
    /// Gets the storage mode used when no bootstrap override exists.
    /// </summary>
    StorageMode DefaultStorageMode { get; }

    /// <summary>
    /// Reads the configured storage mode from the runtime bootstrap file when available.
    /// </summary>
    /// <returns>The configured storage mode for the current runtime.</returns>
    StorageMode GetConfiguredStorageMode();

    /// <summary>
    /// Persists the configured storage mode to the runtime bootstrap file.
    /// </summary>
    /// <param name="storageMode">The storage mode to persist.</param>
    void SetConfiguredStorageMode(StorageMode storageMode);

    /// <summary>
    /// Resolves the data directory used by the requested storage mode.
    /// </summary>
    /// <param name="storageMode">The storage mode to resolve.</param>
    /// <returns>The fully qualified data directory.</returns>
    string GetDataDirectory(StorageMode storageMode);
}
