using System.ComponentModel;
using System.Runtime.InteropServices;
using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;

internal static class NativeInputWriter
{
    internal static void Send(INPUT[] inputs)
    {
        uint accepted = SendInput((uint)inputs.Length, inputs, INPUT.Size);
        if (accepted == inputs.Length)
        {
            return;
        }

        int error = Marshal.GetLastWin32Error();
        if (accepted > 0 && accepted < inputs.Length)
        {
            // Complete a partial pair so a failed key press does not leave a modifier held.
            INPUT[] remaining = inputs[(int)accepted..];
            SendInput((uint)remaining.Length, remaining, INPUT.Size);
        }

        throw new Win32Exception(error, "Windows did not accept the complete input pulse.");
    }
}
