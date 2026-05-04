using System.Windows;

namespace KeepMeAlive.Views;

/// <summary>
/// Unsaved-changes confirmation dialog.
/// After <see cref="ShowDialog"/> returns, read <see cref="SaveChosen"/>.
/// </summary>
public partial class UnsavedChangesDialog : Window
{
    /// <summary>Gets a value indicating whether the user chose to save.</summary>
    public bool SaveChosen { get; private set; }

    /// <summary>Initializes the dialog.</summary>
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveChosen = true;
        Close();
    }

    private void DiscardButton_Click(object sender, RoutedEventArgs e)
    {
        SaveChosen = false;
        Close();
    }
}
