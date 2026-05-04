using KeepMeAlive.Models;

namespace KeepMeAlive.Services.Interfaces;

public interface ISettingsService
{
    AppSettings Current { get; }
    AppSettings Load();
    void Save(AppSettings settings);
}
