using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;

/// <summary>Sends one balanced F13–F24 key pulse.</summary>
public sealed class KeyboardStrategy_VirtualKey : IKeepAliveStrategy
{
    private readonly ushort _virtualKey;

    /// <summary>Creates an input strategy for a supported function key.</summary>
    /// <param name="virtualKey">The F13–F24 virtual-key value.</param>
    /// <exception cref="ArgumentOutOfRangeException">The key is outside F13–F24.</exception>
    public KeyboardStrategy_VirtualKey(ushort virtualKey)
    {
        if (virtualKey is < 124 or > 135)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualKey));
        }

        _virtualKey = virtualKey;
    }

    /// <inheritdoc/>
    public void Pulse()
    {
        INPUT[] inputs =
        [
            new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = _virtualKey } } },
            new() { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = _virtualKey, dwFlags = KEYEVENTF_KEYUP } } }
        ];
        NativeInputWriter.Send(inputs);
    }
}
