using System.Globalization;

namespace StarterApp.Converters;

/// <summary>
/// Returns true when the bound value is not null, otherwise false.
/// Used to toggle IsVisible on elements that should only render when their data is present.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}