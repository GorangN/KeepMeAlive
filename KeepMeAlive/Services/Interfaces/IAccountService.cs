using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Provides account data for the account page.</summary>
public interface IAccountService
{
    /// <summary>Gets the current account profile for the shell UI.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<UserAccountProfile> GetCurrentAsync(CancellationToken cancellationToken = default);
}
