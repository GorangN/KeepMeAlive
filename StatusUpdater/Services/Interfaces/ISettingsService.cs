using StatusUpdater.Models;

namespace StatusUpdater.Services.Interfaces;

public interface ISettingsService
{
    AppSettings Current { get; }
    AppSettings Load();
    void Save(AppSettings settings);
}
