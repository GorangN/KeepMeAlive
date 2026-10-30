namespace KeepMeAlive.Models;

/// <summary>Contains validated input and power preferences for one session.</summary>
/// <param name="Method">The input strategy to use.</param>
/// <param name="IntervalSeconds">The nominal interval between input pulses.</param>
/// <param name="KeepAwakeEnabled">Whether to request system and display power.</param>
/// <param name="VirtualKeyCode">The F13–F24 virtual key used by keyboard input.</param>
/// <param name="MousePixelDelta">The one-to-five-pixel mouse displacement.</param>
public sealed record KeepAliveOptions(
    KeepAliveMethod Method,
    int IntervalSeconds,
    bool KeepAwakeEnabled,
    int VirtualKeyCode,
    int MousePixelDelta)
{
    /// <summary>Rejects settings that cannot safely be used by the engine.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A preference is outside its supported range.</exception>
    public void Validate()
    {
        if (!Enum.IsDefined(Method) || IntervalSeconds is < 20 or > 86400
            || VirtualKeyCode is < 124 or > 135 || MousePixelDelta is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(KeepAliveOptions), "Use an interval of 20–86400 seconds, F13–F24, and a mouse distance of 1–5 pixels.");
        }
    }
}
