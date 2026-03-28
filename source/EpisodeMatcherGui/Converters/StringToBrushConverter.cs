using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EpisodeMatcherGui.Converters;

public class StringToBrushConverter : IValueConverter
{
    public static readonly StringToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try { return SolidColorBrush.Parse(hex); }
            catch { /* fall through */ }
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
