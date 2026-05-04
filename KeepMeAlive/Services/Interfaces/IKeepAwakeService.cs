namespace KeepMeAlive.Services.Interfaces;

public interface IKeepAwakeService
{
    bool IsEnabled { get; }
    void Enable();
    void Refresh();
    void Disable();
}
