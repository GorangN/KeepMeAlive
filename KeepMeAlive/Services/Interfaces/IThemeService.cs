namespace KeepMeAlive.Services.Interfaces;

public interface IThemeService
{
    string CurrentTheme { get; }
    void Apply(string themeName);
}
