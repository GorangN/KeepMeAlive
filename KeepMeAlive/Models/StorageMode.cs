namespace KeepMeAlive.Models;

/// <summary>
/// Defines where KeepMeAlive stores user settings and protected local data.
/// </summary>
public enum StorageMode
{
    /// <summary>
    /// Stores settings under the current user's roaming application data folder.
    /// </summary>
    ProfileAppData,

    /// <summary>
    /// Stores settings and protected local data beside the executable in a local data folder.
    /// </summary>
    PortableLocal,
}
