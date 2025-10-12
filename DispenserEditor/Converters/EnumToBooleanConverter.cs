using System;
using System.Globalization;
using System.Windows.Data;

namespace DispenserEditor.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        return value.ToString()?.Equals(parameter.ToString()) == true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null)
        {
            return Binding.DoNothing;
        }

        if (value is bool isChecked && isChecked)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        return Binding.DoNothing;
    }
}
