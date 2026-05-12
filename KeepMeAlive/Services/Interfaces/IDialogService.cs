namespace KeepMeAlive.Services.Interfaces;

/// <summary>Provides application-level modal dialog interactions.</summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the "Unsaved Changes" dialog and returns the user's choice.
    /// </summary>
    /// <returns><see langword="true"/> if the user chose Save; <see langword="false"/> to discard.</returns>
    bool ShowUnsavedChangesDialog();

    /// <summary>
    /// Shows a confirmation dialog and returns whether the user accepted the action.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns><see langword="true"/> when the user accepted the action; otherwise, <see langword="false"/>.</returns>
    bool ShowConfirmation(string title, string message);
}
