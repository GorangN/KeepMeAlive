using KeepMeAlive.Helpers;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace KeepMeAlive.Services;

/// <summary>Runs non-forced shutdown/restart or native sleep through Windows.</summary>
public sealed class WindowsSystemActionExecutor : ISystemActionExecutor
{
    /// <inheritdoc/>
    public void Execute(ScheduledAction action)
    {
        if (action == ScheduledAction.Sleep)
        {
            if (!Interop.SetSuspendState(false, false, false))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return;
        }

        string arguments = action switch
        {
            ScheduledAction.Shutdown => "/s /t 0",
            ScheduledAction.Restart => "/r /t 0",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe"),
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Windows could not start the scheduled action.");
        if (!process.WaitForExit(10000) || process.ExitCode != 0)
        {
            throw new InvalidOperationException("Windows rejected the scheduled action.");
        }
    }
}
