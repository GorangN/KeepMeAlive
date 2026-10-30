namespace KeepMeAlive.Services.Interfaces;

/// <summary>Reports whether input can be sent to the current interactive session.</summary>
public interface ISessionMonitor
{
    /// <summary>Gets whether the current input desktop is available and unlocked.</summary>
    bool CanInject { get; }
}
