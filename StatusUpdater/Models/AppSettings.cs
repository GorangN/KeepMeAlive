namespace StatusUpdater.Models;

public class AppSettings
{
    public string KeepAliveMethod { get; set; } = "Keyboard";
    public int IntervalSeconds { get; set; } = 60;
    public bool KeepAwakeEnabled { get; set; } = false;
    public int VirtualKeyCode { get; set; } = 126; // F22
    public int MousePixelDelta { get; set; } = 2;
    public string Theme { get; set; } = "Dark";
    public bool StartOnBoot { get; set; } = false;
    public bool ShowNotifications { get; set; } = true;
    public string LicenseKey { get; set; } = "";

    /// <summary>Gets or sets a value indicating whether the scheduled action is armed.</summary>
    public bool ScheduledActionEnabled { get; set; } = false;

    /// <summary>Gets or sets the system action to perform at the scheduled time.</summary>
    public ScheduledAction ScheduledActionType { get; set; } = ScheduledAction.Shutdown;

    /// <summary>
    /// Gets or sets the scheduled execution time in <c>dd:MM:yyyy:HH:mm:ss</c> format.
    /// An empty string means no time has been configured.
    /// </summary>
    public string ScheduledActionTime { get; set; } = string.Empty;
}
