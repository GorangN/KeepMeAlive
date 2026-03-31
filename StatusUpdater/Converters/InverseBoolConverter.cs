using System.Globalization;
using System.Windows.Data;

namespace StatusUpdater.Converters;

/// <summary>Inverts a bool — used for IsEnabled="{Binding IsRunning, Converter=...}"</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}
