namespace KeepMeAlive.Services.Interfaces;

/// <summary>Provides application-level modal dialog interactions.</summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the "Unsaved Changes" dialog and returns the user's choice.
    /// </summary>
    /// <returns><see langword="true"/> if the user chose Save; <see langword="false"/> to discard.</returns>
    bool ShowUnsavedChangesDialog();
}
