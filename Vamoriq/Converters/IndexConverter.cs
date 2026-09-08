using System.Globalization;

namespace Vamoriq.Converters
{
    public class IndexConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {

            if (value is string instruction)
            {

                return "1";
            }

            if (value is int index)
            {
                return (index + 1).ToString();
            }

            return "1";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
