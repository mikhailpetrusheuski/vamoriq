using System.Globalization;

namespace Vamoriq.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isGenerating && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return isGenerating ? parts[0] : parts[1];
                }
            }
            return "Generate";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
