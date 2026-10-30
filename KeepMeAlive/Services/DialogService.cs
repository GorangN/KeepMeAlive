using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.Windows;
namespace KeepMeAlive.Services;
/// <summary>Hosts accessible native confirmation dialogs.</summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc/>
    public UnsavedChangesChoice ShowUnsavedChangesDialog()
    {
        var result = MessageBox.Show(System.Windows.Application.Current.MainWindow,
            "Save your changes before leaving this page?", "Unsaved changes", MessageBoxButton.YesNoCancel,
            MessageBoxImage.None, MessageBoxResult.Cancel);
        return result switch { MessageBoxResult.Yes => UnsavedChangesChoice.Save, MessageBoxResult.No => UnsavedChangesChoice.Discard, _ => UnsavedChangesChoice.Cancel };
    }
    /// <inheritdoc/>
    public bool ShowConfirmation(string title, string message) =>
        MessageBox.Show(System.Windows.Application.Current.MainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No) == MessageBoxResult.Yes;
}
