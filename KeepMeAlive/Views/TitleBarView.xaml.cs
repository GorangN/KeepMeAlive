using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KeepMeAlive.Views;

/// <summary>
/// Title bar UserControl providing drag-to-move, minimize, and hide-to-tray.
/// Code-behind is limited to pure window-state UI behaviors (DragMove, WindowState)
/// that have no MVVM equivalent — same exception as the existing MainWindowView.xaml.cs.
/// </summary>
public partial class TitleBarView : UserControl
{
    /// <summary>Initializes the title bar control.</summary>
    public TitleBarView()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            Window.GetWindow(this)?.DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }
}
