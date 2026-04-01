using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;
public class MouseStrategy : IKeepAliveStrategy
{
    private readonly int _px;
    public MouseStrategy(int px) { _px = Math.Clamp(px, 1, 5); }

    public void Pulse()
    {
        var inputs = new INPUT[2];
        inputs[0] = new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = _px, dy = 0, dwFlags = MOUSEEVENTF_MOVE } } };
        inputs[1] = new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = -_px, dy = 0, dwFlags = MOUSEEVENTF_MOVE } } };
        SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }
}
