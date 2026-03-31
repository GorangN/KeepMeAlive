using StatusUpdater.Services.Interfaces;
using System.Windows.Threading;
using static StatusUpdater.Helpers.Interop;

namespace StatusUpdater.Services;

public class IdleMonitorService : IIdleMonitorService, IDisposable
{
    private readonly DispatcherTimer _timer;
    private int _idleSeconds;

    public int IdleSeconds
    {
        get => _idleSeconds;
        private set
        {
            if (_idleSeconds != value)
            {
                _idleSeconds = value;
                IdleUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? IdleUpdated;

    public IdleMonitorService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => IdleSeconds = GetIdleSeconds();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private static int GetIdleSeconds()
    {
        var lii = new LASTINPUTINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref lii)) return -1;
        uint tick = (uint)Environment.TickCount;
        return (int)((tick - lii.dwTime) / 1000);
    }

    public void Dispose() => _timer.Stop();
}
