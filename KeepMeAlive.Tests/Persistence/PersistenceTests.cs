using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Text;

namespace KeepMeAlive.Tests.Persistence;

/// <summary>Exercises storage recovery, failed transactions, and protected-session persistence.</summary>
public sealed class PersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "KeepMeAlive-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Checks that callers cannot mutate the committed state through a returned settings object.</summary>
    [Fact]
    public void CurrentSettingsAreDetachedUntilSaved()
    {
        var runtime = CreateRuntime();
        var settings = new SettingsService(runtime, new TestSecrets());
        var draft = settings.Current;
        draft.IntervalSeconds = 90;
        Assert.Equal(60, settings.Current.IntervalSeconds);
        settings.Save(draft);
        Assert.Equal(90, settings.Current.IntervalSeconds);
        Assert.Equal(0, runtime.BootstrapWrites);
    }

    /// <summary>Checks that a damaged primary file recovers the preceding complete settings file.</summary>
    [Fact]
    public void CorruptPrimaryRecoversPreviousSettingsWithoutOverwritingEvidence()
    {
        var runtime = CreateRuntime();
        var settings = new SettingsService(runtime, new TestSecrets());
        settings.Save(new AppSettings { IntervalSeconds = 90 });
        settings.Save(new AppSettings { IntervalSeconds = 120 });
        File.WriteAllText(settings.CurrentSettingsPath, "{broken");
        var loaded = new SettingsService(runtime, new TestSecrets());
        Assert.Equal(90, loaded.Load().IntervalSeconds);
        Assert.Contains("Recovered", loaded.LastError);
        Assert.Equal("{broken", File.ReadAllText(settings.CurrentSettingsPath));
    }

    /// <summary>Checks that invalid values cannot replace the last committed settings.</summary>
    [Fact]
    public void InvalidSettingsDoNotCommit()
    {
        var settings = new SettingsService(CreateRuntime(), new TestSecrets());
        settings.Save(new AppSettings());
        Assert.Throws<InvalidDataException>(() => settings.Save(new AppSettings { IntervalSeconds = 0 }));
        Assert.Equal(60, settings.Current.IntervalSeconds);
    }

    /// <summary>Checks that an unwritable destination cannot replace the in-memory committed state.</summary>
    [Fact]
    public void WriteFailureKeepsCommittedState()
    {
        var runtime = CreateRuntime();
        Directory.CreateDirectory(runtime.ApplicationDirectory);
        File.WriteAllText(runtime.GetDataDirectory(StorageMode.PortableLocal), "blocked");
        var settings = new SettingsService(runtime, new TestSecrets());
        Assert.Throws<IOException>(() => settings.Save(new AppSettings { IntervalSeconds = 90 }));
        Assert.Equal(60, settings.Current.IntervalSeconds);
    }

    /// <summary>Checks that failed secret copying leaves the original settings and location intact.</summary>
    [Fact]
    public void SecretCopyFailureDoesNotCommitMigration()
    {
        var runtime = CreateRuntime();
        var settings = new SettingsService(runtime, new TestSecrets { FailCopy = true });
        settings.Save(new AppSettings { IntervalSeconds = 90 });
        var sourcePath = settings.CurrentSettingsPath;
        Assert.False(settings.MigrateStorage(StorageMode.ProfileAppData));
        Assert.Equal(StorageMode.PortableLocal, settings.CurrentStorageMode);
        Assert.True(File.Exists(sourcePath));
        Assert.Equal(90, settings.Current.IntervalSeconds);
        Assert.Equal(0, runtime.BootstrapWrites);
    }

    /// <summary>Checks that bootstrap commit failure retains the original location and recoverable source.</summary>
    [Fact]
    public void BootstrapFailureDoesNotCommitMigration()
    {
        var runtime = CreateRuntime();
        runtime.FailBootstrap = true;
        var settings = new SettingsService(runtime, new TestSecrets());
        settings.Save(new AppSettings { IntervalSeconds = 90 });
        var sourcePath = settings.CurrentSettingsPath;
        Assert.False(settings.MigrateStorage(StorageMode.ProfileAppData));
        Assert.Equal(StorageMode.PortableLocal, settings.CurrentStorageMode);
        Assert.True(File.Exists(sourcePath));
        Assert.Equal(90, settings.Load().IntervalSeconds);
    }

    /// <summary>Checks that a completed migration preserves the source as a recovery copy.</summary>
    [Fact]
    public void SuccessfulMigrationVerifiesSecretsBeforeCommittingLocation()
    {
        var runtime = CreateRuntime();
        var secrets = new TestSecrets();
        var settings = new SettingsService(runtime, secrets);
        settings.Save(new AppSettings { IntervalSeconds = 90 });
        var sourcePath = settings.CurrentSettingsPath;
        Assert.True(settings.MigrateStorage(StorageMode.ProfileAppData));
        Assert.Equal(StorageMode.ProfileAppData, runtime.GetConfiguredStorageMode());
        Assert.True(secrets.Copied);
        Assert.True(File.Exists(sourcePath));
        Assert.Equal(90, new SettingsService(runtime, secrets).Load().IntervalSeconds);
    }

    /// <summary>Checks that token contents are encrypted and that logout clears primary and recovery copies.</summary>
    [Fact]
    public void SessionIsProtectedAndLogoutRemovesRecoveryCopy()
    {
        var runtime = CreateRuntime();
        var secrets = new SecretStore(runtime);
        secrets.SaveSession("first-private-refresh-token");
        secrets.SaveSession("second-private-refresh-token");
        var path = Path.Combine(runtime.GetDataDirectory(StorageMode.PortableLocal), "Secrets", "session.bin");
        Assert.Equal("second-private-refresh-token", secrets.GetSession());
        Assert.DoesNotContain("private-refresh-token", Encoding.UTF8.GetString(File.ReadAllBytes(path)));
        Assert.True(File.Exists(path + ".bak"));
        secrets.DeleteSession();
        Assert.False(File.Exists(path));
        Assert.False(File.Exists(path + ".bak"));
    }

    /// <summary>Checks that truncated protected sessions fail explicitly instead of appearing successfully restored.</summary>
    [Fact]
    public void CorruptSessionFailsExplicitly()
    {
        var runtime = CreateRuntime();
        var secrets = new SecretStore(runtime);
        secrets.SaveSession("private-token");
        var path = Path.Combine(runtime.GetDataDirectory(StorageMode.PortableLocal), "Secrets", "session.bin");
        File.WriteAllBytes(path, [1, 2, 3]);
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() => secrets.GetSession());
    }

    /// <summary>Checks quoting and the apphost requirement for Windows startup.</summary>
    [Fact]
    public void StartupUsesQuotedExecutableInsteadOfDll()
    {
        Assert.Equal("\"C:\\Program Files\\KeepMeAlive\\KeepMeAlive.exe\" --silent",
            StartupService.BuildCommand(@"C:\Program Files\KeepMeAlive\KeepMeAlive.exe", true));
        Assert.Throws<ArgumentException>(() => StartupService.BuildCommand(@"C:\KeepMeAlive\KeepMeAlive.dll", false));
    }

    /// <summary>Deletes only the temporary directory created by this test instance.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory)) { Directory.Delete(_directory, recursive: true); }
    }

    private TestRuntime CreateRuntime() => new(_directory);

    private sealed class TestRuntime(string directory) : IAppRuntimeModeService
    {
        private StorageMode _mode = StorageMode.PortableLocal;
        public string ApplicationDirectory => Path.Combine(directory, "app");
        public bool IsPortableDistribution => true;
        public StorageMode DefaultStorageMode => StorageMode.PortableLocal;
        public bool FailBootstrap { get; set; }
        public int BootstrapWrites { get; private set; }
        public StorageMode GetConfiguredStorageMode() => _mode;
        public void SetConfiguredStorageMode(StorageMode storageMode)
        {
            if (FailBootstrap) { throw new IOException("Simulated bootstrap failure."); }
            BootstrapWrites++;
            _mode = storageMode;
        }
        public string GetDataDirectory(StorageMode storageMode) => storageMode == StorageMode.PortableLocal
            ? Path.Combine(ApplicationDirectory, "Data") : Path.Combine(directory, "profile");
    }

    private sealed class TestSecrets : ISecretStore
    {
        public bool FailCopy { get; set; }
        public bool Copied { get; private set; }
        public string GetLicenseKey() => string.Empty;
        public string GetLicenseKey(StorageMode storageMode) => string.Empty;
        public void SaveLicenseKey(string? licenseKey) { }
        public void SaveLicenseKey(string? licenseKey, StorageMode storageMode) { }
        public void DeleteLicenseKey(StorageMode storageMode) { }
        public string GetSession() => string.Empty;
        public void SaveSession(string? session) { }
        public void DeleteSession() { }
        public void CopyTo(StorageMode sourceMode, StorageMode targetMode)
        {
            if (FailCopy) { throw new IOException("Simulated protected store failure."); }
            Copied = true;
        }
        public void DeleteAll(StorageMode storageMode) { }
    }
}
