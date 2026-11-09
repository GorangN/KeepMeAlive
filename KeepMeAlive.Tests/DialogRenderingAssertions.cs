using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KeepMeAlive.Presentation;
using KeepMeAlive.Views;

namespace KeepMeAlive.Tests;

internal static class DialogRenderingAssertions
{
    internal static void VerifyAndRender(string theme, string output)
    {
        foreach (bool saveDialog in new[] { true, false })
        {
            bool? choice = false;
            var model = new ConfirmationDialogViewModel("Unsaved changes", "Save your changes before leaving this page?", saveDialog, result => choice = result);
            var dialog = new ConfirmationDialogView { DataContext = model };
            var root = Assert.IsAssignableFrom<FrameworkElement>(dialog.Content);
            root.Measure(new Size(460, double.PositiveInfinity));
            root.Arrange(new Rect(new Point(), root.DesiredSize));
            root.UpdateLayout();
            var buttons = Descendants(root).OfType<Button>().Where(button => button.Visibility == Visibility.Visible).ToArray();
            Assert.Equal(saveDialog ? 4 : 3, buttons.Length);
            Assert.All(buttons, button => Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button))));
            Assert.Same(model.CancelCommand, Assert.Single(buttons, button => button.IsDefault).Command);
            model.AcceptCommand.Execute(null);
            Assert.True(choice);
            model.DiscardCommand.Execute(null);
            Assert.False(choice);
            model.CancelCommand.Execute(null);
            Assert.Null(choice);
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Path.Combine(output, $"Dialog-{(saveDialog ? "Save" : "Confirm")}-{theme}.png")))
            {
                encoder.Save(file);
            }
            dialog.Close();
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child)) { yield return descendant; }
        }
    }
}
