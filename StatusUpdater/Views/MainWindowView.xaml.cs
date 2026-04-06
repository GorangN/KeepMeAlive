using Microsoft.Extensions.DependencyInjection;
using KeepMeAlive.ViewModels;
using System.Windows;

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
}
