using System.Globalization;
using System.Windows.Data;

namespace DeskNotes.Converters;

public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime timestamp)
            return "Gerade eben";

        var diff = DateTime.Now - timestamp;

        if (diff.TotalMinutes < 1)
            return "Gerade eben";
        if (diff.TotalMinutes < 60)
            return $"vor {(int)diff.TotalMinutes} Min";
        if (diff.TotalHours < 24)
            return $"vor {(int)diff.TotalHours} Std";
        if (diff.TotalDays < 7)
            return $"vor {(int)diff.TotalDays} Tagen";

        return timestamp.ToString("dd.MM.yyyy", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}