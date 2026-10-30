namespace KeepMeAlive.Services.Interfaces;

/// <summary>Reads and updates current-user Windows startup registration.</summary>
public interface IStartupService
{
    /// <summary>Gets whether this application's executable is registered for Windows startup.</summary>
    bool IsStartupEnabled { get; }
    /// <summary>Updates the current user's executable startup registration.</summary>
    /// <param name="enable">Whether to register the application.</param>
    /// <param name="minimizedToTray">Whether startup should hide the window in the tray.</param>
    /// <exception cref="System.IO.IOException">Windows startup settings cannot be updated.</exception>
    /// <exception cref="UnauthorizedAccessException">The user cannot write the startup registration.</exception>
    void SetStartup(bool enable, bool minimizedToTray);
}
