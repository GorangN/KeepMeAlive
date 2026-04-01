namespace KeepMeAlive.Services.Interfaces;

public interface IUpdateService
{
    string LatestVersion { get; }
    string ReleaseUrl { get; }
    Task<bool> CheckForUpdateAsync();
}
