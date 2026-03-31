using Microsoft.Extensions.DependencyInjection;
using StatusUpdater.ViewModels;
using System.Windows;

namespace StatusUpdater;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }
}
