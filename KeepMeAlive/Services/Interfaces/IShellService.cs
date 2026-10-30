namespace KeepMeAlive.Services.Interfaces;
/// <summary>Presentation operations that do not expose WPF objects to application logic.</summary>
public interface IShellService
{
    /// <summary>Shows and activates the window.</summary>
    void Show();
    /// <summary>Hides the window in the tray.</summary>
    void Hide();
    /// <summary>Minimizes the window.</summary>
    void Minimize();
    /// <summary>Toggles the maximized state.</summary>
    void ToggleMaximize();
    /// <summary>Requests coordinated shutdown.</summary>
    void Exit();
    /// <summary>Updates the tray state.</summary>
    /// <param name="running">Whether the engine is running.</param>
    void SetRunning(bool running);
    /// <summary>Displays an enabled notification.</summary>
    /// <param name="title">The title.</param>
    /// <param name="body">The message.</param>
    void Notify(string title, string body);
}
