using Microsoft.Extensions.DependencyInjection;
using StatusUpdater.ViewModels;
using System.Windows;

namespace StatusUpdater.Views;

/// <summary>
/// Main application window — shell that hosts the dashboard and settings overlay.
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

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }
}
