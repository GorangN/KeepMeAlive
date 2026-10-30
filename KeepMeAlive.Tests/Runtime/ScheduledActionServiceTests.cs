using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using System.Globalization;

namespace KeepMeAlive.Tests.Runtime;

public sealed class ScheduledActionServiceTests
{
    [Fact]
    public void ExecutesOnceAndPersistsDisarmedBeforeNativeAction()
    {
        var clock = new FakeTimeProvider();
        var settings = new RuntimeSettingsStore();
        settings.Save(new AppSettings { ScheduledActionEnabled = true });
        var executor = new Executor { BeforeExecute = () => Assert.False(settings.Current.ScheduledActionEnabled) };
        using var scheduler = new ScheduledActionService(executor, settings, clock);
        scheduler.Schedule(ScheduledAction.Restart, clock.GetUtcNow().AddSeconds(10).UtcDateTime);

        clock.Advance(TimeSpan.FromSeconds(10));
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(1, executor.ExecutionCount);
        Assert.False(scheduler.IsScheduled);
        Assert.Null(scheduler.ScheduledAt);
    }

    [Fact]
    public void CancellingSuppressesEvenAnAlreadyQueuedCallback()
    {
        var clock = new QueuedClock();
        var executor = new Executor();
        using var scheduler = new ScheduledActionService(executor, new RuntimeSettingsStore(), clock);
        scheduler.Schedule(ScheduledAction.Shutdown, clock.GetUtcNow().AddSeconds(10).UtcDateTime);
        Action queued = clock.CaptureCallback();
        scheduler.Cancel();
        clock.Jump(TimeSpan.FromSeconds(10));
        queued();

        Assert.Equal(0, executor.ExecutionCount);
        Assert.False(scheduler.IsScheduled);
    }

    [Fact]
    public void ReplacementSuppressesTheOldQueuedCallback()
    {
        var clock = new QueuedClock();
        var executor = new Executor();
        using var scheduler = new ScheduledActionService(executor, new RuntimeSettingsStore(), clock);
        scheduler.Schedule(ScheduledAction.Shutdown, clock.GetUtcNow().AddSeconds(10).UtcDateTime);
        Action old = clock.CaptureCallback();
        scheduler.Schedule(ScheduledAction.Sleep, clock.GetUtcNow().AddMinutes(5).UtcDateTime);
        clock.Jump(TimeSpan.FromSeconds(10));
        old();

        Assert.Equal(0, executor.ExecutionCount);
        Assert.True(scheduler.IsScheduled);
    }

    [Fact]
    public void MissedScheduleIsSkippedAfterLongSuspend()
    {
        var clock = new QueuedClock();
        var executor = new Executor();
        using var scheduler = new ScheduledActionService(executor, new RuntimeSettingsStore(), clock);
        scheduler.Schedule(ScheduledAction.Sleep, clock.GetUtcNow().AddSeconds(10).UtcDateTime);
        Action queued = clock.CaptureCallback();
        clock.Jump(TimeSpan.FromHours(1));
        queued();

        Assert.Equal(0, executor.ExecutionCount);
        Assert.False(scheduler.IsScheduled);
        Assert.Contains("missed", scheduler.LastError);
    }

    [Fact]
    public void PersistenceFailurePreventsTheSystemAction()
    {
        var clock = new FakeTimeProvider();
        var settings = new RuntimeSettingsStore { FailSave = true };
        var executor = new Executor();
        using var scheduler = new ScheduledActionService(executor, settings, clock);
        scheduler.Schedule(ScheduledAction.Shutdown, clock.GetUtcNow().AddSeconds(10).UtcDateTime);

        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0, executor.ExecutionCount);
        Assert.False(scheduler.IsScheduled);
        Assert.NotNull(scheduler.LastError);
    }

    [Fact]
    public void NativeFailureIsReportedWithoutEscapingTheTimerCallback()
    {
        var clock = new FakeTimeProvider();
        var executor = new Executor { Fail = true };
        using var scheduler = new ScheduledActionService(executor, new RuntimeSettingsStore(), clock);
        scheduler.Schedule(ScheduledAction.Shutdown, clock.GetUtcNow().AddSeconds(10).UtcDateTime);

        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.False(scheduler.IsScheduled);
        Assert.NotNull(scheduler.LastError);
    }

    [Fact]
    public void RestoresOnlyFuturePersistedSchedules()
    {
        var clock = new FakeTimeProvider();
        var settings = new RuntimeSettingsStore();
        settings.Save(new AppSettings
        {
            ScheduledActionEnabled = true,
            ScheduledActionType = ScheduledAction.Sleep,
            ScheduledActionTime = clock.GetUtcNow().AddHours(1).LocalDateTime.ToString("dd:MM:yyyy:HH:mm:ss", CultureInfo.InvariantCulture)
        });
        using var scheduler = new ScheduledActionService(new Executor(), settings, clock);
        scheduler.Restore();
        Assert.True(scheduler.IsScheduled);

        scheduler.Cancel();
        clock.Advance(TimeSpan.FromHours(2));
        scheduler.Restore();
        Assert.False(scheduler.IsScheduled);
        Assert.False(settings.Current.ScheduledActionEnabled);
    }

    [Fact]
    public void DistantScheduleDoesNotOverflowNativeTimer()
    {
        var clock = new FakeTimeProvider();
        using var scheduler = new ScheduledActionService(new Executor(), new RuntimeSettingsStore(), clock);
        scheduler.Schedule(ScheduledAction.Sleep, clock.GetUtcNow().AddYears(1).UtcDateTime);
        Assert.True(scheduler.IsScheduled);
    }

    [Fact]
    public void RestoringDisabledSettingsCancelsAnAlreadyArmedAction()
    {
        var clock = new FakeTimeProvider();
        var executor = new Executor();
        using var scheduler = new ScheduledActionService(executor, new RuntimeSettingsStore(), clock);
        scheduler.Schedule(ScheduledAction.Shutdown, clock.GetUtcNow().AddSeconds(10).UtcDateTime);
        scheduler.Restore();
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.False(scheduler.IsScheduled);
        Assert.Equal(0, executor.ExecutionCount);
    }

    [Theory]
    [InlineData(3, 29)]
    [InlineData(10, 25)]
    public void RejectsInvalidAndAmbiguousDaylightSavingTimes(int month, int day)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 29);
        var end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 25);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31), TimeSpan.FromHours(1), start, end);
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test-zone", TimeSpan.FromHours(1),
            "Test-zone", "Standard", "Summer", [rule]);
        using var scheduler = new ScheduledActionService(new Executor(), new RuntimeSettingsStore(), clock, zone);

        Assert.Throws<ArgumentException>(() => scheduler.Schedule(ScheduledAction.Sleep,
            new DateTime(2026, month, day, 2, 30, 0, DateTimeKind.Unspecified)));
        Assert.False(scheduler.IsScheduled);
    }

    private sealed class Executor : ISystemActionExecutor
    {
        internal int ExecutionCount { get; private set; }
        internal Action? BeforeExecute { get; init; }
        internal bool Fail { get; init; }

        public void Execute(ScheduledAction action)
        {
            BeforeExecute?.Invoke();
            ExecutionCount++;
            if (Fail)
            {
                throw new InvalidOperationException("Blocked");
            }
        }
    }

    private sealed class QueuedClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        private TimerCallback? _callback;
        private object? _state;
        public override DateTimeOffset GetUtcNow() => _now;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _callback = callback;
            _state = state;
            return new QueuedTimer();
        }

        internal Action CaptureCallback()
        {
            TimerCallback callback = _callback!;
            object? state = _state;
            return () => callback(state);
        }

        internal void Jump(TimeSpan amount) => _now += amount;

        private sealed class QueuedTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
