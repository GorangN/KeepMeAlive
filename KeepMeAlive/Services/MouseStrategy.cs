using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;

/// <summary>Sends one small relative pointer movement and its inverse.</summary>
public sealed class MouseStrategy : IKeepAliveStrategy
{
    private readonly int _pixels;

    /// <summary>Creates a pointer strategy with a validated displacement.</summary>
    /// <param name="pixels">The distance of one to five pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">The distance is unsupported.</exception>
    public MouseStrategy(int pixels)
    {
        if (pixels is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(pixels));
        }

        _pixels = pixels;
    }

    /// <inheritdoc/>
    public void Pulse()
    {
        INPUT[] inputs =
        [
            new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = _pixels, dwFlags = MOUSEEVENTF_MOVE } } },
            new() { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dx = -_pixels, dwFlags = MOUSEEVENTF_MOVE } } }
        ];
        NativeInputWriter.Send(inputs);
    }
}
