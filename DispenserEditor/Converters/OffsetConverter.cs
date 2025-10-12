using System;
using System.Globalization;
using System.Windows.Data;

namespace DispenserEditor.Converters
{
    public class OffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double number)
            {
                if (parameter is string parameterString && double.TryParse(parameterString, out var offset))
                {
                    return number + offset;
                }

                return number;
            }

            return 0d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}