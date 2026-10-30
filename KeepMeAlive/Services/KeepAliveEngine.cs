using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Runs selected input, pauses for locked sessions, and releases power deterministically.</summary>
public sealed class KeepAliveEngine : IKeepAliveEngine
{
    private readonly IInputService _input;
    private readonly IKeepAwakeService _power;
    private readonly ISessionMonitor _session;
    private readonly TimeProvider _clock;
    private int _running;
    private volatile bool _keepAwake;

    /// <summary>Creates an engine with injectable operating-system services and clock.</summary>
    /// <param name="input">The selected-input adapter.</param>
    /// <param name="power">The power-request adapter.</param>
    /// <param name="session">The interactive-session monitor.</param>
    /// <param name="clock">The clock used for delays and status.</param>
    public KeepAliveEngine(IInputService input, IKeepAwakeService power, ISessionMonitor session, TimeProvider clock)
    {
        _input = input;
        _power = power;
        _session = session;
        _clock = clock;
    }

    /// <inheritdoc/>
    public event EventHandler<KeepAliveStatus>? StatusChanged;

    /// <inheritdoc/>
    public void SetKeepAwake(bool enabled) => _keepAwake = enabled;

    /// <inheritdoc/>
    public async Task RunAsync(KeepAliveOptions options, CancellationToken cancellationToken)
    {
        options.Validate();
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException("A keep-alive session is already running.");
        }

        _keepAwake = options.KeepAwakeEnabled;
        string? error = null;
        DateTimeOffset nextPulse = _clock.GetUtcNow();
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool available = _session.CanInject;
                if (_keepAwake && available && !_power.IsEnabled)
                {
                    _power.Enable();
                }
                else if ((!_keepAwake || !available) && _power.IsEnabled)
                {
                    _power.Disable();
                }

                if (available && _clock.GetUtcNow() >= nextPulse)
                {
                    _input.Pulse(options);
                    nextPulse = _clock.GetUtcNow().AddSeconds(Math.Max(20, options.IntervalSeconds + Random.Shared.Next(-15, 16)));
                }

                StatusChanged?.Invoke(this, new KeepAliveStatus(true, !available, available ? nextPulse : null, null));
                await Task.Delay(TimeSpan.FromSeconds(1), _clock, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            error = "Windows could not complete the input or power request. Check device policies and try again.";
        }
        finally
        {
            try
            {
                _power.Disable();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                error = "Windows could not release the power request. Close the application before trying again.";
            }

            Interlocked.Exchange(ref _running, 0);
            StatusChanged?.Invoke(this, new KeepAliveStatus(false, false, null, error));
        }
    }
}
