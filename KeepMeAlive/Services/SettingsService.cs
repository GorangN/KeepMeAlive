using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeepMeAlive.Services;

/// <summary>Validates preferences and commits them through recoverable atomic file replacement.</summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IAppRuntimeModeService _runtimeModeService;
    private readonly ISecretStore _secretStore;
    private readonly object _gate = new();
    private AppSettings _current = new();
    private bool _preserveDamagedPrimary;

    /// <inheritdoc/>
    public AppSettings Current { get { lock (_gate) { return Clone(_current); } } }
    /// <inheritdoc/>
    public StorageMode CurrentStorageMode { get; private set; }
    /// <inheritdoc/>
    public string CurrentSettingsDirectory => _runtimeModeService.GetDataDirectory(CurrentStorageMode);
    /// <inheritdoc/>
    public string CurrentSettingsPath => Path.Combine(CurrentSettingsDirectory, "settings.json");
    /// <inheritdoc/>
    public string? LastError { get; private set; }

    /// <summary>Creates a settings store with the runtime paths and protected secrets it migrates.</summary>
    /// <param name="runtimeModeService">Resolves committed storage locations.</param>
    /// <param name="secretStore">Copies and verifies protected data during migration.</param>
    public SettingsService(IAppRuntimeModeService runtimeModeService, ISecretStore secretStore)
    {
        _runtimeModeService = runtimeModeService;
        _secretStore = secretStore;
        CurrentStorageMode = runtimeModeService.GetConfiguredStorageMode();
    }

    /// <inheritdoc/>
    public AppSettings Load()
    {
        lock (_gate)
        {
            CurrentStorageMode = _runtimeModeService.GetConfiguredStorageMode();
            LastError = null;
            _preserveDamagedPrimary = false;
            foreach (var path in new[] { CurrentSettingsPath, CurrentSettingsPath + ".bak" })
            {
                if (!File.Exists(path)) { continue; }
                try
                {
                    var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions)
                        ?? throw new InvalidDataException("Settings file is empty.");
                    settings.StorageMode = CurrentStorageMode;
                    Validate(settings);
                    _current = settings;
                    if (path.EndsWith(".bak", StringComparison.Ordinal))
                    {
                        LastError = "Recovered your previous settings. The damaged file has been preserved.";
                    }
                    return Clone(_current);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
                {
                    if (path == CurrentSettingsPath) { _preserveDamagedPrimary = true; }
                    LastError = "Your settings could not be loaded. Defaults are in use; the original file has been preserved.";
                }
            }
            _current = new AppSettings { StorageMode = CurrentStorageMode };
            return Clone(_current);
        }
    }

    /// <inheritdoc/>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate)
        {
            var candidate = Clone(settings);
            candidate.StorageMode = CurrentStorageMode;
            Validate(candidate);
            if (_preserveDamagedPrimary && File.Exists(CurrentSettingsPath))
            {
                File.Move(CurrentSettingsPath, CurrentSettingsPath + ".damaged-" + Guid.NewGuid().ToString("N"));
                _preserveDamagedPrimary = false;
            }
            AtomicFile.Write(CurrentSettingsPath, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(candidate, JsonOptions)));
            _current = candidate;
            LastError = null;
        }
    }

    /// <inheritdoc/>
    public bool MigrateStorage(StorageMode targetStorageMode)
    {
        lock (_gate)
        {
            if (!Enum.IsDefined(targetStorageMode)) { LastError = "The storage mode is invalid."; return false; }
            if (CurrentStorageMode == targetStorageMode) { return true; }
            var sourceMode = CurrentStorageMode;
            var candidate = Clone(_current);
            candidate.StorageMode = targetStorageMode;
            var targetPath = Path.Combine(_runtimeModeService.GetDataDirectory(targetStorageMode), "settings.json");
            try
            {
                AtomicFile.Write(targetPath, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(candidate, JsonOptions)));
                var verified = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(targetPath), JsonOptions);
                if (verified is null || JsonSerializer.Serialize(verified, JsonOptions) != JsonSerializer.Serialize(candidate, JsonOptions))
                {
                    throw new IOException("Settings verification failed.");
                }
                _secretStore.CopyTo(sourceMode, targetStorageMode);
                _runtimeModeService.SetConfiguredStorageMode(targetStorageMode);
                if (_runtimeModeService.GetConfiguredStorageMode() != targetStorageMode)
                {
                    throw new IOException("The new storage location could not be committed.");
                }
                CurrentStorageMode = targetStorageMode;
                _current = candidate;
                LastError = null;
                // Source files remain as protected recovery copies until the user removes them.
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or System.Security.Cryptography.CryptographicException)
            {
                LastError = "Storage could not be moved. Your original data and location have been retained.";
                return false;
            }
        }
    }

    private static AppSettings Clone(AppSettings settings) =>
        JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, JsonOptions), JsonOptions)!;

    private static void Validate(AppSettings settings)
    {
        if (settings.IntervalSeconds is < 20 or > 86400) { throw new InvalidDataException("Interval must be between 20 and 86400 seconds."); }
        if (settings.MousePixelDelta is < 1 or > 5) { throw new InvalidDataException("Mouse movement must be between 1 and 5 pixels."); }
        if (settings.VirtualKeyCode is < 124 or > 135) { throw new InvalidDataException("Choose an unused function key between F13 and F24."); }
        if (settings.KeepAliveMethod is not ("Keyboard" or "Mouse")) { throw new InvalidDataException("The input method is invalid."); }
        if (settings.Theme is not ("Light" or "Dark" or "System")) { throw new InvalidDataException("The theme is invalid."); }
        if (!Enum.IsDefined(settings.ScheduledActionType)) { throw new InvalidDataException("The scheduled action is invalid."); }
        if (!Enum.IsDefined(settings.StorageMode)) { throw new InvalidDataException("The storage mode is invalid."); }
    }
}
