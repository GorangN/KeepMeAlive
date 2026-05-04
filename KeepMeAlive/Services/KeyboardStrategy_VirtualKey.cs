using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;
public class KeyboardStrategy_VirtualKey : IKeepAliveStrategy
{
    private readonly ushort _vk;
    public KeyboardStrategy_VirtualKey(ushort vk) { _vk = vk; }

    public void Pulse()
    {
        var inputs = new INPUT[2];
        inputs[0] = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = _vk, dwFlags = 0 } } };
        inputs[1] = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = _vk, dwFlags = KEYEVENTF_KEYUP } } };
        SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }
}
