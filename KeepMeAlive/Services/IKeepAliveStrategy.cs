namespace KeepMeAlive.Services;

/// <summary>Generates a single balanced native input pulse.</summary>
public interface IKeepAliveStrategy
{
    /// <summary>Sends selected input and checks that Windows accepted it.</summary>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows rejected the input.</exception>
    void Pulse();
}
