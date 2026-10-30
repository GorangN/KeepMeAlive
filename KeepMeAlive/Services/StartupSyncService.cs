using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Refreshes account data only when explicitly enabled by startup preferences.</summary>
public sealed class StartupSyncService(IAccountService accounts) : IStartupSyncService
{
    /// <inheritdoc/>
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        await accounts.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
    }
}
