using System.Globalization;
using System.Windows.Data;

namespace KeepMeAlive.Converters;

/// <summary>Chooses compact page gutters when the desktop window has limited width.</summary>
public sealed class CompactLayoutConverter : IValueConverter
{
    /// <summary>Returns whether the window width is below 1000 device-independent pixels.</summary>
    /// <param name="value">The current window width.</param>
    /// <param name="targetType">The binding target type.</param>
    /// <param name="parameter">An unused binding parameter.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>True for a compact window; otherwise false.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is double width && width < 1000;

    /// <summary>Leaves source width unchanged because it is controlled by the window.</summary>
    /// <param name="value">The binding target value.</param>
    /// <param name="targetType">The source type.</param>
    /// <param name="parameter">An unused binding parameter.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>The binding sentinel that leaves the source unchanged.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
