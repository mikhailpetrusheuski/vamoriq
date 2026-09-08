using System.Globalization;

namespace Vamoriq.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colorNames)
            {

                var separator = colorNames.Contains('|') ? '|' : ',';
                var colors = colorNames.Split(separator);
                if (colors.Length == 2)
                {
                    var trueColor = colors[0].Trim();
                    var falseColor = colors[1].Trim();

                    return boolValue ? trueColor : falseColor;
                }
            }

            return "SurfaceVariant";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
