using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.ViewModels;
using KeepMeAlive.Views;
using System.IO.Pipes;
using System.Threading;
using System.Windows;

namespace KeepMeAlive;

public partial class App : Application
{
    private const string MutexName = "KeepMeAlive_SingleInstance_v1";
    private const string PipeName = "KeepMeAlive_Pipe";

    public static IServiceProvider Services { get; private set; } = null!;

    private Mutex? _mutex;
    private bool _ownsMutex;
    private bool _isExiting;
    private TaskbarIcon? _trayIcon;
    private MainViewModel? _mainViewModel;
    private volatile bool _isMainViewModelReady;
    private int _pendingShowRequest;

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
        _mainViewModel = mainViewModel;
        _isMainViewModelReady = true;
        FlushPendingShowRequest();

        // Wire window events — close button minimizes to tray (no Deactivated auto-hide)
        mainWindow.Closing += (s, ev) =>
        {
            if (!_isExiting)
            {
                ev.Cancel = true;
                mainViewModel.HideWindowCommand.Execute(null);
            }
        };

        SessionEnding += (_, _) => { _isExiting = true; };

        // Show window unless --silent argument or user chose to start in tray
        bool silentArg = e.Args.Contains("--silent");
        bool silentSetting = settingsService.Current.StartMinimizedToTray;
        bool silent = silentArg || silentSetting;

        if (!silent)
        {
            mainViewModel.ShowWindowCommand.Execute(null);
        }
        else if (silentSetting && settingsService.Current.ShowNotifications)
        {
            // Inform non-tech users where the app went.
            // DispatcherPriority.Background lets the tray icon's Win32 handle
            // finish registering before ShowBalloonTip is called.
            _ = Dispatcher.InvokeAsync(
                () => _trayIcon?.ShowBalloonTip(
                    "KeepMeAlive",
                    "KeepMeAlive is running in the system tray \u2014 click here to open.",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        if (settingsService.Current.EnableStartupAccountSync)
        {
            _ = Services.GetRequiredService<IStartupSyncService>().SyncAsync();
        }

        if (settingsService.Current.EnableAutomaticUpdateChecks)
        {
            _ = Services.GetRequiredService<UpdateViewModel>().CheckCommand.ExecuteAsync(null);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IAppRuntimeModeService, AppRuntimeModeService>();
        services.AddSingleton<System.Net.Http.HttpClient>();
        services.AddSingleton(WeakReferenceMessenger.Default);
        services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(
            _ => WeakReferenceMessenger.Default);

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISecretStore, SecretStore>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IStartupSyncService, StartupSyncService>();
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IKeepAwakeService, KeepAwakeService>();
        services.AddSingleton<IIdleMonitorService, IdleMonitorService>();
        services.AddSingleton<IScheduledActionService, ScheduledActionService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AccountViewModel>();
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
                        RequestShowWindow();
                    }
                }
                catch { /* server loop restart on error */ }
            }
        });
    }

    private void RequestShowWindow()
    {
        if (!_isMainViewModelReady || _mainViewModel is null)
        {
            Interlocked.Exchange(ref _pendingShowRequest, 1);
            return;
        }

        _ = Dispatcher.InvokeAsync(() => _mainViewModel.ShowWindowCommand.Execute(null));
    }

    private void FlushPendingShowRequest()
    {
        if (!_isMainViewModelReady
            || _mainViewModel is null
            || Interlocked.Exchange(ref _pendingShowRequest, 0) == 0)
        {
            return;
        }

        _ = Dispatcher.InvokeAsync(() => _mainViewModel.ShowWindowCommand.Execute(null));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _trayIcon?.Dispose();
        if (_ownsMutex) { _mutex?.ReleaseMutex(); }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
