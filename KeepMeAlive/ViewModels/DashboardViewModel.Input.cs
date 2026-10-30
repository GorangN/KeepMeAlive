using System.Globalization;

namespace KeepMeAlive.ViewModels;

public partial class DashboardViewModel
{
    private string _intervalSecondsText = "60";
    private string _virtualKeyCodeText = "126";
    private string _mousePixelDeltaText = "2";
    private bool _editingInputText;

    /// <summary>Gets or sets the interval draft exactly as entered, including invalid input.</summary>
    public string IntervalSecondsText
    {
        get => _intervalSecondsText;
        set
        {
            if (!SetProperty(ref _intervalSecondsText, value ?? string.Empty))
            {
                return;
            }

            _editingInputText = true;
            try
            {
                if (TryReadNumber(_intervalSecondsText, 20, 86400, out int interval))
                {
                    IntervalSeconds = interval;
                }
            }
            finally { _editingInputText = false; }
            OnInputDraftChanged();
        }
    }

    /// <summary>Gets or sets the function-key draft exactly as entered.</summary>
    public string VirtualKeyCodeText
    {
        get => _virtualKeyCodeText;
        set
        {
            if (!SetProperty(ref _virtualKeyCodeText, value ?? string.Empty))
            {
                return;
            }

            _editingInputText = true;
            try
            {
                if (TryReadNumber(_virtualKeyCodeText, 124, 135, out int key))
                {
                    VirtualKeyCode = key;
                }
            }
            finally { _editingInputText = false; }
            OnInputDraftChanged();
        }
    }

    /// <summary>Gets or sets the mouse-distance draft exactly as entered.</summary>
    public string MousePixelDeltaText
    {
        get => _mousePixelDeltaText;
        set
        {
            if (!SetProperty(ref _mousePixelDeltaText, value ?? string.Empty))
            {
                return;
            }

            _editingInputText = true;
            try
            {
                if (TryReadNumber(_mousePixelDeltaText, 1, 5, out int distance))
                {
                    MousePixelDelta = distance;
                }
            }
            finally { _editingInputText = false; }
            OnInputDraftChanged();
        }
    }

    /// <summary>Gets the first input correction required before saving or starting a session.</summary>
    public string InputError => !TryReadNumber(IntervalSecondsText, 20, 86400, out _)
        ? "Enter a pulse interval from 20 to 86400 seconds."
        : !TryReadNumber(VirtualKeyCodeText, 124, 135, out _)
            ? "Enter a virtual key code from 124 to 135 (F13–F24)."
            : !TryReadNumber(MousePixelDeltaText, 1, 5, out _)
                ? "Enter a mouse distance from 1 to 5 pixels."
                : string.Empty;

    private void ResetInputText()
    {
        IntervalSecondsText = IntervalSeconds.ToString(CultureInfo.InvariantCulture);
        VirtualKeyCodeText = VirtualKeyCode.ToString(CultureInfo.InvariantCulture);
        MousePixelDeltaText = MousePixelDelta.ToString(CultureInfo.InvariantCulture);
    }

    private void OnInputDraftChanged()
    {
        MarkDirty();
        OnPropertyChanged(nameof(InputError));
        StartCommand.NotifyCanExecuteChanged();
    }

    private static bool TryReadNumber(string text, int minimum, int maximum, out int value) =>
        int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && value >= minimum && value <= maximum;
}
