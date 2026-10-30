namespace KeepMeAlive.Messages;

/// <summary>Notifies the shell when the actual keep-alive session starts or stops.</summary>
/// <param name="IsRunning">Whether a keep-alive session currently owns the engine.</param>
public record KeepAliveStatusMessage(bool IsRunning);
