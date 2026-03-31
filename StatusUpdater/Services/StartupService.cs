using Microsoft.Win32;
using StatusUpdater.Services.Interfaces;
using System.Reflection;

namespace StatusUpdater.Services;

public class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "StatusUpdater";

    private static string ExePath =>
        $"\"{Assembly.GetExecutingAssembly().Location}\" --silent";

    public bool IsStartupEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                return key?.GetValue(AppName) is string val && val.Contains("StatusUpdater");
            }
            catch { return false; }
        }
    }

    public void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key == null) return;

            if (enable)
                key.SetValue(AppName, ExePath);
            else
                key.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch { /* silently ignore registry errors */ }
    }
}
