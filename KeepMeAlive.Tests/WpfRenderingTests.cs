using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Services.Interfaces;
using KeepMeAlive.ViewModels;
using KeepMeAlive.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KeepMeAlive.Tests;

public sealed class WpfRenderingTests
{
    [Fact]
    public async Task ScreensLoadAndRenderWithRealCompositionAndNoBindingErrors()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                var services = new ServiceCollection();
                ApplicationHost.ConfigureServices(services);
                var temporary = Path.Combine(Path.GetTempPath(), "KeepMeAlive-render-" + Guid.NewGuid().ToString("N"));
                services.RemoveAll<IAppRuntimeModeService>();
                services.AddSingleton<IAppRuntimeModeService>(new AppRuntimeModeService(temporary, temporary));
                var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
                provider.GetRequiredService<ISettingsService>().Load();
                var main = provider.GetRequiredService<MainViewModel>();
                var window = new MainWindowView { DataContext = main };
                app.MainWindow = window;
                var content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
                var listener = new BindingErrors();
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                var output = Path.GetFullPath("../../../../artifacts/ui-preview", AppContext.BaseDirectory);
                Directory.CreateDirectory(output);
                foreach (var theme in new[] { "Light", "Dark", "HighContrast" })
                {
                    foreach (var page in new[] { NavigationPage.Dashboard, NavigationPage.Scheduling, NavigationPage.Advanced, NavigationPage.Account })
                    {
                        main.NavigateCommand.Execute(page);
                        foreach (var size in new[] { new Size(860, 580), new Size(1020, 680) })
                        {
                            content.Measure(size);
                            content.Arrange(new Rect(size));
                            content.UpdateLayout();
                            var names = AssertNamedInteractiveControls(content);
                            foreach (var navigation in new[] { "Dashboard", "Scheduling", "Advanced", "Account" })
                            {
                                Assert.Contains(navigation, names);
                            }
                            Assert.Contains("Quit KeepMeAlive", names);
                            if (page == NavigationPage.Dashboard)
                            {
                                Assert.Contains("Preferences", names);
                                var interval = VisualDescendants(content).OfType<TextBox>().Single(control =>
                                    AutomationProperties.GetName(control) == "Pulse interval in seconds" && IsVisibleWithin(control, content));
                                interval.Text = "abc";
                                interval.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                                Assert.Equal("abc", main.DashboardViewModel.IntervalSecondsText);
                                Assert.False(main.DashboardViewModel.StartCommand.CanExecute(null));
                                Assert.NotEmpty(main.DashboardViewModel.InputError);
                                var start = VisualDescendants(content).OfType<Button>().Single(control =>
                                    ReferenceEquals(control.Command, main.DashboardViewModel.StartCommand));
                                Assert.False(start.IsEnabled);
                                interval.Text = "90";
                                interval.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
                                Assert.True(main.DashboardViewModel.StartCommand.CanExecute(null));
                                Assert.True(start.IsEnabled);
                                main.DashboardViewModel.DiscardChanges();
                                Assert.Equal("60", interval.Text);
                            }
                            if (page == NavigationPage.Advanced)
                            {
                                Assert.Contains("Activity method", names);
                                Assert.Contains("Pulse interval in seconds", names);
                                Assert.Contains("Virtual key code", names);
                                main.DashboardViewModel.SelectedMethod = KeepAliveMethod.Mouse;
                                content.UpdateLayout();
                                Assert.Contains("Mouse movement in pixels", AssertNamedInteractiveControls(content));
                                main.DashboardViewModel.DiscardChanges();
                                content.UpdateLayout();
                            }
                            if (page == NavigationPage.Scheduling)
                            {
                                main.SettingsViewModel.ScheduledActionEnabled = true;
                                content.UpdateLayout();
                                var datePicker = VisualDescendants(content).OfType<DatePicker>().Single(control =>
                                    AutomationProperties.GetName(control) == "Scheduled date" && IsVisibleWithin(control, content));
                                datePicker.ApplyTemplate();
                                var dateTextBox = Assert.IsType<DatePickerTextBox>(datePicker.Template.FindName("PART_TextBox", datePicker));
                                datePicker.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                                dateTextBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                                main.SettingsViewModel.ScheduledActionEnabled = false;
                                main.SettingsViewModel.ScheduledActionEnabled = true;
                                content.UpdateLayout();
                                Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.ApplicationIdle);
                                Assert.True(datePicker.IsEnabled);
                                Assert.True(dateTextBox.IsReadOnly);
                                var datePeer = UIElementAutomationPeer.CreatePeerForElement(dateTextBox)!;
                                var dateValue = Assert.IsAssignableFrom<IValueProvider>(datePeer.GetPattern(PatternInterface.Value));
                                Assert.True(dateValue.IsReadOnly);
                                var calendarButton = Assert.IsAssignableFrom<Button>(datePicker.Template.FindName("PART_Button", datePicker));
                                Assert.True(calendarButton.IsEnabled);
                                Assert.Equal("Open calendar", UIElementAutomationPeer.CreatePeerForElement(calendarButton)!.GetName());
                                TablerRenderingAssertions.HasIcon(calendarButton, Assert.IsAssignableFrom<Geometry>(app.FindResource("Tabler.Calendar")));
                                Assert.Contains("calendar", AutomationProperties.GetHelpText(datePicker), StringComparison.OrdinalIgnoreCase);
                                var selectedDate = DateTime.Today.AddDays(2);
                                datePicker.SetCurrentValue(DatePicker.SelectedDateProperty, selectedDate);
                                datePicker.GetBindingExpression(DatePicker.SelectedDateProperty)!.UpdateSource();
                                Assert.Equal(selectedDate, main.SettingsViewModel.ScheduledDate);
                                main.SettingsViewModel.DiscardCommand.Execute(null);
                            }
                            ThemeService.ApplyResources(theme == "HighContrast" ? "Light" : theme, theme == "HighContrast");
                            if (theme == "HighContrast")
                            {
                                Assert.Same(SystemColors.WindowTextBrush, app.FindResource("ErrorBrush"));
                                HighContrastRenderingAssertions.ControlsUseSystemBrushes(app);
                            }
                            content.UpdateLayout();
                            if (theme != "HighContrast")
                            {
                                Assert.Equal(ColorConverter.ConvertFromString(theme == "Dark" ? "#111113" : "#F2F2F7"),
                                    Assert.IsType<SolidColorBrush>(app.FindResource("BackgroundBrush")).Color);
                            }
                            TablerRenderingAssertions.IconsUseParentBrush(content, app);
                            foreach (var scale in new[] { 1.0, 1.5, 2.0 })
                            {
                            var bitmap = new RenderTargetBitmap((int)(size.Width * scale), (int)(size.Height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                            bitmap.Render(content);
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            using var file = File.Create(Path.Combine(output, $"{page}-{theme}-{size.Width}-{scale * 100}.png"));
                            encoder.Save(file);
                            Assert.True(file.Length > 5000);
                            }
                        }
                    }
                }
                ThemeService.ApplyResources("Light", false);
                HighContrastRenderingAssertions.NormalControlBrushesReturn(app);
                TablerRenderingAssertions.ControlAffordancesUseTabler(app);
                var methods = new MethodsPageView { DataContext = main.DashboardViewModel };
                methods.Measure(new Size(640, 600));
                methods.Arrange(new Rect(0, 0, 640, 600));
                methods.UpdateLayout();
                var methodNames = AssertNamedInteractiveControls(methods);
                Assert.Contains("Activity method", methodNames);
                Assert.Contains("Pulse interval in seconds", methodNames);
                Assert.Contains("Keep display and system awake", methodNames);
                PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                Assert.Empty(listener.Errors);
                provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
                window.Close();
                app.Shutdown();
                completion.SetResult();
            }
            catch (Exception exception) { completion.SetException(exception); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(45));
    }

    private static HashSet<string> AssertNamedInteractiveControls(DependencyObject root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var control in VisualDescendants(root).OfType<Control>())
        {
            if (control is not ButtonBase and not TextBox and not ComboBox and not DatePicker
                || control.TemplatedParent is not null && control.TemplatedParent is not Expander
                || !IsVisibleWithin(control, root))
            {
                continue;
            }

            var peer = UIElementAutomationPeer.CreatePeerForElement(control);
            Assert.NotNull(peer);
            string name = peer.GetName();
            Assert.True(!string.IsNullOrWhiteSpace(name), $"{control.GetType().Name} has no accessible name on the visible page.");
            names.Add(name);
        }

        Assert.NotEmpty(names);
        return names;
    }

    private static IEnumerable<DependencyObject> VisualDescendants(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in VisualDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsVisibleWithin(DependencyObject element, DependencyObject root)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: not Visibility.Visible })
            {
                return false;
            }

            if (ReferenceEquals(current, root))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class BindingErrors : TraceListener
    {
        public List<string> Errors { get; } = [];
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) { Errors.Add(message); } }
        public override void WriteLine(string? message) => Write(message);
    }
}
