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
}
