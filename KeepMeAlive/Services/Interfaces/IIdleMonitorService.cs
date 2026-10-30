namespace KeepMeAlive.Services.Interfaces;

/// <summary>Samples time since Windows last received an input event.</summary>
public interface IIdleMonitorService
{
    /// <summary>Gets seconds since the last input, or -1 when unavailable.</summary>
    int IdleSeconds { get; }

    /// <summary>Starts periodic sampling.</summary>
    void Start();

    /// <summary>Stops periodic sampling.</summary>
    void Stop();

    /// <summary>Occurs after a sample changes.</summary>
    event EventHandler? IdleUpdated;
}
