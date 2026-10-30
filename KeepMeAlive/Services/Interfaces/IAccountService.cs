using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

/// <summary>Provides account data for the account page.</summary>
public interface IAccountService
{
    /// <summary>Gets the last successfully retrieved profile, if any.</summary>
    UserAccountProfile? Current { get; }
    /// <summary>Gets the current account profile for the shell UI.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The authenticated profile, or null when signed out.</returns>
    Task<UserAccountProfile?> GetCurrentAsync(CancellationToken cancellationToken = default);
    /// <summary>Changes the current account display name.</summary>
    /// <param name="displayName">A display name between one and one hundred characters.</param>
    /// <param name="cancellationToken">Cancellation for the API request.</param>
    /// <returns>The updated account profile.</returns>
    Task<UserAccountProfile> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default);
    /// <summary>Clears the locally cached profile after logout.</summary>
    void Clear();
}
