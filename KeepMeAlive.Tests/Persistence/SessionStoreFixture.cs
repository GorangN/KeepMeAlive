using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Persistence;

internal sealed class SessionStoreFixture : IAppRuntimeModeService, IDisposable
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("KeepMeAlive-Portable-Secret");
    private readonly string _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "KeepMeAlive-session-tests", Guid.NewGuid().ToString("N")));
    private StorageMode _mode = StorageMode.PortableLocal;

    public string ApplicationDirectory => Path.Combine(_root, "app");
    public bool IsPortableDistribution => true;
    public StorageMode DefaultStorageMode => StorageMode.PortableLocal;
    public SecretStore Store { get; }
    public SessionStoreFixture() => Store = new SecretStore(this);
    public StorageMode GetConfiguredStorageMode() => _mode;
    public void SetConfiguredStorageMode(StorageMode storageMode) => _mode = storageMode;
    public string GetDataDirectory(StorageMode storageMode) => Path.Combine(_root, storageMode.ToString());
    public string SessionPath(StorageMode mode, bool backup = false) => Path.Combine(GetDataDirectory(mode), "Secrets", "session.bin" + (backup ? ".bak" : string.Empty));
    public string RecoveryPath(StorageMode mode, bool backup = false) => Path.Combine(GetDataDirectory(mode), "Secrets", "migration-recovery.bin" + (backup ? ".bak" : string.Empty));

    public void Write(string path, string plaintext)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), Entropy, DataProtectionScope.CurrentUser));
    }
    public string Read(string path) => Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser));
    public static string Session(string token, string? sessionId = null, string issuer = "https://auth.example.test/neondb/auth")
    {
        var values = new Dictionary<string, string>
        {
            ["Issuer"] = issuer, ["ClientId"] = "desktop", ["ApiBaseUrl"] = "https://api.example.test/", ["RefreshToken"] = token,
        };
        if (sessionId is not null) { values["SessionId"] = sessionId; }
        return JsonSerializer.Serialize(values);
    }
    public void WriteRecovery(StorageMode mode, string session, bool backup = false) =>
        Write(RecoveryPath(mode, backup), JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["license"] = "keep-this-license-recovery", ["session"] = session, ["note"] = "preserve-other-fields",
        }));
    public Dictionary<string, byte[]> Snapshot(StorageMode mode) => Directory.GetFiles(GetDataDirectory(mode), "*", SearchOption.AllDirectories)
        .ToDictionary(path => path, File.ReadAllBytes);
    public void Dispose()
    {
        var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "KeepMeAlive-session-tests")) + Path.DirectorySeparatorChar;
        if (!_root.StartsWith(expected, StringComparison.OrdinalIgnoreCase)) { throw new InvalidOperationException("Unexpected test data path."); }
        if (Directory.Exists(_root)) { Directory.Delete(_root, recursive: true); }
    }
}
