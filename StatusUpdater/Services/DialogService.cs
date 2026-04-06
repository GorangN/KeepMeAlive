using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.Views;
using System.Windows;

namespace KeepMeAlive.Services;

/// <summary>Shows application modal dialogs using custom WPF windows.</summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc/>
    public bool ShowUnsavedChangesDialog()
    {
        var owner = Application.Current.Windows
            .OfType<MainWindowView>()
            .FirstOrDefault();

        var dialog = new UnsavedChangesDialog { Owner = owner };

        // Size and position the overlay to exactly cover the owner window
        // so the semi-transparent backdrop aligns with the app chrome.
        if (owner != null)
        {
            dialog.Left = owner.Left;
            dialog.Top = owner.Top;
            dialog.Width = owner.ActualWidth;
            dialog.Height = owner.ActualHeight;
        }

        dialog.ShowDialog();
        return dialog.SaveChosen;
    }
}
