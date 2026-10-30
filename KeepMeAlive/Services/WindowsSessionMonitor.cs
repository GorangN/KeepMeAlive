using KeepMeAlive.Services.Interfaces;
using System.Runtime.InteropServices;
using System.Text;

namespace KeepMeAlive.Services;

/// <summary>Allows input only on the user's normal unlocked input desktop.</summary>
public sealed class WindowsSessionMonitor : ISessionMonitor
{
    /// <inheritdoc/>
    public bool CanInject
    {
        get
        {
            if (!IsSessionActive())
            {
                return false;
            }

            IntPtr desktop = OpenInputDesktop(0, false, 1);
            if (desktop == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var name = new StringBuilder(256);
                return GetUserObjectInformation(desktop, 2, name, name.Capacity * sizeof(char), out _)
                    && string.Equals(name.ToString(), "Default", StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                CloseDesktop(desktop);
            }
        }
    }

    private static bool IsSessionActive()
    {
        if (!WTSQuerySessionInformation(IntPtr.Zero, -1, 8, out IntPtr information, out int length))
        {
            return false;
        }

        try
        {
            return length >= sizeof(int) && Marshal.ReadInt32(information) == 0;
        }
        finally
        {
            WTSFreeMemory(information);
        }
    }

    [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WTSQuerySessionInformation(IntPtr server, int sessionId, int informationClass, out IntPtr information, out int length);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr information);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder information, int length, out int needed);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseDesktop(IntPtr desktop);
}
