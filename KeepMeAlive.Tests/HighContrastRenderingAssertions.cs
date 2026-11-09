using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KeepMeAlive.Tests;

internal static class HighContrastRenderingAssertions
{
    internal static void ControlsUseSystemBrushes(App app)
    {
        var combo = new ComboBox { Style = (Style)app.FindResource("ComboBoxStyle"), ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0, Width = 200 };
        Layout(combo, new Size(200, 40));
        var arrow = Assert.Single(Descendants(combo).OfType<Path>(), path => path.Name == "Arrow");
        AssertBrush(SystemColors.WindowTextBrush, arrow.Fill);
        AssertBrush(SystemColors.WindowBrush, combo.Background);
        Assert.NotEqual(Assert.IsType<SolidColorBrush>(combo.Background).Color, Assert.IsType<SolidColorBrush>(arrow.Fill).Color);
        var popup = Assert.IsType<Popup>(combo.Template.FindName("PART_Popup", combo));
        var popupBorder = Assert.IsType<Border>(combo.Template.FindName("PopupBorder", combo));
        AssertBrush(SystemColors.WindowBrush, popupBorder.Background);
        AssertBrush(SystemColors.WindowTextBrush, popupBorder.BorderBrush);
        Assert.NotNull(popup.Child);

        var option = new ComboBoxItem { Style = (Style)app.FindResource("Clock.ComboBoxItem"), Content = "Unselected option" };
        Layout(option, new Size(200, 40));
        var label = Assert.IsType<ContentPresenter>(option.Template.FindName("ContentPresenter", option));
        AssertBrush(SystemColors.WindowTextBrush, TextElement.GetForeground(label));
        AssertBrush(SystemColors.WindowTextBrush, Assert.Single(Descendants(label).OfType<TextBlock>()).Foreground);
        option.IsSelected = true;
        Layout(option, new Size(200, 40));
        AssertBrush(SystemColors.HighlightTextBrush, TextElement.GetForeground(label));
        AssertBrush(SystemColors.HighlightTextBrush, Assert.Single(Descendants(label).OfType<TextBlock>()).Foreground);
        var selection = Assert.IsType<Border>(option.Template.FindName("Border", option));
        AssertBrush(SystemColors.HighlightBrush, selection.Background);

        var calendar = new Calendar { Style = (Style)app.FindResource("Clock.Calendar"), DisplayDate = new DateTime(2030, 5, 15) };
        Layout(calendar, new Size(280, 240));
        AssertBrush(SystemColors.WindowBrush, calendar.Background);
        AssertBrush(SystemColors.WindowTextBrush, calendar.Foreground);
        AssertBrush(SystemColors.HighlightTextBrush, (Brush)app.FindResource("SelectedForegroundBrush"));
        AssertBrush(SystemColors.HighlightBrush, (Brush)app.FindResource("SelectedBackgroundBrush"));
        var ordinaryDay = Descendants(calendar).OfType<CalendarDayButton>().First(day => !day.IsInactive && !day.IsToday && !day.IsSelected);
        AssertBrush(SystemColors.WindowTextBrush, ordinaryDay.Foreground);
        calendar.SelectedDate = calendar.DisplayDate;
        Layout(calendar, new Size(280, 240));
        var selectedDay = Assert.Single(Descendants(calendar).OfType<CalendarDayButton>(), day => day.IsSelected);
        var dayLabel = Assert.IsType<ContentPresenter>(selectedDay.Template.FindName("NormalText", selectedDay));
        AssertBrush(SystemColors.HighlightTextBrush, TextElement.GetForeground(dayLabel));
        AssertBrush(SystemColors.HighlightTextBrush, Assert.Single(Descendants(dayLabel).OfType<TextBlock>()).Foreground);
        var calendarItem = Assert.Single(Descendants(calendar).OfType<CalendarItem>());
        var header = Assert.IsType<Button>(calendarItem.Template.FindName("PART_HeaderButton", calendarItem));
        AssertBrush(SystemColors.WindowTextBrush, Assert.Single(Descendants(header).OfType<TextBlock>()).Foreground);
        var daySelection = Assert.IsType<Border>(selectedDay.Template.FindName("Surface", selectedDay));
        AssertBrush(SystemColors.HighlightBrush, daySelection.Background);
    }

    internal static void NormalControlBrushesReturn(App app)
    {
        var combo = new ComboBox { Style = (Style)app.FindResource("ComboBoxStyle"), ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0, Width = 200 };
        Layout(combo, new Size(200, 40));
        var arrow = Assert.Single(Descendants(combo).OfType<Path>(), path => path.Name == "Arrow");
        AssertBrush(Assert.IsType<SolidColorBrush>(app.FindResource("OnSurfaceBrush")), arrow.Fill);
        var popupBorder = Assert.IsType<Border>(combo.Template.FindName("PopupBorder", combo));
        AssertBrush(Assert.IsType<SolidColorBrush>(app.FindResource("SurfaceContainerLowestBrush")), popupBorder.Background);
    }

    private static void AssertBrush(SolidColorBrush expected, Brush actual) => Assert.Equal(expected.Color, Assert.IsType<SolidColorBrush>(actual).Color);

    private static void Layout(FrameworkElement element, Size size)
    {
        element.ApplyTemplate();
        element.Measure(size);
        element.Arrange(new Rect(size));
        element.UpdateLayout();
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
