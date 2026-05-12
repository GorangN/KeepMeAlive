using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

public interface ISettingsService
{
    AppSettings Current { get; }
    StorageMode CurrentStorageMode { get; }
    string CurrentSettingsDirectory { get; }
    string CurrentSettingsPath { get; }
    AppSettings Load();
    void Save(AppSettings settings);
    bool MigrateStorage(StorageMode targetStorageMode);
}
