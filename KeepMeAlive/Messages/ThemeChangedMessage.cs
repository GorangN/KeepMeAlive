namespace KeepMeAlive.Messages;

/// <summary>Notifies application components when a saved appearance preference changes.</summary>
/// <param name="NewTheme">The selected Light, Dark, or System appearance.</param>
public record ThemeChangedMessage(string NewTheme);
