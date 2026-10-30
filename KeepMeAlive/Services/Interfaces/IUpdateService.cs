namespace KeepMeAlive.Services.Interfaces;

/// <summary>Checks published GitHub releases without installing updates.</summary>
public interface IUpdateService
{
    /// <summary>Gets the latest successfully retrieved version.</summary>
    string LatestVersion { get; }
    /// <summary>Gets a trusted release page URL.</summary>
    string ReleaseUrl { get; }
    /// <summary>Checks the latest release again, honoring cancellation.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>Whether a newer stable version exists.</returns>
    Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
