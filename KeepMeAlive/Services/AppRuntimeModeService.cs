using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeepMeAlive.Services;

/// <summary>
/// Resolves runtime-specific file locations and persists the selected storage mode beside the executable.
/// </summary>
public sealed class AppRuntimeModeService : IAppRuntimeModeService
{
    private const string PortableMarkerFileName = "KeepMeAlive.portable";
    private const string BootstrapFileName = "KeepMeAlive.bootstrap.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets the directory that contains the running application binaries.
    /// </summary>
    public string ApplicationDirectory { get; } = AppContext.BaseDirectory;

    /// <summary>
    /// Gets a value indicating whether the current distribution was published as a portable package.
    /// </summary>
    public bool IsPortableDistribution { get; }

    /// <summary>
    /// Gets the storage mode used when no bootstrap override exists.
    /// </summary>
    public StorageMode DefaultStorageMode => IsPortableDistribution
        ? StorageMode.PortableLocal
        : StorageMode.ProfileAppData;

    private string BootstrapPath => Path.Combine(ApplicationDirectory, BootstrapFileName);

    /// <summary>
    /// Initializes a new instance of the <see cref="AppRuntimeModeService"/> class.
    /// </summary>
    public AppRuntimeModeService()
    {
        IsPortableDistribution = File.Exists(Path.Combine(ApplicationDirectory, PortableMarkerFileName));
    }

    /// <inheritdoc/>
    public StorageMode GetConfiguredStorageMode()
    {
        try
        {
            if (!File.Exists(BootstrapPath))
            {
                return DefaultStorageMode;
            }

            var json = File.ReadAllText(BootstrapPath);
            var bootstrap = JsonSerializer.Deserialize<RuntimeBootstrap>(json, JsonOptions);

            return bootstrap?.StorageMode ?? DefaultStorageMode;
        }
        catch
        {
            return DefaultStorageMode;
        }
    }

    /// <inheritdoc/>
    public void SetConfiguredStorageMode(StorageMode storageMode)
    {
        try
        {
            var bootstrap = new RuntimeBootstrap
            {
                StorageMode = storageMode
            };

            Directory.CreateDirectory(ApplicationDirectory);
            var json = JsonSerializer.Serialize(bootstrap, JsonOptions);
            File.WriteAllText(BootstrapPath, json);
        }
        catch
        {
            // Ignore bootstrap write failures and fall back to runtime defaults next launch.
        }
    }

    /// <inheritdoc/>
    public string GetDataDirectory(StorageMode storageMode)
    {
        return storageMode switch
        {
            StorageMode.PortableLocal => Path.Combine(ApplicationDirectory, "Data"),
            _ => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KeepMeAlive"),
        };
    }
}
