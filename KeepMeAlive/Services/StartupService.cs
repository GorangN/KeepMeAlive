using Microsoft.Win32;
using KeepMeAlive.Services.Interfaces;
using System.IO;

namespace KeepMeAlive.Services;

/// <summary>Registers the application host executable for the current user's Windows sign-in.</summary>
public sealed class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeepMeAlive";

    /// <inheritdoc/>
    public bool IsStartupEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            return IsOwnedCommand(key?.GetValue(AppName) as string, ExecutablePath);
        }
    }

    private static string ExecutablePath
    {
        get
        {
            var path = Path.Combine(AppContext.BaseDirectory, "KeepMeAlive.exe");
            if (!File.Exists(path)) { throw new FileNotFoundException("The KeepMeAlive executable could not be located.", path); }
            return path;
        }
    }

    /// <summary>Creates the quoted apphost command used by Windows startup.</summary>
    /// <param name="executablePath">The full path to the application executable.</param>
    /// <param name="minimizedToTray">Whether to start without displaying the main window.</param>
    /// <returns>A quoted startup command.</returns>
    /// <exception cref="ArgumentException">The path is not an absolute executable path.</exception>
    public static string BuildCommand(string executablePath, bool minimizedToTray)
    {
        if (!Path.IsPathFullyQualified(executablePath) || executablePath.Contains('"') ||
            !string.Equals(Path.GetExtension(executablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("An absolute executable path is required.", nameof(executablePath));
        }
        return "\"" + executablePath + "\"" + (minimizedToTray ? " --silent" : string.Empty);
    }

    /// <summary>Checks whether a startup value belongs to the specified application executable.</summary>
    /// <param name="command">The stored startup command, or null when none exists.</param>
    /// <param name="executablePath">The absolute executable path owned by this installation.</param>
    /// <returns>True only for a quoted normal or minimized command emitted for this executable.</returns>
    /// <exception cref="ArgumentException">The executable path is not an absolute executable path.</exception>
    public static bool IsOwnedCommand(string? command, string executablePath) =>
        string.Equals(command?.Trim(), BuildCommand(executablePath, false), StringComparison.OrdinalIgnoreCase) ||
        string.Equals(command?.Trim(), BuildCommand(executablePath, true), StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void SetStartup(bool enable, bool minimizedToTray)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, writable: true)
            ?? throw new IOException("Windows startup settings could not be opened.");
        if (enable)
        {
            var command = BuildCommand(ExecutablePath, minimizedToTray);
            key.SetValue(AppName, command, RegistryValueKind.String);
            if (!string.Equals(key.GetValue(AppName) as string, command, StringComparison.Ordinal))
            {
                throw new IOException("Windows startup registration could not be verified.");
            }
        }
        else if (IsOwnedCommand(key.GetValue(AppName) as string, ExecutablePath))
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
