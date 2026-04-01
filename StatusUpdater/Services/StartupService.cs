using Microsoft.Win32;
using KeepMeAlive.Services.Interfaces;
using System.Reflection;

namespace KeepMeAlive.Services;

public class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeepMeAlive";

    private static string ExePath =>
        $"\"{Assembly.GetExecutingAssembly().Location}\" --silent";

    public bool IsStartupEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                return key?.GetValue(AppName) is string val && val.Contains("KeepMeAlive");
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
