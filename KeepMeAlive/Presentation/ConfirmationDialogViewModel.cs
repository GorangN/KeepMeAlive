using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace KeepMeAlive.Presentation;

/// <summary>Exposes confirmation text and explicit modal choices without placing actions in the view.</summary>
public sealed class ConfirmationDialogViewModel
{
    /// <summary>Gets the dialog heading and accessible window title.</summary>
    public string Title { get; }

    /// <summary>Gets the explanation shown before the user chooses an action.</summary>
    public string Message { get; }

    /// <summary>Gets whether the dialog offers separate save and discard choices.</summary>
    public bool IsSaveDialog { get; }

    /// <summary>Gets the affirmative button label, including its keyboard access key.</summary>
    public string AcceptLabel => IsSaveDialog ? "_Save" : "_Yes";

    /// <summary>Gets the accessible affirmative action name without access-key markup.</summary>
    public string AcceptName => IsSaveDialog ? "Save changes" : "Yes";

    /// <summary>Gets the safe default button label, including its keyboard access key.</summary>
    public string CancelLabel => IsSaveDialog ? "_Cancel" : "_No";

    /// <summary>Gets the safe default action name without access-key markup.</summary>
    public string CancelName => IsSaveDialog ? "Cancel navigation" : "No";

    /// <summary>Gets the command that saves changes or accepts the confirmation.</summary>
    public ICommand AcceptCommand { get; }

    /// <summary>Gets the command that discards the current draft before navigating.</summary>
    public ICommand DiscardCommand { get; }

    /// <summary>Gets the command that cancels navigation or declines confirmation.</summary>
    public ICommand CancelCommand { get; }

    /// <summary>Creates the modal presentation using a host-provided result callback.</summary>
    /// <param name="title">The window title and primary heading.</param>
    /// <param name="message">The explanation accompanying the available choices.</param>
    /// <param name="isSaveDialog">Whether to expose Save, Discard and Cancel instead of Yes and No.</param>
    /// <param name="close">Closes the modal with true for accept, false for discard, or null for cancellation.</param>
    /// <exception cref="ArgumentNullException">The close callback is null.</exception>
    public ConfirmationDialogViewModel(string title, string message, bool isSaveDialog, Action<bool?> close)
    {
        ArgumentNullException.ThrowIfNull(close);
        Title = title;
        Message = message;
        IsSaveDialog = isSaveDialog;
        AcceptCommand = new RelayCommand(() => close(true));
        DiscardCommand = new RelayCommand(() => close(false));
        CancelCommand = new RelayCommand(() => close(null));
    }
}
