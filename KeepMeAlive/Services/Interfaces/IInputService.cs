using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Injects the configured local input through the operating system.</summary>
public interface IInputService
{
    /// <summary>Generates exactly one selected input pulse.</summary>
    /// <param name="options">Validated preferences for the pulse.</param>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows refused the input.</exception>
    void Pulse(KeepAliveOptions options);
}
