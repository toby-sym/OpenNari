using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenNari;

public sealed class ViewportWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // The settings grid has 40 pixels of margin on both sides.
        return value is double width ? Math.Max(0, width - 80) : 0d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
