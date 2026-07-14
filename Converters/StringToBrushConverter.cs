using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeskNotes.Converters;

public class StringToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush DefaultBrush = CreateBrush("#5FA8FF");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string color || string.IsNullOrWhiteSpace(color))
            return DefaultBrush;

        try
        {
            return CreateBrush(color);
        }
        catch
        {
            return DefaultBrush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush CreateBrush(string color) =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)!);
}