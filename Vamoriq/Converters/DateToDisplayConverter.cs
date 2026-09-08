using System.Globalization;

namespace Vamoriq.Converters
{
    public class DateToDisplayConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt && dt > DateTime.MinValue.AddDays(1))
            {
                return dt.ToLocalTime().ToString("MMM dd, yyyy", culture);
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            return DateTime.UtcNow;
        }
    }
}
