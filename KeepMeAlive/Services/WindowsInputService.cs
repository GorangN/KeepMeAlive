using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Selects the single Windows input strategy configured for the pulse.</summary>
public sealed class WindowsInputService : IInputService
{
    /// <inheritdoc/>
    public void Pulse(KeepAliveOptions options)
    {
        options.Validate();
        IKeepAliveStrategy strategy = options.Method == KeepAliveMethod.Mouse
            ? new MouseStrategy(options.MousePixelDelta)
            : new KeyboardStrategy_VirtualKey((ushort)options.VirtualKeyCode);
        strategy.Pulse();
    }
}
