using Hardcodet.Wpf.TaskbarNotification;
using KeepMeAlive.Services.Interfaces;
using System.Windows;
using System.Windows.Media.Imaging;
namespace KeepMeAlive.Services;

/// <summary>Implements native window and tray presentation operations.</summary>
public sealed class ShellService : IShellService
{
    private Window? _window;
    private TaskbarIcon? _tray;
    /// <summary>Attaches the composition root's native presentation objects.</summary>
    /// <param name="window">The main window.</param>
    /// <param name="tray">The tray icon.</param>
    public void Attach(Window window, TaskbarIcon tray)
    {
        _window = window;
        _tray = tray;
        WindowWorkArea.Attach(window);
    }
    /// <inheritdoc/>
    public void Show() => Dispatch(() => { _window?.Show(); if (_window != null) { _window.WindowState = WindowState.Normal; _window.Activate(); } });
    /// <inheritdoc/>
    public void Hide() => Dispatch(() => _window?.Hide());
    /// <inheritdoc/>
    public void Minimize() => Dispatch(() => { if (_window != null) { SystemCommands.MinimizeWindow(_window); } });
    /// <inheritdoc/>
    public void ToggleMaximize() => Dispatch(() => { if (_window != null) { if (_window.WindowState == WindowState.Maximized) { SystemCommands.RestoreWindow(_window); } else { SystemCommands.MaximizeWindow(_window); } } });
    /// <inheritdoc/>
    public void Exit() => Dispatch(() => System.Windows.Application.Current.Shutdown());
    /// <inheritdoc/>
    public void SetRunning(bool running) => Dispatch(() =>
    {
        if (_tray == null) { return; }
        _tray.ToolTipText = running ? "KeepMeAlive - Running" : "KeepMeAlive - Stopped";
        _tray.IconSource = new BitmapImage(new Uri($"pack://application:,,,/KeepMeAlive;component/Resources/Icons/tray_{(running ? "active" : "inactive")}.ico"));
    });
    /// <inheritdoc/>
    public void Notify(string title, string body) => Dispatch(() => _tray?.ShowBalloonTip(title, body, BalloonIcon.None));
    private static void Dispatch(Action action) => System.Windows.Application.Current.Dispatcher.Invoke(action);
}
