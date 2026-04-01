using Microsoft.Extensions.DependencyInjection;
using KeepMeAlive.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace KeepMeAlive.Views;

/// <summary>
/// Main application window — hosts all settings, controls, and status in one consolidated view.
/// </summary>
public partial class MainWindowView : Window
{
    /// <summary>
    /// Initializes the window and binds the <see cref="MainViewModel"/> from the DI container.
    /// </summary>
    public MainWindowView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}
