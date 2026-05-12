using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>
/// Local stub for future startup account and license synchronization.
/// </summary>
public sealed class StartupSyncService : IStartupSyncService
{
    /// <inheritdoc/>
    public Task SyncAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
