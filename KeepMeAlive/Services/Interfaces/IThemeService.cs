namespace KeepMeAlive.Services.Interfaces;

/// <summary>Applies the selected application appearance.</summary>
public interface IThemeService
{
    /// <summary>Gets the currently resolved appearance.</summary>
    string CurrentTheme { get; }
    /// <summary>Applies a validated appearance preference.</summary>
    /// <param name="themeName">Light, Dark, or System.</param>
    void Apply(string themeName);
}
