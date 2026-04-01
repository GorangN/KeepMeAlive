namespace KeepMeAlive.Services.Interfaces;

public interface IStartupService
{
    bool IsStartupEnabled { get; }
    void SetStartup(bool enable);
}
