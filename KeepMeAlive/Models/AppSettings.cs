namespace KeepMeAlive.Models;

/// <summary>Local preferences for input, appearance, privacy, storage, and scheduled system actions.</summary>
public class AppSettings
{
    /// <summary>Gets or sets the persisted local data location.</summary>
    public StorageMode StorageMode { get; set; } = StorageMode.ProfileAppData;
    /// <summary>Gets or sets the selected input strategy.</summary>
    public string KeepAliveMethod { get; set; } = "Keyboard";
    /// <summary>Gets or sets the base delay between pulses in seconds.</summary>
    public int IntervalSeconds { get; set; } = 60;
    /// <summary>Gets or sets whether an active session prevents display/system sleep.</summary>
    public bool KeepAwakeEnabled { get; set; }
    /// <summary>Gets or sets the virtual-key code, restricted to F13 through F24.</summary>
    public int VirtualKeyCode { get; set; } = 126;
    /// <summary>Gets or sets the mouse movement distance from one through five pixels.</summary>
    public int MousePixelDelta { get; set; } = 2;
    /// <summary>Gets or sets Light, Dark, or System appearance.</summary>
    public string Theme { get; set; } = "Light";
    /// <summary>Gets or sets whether the app launches when the current user signs in.</summary>
    public bool StartOnBoot { get; set; }
    /// <summary>Gets or sets whether local status notifications are shown.</summary>
    public bool ShowNotifications { get; set; } = true;
    /// <summary>Gets or sets whether automatic network update checks are permitted.</summary>
    public bool EnableAutomaticUpdateChecks { get; set; }
    /// <summary>Gets or sets whether startup network account synchronization is permitted.</summary>
    public bool EnableStartupAccountSync { get; set; }
    /// <summary>Gets or sets whether startup leaves the window hidden in the tray.</summary>
    public bool StartMinimizedToTray { get; set; }
    /// <summary>Gets or sets whether a one-time system action is armed.</summary>
    public bool ScheduledActionEnabled { get; set; }
    /// <summary>Gets or sets the system action to perform once.</summary>
    public ScheduledAction ScheduledActionType { get; set; } = ScheduledAction.Shutdown;
    /// <summary>Gets or sets the legacy-compatible local execution time in dd:MM:yyyy:HH:mm:ss format.</summary>
    public string ScheduledActionTime { get; set; } = string.Empty;
}
