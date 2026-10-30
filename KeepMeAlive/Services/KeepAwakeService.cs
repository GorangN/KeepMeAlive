using KeepMeAlive.Services.Interfaces;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeepMeAlive.Services;

/// <summary>Owns handle-based power requests that safely cross asynchronous threads.</summary>
public sealed class KeepAwakeService : IKeepAwakeService, IDisposable
{
    private readonly object _gate = new();
    private SafeFileHandle? _request;

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get
        {
            lock (_gate)
            {
                return _request is { IsInvalid: false, IsClosed: false };
            }
        }
    }

    /// <inheritdoc/>
    public void Enable()
    {
        lock (_gate)
        {
            if (_request is not null)
            {
                return;
            }

            IntPtr reason = Marshal.StringToHGlobalUni("KeepMeAlive session is active");
            try
            {
                var context = new PowerReasonContext { Version = 0, Flags = 1, Reason = reason };
                var request = PowerCreateRequest(ref context);
                if (request.IsInvalid)
                {
                    int error = Marshal.GetLastWin32Error();
                    request.Dispose();
                    throw new Win32Exception(error);
                }

                if (!PowerSetRequest(request, 0) || !PowerSetRequest(request, 1))
                {
                    int error = Marshal.GetLastWin32Error();
                    request.Dispose();
                    throw new Win32Exception(error);
                }

                _request = request;
            }
            finally
            {
                Marshal.FreeHGlobal(reason);
            }
        }
    }

    /// <inheritdoc/>
    public void Refresh() => Enable();

    /// <inheritdoc/>
    public void Disable()
    {
        lock (_gate)
        {
            if (_request is null)
            {
                return;
            }

            // Closing the last handle clears its requests even after an API failure.
            _request.Dispose();
            _request = null;
        }
    }

    /// <summary>Releases any owned Windows power request.</summary>
    public void Dispose() => Disable();

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct PowerReasonContext
    {
        [FieldOffset(0)]
        internal uint Version;
        [FieldOffset(4)]
        internal uint Flags;
        [FieldOffset(8)]
        internal IntPtr Reason;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle PowerCreateRequest(ref PowerReasonContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(SafeFileHandle request, int requestType);
}
