using KeepMeAlive.Services.Interfaces;
using System.Runtime.InteropServices;
using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;

/// <summary>Samples Windows last-input time without owning a WPF timer.</summary>
public sealed class IdleMonitorService : IIdleMonitorService, IDisposable
{
    private Timer? _timer;
    private int _idleSeconds = -1;

    /// <inheritdoc/>
    public int IdleSeconds => Volatile.Read(ref _idleSeconds);

    /// <inheritdoc/>
    public event EventHandler? IdleUpdated;

    /// <inheritdoc/>
    public void Start()
    {
        _timer ??= new Timer(_ => Sample(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    /// <inheritdoc/>
    public void Stop()
    {
        Interlocked.Exchange(ref _timer, null)?.Dispose();
    }

    /// <summary>Stops background sampling.</summary>
    public void Dispose() => Stop();

    private void Sample()
    {
        var information = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        int seconds = GetLastInputInfo(ref information)
            ? (int)(unchecked((uint)Environment.TickCount - information.dwTime) / 1000)
            : -1;
        if (Interlocked.Exchange(ref _idleSeconds, seconds) != seconds)
        {
            IdleUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
