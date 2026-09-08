using Vamoriq.Views;
using Vamoriq.Extensions;
using Vamoriq.Resources.Localization;
using Vamoriq.Services.Interfaces;

namespace Vamoriq;

public partial class AppShell : Shell
{
    private IThemeManager? _themeManager;
    private static bool _routesRegistered;

    public AppShell()
    {
        InitializeComponent();

        if (!_routesRegistered)
        {
            Routing.RegisterRoute("MissionDetail", typeof(MissionDetailPage));
            _routesRegistered = true;
        }

        LocalizationResourceManager.Instance.PropertyChanged += (s, e) =>
        {
            UpdateTabBarTitles();
        };

        try
        {
            _themeManager = Application.Current?.Handler?.MauiContext?.Services?.GetService<IThemeManager>();
            if (_themeManager != null)
            {

                _themeManager.ThemeChanged += (s, theme) =>
                {
                    UpdateShellColors();
                };
            }
        }
        catch
        {

        }

        UpdateTabBarTitles();
        UpdateShellColors();
    }

    private void UpdateTabBarTitles()
    {
        MissionTab.Title = AppResources.Mission_Title;
        ProgressTab.Title = AppResources.Progress_Title;
        SettingsTab.Title = AppResources.Settings_Title;
    }

    public void UpdateShellColors()
    {

        if (Application.Current?.UserAppTheme == AppTheme.Dark)
        {

            Shell.SetBackgroundColor(this, Colors.Black);
            Shell.SetForegroundColor(this, Colors.White);
            Shell.SetTitleColor(this, Colors.White);
            Shell.SetTabBarBackgroundColor(this, Color.FromArgb("#1C1C1E"));
            Shell.SetTabBarForegroundColor(this, Color.FromArgb("#C49340"));
            Shell.SetTabBarTitleColor(this, Color.FromArgb("#C49340"));
            Shell.SetTabBarUnselectedColor(this, Color.FromArgb("#8E8E93"));
            Shell.SetUnselectedColor(this, Color.FromArgb("#B3B3B3"));
        }
        else
        {

            Shell.SetBackgroundColor(this, Colors.White);
            Shell.SetForegroundColor(this, Colors.Black);
            Shell.SetTitleColor(this, Colors.Black);
            Shell.SetTabBarBackgroundColor(this, Colors.White);
            Shell.SetTabBarForegroundColor(this, Color.FromArgb("#C49340"));
            Shell.SetTabBarTitleColor(this, Color.FromArgb("#C49340"));
            Shell.SetTabBarUnselectedColor(this, Color.FromArgb("#8A8A8A"));
            Shell.SetUnselectedColor(this, Color.FromArgb("#8A8A8A"));
        }
    }
}
