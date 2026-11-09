using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.Tests.Runtime;
using KeepMeAlive.ViewModels;
using Microsoft.Extensions.Time.Testing;

namespace KeepMeAlive.Tests;

/// <summary>Checks pulse presentation against controlled engine deadlines and a controllable clock.</summary>
public sealed class PulseCountdownTests
{
    /// <summary>Checks that the stopped preview follows edited and discarded intervals.</summary>
    [Fact]
    public async Task StoppedPreviewTracksConfiguredIntervalIncludingTwentyFourHours()
    {
        var (model, _, _) = Create();
        await using var lifetime = model;
        Assert.Equal("00:01:00", model.PulseCountdownDisplay);
        model.IntervalSeconds = 86400;
        Assert.Equal("24:00:00", model.PulseCountdownDisplay);
        Assert.False(model.HasPulseCountdown);
        Assert.Equal(0, model.PulseProgress);
        model.DiscardChanges();
        Assert.Equal("00:01:00", model.PulseCountdownDisplay);
    }

    /// <summary>Checks ceiling-based time display and progress for an interval differing from the preference.</summary>
    [Fact]
    public async Task CountdownRoundsUpAndRingUsesTheObservedJitteredCycle()
    {
        var (model, engine, clock) = Create();
        await using var lifetime = model;
        var deadline = clock.GetUtcNow().AddSeconds(75.4);
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal("00:01:16", model.PulseCountdownDisplay);
        Assert.True(model.HasPulseCountdown);
        Assert.Equal(1, model.PulseProgress);

        clock.Advance(TimeSpan.FromSeconds(1));
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal("00:01:15", model.PulseCountdownDisplay);
        Assert.Equal(74.4 / 75.4, model.PulseProgress, 8);
        clock.Advance(TimeSpan.FromSeconds(74.4));
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal("00:00:00", model.PulseCountdownDisplay);
        Assert.Equal(0, model.PulseProgress);
    }

    /// <summary>Checks that successive engine cycles do not reuse the preceding cycle's denominator.</summary>
    [Fact]
    public async Task ANewEngineDeadlineRestartsTheRingWithItsOwnCycleLength()
    {
        var (model, engine, clock) = Create();
        await using var lifetime = model;
        var firstDeadline = clock.GetUtcNow().AddSeconds(45);
        engine.Publish(new(true, false, firstDeadline, null));
        clock.Advance(TimeSpan.FromSeconds(45));
        var nextDeadline = clock.GetUtcNow().AddSeconds(72);
        engine.Publish(new(true, false, nextDeadline, null));
        Assert.Equal("00:01:12", model.PulseCountdownDisplay);
        Assert.Equal(1, model.PulseProgress);
        clock.Advance(TimeSpan.FromSeconds(36));
        engine.Publish(new(true, false, nextDeadline, null));
        Assert.Equal("00:00:36", model.PulseCountdownDisplay);
        Assert.Equal(0.5, model.PulseProgress);
    }

    /// <summary>Checks the paused placeholder and continuity when the engine resumes its existing deadline.</summary>
    [Fact]
    public async Task PausingNeutralizesTheRingAndResumingKeepsItsOriginalBaseline()
    {
        var (model, engine, clock) = Create();
        await using var lifetime = model;
        var deadline = clock.GetUtcNow().AddSeconds(60);
        engine.Publish(new(true, false, deadline, null));
        clock.Advance(TimeSpan.FromSeconds(20));
        engine.Publish(new(true, true, null, null));
        Assert.Equal("--:--:--", model.PulseCountdownDisplay);
        Assert.False(model.HasPulseCountdown);
        Assert.Equal(0, model.PulseProgress);
        clock.Advance(TimeSpan.FromSeconds(10));
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal("00:00:30", model.PulseCountdownDisplay);
        Assert.True(model.HasPulseCountdown);
        Assert.Equal(0.5, model.PulseProgress);
    }

    /// <summary>Checks a new countdown after an unavailable session outlasts the previous deadline.</summary>
    [Fact]
    public async Task ResumingAfterTheEngineChoosesANewDeadlineUsesTheNewCycle()
    {
        var (model, engine, clock) = Create();
        await using var lifetime = model;
        engine.Publish(new(true, false, clock.GetUtcNow().AddSeconds(60), null));
        engine.Publish(new(true, true, null, null));
        clock.Advance(TimeSpan.FromSeconds(90));
        var deadline = clock.GetUtcNow().AddSeconds(48);
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal("00:00:48", model.PulseCountdownDisplay);
        Assert.Equal(1, model.PulseProgress);
    }

    /// <summary>Checks that stop and error notifications reset presentation and the retained cycle.</summary>
    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "Input failed")]
    [InlineData(true, "Input failed")]
    public async Task StoppingOrFailingClearsTheCountdownAndItsCycle(bool running, string? error)
    {
        var (model, engine, clock) = Create();
        await using var lifetime = model;
        var deadline = clock.GetUtcNow().AddSeconds(75);
        engine.Publish(new(true, false, deadline, null));
        clock.Advance(TimeSpan.FromSeconds(25));
        engine.Publish(new(running, false, deadline, error));
        Assert.Equal("00:01:00", model.PulseCountdownDisplay);
        Assert.False(model.HasPulseCountdown);
        Assert.Equal(0, model.PulseProgress);
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal(1, model.PulseProgress);
    }

    /// <summary>Checks neutral missing-deadline states and clamped overdue values.</summary>
    [Fact]
    public async Task OverdueOrMissingDeadlinesCannotProduceNegativeOrInvalidProgress()
    {
        var (model, engine, clock) = Create();
        await using var lifetime = model;
        var deadline = clock.GetUtcNow().AddSeconds(20);
        engine.Publish(new(true, false, deadline, null));
        clock.Advance(TimeSpan.FromSeconds(25));
        engine.Publish(new(true, false, deadline, null));
        Assert.Equal("00:00:00", model.PulseCountdownDisplay);
        Assert.Equal(0, model.PulseProgress);
        engine.Publish(new(true, false, clock.GetUtcNow().AddSeconds(-2), null));
        Assert.Equal(0, model.PulseProgress);
        engine.Publish(new(true, false, null, null));
        Assert.False(model.HasPulseCountdown);
        Assert.Equal(0, model.PulseProgress);
        Assert.Equal("00:01:00", model.PulseCountdownDisplay);
    }

    private static (DashboardViewModel Model, Engine Engine, FakeTimeProvider Clock) Create()
    {
        var engine = new Engine();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        return (new DashboardViewModel(engine, new IdleMonitor(), new RuntimeSettingsStore(), new WeakReferenceMessenger(), clock), engine, clock);
    }

    private sealed class Engine : IKeepAliveEngine
    {
        public event EventHandler<KeepAliveStatus>? StatusChanged;
        internal void Publish(KeepAliveStatus status) => StatusChanged?.Invoke(this, status);
        public Task RunAsync(KeepAliveOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
        public void SetKeepAwake(bool enabled) { }
    }

    private sealed class IdleMonitor : IIdleMonitorService
    {
        public int IdleSeconds => 0;
        public event EventHandler? IdleUpdated { add { } remove { } }
        public void Start() { }
        public void Stop() { }
    }
}
