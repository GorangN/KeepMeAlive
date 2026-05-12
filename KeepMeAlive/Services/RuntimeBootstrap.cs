using KeepMeAlive.Models;

namespace KeepMeAlive.Services;

/// <summary>
/// Stores runtime decisions that must be available before the main settings file is loaded.
/// </summary>
internal sealed class RuntimeBootstrap
{
    /// <summary>
    /// Gets or sets the selected storage mode for the current runtime.
    /// </summary>
    public StorageMode StorageMode { get; set; }
}
