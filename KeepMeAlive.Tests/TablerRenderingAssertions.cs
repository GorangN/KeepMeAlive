using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace KeepMeAlive.Tests;

internal static class TablerRenderingAssertions
{
    internal static void IconsUseParentBrush(DependencyObject root, App app)
    {
        var style = Assert.IsType<Style>(app.FindResource("TablerIconStyle"));
        var icons = Descendants(root).OfType<ContentControl>().Where(control =>
            ReferenceEquals(control.Style, style) && control.ActualWidth > 0 && control.ActualHeight > 0).ToArray();
        Assert.NotEmpty(icons);
        foreach (var icon in icons)
        {
            Assert.IsAssignableFrom<Geometry>(icon.Content);
            Assert.False(icon.Focusable);
            Assert.False(icon.IsHitTestVisible);
            var path = Assert.Single(Descendants(icon).OfType<System.Windows.Shapes.Path>());
            Assert.Same(icon.Foreground, path.Stroke);
            Assert.Same(icon.Content, path.Data);
        }
    }

    internal static void HasIcon(DependencyObject root, Geometry expected)
    {
        Assert.Contains(Descendants(root).OfType<ContentControl>(), control => ReferenceEquals(control.Content, expected));
    }

    internal static void ControlAffordancesUseTabler(App app)
    {
        var combo = new ComboBox { Style = (Style)app.FindResource("ComboBoxStyle"), ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0, Width = 200 };
        Layout(combo, new Size(200, 40));
        var arrow = Assert.Single(Descendants(combo).OfType<System.Windows.Shapes.Path>(), path => path.Name == "Arrow");
        Assert.Same(app.FindResource("ComboBoxDownArrowGeometry"), arrow.Data);
        Assert.True(arrow.Data.GetArea() > 0);

        var check = new CheckBox { Style = (Style)app.FindResource(typeof(CheckBox)), IsChecked = true, IsThreeState = true, Content = "Preference" };
        Layout(check, new Size(200, 40));
        HasIcon(check, Assert.IsAssignableFrom<Geometry>(app.FindResource("Tabler.Check")));
        check.IsChecked = null;
        Layout(check, new Size(200, 40));
        HasIcon(check, Assert.IsAssignableFrom<Geometry>(app.FindResource("Tabler.Minus")));

        var initialDate = new DateTime(2030, 5, 15);
        var calendar = new Calendar { Style = (Style)app.FindResource("Clock.Calendar"), DisplayDate = initialDate };
        Layout(calendar, new Size(280, 240));
        var item = Assert.Single(Descendants(calendar).OfType<CalendarItem>());
        var previous = Assert.IsType<Button>(item.Template.FindName("PART_PreviousButton", item));
        var next = Assert.IsType<Button>(item.Template.FindName("PART_NextButton", item));
        HasIcon(previous, Assert.IsAssignableFrom<Geometry>(app.FindResource("Tabler.ChevronLeft")));
        HasIcon(next, Assert.IsAssignableFrom<Geometry>(app.FindResource("Tabler.ChevronRight")));
        Assert.Equal("Previous month", UIElementAutomationPeer.CreatePeerForElement(previous)!.GetName());
        Assert.Equal("Next month", UIElementAutomationPeer.CreatePeerForElement(next)!.GetName());
        previous.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(initialDate.AddMonths(-1).Month, calendar.DisplayDate.Month);
        next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(initialDate.Month, calendar.DisplayDate.Month);
        var header = Assert.IsType<Button>(item.Template.FindName("PART_HeaderButton", item));
        header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout(calendar, new Size(280, 240));
        Assert.Equal(CalendarMode.Year, calendar.DisplayMode);
        var yearView = Assert.IsType<Grid>(item.Template.FindName("PART_YearView", item));
        var monthView = Assert.IsType<Grid>(item.Template.FindName("PART_MonthView", item));
        Assert.Equal(Visibility.Visible, yearView.Visibility);
        Assert.Equal(Visibility.Collapsed, monthView.Visibility);
        Assert.Equal(12, yearView.Children.OfType<CalendarButton>().Count());
        Assert.All(yearView.Children.OfType<CalendarButton>(), button => Assert.True(button.ActualWidth > 0));
        header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout(calendar, new Size(280, 240));
        Assert.Equal(CalendarMode.Decade, calendar.DisplayMode);
        Assert.Equal(Visibility.Visible, yearView.Visibility);
        Assert.False(header.IsEnabled);
        var year = Assert.Single(yearView.Children.OfType<CalendarButton>(), button =>
            button.DataContext is DateTime date && date.Year == 2032);
        year.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout(calendar, new Size(280, 240));
        Assert.Equal(CalendarMode.Year, calendar.DisplayMode);
        Assert.Equal(2032, calendar.DisplayDate.Year);
        Assert.Equal(Visibility.Visible, yearView.Visibility);
        var august = Assert.Single(yearView.Children.OfType<CalendarButton>(), button =>
            button.DataContext is DateTime date && date.Month == 8);
        august.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout(calendar, new Size(280, 240));
        Assert.Equal(CalendarMode.Month, calendar.DisplayMode);
        Assert.Equal(2032, calendar.DisplayDate.Year);
        Assert.Equal(8, calendar.DisplayDate.Month);
        Assert.Equal(Visibility.Visible, monthView.Visibility);
        Assert.NotEqual(Visibility.Visible, yearView.Visibility);

        foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
        {
            var scrollbar = new ScrollBar { Style = (Style)app.FindResource("Clock.ScrollBar"), Orientation = orientation, Maximum = 10, Value = 5, ViewportSize = 2 };
            Layout(scrollbar, orientation == Orientation.Vertical ? new Size(20, 200) : new Size(200, 20));
            var buttons = Descendants(scrollbar).OfType<RepeatButton>().Where(button =>
                button.Command == ScrollBar.PageUpCommand || button.Command == ScrollBar.PageDownCommand ||
                button.Command == ScrollBar.PageLeftCommand || button.Command == ScrollBar.PageRightCommand).ToArray();
            Assert.Equal(2, buttons.Length);
            foreach (var button in buttons)
            {
                Assert.False(button.Focusable);
                Assert.False(button.IsTabStop);
            }
            var thumb = Assert.Single(Descendants(scrollbar).OfType<Thumb>());
            Assert.True(thumb.ActualWidth > 0);
            Assert.True(thumb.ActualHeight > 0);
            Assert.Empty(Descendants(scrollbar).OfType<System.Windows.Shapes.Path>());
        }
    }

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
