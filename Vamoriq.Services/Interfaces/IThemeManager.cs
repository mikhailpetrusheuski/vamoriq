namespace Vamoriq.Services.Interfaces;

public interface IThemeManager
{
    string CurrentTheme { get; }
    Task SetThemeAsync(string theme);
    Task<string> GetThemeAsync();
    Task InitializeAsync();
    event EventHandler<string> ThemeChanged;
    bool IsDarkTheme { get; }
    bool IsLightTheme { get; }
    bool IsSystemTheme { get; }
}
