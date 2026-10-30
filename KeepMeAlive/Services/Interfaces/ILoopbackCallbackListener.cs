namespace KeepMeAlive.Services.Interfaces;

/// <summary>Receives an OAuth redirect over a loopback-only socket.</summary>
public interface ILoopbackCallbackListener
{
    /// <summary>Binds a registered callback address before opening the browser.</summary>
    /// <param name="launch">Starts authorization using the bound redirect URI.</param>
    /// <param name="cancellationToken">Cancellation or timeout for the callback.</param>
    /// <returns>The exact callback URI received from the browser.</returns>
    Task<Uri> ListenAsync(Func<Uri, Task> launch, CancellationToken cancellationToken);
}
