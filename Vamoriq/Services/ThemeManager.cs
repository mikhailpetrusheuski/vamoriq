using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services;

public class ThemeManager : IThemeManager
{
    private const string ThemeKey = "AppTheme";

    public event EventHandler<string>? ThemeChanged;

    public string CurrentTheme
    {
        get
        {
            var savedTheme = Preferences.Get(ThemeKey, "Dark");
            return savedTheme;
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        Preferences.Set(ThemeKey, theme);

        var appTheme = theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        Application.Current.UserAppTheme = appTheme;
        ThemeChanged?.Invoke(this, theme);
        await Task.CompletedTask;
    }

    public async Task<string> GetThemeAsync()
    {
        await Task.CompletedTask;
        return CurrentTheme;
    }

    public async Task InitializeAsync()
    {
        var theme = await GetThemeAsync();
        await SetThemeAsync(theme);
    }

    public bool IsDarkTheme => Application.Current?.UserAppTheme == AppTheme.Dark;
    public bool IsLightTheme => Application.Current?.UserAppTheme == AppTheme.Light;
    public bool IsSystemTheme => Application.Current?.UserAppTheme == AppTheme.Unspecified;
}
