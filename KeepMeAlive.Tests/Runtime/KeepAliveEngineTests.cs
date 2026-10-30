using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using System.ComponentModel;

namespace KeepMeAlive.Tests.Runtime;

public sealed class KeepAliveEngineTests
{
    [Theory]
    [InlineData(KeepAliveMethod.Keyboard)]
    [InlineData(KeepAliveMethod.Mouse)]
    public async Task RunsOnlyTheSelectedInputAndReleasesPowerOnCancellation(KeepAliveMethod method)
    {
        var clock = new FakeTimeProvider();
        var input = new Input();
        var power = new Power();
        var engine = new KeepAliveEngine(input, power, new Session(), clock);
        using var cancellation = new CancellationTokenSource();
        Task run = engine.RunAsync(new(method, 60, true, 126, 2), cancellation.Token);

        Assert.Single(input.Options);
        Assert.Equal(method, input.Options[0].Method);
        Assert.True(power.IsEnabled);
        cancellation.Cancel();
        await run;
        Assert.False(power.IsEnabled);
    }

    [Fact]
    public async Task NativeFailureStopsTheEngineAndReleasesPower()
    {
        var power = new Power();
        var engine = new KeepAliveEngine(new Input { Fail = true }, power, new Session(), new FakeTimeProvider());
        KeepAliveStatus? status = null;
        engine.StatusChanged += (_, value) => status = value;

        await engine.RunAsync(new(KeepAliveMethod.Keyboard, 60, true, 126, 2), CancellationToken.None);

        Assert.False(power.IsEnabled);
        Assert.NotNull(status);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
    }

    [Fact]
    public async Task LockedSessionPausesInputAndResumesWhenAvailable()
    {
        var clock = new FakeTimeProvider();
        var input = new Input();
        var session = new Session { CanInject = false };
        var power = new Power();
        var engine = new KeepAliveEngine(input, power, session, clock);
        using var cancellation = new CancellationTokenSource();
        Task run = engine.RunAsync(new(KeepAliveMethod.Mouse, 60, true, 126, 2), cancellation.Token);

        Assert.Empty(input.Options);
        Assert.False(power.IsEnabled);
        session.CanInject = true;
        clock.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => input.Options.Count == 1);
        Assert.True(power.IsEnabled);
        cancellation.Cancel();
        await run;
    }

    [Fact]
    public async Task DisablingPowerDuringSessionReleasesExistingRequest()
    {
        var clock = new FakeTimeProvider();
        var power = new Power();
        var engine = new KeepAliveEngine(new Input(), power, new Session(), clock);
        using var cancellation = new CancellationTokenSource();
        Task run = engine.RunAsync(new(KeepAliveMethod.Keyboard, 60, true, 126, 2), cancellation.Token);
        engine.SetKeepAwake(false);

        clock.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => !power.IsEnabled);
        cancellation.Cancel();
        await run;
    }

    [Fact]
    public async Task LockingAnActiveSessionReleasesPowerAndPublishesPausedState()
    {
        var clock = new FakeTimeProvider();
        var input = new Input();
        var power = new Power();
        var session = new Session();
        var engine = new KeepAliveEngine(input, power, session, clock);
        KeepAliveStatus? lastStatus = null;
        engine.StatusChanged += (_, status) => lastStatus = status;
        using var cancellation = new CancellationTokenSource();
        Task run = engine.RunAsync(new(KeepAliveMethod.Keyboard, 60, true, 126, 2), cancellation.Token);
        session.CanInject = false;
        clock.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => lastStatus?.IsPaused == true);
        Assert.False(power.IsEnabled);
        Assert.Null(lastStatus!.NextPulseAt);
        Assert.Single(input.Options);
        cancellation.Cancel();
        await run;
    }

    [Fact]
    public async Task AStoppedEngineCanStartAnotherSession()
    {
        var input = new Input();
        var engine = new KeepAliveEngine(input, new Power(), new Session(), new FakeTimeProvider());
        var options = new KeepAliveOptions(KeepAliveMethod.Keyboard, 60, false, 126, 2);
        for (int session = 0; session < 2; session++)
        {
            using var cancellation = new CancellationTokenSource();
            Task run = engine.RunAsync(options, cancellation.Token);
            cancellation.Cancel();
            await run;
        }

        Assert.Equal(2, input.Options.Count);
    }

    [Fact]
    public async Task RejectsSecondConcurrentSession()
    {
        var engine = new KeepAliveEngine(new Input(), new Power(), new Session(), new FakeTimeProvider());
        using var cancellation = new CancellationTokenSource();
        var options = new KeepAliveOptions(KeepAliveMethod.Keyboard, 60, false, 126, 2);
        Task first = engine.RunAsync(options, cancellation.Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunAsync(options, CancellationToken.None));
        cancellation.Cancel();
        await first;
    }

    [Theory]
    [InlineData(19, 126, 2)]
    [InlineData(86401, 126, 2)]
    [InlineData(60, 123, 2)]
    [InlineData(60, 136, 2)]
    [InlineData(60, 126, 0)]
    [InlineData(60, 126, 6)]
    public void RejectsUnsafeOptions(int interval, int key, int distance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KeepAliveOptions(KeepAliveMethod.Keyboard, interval, false, key, distance).Validate());
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!predicate())
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    private sealed class Input : IInputService
    {
        internal List<KeepAliveOptions> Options { get; } = [];
        internal bool Fail { get; init; }
        public void Pulse(KeepAliveOptions options)
        {
            if (Fail)
            {
                throw new Win32Exception("Input blocked");
            }

            Options.Add(options);
        }
    }

    private sealed class Power : IKeepAwakeService
    {
        public bool IsEnabled { get; private set; }
        public void Enable() => IsEnabled = true;
        public void Refresh() => Enable();
        public void Disable() => IsEnabled = false;
    }

    private sealed class Session : ISessionMonitor
    {
        public bool CanInject { get; set; } = true;
    }
}
