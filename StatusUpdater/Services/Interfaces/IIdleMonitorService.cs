namespace KeepMeAlive.Services.Interfaces;

public interface IIdleMonitorService
{
    int IdleSeconds { get; }
    void Start();
    void Stop();
    event EventHandler? IdleUpdated;
}
