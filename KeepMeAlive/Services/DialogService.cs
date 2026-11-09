using KeepMeAlive.Models;
using KeepMeAlive.Presentation;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.Views;

namespace KeepMeAlive.Services;

/// <summary>Hosts themed, owned modal dialogs while keeping choices behind the application interface.</summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc/>
    public UnsavedChangesChoice ShowUnsavedChangesDialog()
    {
        return Show("Unsaved changes", "Save your changes before leaving this page?", isSaveDialog: true) switch
        {
            true => UnsavedChangesChoice.Save,
            false => UnsavedChangesChoice.Discard,
            null => UnsavedChangesChoice.Cancel
        };
    }
    /// <inheritdoc/>
    public bool ShowConfirmation(string title, string message) =>
        Show(title, message, isSaveDialog: false) == true;

    private static bool? Show(string title, string message, bool isSaveDialog)
    {
        var window = new ConfirmationDialogView();
        var owner = System.Windows.Application.Current.MainWindow;
        if (owner is not null)
        {
            window.Owner = owner;
        }
        bool? choice = null;
        window.DataContext = new ConfirmationDialogViewModel(title, message, isSaveDialog, result =>
        {
            choice = result;
            window.DialogResult = result == true;
        });
        WindowMaterial.Attach(window);
        window.ShowDialog();
        return choice;
    }
}
