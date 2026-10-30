using CommunityToolkit.Mvvm.Messaging;
using Hardcodet.Wpf.TaskbarNotification;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.ViewModels;
using KeepMeAlive.Views;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.IO.Pipes;
using System.Net.Http;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;

namespace KeepMeAlive;

/// <summary>Owns the composition root, single-instance IPC, startup work, and application lifetime.</summary>
internal sealed class ApplicationHost : IDisposable
{
    private readonly App _app;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly string _identity = $"KeepMeAlive-{WindowsIdentity.GetCurrent().User?.Value}-{System.Diagnostics.Process.GetCurrentProcess().SessionId}";
    private ServiceProvider? _services;
    private Mutex? _mutex;
    private bool _ownsMutex;
    private TaskbarIcon? _tray;
    private MainWindowView? _window;
    private ShellService? _shell;
    private bool _pendingShow;
    private Task? _pipeTask;
    private Task? _startupTask;

    public ApplicationHost(App app)
    {
        _app = app;
        _app.Startup += OnStartup;
        _app.DispatcherUnhandledException += OnUnhandledException;
        _app.SessionEnding += (_, _) => _app.Shutdown();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(_app.Dispatcher));
            _mutex = new Mutex(true, $"Local\\{_identity}", out _ownsMutex);
            if (!_ownsMutex)
            {
                using var client = new NamedPipeClientStream(".", _identity, PipeDirection.Out, PipeOptions.Asynchronous);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                try
                {
                    await client.ConnectAsync(timeout.Token);
                    using var writer = new StreamWriter(client);
                    await writer.WriteLineAsync("SHOW");
                    await writer.FlushAsync(timeout.Token);
                }
                catch (Exception exception) when (exception is IOException or OperationCanceledException)
                {
                    LocalDiagnostics.Write("activate", exception);
                }
                _app.Shutdown();
                return;
            }

            _pipeTask = Task.Run(() => ListenAsync(_lifetime.Token));
            var services = new ServiceCollection();
            ConfigureServices(services);
            _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            var settings = _services.GetRequiredService<ISettingsService>();
            settings.Load();
            _services.GetRequiredService<IThemeService>().Apply(settings.Current.Theme);
            _services.GetRequiredService<IScheduledActionService>().Restore();

            var main = _services.GetRequiredService<MainViewModel>();
            _window = new MainWindowView { DataContext = main };
            _app.MainWindow = _window;
            var resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/KeepMeAlive;component/TrayIcon/TrayIconView.xaml") };
            _tray = (TaskbarIcon)resources["TrayIcon"];
            _tray.DataContext = main;
            _shell = _services.GetRequiredService<ShellService>();
            _shell.Attach(_window, _tray);
            _window.Closing += (_, args) => { args.Cancel = true; _shell.Hide(); };

            if (_pendingShow || (!e.Args.Contains("--silent") && !settings.Current.StartMinimizedToTray)) { _shell.Show(); }
            _startupTask = Task.Run(() => SynchronizeAsync(settings.Current.EnableStartupAccountSync, settings.Current.EnableAutomaticUpdateChecks, _lifetime.Token));
        }
        catch (Exception exception)
        {
            LocalDiagnostics.Write("startup", exception);
            MessageBox.Show("KeepMeAlive could not start safely. Check the local diagnostic log and your storage permissions.", "KeepMeAlive", MessageBoxButton.OK, MessageBoxImage.None);
            _app.Shutdown(1);
        }
    }

    internal static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IAppRuntimeModeService, AppRuntimeModeService>();
        services.AddSingleton<ISecretStore, SecretStore>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) });
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IKeepAwakeService, KeepAwakeService>();
        services.AddSingleton<IIdleMonitorService, IdleMonitorService>();
        services.AddSingleton<IInputService, WindowsInputService>();
        services.AddSingleton<ISessionMonitor, WindowsSessionMonitor>();
        services.AddSingleton<IKeepAliveEngine, KeepAliveEngine>();
        services.AddSingleton<ISystemActionExecutor, WindowsSystemActionExecutor>();
        services.AddSingleton<IScheduledActionService, ScheduledActionService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ShellService>();
        services.AddSingleton<IShellService>(provider => provider.GetRequiredService<ShellService>());
        services.AddSingleton(NeonConfiguration.Load());
        services.AddSingleton<IBrowserLauncher, SystemBrowserLauncher>();
        services.AddSingleton<ILoopbackCallbackListener, LoopbackCallbackListener>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<ILicenseService, LicenseService>();
        services.AddSingleton<IStartupSyncService, StartupSyncService>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AccountViewModel>();
        services.AddSingleton<UpdateViewModel>();
        services.AddSingleton<MainViewModel>();
    }

    private async Task SynchronizeAsync(bool accounts, bool updates, CancellationToken cancellationToken)
    {
        try
        {
            if (accounts)
            {
                await _services!.GetRequiredService<IStartupSyncService>().SyncAsync(cancellationToken);
                await _app.Dispatcher.InvokeAsync(() => _services!.GetRequiredService<AccountViewModel>().RefreshFromCache(), DispatcherPriority.Normal, cancellationToken);
            }
            if (updates)
            {
                var checker = _services!.GetRequiredService<UpdateViewModel>();
                using var registration = cancellationToken.Register(checker.CheckCommand.Cancel);
                await checker.CheckCommand.ExecuteAsync(null);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or IOException
            or System.Security.Cryptography.CryptographicException or System.ComponentModel.Win32Exception
            or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            LocalDiagnostics.Write("startup-sync", exception);
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(_identity, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken);
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                readTimeout.CancelAfter(TimeSpan.FromSeconds(2));
                using var reader = new StreamReader(server);
                var command = new char[6];
                var length = await reader.ReadBlockAsync(command, readTimeout.Token);
                if (new string(command, 0, length).TrimEnd('\r', '\n') == "SHOW")
                {
                    await _app.Dispatcher.InvokeAsync(() =>
                    {
                        if (_shell is null) { _pendingShow = true; }
                        else { _shell.Show(); }
                    }, DispatcherPriority.Normal, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception exception) when (exception is IOException or OperationCanceledException)
            {
                LocalDiagnostics.Write("ipc", exception);
                try { await Task.Delay(250, cancellationToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LocalDiagnostics.Write("unhandled", e.Exception);
        e.Handled = true;
        _app.Shutdown(1);
    }

    public void Dispose()
    {
        _lifetime.Cancel();
        try
        {
            foreach (var task in new[] { _pipeTask, _startupTask })
            {
                try { task?.GetAwaiter().GetResult(); }
                catch (OperationCanceledException) { }
                catch (Exception exception) { LocalDiagnostics.Write("background-shutdown", exception); }
            }
        }
        finally
        {
            try
            {
                _services?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception exception) { LocalDiagnostics.Write("service-shutdown", exception); }
            finally
            {
                _tray?.Dispose();
                if (_ownsMutex) { _mutex?.ReleaseMutex(); }
                _mutex?.Dispose();
                _lifetime.Dispose();
            }
        }
    }
}
