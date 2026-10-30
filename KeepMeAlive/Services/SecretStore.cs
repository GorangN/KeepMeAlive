using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KeepMeAlive.Services;

/// <summary>Stores legacy keys and renewable sessions with Windows user-bound protection.</summary>
public sealed class SecretStore : ISecretStore
{
    private const string CredentialTargetName = "KeepMeAlive/LicenseKey";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("KeepMeAlive-Portable-Secret");
    private readonly IAppRuntimeModeService _runtimeModeService;
    private readonly object _gate = new();

    /// <summary>Creates the protected store for the application's runtime paths.</summary>
    /// <param name="runtimeModeService">Resolves the active mode and protected file locations.</param>
    public SecretStore(IAppRuntimeModeService runtimeModeService) => _runtimeModeService = runtimeModeService;

    /// <inheritdoc/>
    public string GetLicenseKey() => GetLicenseKey(_runtimeModeService.GetConfiguredStorageMode());
    /// <inheritdoc/>
    public string GetLicenseKey(StorageMode storageMode)
    {
        lock (_gate)
        {
            return storageMode == StorageMode.PortableLocal
                ? ReadProtected(PathFor(storageMode, "license.bin"))
                : WindowsCredentialStore.Read(CredentialTargetName);
        }
    }
    /// <inheritdoc/>
    public void SaveLicenseKey(string? licenseKey) => SaveLicenseKey(licenseKey, _runtimeModeService.GetConfiguredStorageMode());
    /// <inheritdoc/>
    public void SaveLicenseKey(string? licenseKey, StorageMode storageMode)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(licenseKey)) { DeleteLicenseKey(storageMode); return; }
            if (storageMode == StorageMode.PortableLocal) { WriteProtected(PathFor(storageMode, "license.bin"), licenseKey.Trim()); }
            else { WindowsCredentialStore.Write(CredentialTargetName, licenseKey.Trim()); }
        }
    }
    /// <inheritdoc/>
    public void DeleteLicenseKey(StorageMode storageMode)
    {
        lock (_gate)
        {
            if (storageMode == StorageMode.PortableLocal) { DeleteProtected(PathFor(storageMode, "license.bin")); }
            else { WindowsCredentialStore.Delete(CredentialTargetName); }
        }
    }
    /// <inheritdoc/>
    public string GetSession()
    {
        lock (_gate) { return ReadProtected(PathFor(_runtimeModeService.GetConfiguredStorageMode(), "session.bin")); }
    }
    /// <inheritdoc/>
    public void SaveSession(string? session)
    {
        lock (_gate)
        {
            if (string.IsNullOrEmpty(session)) { DeleteSession(); return; }
            WriteProtected(PathFor(_runtimeModeService.GetConfiguredStorageMode(), "session.bin"), session);
        }
    }
    /// <inheritdoc/>
    public void DeleteSession()
    {
        lock (_gate)
        {
            var activeMode = _runtimeModeService.GetConfiguredStorageMode();
            var owner = ReadSessionIdentity(TryReadProtected(PathFor(activeMode, "session.bin")));
            DeleteProtected(PathFor(activeMode, "session.bin"));
            foreach (var mode in Enum.GetValues<StorageMode>())
            {
                foreach (var suffix in new[] { string.Empty, ".bak" })
                {
                    var sessionPath = PathFor(mode, "session.bin") + suffix;
                    if (mode != activeMode && owner is not null &&
                        ReadSessionIdentity(TryReadProtected(sessionPath)) is { } candidate && candidate.IsSameFamily(owner))
                    {
                        if (File.Exists(sessionPath)) { File.Delete(sessionPath); }
                    }
                    RedactRecoverySession(PathFor(mode, "migration-recovery.bin") + suffix, owner, mode == activeMode);
                }
            }
        }
    }
    /// <inheritdoc/>
    public void CopyTo(StorageMode sourceMode, StorageMode targetMode)
    {
        lock (_gate)
        {
            var sourceLicense = GetLicenseKey(sourceMode);
            var sourceSession = ReadProtected(PathFor(sourceMode, "session.bin"));
            var oldLicense = GetLicenseKey(targetMode);
            var oldSession = ReadProtected(PathFor(targetMode, "session.bin"));
            if (oldLicense.Length > 0 || oldSession.Length > 0)
            {
                WriteProtected(PathFor(targetMode, "migration-recovery.bin"),
                    JsonSerializer.Serialize(new Dictionary<string, string> { ["license"] = oldLicense, ["session"] = oldSession }));
            }
            try
            {
                SaveLicenseKey(sourceLicense, targetMode);
                ReplaceSession(targetMode, sourceSession);
                if (GetLicenseKey(targetMode) != sourceLicense || ReadProtected(PathFor(targetMode, "session.bin")) != sourceSession)
                {
                    throw new IOException("Protected storage verification failed.");
                }
            }
            catch
            {
                SaveLicenseKey(oldLicense, targetMode);
                ReplaceSession(targetMode, oldSession);
                throw;
            }
        }
    }
    /// <inheritdoc/>
    public void DeleteAll(StorageMode storageMode)
    {
        lock (_gate)
        {
            DeleteLicenseKey(storageMode);
            DeleteProtected(PathFor(storageMode, "session.bin"));
            DeleteProtected(PathFor(storageMode, "migration-recovery.bin"));
        }
    }

    private void ReplaceSession(StorageMode mode, string value)
    {
        var path = PathFor(mode, "session.bin");
        if (value.Length == 0) { DeleteProtected(path); }
        else { WriteProtected(path, value); }
    }
    private string PathFor(StorageMode mode, string name) => Path.Combine(_runtimeModeService.GetDataDirectory(mode), "Secrets", name);
    private static string ReadProtected(string path)
    {
        if (!File.Exists(path)) { return string.Empty; }
        var plaintext = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
        try { return Encoding.UTF8.GetString(plaintext); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
    private static void WriteProtected(string path, string value, bool preserveBackup = true)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        try
        {
            var protectedBytes = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
            if (preserveBackup) { AtomicFile.Write(path, protectedBytes); }
            else { AtomicFile.WriteWithoutBackup(path, protectedBytes); }
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
    private static void DeleteProtected(string path)
    {
        if (File.Exists(path)) { File.Delete(path); }
        if (File.Exists(path + ".bak")) { File.Delete(path + ".bak"); }
    }

    private static string? TryReadProtected(string path)
    {
        try { return ReadProtected(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException) { return null; }
    }

    private static OAuthSession? ReadSessionIdentity(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) { return null; }
        try
        {
            var session = JsonSerializer.Deserialize<OAuthSession>(json);
            return session is not null && !string.IsNullOrWhiteSpace(session.Issuer) &&
                !string.IsNullOrWhiteSpace(session.ClientId) && !string.IsNullOrWhiteSpace(session.ApiBaseUrl) &&
                (!string.IsNullOrWhiteSpace(session.SessionId) || !string.IsNullOrWhiteSpace(session.RefreshToken)) ? session : null;
        }
        catch (JsonException) { return null; }
    }

    private static void RedactRecoverySession(string path, OAuthSession? owner, bool activeMode)
    {
        var json = TryReadProtected(path);
        if (string.IsNullOrWhiteSpace(json)) { return; }
        JsonObject? recovery;
        try { recovery = JsonNode.Parse(json) as JsonObject; }
        catch (JsonException) { return; }
        if (recovery?["session"] is not JsonValue value || !value.TryGetValue<string>(out var session)) { return; }
        if (!activeMode && (owner is null || ReadSessionIdentity(session) is not { } candidate || !candidate.IsSameFamily(owner))) { return; }
        recovery.Remove("session");
        WriteProtected(path, recovery.ToJsonString(), preserveBackup: false);
    }
}
