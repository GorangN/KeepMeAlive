namespace KeepMeAlive.Models;
/// <summary>The decision made before leaving an edited page.</summary>
public enum UnsavedChangesChoice
{
    /// <summary>Keep editing without navigating.</summary>
    Cancel,
    /// <summary>Persist edits before navigating.</summary>
    Save,
    /// <summary>Restore saved values before navigating.</summary>
    Discard
}
