namespace KeepMeAlive.Services.Interfaces;

public interface IStartupService
{
    bool IsStartupEnabled { get; }
    /// <param name="enable">Whether to register the app in the Windows startup registry.</param>
    /// <param name="minimizedToTray">When <see langword="true"/>, appends <c>--silent</c> so the window stays hidden on boot.</param>
    void SetStartup(bool enable, bool minimizedToTray);
}
