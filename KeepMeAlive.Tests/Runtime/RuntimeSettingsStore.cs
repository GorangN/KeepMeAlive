using System.IO;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Runtime;

internal sealed class RuntimeSettingsStore : ISettingsService
{
    private AppSettings _current = new();
    internal bool FailSave { get; set; }
    public AppSettings Current => Clone(_current);
    public StorageMode CurrentStorageMode => StorageMode.ProfileAppData;
    public string CurrentSettingsDirectory => string.Empty;
    public string CurrentSettingsPath => string.Empty;
    public string? LastError => null;
    public AppSettings Load() => Current;
    public void Save(AppSettings settings)
    {
        if (FailSave)
        {
            throw new IOException("Simulated unavailable storage");
        }

        _current = Clone(settings);
    }

    public bool MigrateStorage(StorageMode targetStorageMode) => false;

    private static AppSettings Clone(AppSettings value) => new()
    {
        ScheduledActionEnabled = value.ScheduledActionEnabled,
        ScheduledActionTime = value.ScheduledActionTime,
        ScheduledActionType = value.ScheduledActionType,
        KeepAliveMethod = value.KeepAliveMethod,
        IntervalSeconds = value.IntervalSeconds,
        VirtualKeyCode = value.VirtualKeyCode,
        MousePixelDelta = value.MousePixelDelta,
        KeepAwakeEnabled = value.KeepAwakeEnabled
    };
}
