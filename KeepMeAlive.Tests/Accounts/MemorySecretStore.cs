using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class MemorySecretStore : ISecretStore
{
    public string Session { get; set; } = string.Empty;
    public string GetSession() => Session;
    public void SaveSession(string? session) => Session = session ?? string.Empty;
    public void DeleteSession() => Session = string.Empty;
    public string GetLicenseKey() => string.Empty;
    public string GetLicenseKey(StorageMode storageMode) => string.Empty;
    public void SaveLicenseKey(string? licenseKey) { }
    public void SaveLicenseKey(string? licenseKey, StorageMode storageMode) { }
    public void DeleteLicenseKey(StorageMode storageMode) { }
    public void CopyTo(StorageMode sourceMode, StorageMode targetMode) { }
    public void DeleteAll(StorageMode storageMode) => DeleteSession();
}
