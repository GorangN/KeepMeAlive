using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;

/// <summary>Sends one balanced Shift scan-code pulse for legacy callers.</summary>
public sealed class KeyboardStrategy_ScanCodeShift : IKeepAliveStrategy
{
    /// <inheritdoc/>
    public void Pulse()
    {
        const ushort shiftScanCode = 0x2A;
        INPUT[] inputs =
        [
            new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = shiftScanCode, dwFlags = KEYEVENTF_SCANCODE } } },
            new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = shiftScanCode, dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP } } }
        ];
        NativeInputWriter.Send(inputs);
    }
}
