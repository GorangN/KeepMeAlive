using KeepMeAlive.Models;

namespace KeepMeAlive.Messages;

/// <summary>
/// Announces that the active settings storage mode has changed.
/// </summary>
/// <param name="StorageMode">The newly active storage mode.</param>
public record StorageChangedMessage(StorageMode StorageMode);
