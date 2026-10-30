namespace KeepMeAlive.Services.Interfaces;

/// <summary>Owns Windows requests preventing idle system and display sleep.</summary>
public interface IKeepAwakeService
{
    /// <summary>Gets whether a power request is currently owned.</summary>
    bool IsEnabled { get; }

    /// <summary>Acquires system and display power requests.</summary>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows refused a power request.</exception>
    void Enable();

    /// <summary>Ensures the request remains enabled.</summary>
    /// <exception cref="System.ComponentModel.Win32Exception">Windows refused a power request.</exception>
    void Refresh();

    /// <summary>Releases owned requests, if present.</summary>
    void Disable();
}
