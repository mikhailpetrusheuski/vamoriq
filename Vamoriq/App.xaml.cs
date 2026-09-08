using Vamoriq.Services.Interfaces;
using Vamoriq.ViewModels;
using Vamoriq.Views;
using Microsoft.Extensions.Logging;

namespace Vamoriq;

public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private readonly ITokenRefreshService _tokenRefreshService;
    private readonly IAuthService _authService;
    private readonly IPreferencesService _preferencesService;
    private readonly ILocalizationService _localizationService;
    private readonly IThemeManager _themeManager;
    private readonly IPhotoStorageService _photoStorageService;
    private readonly INotificationService _notificationService;
    private MainViewModel? _welcomeViewModel;

    public App(ILogger<App> logger, ITokenRefreshService tokenRefreshService, IAuthService authService, IPreferencesService preferencesService, ILocalizationService localizationService, IThemeManager themeManager, IPhotoStorageService photoStorageService, INotificationService notificationService)
    {
        _logger = logger;
        _tokenRefreshService = tokenRefreshService;
        _authService = authService;
        _preferencesService = preferencesService;
        _localizationService = localizationService;
        _themeManager = themeManager;
        _photoStorageService = photoStorageService;
        _notificationService = notificationService;

        InitializeThemeBeforeUI();

        InitializeComponent();

        InitializeLocalization();

        MainPage = new AppShell();
        _logger.LogInformation("App.ctor completed");
    }

    private void InitializeThemeBeforeUI()
    {
        try
        {
            string savedTheme;

            if (!Preferences.ContainsKey("AppTheme"))
            {

                savedTheme = "Dark";
                Preferences.Set("AppTheme", savedTheme);
                _logger.LogInformation("First time setup: Setting dark theme as default");
            }
            else
            {

                savedTheme = Preferences.Get("AppTheme", "Dark");
                _logger.LogInformation($"Loading saved theme preference: {savedTheme}");
            }

            var appTheme = savedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };

            Application.Current.UserAppTheme = appTheme;
            _logger.LogInformation($"Theme set before UI initialization: {savedTheme}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set initial theme, defaulting to Dark");

            Application.Current.UserAppTheme = AppTheme.Dark;
        }
    }

    private void InitializeLocalization()
    {
        try
        {

            var currentCulture = _localizationService.CurrentCulture;

            Extensions.LocalizationResourceManager.Instance.SetCulture(currentCulture);

            _logger.LogInformation($"Localization initialized with culture: {currentCulture.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize localization");
        }
    }

    protected override async void OnStart()
    {
        base.OnStart();
        _logger.LogInformation("App.OnStart");

        SetWelcomeLoadingState(true);
        try
        {

            try
            {
                await _themeManager.InitializeAsync();
                _logger.LogInformation($"Theme re-initialized in OnStart: {_themeManager.CurrentTheme}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize theme in OnStart");
            }

            try
            {

                var consentAsked = _preferencesService.Get("gdpr_consent_asked", false);
                if (!consentAsked)
                {
                    await Shell.Current.GoToAsync("///LegalConsent");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consent check failed");
            }

            try
            {
                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (isAuthenticated)
                {
                    await _tokenRefreshService.StartBackgroundRefreshAsync();
                    _logger.LogInformation("Background token refresh started");
                    await Shell.Current.GoToAsync("///Mission");
                }
                else
                {
                    await Shell.Current.GoToAsync("///Welcome");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting background services");
            }
        }
        finally
        {
            SetWelcomeLoadingState(false);
        }

        _ = Task.Run(async () =>
        {
            try { await _photoStorageService.CleanupOldPhotosAsync(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Photo cleanup on start failed"); }
        });
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        _logger.LogInformation("App.OnSleep");

    }

    protected override async void OnResume()
    {
        base.OnResume();
        _logger.LogInformation("App.OnResume");

        try
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            if (isAuthenticated && !_tokenRefreshService.IsRunning)
            {
                await _tokenRefreshService.StartBackgroundRefreshAsync();
                _logger.LogInformation("Background token refresh restarted");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting background services");
        }
    }

    private void SetWelcomeLoadingState(bool isBusy)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var vm = GetWelcomeViewModel();
            if (vm != null)
            {
                vm.IsBusy = isBusy;
            }
        });
    }

    private MainViewModel? GetWelcomeViewModel()
    {
        if (_welcomeViewModel != null)
        {
            return _welcomeViewModel;
        }

        try
        {
            if (Shell.Current?.CurrentPage is MainPage currentMain && currentMain.BindingContext is MainViewModel currentVm)
            {
                _welcomeViewModel = currentVm;
                return currentVm;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve Welcome view model");
        }

        return null;
    }
}
