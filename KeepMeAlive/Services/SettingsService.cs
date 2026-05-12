using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeepMeAlive.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IAppRuntimeModeService _runtimeModeService;

    public AppSettings Current { get; private set; } = new();

    public StorageMode CurrentStorageMode { get; private set; }

    public string CurrentSettingsDirectory =>
        _runtimeModeService.GetDataDirectory(CurrentStorageMode);

    public string CurrentSettingsPath =>
        Path.Combine(CurrentSettingsDirectory, "settings.json");

    public SettingsService(IAppRuntimeModeService runtimeModeService)
    {
        _runtimeModeService = runtimeModeService;
        CurrentStorageMode = _runtimeModeService.GetConfiguredStorageMode();
    }

    public AppSettings Load()
    {
        CurrentStorageMode = _runtimeModeService.GetConfiguredStorageMode();

        try
        {
            if (File.Exists(CurrentSettingsPath))
            {
                var json = File.ReadAllText(CurrentSettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                Current.StorageMode = CurrentStorageMode;
                return Current;
            }
        }
        catch { /* return defaults on any error */ }

        Current = new AppSettings
        {
            StorageMode = CurrentStorageMode
        };
        return Current;
    }

    public void Save(AppSettings settings)
    {
        settings.StorageMode = CurrentStorageMode;
        Current = settings;
        if (TryWriteSettings(settings, CurrentSettingsDirectory, CurrentSettingsPath))
        {
            _runtimeModeService.SetConfiguredStorageMode(CurrentStorageMode);
        }
    }

    public bool MigrateStorage(StorageMode targetStorageMode)
    {
        if (CurrentStorageMode == targetStorageMode)
        {
            _runtimeModeService.SetConfiguredStorageMode(targetStorageMode);
            Current.StorageMode = targetStorageMode;
            return true;
        }

        var originalStorageMode = CurrentStorageMode;
        var sourcePath = CurrentSettingsPath;
        var sourceDirectory = CurrentSettingsDirectory;
        var targetDirectory = _runtimeModeService.GetDataDirectory(targetStorageMode);
        var targetPath = Path.Combine(targetDirectory, "settings.json");

        Current.StorageMode = targetStorageMode;
        if (!TryWriteSettings(Current, targetDirectory, targetPath))
        {
            Current.StorageMode = originalStorageMode;
            return false;
        }

        CurrentStorageMode = targetStorageMode;
        _runtimeModeService.SetConfiguredStorageMode(targetStorageMode);

        try
        {
            if (File.Exists(sourcePath) && !string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(sourcePath);
            }

            if (Directory.Exists(sourceDirectory)
                && !string.Equals(sourceDirectory, targetDirectory, StringComparison.OrdinalIgnoreCase)
                && !Directory.EnumerateFileSystemEntries(sourceDirectory).Any())
            {
                Directory.Delete(sourceDirectory);
            }
        }
        catch
        {
            // Ignore best-effort cleanup failures after migration.
        }

        return true;
    }

    private static bool TryWriteSettings(AppSettings settings, string directory, string path)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
