namespace KeepMeAlive.Services.Interfaces;

/// <summary>Opens a validated HTTPS authentication page in the default browser.</summary>
public interface IBrowserLauncher
{
    /// <summary>Opens the specified authentication URL.</summary>
    /// <param name="uri">An HTTPS authorization URL.</param>
    void Open(Uri uri);
}
