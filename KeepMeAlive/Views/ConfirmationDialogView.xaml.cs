using System.Windows;

namespace KeepMeAlive.Views;

/// <summary>Presents a themed modal confirmation or unsaved-changes decision.</summary>
public partial class ConfirmationDialogView : Window
{
    /// <summary>Initializes the modal visual tree and its command bindings.</summary>
    public ConfirmationDialogView()
    {
        InitializeComponent();
    }
}
