using System.Globalization;

namespace Vamoriq.Converters
{
    public class BoolToIntConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string intValues)
            {
                var values = intValues.Split(',');
                if (values.Length == 2 && int.TryParse(values[0].Trim(), out int trueValue) && int.TryParse(values[1].Trim(), out int falseValue))
                {
                    return boolValue ? trueValue : falseValue;
                }
            }

            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
