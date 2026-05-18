using System.Globalization;
using System.Windows.Data;

namespace EDAI.UI.Converters;

/// <summary>
/// Converts an enum value to bool for RadioButton binding.
/// ConverterParameter = the enum value this button represents (as a string).
/// </summary>
public sealed class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string s)
            return Enum.Parse(targetType, s);
        return System.Windows.Data.Binding.DoNothing;
    }
}
