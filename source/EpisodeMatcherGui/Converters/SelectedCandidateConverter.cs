using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using EpisodeMatcherCore;

namespace EpisodeMatcherGui.Converters;

public class SelectedCandidateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value = SelectedCandidate from the ViewModel
        // parameter = Current MatchCandidate from the ItemsControl
        
        if (value is MatchCandidate selected && parameter is MatchCandidate current)
        {
            // Since MatchCandidate is a record, we can use direct equality comparison
            if (selected.Equals(current))
            {
                return new SolidColorBrush(Color.Parse("#6c7086")); // Lighter highlight
            }
        }

        // Default button color
        return new SolidColorBrush(Color.Parse("#45475a"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
