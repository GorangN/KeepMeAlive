using KeepMeAlive.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeepMeAlive.Converters;

/// <summary>
/// Converts a <see cref="NavigationPage"/> value to <see cref="Visibility"/>.
/// Returns <see cref="Visibility.Visible"/> when the bound value matches the
/// <see cref="NavigationPage"/> name passed as <c>ConverterParameter</c>.
/// </summary>
public sealed class NavigationPageToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NavigationPage current && parameter is string targetName
            && Enum.TryParse<NavigationPage>(targetName, out var target))
        {
            return current == target ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
