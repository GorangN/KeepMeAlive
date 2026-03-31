using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StatusUpdater.Converters;

/// <summary>
/// True → RunningBrush (Twitter green = running), False → StoppedBrush (grey = stopped)
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isTrue = value is bool b && b;
        var key = isTrue ? "RunningBrush" : "StoppedBrush";
        return Application.Current.Resources[key] as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
