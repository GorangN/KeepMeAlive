using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace KeepMeAlive.Behaviors;

/// <summary>Restricts a DatePicker's editable display to calendar selection while retaining its calendar controls.</summary>
public static class CalendarOnlyDatePicker
{
    /// <summary>Identifies whether date text entry is disabled for a DatePicker.</summary>
    public static readonly DependencyProperty IsCalendarOnlyProperty = DependencyProperty.RegisterAttached(
        "IsCalendarOnly", typeof(bool), typeof(CalendarOnlyDatePicker), new PropertyMetadata(false, OnCalendarOnlyChanged));

    private static readonly DependencyProperty TextBoxProperty = DependencyProperty.RegisterAttached(
        "TextBox", typeof(DatePickerTextBox), typeof(CalendarOnlyDatePicker));
    private static readonly DependencyPropertyDescriptor TemplateDescriptor =
        DependencyPropertyDescriptor.FromProperty(Control.TemplateProperty, typeof(DatePicker))!;

    /// <summary>Gets whether the date must be selected through the calendar.</summary>
    /// <param name="datePicker">The date picker to inspect.</param>
    /// <returns>Whether direct date-text editing is disabled.</returns>
    public static bool GetIsCalendarOnly(DatePicker datePicker) => (bool)datePicker.GetValue(IsCalendarOnlyProperty);

    /// <summary>Enables or disables calendar-only date selection.</summary>
    /// <param name="datePicker">The date picker to configure.</param>
    /// <param name="value">Whether direct date-text editing should be disabled.</param>
    public static void SetIsCalendarOnly(DatePicker datePicker, bool value) => datePicker.SetValue(IsCalendarOnlyProperty, value);

    private static void OnCalendarOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        var picker = (DatePicker)sender;
        if ((bool)eventArgs.NewValue)
        {
            picker.Loaded += OnLoaded;
            picker.Unloaded += OnUnloaded;
            if (picker.IsLoaded) { Attach(picker); }
        }
        else
        {
            picker.Loaded -= OnLoaded;
            picker.Unloaded -= OnUnloaded;
            Detach(picker);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs eventArgs) => Attach((DatePicker)sender);
    private static void OnUnloaded(object sender, RoutedEventArgs eventArgs) => Detach((DatePicker)sender);
    private static void OnTemplateChanged(object? sender, EventArgs eventArgs) => Attach((DatePicker)sender!);

    private static void Attach(DatePicker picker)
    {
        TemplateDescriptor.RemoveValueChanged(picker, OnTemplateChanged);
        picker.ApplyTemplate();
        if (picker.Template?.FindName("PART_Button", picker) is Button calendarButton)
        {
            AutomationProperties.SetName(calendarButton, "Open calendar");
        }
        var textBox = picker.Template?.FindName("PART_TextBox", picker) as DatePickerTextBox;
        var previous = picker.GetValue(TextBoxProperty) as DatePickerTextBox;
        if (!ReferenceEquals(previous, textBox))
        {
            if (previous is not null) { previous.IsEnabledChanged -= OnTextBoxEnabledChanged; }
            picker.SetValue(TextBoxProperty, textBox);
            if (textBox is not null) { textBox.IsEnabledChanged += OnTextBoxEnabledChanged; }
        }
        if (textBox is not null) { textBox.IsReadOnly = true; }
        TemplateDescriptor.AddValueChanged(picker, OnTemplateChanged);
    }

    private static void OnTextBoxEnabledChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        // DatePickerTextBox itself resets IsReadOnly to !IsEnabled; run after that framework handler.
        ((DatePickerTextBox)sender).IsReadOnly = true;
    }

    private static void Detach(DatePicker picker)
    {
        TemplateDescriptor.RemoveValueChanged(picker, OnTemplateChanged);
        if (picker.GetValue(TextBoxProperty) is DatePickerTextBox textBox)
        {
            textBox.IsEnabledChanged -= OnTextBoxEnabledChanged;
            textBox.ClearValue(TextBoxBase.IsReadOnlyProperty);
        }
        picker.ClearValue(TextBoxProperty);
    }
}
