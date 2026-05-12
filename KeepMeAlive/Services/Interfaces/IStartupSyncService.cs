namespace KeepMeAlive.Services.Interfaces;

/// <summary>
/// Performs any startup-time account or license synchronization that may be enabled by user preference.
/// </summary>
public interface IStartupSyncService
{
    /// <summary>
    /// Runs the startup synchronization routine.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the sync routine finishes.</returns>
    Task SyncAsync(CancellationToken cancellationToken = default);
}
