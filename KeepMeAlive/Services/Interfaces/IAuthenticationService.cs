namespace KeepMeAlive.Services.Interfaces;

/// <summary>Manages browser-based public-client authentication and protected refresh credentials.</summary>
public interface IAuthenticationService
{
    /// <summary>Gets whether the desktop client has been registered for this deployment.</summary>
    bool IsConfigured { get; }
    /// <summary>Signs in through the system browser using PKCE.</summary>
    /// <param name="cancellationToken">Cancellation for the interactive flow.</param>
    /// <returns>A task completing after a verified API session is established.</returns>
    Task SignInAsync(CancellationToken cancellationToken = default);
    /// <summary>Obtains an access token, restoring and refreshing a protected session if needed.</summary>
    /// <param name="cancellationToken">Cancellation for discovery or refresh.</param>
    /// <returns>An access token, or null when no valid local session exists.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    /// <summary>Clears local credentials and attempts remote refresh-token revocation.</summary>
    /// <param name="cancellationToken">Cancellation for remote revocation.</param>
    /// <returns>A task completing after local logout.</returns>
    Task SignOutAsync(CancellationToken cancellationToken = default);
}
