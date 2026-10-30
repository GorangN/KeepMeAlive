using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.ViewModels;

namespace KeepMeAlive.Tests.Runtime;

public sealed class DashboardInputTests
{
    [Theory]
    [InlineData("interval", "abc")]
    [InlineData("interval", "")]
    [InlineData("interval", "999999999999999999")]
    [InlineData("interval", "19")]
    [InlineData("interval", "86401")]
    [InlineData("key", "abc")]
    [InlineData("key", "")]
    [InlineData("key", "999999999999999999")]
    [InlineData("key", "123")]
    [InlineData("key", "136")]
    [InlineData("mouse", "abc")]
    [InlineData("mouse", "")]
    [InlineData("mouse", "999999999999999999")]
    [InlineData("mouse", "0")]
    [InlineData("mouse", "6")]
    public async Task InvalidDraftCannotSaveOrStartUsingThePreviousNumber(string field, string text)
    {
        var engine = new Engine();
        var settings = new RuntimeSettingsStore();
        await using var viewModel = Create(engine, settings);
        SetDraft(viewModel, field, text);

        Assert.Equal(text, GetDraft(viewModel, field));
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.NotEmpty(viewModel.InputError);
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.ApplySettings());
        await viewModel.StartCommand.ExecuteAsync(null);
        Assert.Equal(0, engine.Starts);
        Assert.Equal(60, settings.Current.IntervalSeconds);
        Assert.Equal(126, settings.Current.VirtualKeyCode);
        Assert.Equal(2, settings.Current.MousePixelDelta);
        Assert.Equal(text, GetDraft(viewModel, field));
    }

    [Theory]
    [InlineData("interval", "90")]
    [InlineData("key", "130")]
    [InlineData("mouse", "4")]
    public async Task CorrectingInvalidTextEnablesStartAndPersistsTheNewValue(string field, string correction)
    {
        var engine = new Engine();
        var settings = new RuntimeSettingsStore();
        await using var viewModel = Create(engine, settings);
        SetDraft(viewModel, field, "abc");
        int notifications = 0;
        viewModel.StartCommand.CanExecuteChanged += (_, _) => notifications++;
        SetDraft(viewModel, field, correction);

        Assert.Empty(viewModel.InputError);
        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.True(notifications > 0);
        await viewModel.StartCommand.ExecuteAsync(null);
        Assert.Equal(1, engine.Starts);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.Equal(field == "interval" ? 90 : 60, settings.Current.IntervalSeconds);
        Assert.Equal(field == "key" ? 130 : 126, settings.Current.VirtualKeyCode);
        Assert.Equal(field == "mouse" ? 4 : 2, settings.Current.MousePixelDelta);
    }

    [Fact]
    public async Task DiscardRestoresEveryEditorEvenWhenItsNumericValueNeverChanged()
    {
        var settings = new RuntimeSettingsStore();
        settings.Save(new AppSettings { IntervalSeconds = 90, VirtualKeyCode = 130, MousePixelDelta = 4 });
        await using var viewModel = Create(new Engine(), settings);
        viewModel.IntervalSecondsText = "abc";
        viewModel.VirtualKeyCodeText = string.Empty;
        viewModel.MousePixelDeltaText = "999999999999999999";

        viewModel.DiscardChanges();

        Assert.Equal("90", viewModel.IntervalSecondsText);
        Assert.Equal("130", viewModel.VirtualKeyCodeText);
        Assert.Equal("4", viewModel.MousePixelDeltaText);
        Assert.Empty(viewModel.InputError);
        Assert.False(viewModel.HasUnsavedChanges);
        Assert.True(viewModel.StartCommand.CanExecute(null));
    }

    [Fact]
    public async Task FailedSaveRetainsTheCorrectedDraftAndDoesNotStart()
    {
        var engine = new Engine();
        var settings = new RuntimeSettingsStore { FailSave = true };
        await using var viewModel = Create(engine, settings);
        viewModel.IntervalSecondsText = "90";
        await viewModel.StartCommand.ExecuteAsync(null);
        Assert.Equal(0, engine.Starts);
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Equal("90", viewModel.IntervalSecondsText);
        Assert.NotEmpty(viewModel.ErrorMessage);
        Assert.Equal(60, settings.Current.IntervalSeconds);
    }

    [Fact]
    public async Task NumericPropertiesRemainCompatibleWithTheEditors()
    {
        await using var viewModel = Create(new Engine(), new RuntimeSettingsStore());
        viewModel.IntervalSeconds = 120;
        viewModel.VirtualKeyCode = 135;
        viewModel.MousePixelDelta = 5;
        Assert.Equal("120", viewModel.IntervalSecondsText);
        Assert.Equal("135", viewModel.VirtualKeyCodeText);
        Assert.Equal("5", viewModel.MousePixelDeltaText);
        Assert.Empty(viewModel.InputError);
        viewModel.IntervalSeconds = 0;
        Assert.Equal("0", viewModel.IntervalSecondsText);
        Assert.False(viewModel.StartCommand.CanExecute(null));
    }

    private static DashboardViewModel Create(Engine engine, RuntimeSettingsStore settings) =>
        new(engine, new IdleMonitor(), settings, new WeakReferenceMessenger(), TimeProvider.System);

    private static void SetDraft(DashboardViewModel viewModel, string field, string text)
    {
        switch (field)
        {
            case "interval": viewModel.IntervalSecondsText = text; break;
            case "key": viewModel.VirtualKeyCodeText = text; break;
            case "mouse": viewModel.MousePixelDeltaText = text; break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    private static string GetDraft(DashboardViewModel viewModel, string field) => field switch
    {
        "interval" => viewModel.IntervalSecondsText,
        "key" => viewModel.VirtualKeyCodeText,
        "mouse" => viewModel.MousePixelDeltaText,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private sealed class Engine : IKeepAliveEngine
    {
        internal int Starts { get; private set; }
        public event EventHandler<KeepAliveStatus>? StatusChanged;
        public Task RunAsync(KeepAliveOptions options, CancellationToken cancellationToken)
        {
            Starts++;
            StatusChanged?.Invoke(this, new KeepAliveStatus(false, false, null, null));
            return Task.CompletedTask;
        }

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
