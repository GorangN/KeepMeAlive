namespace StatusUpdater.Services.Interfaces;

public interface IIdleMonitorService
{
    int IdleSeconds { get; }
    void Start();
    void Stop();
    event EventHandler? IdleUpdated;
}
