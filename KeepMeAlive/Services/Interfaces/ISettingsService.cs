using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Loads, validates, and atomically persists the application's local preferences.</summary>
public interface ISettingsService
{
    /// <summary>Gets a detached copy of the last committed settings.</summary>
    AppSettings Current { get; }
    /// <summary>Gets the committed storage mode.</summary>
    StorageMode CurrentStorageMode { get; }
    /// <summary>Gets the active settings directory.</summary>
    string CurrentSettingsDirectory { get; }
    /// <summary>Gets the active settings filename.</summary>
    string CurrentSettingsPath { get; }
    /// <summary>Gets the most recent storage or recovery warning, if any.</summary>
    string? LastError { get; }
    /// <summary>Loads valid settings, recovering the previous file when necessary.</summary>
    /// <returns>A detached copy of the loaded settings.</returns>
    AppSettings Load();
    /// <summary>Validates and persists preferences before publishing the new state.</summary>
    /// <param name="settings">The complete settings to commit.</param>
    /// <exception cref="System.IO.IOException">The file cannot be persisted.</exception>
    /// <exception cref="System.IO.InvalidDataException">A setting is invalid.</exception>
    void Save(AppSettings settings);
    /// <summary>Copies and verifies settings and secrets before changing storage location.</summary>
    /// <param name="targetStorageMode">The destination storage mode.</param>
    /// <returns>Whether every required migration step completed.</returns>
    bool MigrateStorage(StorageMode targetStorageMode);
}
