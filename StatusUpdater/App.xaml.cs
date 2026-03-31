using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using StatusUpdater.Services;
using StatusUpdater.Services.Interfaces;
using StatusUpdater.ViewModels;
using StatusUpdater.Views;
using System.IO.Pipes;
using System.Threading;
using System.Windows;

namespace StatusUpdater;

public partial class App : Application
{
    private const string MutexName = "StatusUpdater_SingleInstance_v1";
    private const string PipeName = "StatusUpdater_Pipe";

    public static IServiceProvider Services { get; private set; } = null!;

    private Mutex? _mutex;
    private bool _ownsMutex;
    private bool _isExiting;
    private TaskbarIcon? _trayIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance enforcement
        _mutex = new Mutex(true, MutexName, out _ownsMutex);
        if (!_ownsMutex)
        {
            await SignalExistingInstanceAsync();
            Current.Shutdown();
            return;
        }

        StartPipeServer();

        // Build DI container
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        // Apply saved theme before any window appears
        var settingsService = Services.GetRequiredService<ISettingsService>();
        settingsService.Load();
        Services.GetRequiredService<IThemeService>().Apply(settingsService.Current.Theme);

        // Create and wire tray icon
        var trayResources = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/TrayIcon/TrayIconView.xaml")
        };
        _trayIcon = (TaskbarIcon)trayResources["TrayIcon"];
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        _trayIcon.DataContext = mainViewModel;

        // Create MainWindowView (hidden)
        var mainWindow = Services.GetRequiredService<MainWindowView>();
        mainViewModel.Initialize(mainWindow, _trayIcon);

        // Wire window events
        mainWindow.Deactivated += (_, _) => mainViewModel.HideWindowCommand.Execute(null);
        mainWindow.Closing += (s, ev) =>
        {
            if (!_isExiting)
            {
                ev.Cancel = true;
                mainViewModel.HideWindowCommand.Execute(null);
            }
        };

        SessionEnding += (_, _) => { _isExiting = true; };

        // Show window unless --silent argument
        bool silent = e.Args.Contains("--silent");
        if (!silent)
            mainViewModel.ShowWindowCommand.Execute(null);

        // Check for updates in background
        _ = Services.GetRequiredService<UpdateViewModel>().CheckCommand.ExecuteAsync(null);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<System.Net.Http.HttpClient>();
        services.AddSingleton(WeakReferenceMessenger.Default);
        services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(
            _ => WeakReferenceMessenger.Default);

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IKeepAwakeService, KeepAwakeService>();
        services.AddSingleton<IIdleMonitorService, IdleMonitorService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<UpdateViewModel>();
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddTransient<MainWindowView>();
    }

    private static async Task SignalExistingInstanceAsync()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(500);
            using var writer = new System.IO.StreamWriter(client);
            await writer.WriteLineAsync("SHOW");
        }
        catch { /* existing instance not yet listening, ignore */ }
    }

    private void StartPipeServer()
    {
        _ = Task.Run(async () =>
        {
            while (!_isExiting)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();
                    using var reader = new System.IO.StreamReader(server);
                    var msg = await reader.ReadLineAsync();
                    if (msg == "SHOW")
                    {
                        Dispatcher.Invoke(() =>
                            Services.GetRequiredService<MainViewModel>().ShowWindowCommand.Execute(null));
                    }
                }
                catch { /* server loop restart on error */ }
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _trayIcon?.Dispose();
        if (_ownsMutex) _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
