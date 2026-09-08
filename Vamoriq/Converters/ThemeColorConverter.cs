using System.Globalization;

namespace Vamoriq.Converters;

public class ThemeColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isDarkTheme = Application.Current?.UserAppTheme == AppTheme.Dark;

        if (parameter is string colorNames)
        {
            var colors = colorNames.Split(',');
            if (colors.Length >= 2)
            {
                var lightColor = colors[0].Trim();
                var darkColor = colors[1].Trim();

                return isDarkTheme ? darkColor : lightColor;
            }
        }

        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
