using System.Globalization;

namespace StarterApp.Converters;

/// <summary>
/// Converts a bool to one of two strings supplied via ConverterParameter in the form "trueText|falseText".
/// Example: IsEditMode=true with parameter "Update|Create" returns "Update".
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
                return b ? parts[0] : parts[1];
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}