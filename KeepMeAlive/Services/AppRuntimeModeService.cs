using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeepMeAlive.Services;

/// <summary>Resolves portable/profile paths and commits storage selection atomically.</summary>
public sealed class AppRuntimeModeService : IAppRuntimeModeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly string _profileDirectory;
    private string LegacyBootstrapPath => Path.Combine(ApplicationDirectory, "KeepMeAlive.bootstrap.json");
    private string BootstrapPath => IsPortableDistribution ? LegacyBootstrapPath : Path.Combine(_profileDirectory, "runtime.json");

    /// <inheritdoc/>
    public string ApplicationDirectory { get; }
    /// <inheritdoc/>
    public bool IsPortableDistribution { get; }
    /// <inheritdoc/>
    public StorageMode DefaultStorageMode => IsPortableDistribution ? StorageMode.PortableLocal : StorageMode.ProfileAppData;

    /// <summary>Creates the runtime path resolver for this application and Windows profile.</summary>
    public AppRuntimeModeService() : this(AppContext.BaseDirectory,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeepMeAlive")) { }

    /// <summary>Creates an isolated path resolver for an installation and profile directory.</summary>
    /// <param name="applicationDirectory">The installation directory.</param>
    /// <param name="profileDirectory">The profile data directory.</param>
    public AppRuntimeModeService(string applicationDirectory, string profileDirectory)
    {
        ApplicationDirectory = Path.GetFullPath(applicationDirectory);
        _profileDirectory = Path.GetFullPath(profileDirectory);
        IsPortableDistribution = File.Exists(Path.Combine(ApplicationDirectory, "KeepMeAlive.portable"));
    }

    /// <inheritdoc/>
    public StorageMode GetConfiguredStorageMode()
    {
        foreach (var path in new[] { BootstrapPath, BootstrapPath + ".bak", LegacyBootstrapPath })
        {
            if (!File.Exists(path)) { continue; }
            try
            {
                var bootstrap = JsonSerializer.Deserialize<RuntimeBootstrap>(File.ReadAllText(path), JsonOptions);
                if (bootstrap is not null && Enum.IsDefined(bootstrap.StorageMode)) { return bootstrap.StorageMode; }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { }
        }
        return DefaultStorageMode;
    }

    /// <inheritdoc/>
    public void SetConfiguredStorageMode(StorageMode storageMode)
    {
        if (!Enum.IsDefined(storageMode)) { throw new ArgumentOutOfRangeException(nameof(storageMode)); }
        var json = JsonSerializer.Serialize(new RuntimeBootstrap { StorageMode = storageMode }, JsonOptions);
        AtomicFile.Write(BootstrapPath, Encoding.UTF8.GetBytes(json));
    }

    /// <inheritdoc/>
    public string GetDataDirectory(StorageMode storageMode) => storageMode switch
    {
        StorageMode.PortableLocal => Path.Combine(ApplicationDirectory, "Data"),
        StorageMode.ProfileAppData => _profileDirectory,
        _ => throw new ArgumentOutOfRangeException(nameof(storageMode))
    };
}
