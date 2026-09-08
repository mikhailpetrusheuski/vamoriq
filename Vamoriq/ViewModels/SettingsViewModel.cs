using System.Globalization;
using System.Windows.Input;
using Vamoriq.Core.ViewModels;
using Vamoriq.Resources.Localization;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiEmail = Microsoft.Maui.ApplicationModel.Communication.Email;

namespace Vamoriq.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;
        private readonly IPreferencesService? _preferences;
        private readonly IThemeManager? _themeManager;
        private readonly ILocalizationService? _localizationService;
        private readonly INotificationService? _notificationService;

        private bool _isLoadingSettings;

        [ObservableProperty]
        private string _username = "User";

        [ObservableProperty]
        private string _email = "user@example.com";

        [ObservableProperty]
        private bool _isLoggedIn = false;

        [ObservableProperty]
        private string _userEmail = string.Empty;

        [ObservableProperty]
        private bool _weeklyNotifications;

        [ObservableProperty]
        private bool _newRecipeAlerts;

        [ObservableProperty]
        private bool _analyticsEnabled;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private string _city = string.Empty;

        [ObservableProperty]
        private TimeSpan _notificationTime = new TimeSpan(9, 0, 0);

        [ObservableProperty]
        private bool _notificationsEnabled = true;

        [ObservableProperty]
        private int _selectedThemeIndex = 0;

        [ObservableProperty]
        private string _currentTheme = "System";

        [ObservableProperty]
        private string _selectedTheme = "Light";

        [ObservableProperty]
        private int _selectedUnitsIndex = 0;

        [ObservableProperty]
        private string _currentUnits = "Metric";

        [ObservableProperty]
        private int _selectedLanguageIndex = 0;

        [ObservableProperty]
        private string _currentLanguage = "English";

        [ObservableProperty]
        private string _currentLanguageCode = "en";

        public List<string> ThemeOptions { get; } = new() { "System", "Light", "Dark" };
        public List<string> UnitsOptions { get; } = new() { "Metric (ml, cl)", "Imperial (oz)" };
        public List<string> LanguageOptions { get; } = new() { "English", "Русский", "Español", "Italiano", "Deutsch", "Français", "Polski" };

        private readonly Dictionary<string, string> _languageCodeMap = new()
        {
            { "English", "en" },
            { "Русский", "ru" },
            { "Español", "es" },
            { "Italiano", "it" },
            { "Deutsch", "de" },
            { "Français", "fr" },
            { "Polski", "pl" }
        };

        public SettingsViewModel(ILogger<SettingsViewModel> logger) : base(logger)
        {
            Title = "Settings";
            InitializeCommands();
        }

        public SettingsViewModel(
            INavigationService navigationService,
            IAuthService authService,
            ILogger<SettingsViewModel> logger,
            IPreferencesService? preferences = null,
            IThemeManager? themeManager = null,
            ILocalizationService? localizationService = null,
            INotificationService? notificationService = null) : base(logger)
        {
            _navigationService = navigationService;
            _authService = authService;
            _preferences = preferences;
            _themeManager = themeManager;
            _localizationService = localizationService;
            _notificationService = notificationService;
            Title = "Settings";
            InitializeCommands();
            _ = LoadAsync();
        }

        private void InitializeCommands()
        {
            EditSettingsCommand = new RelayCommand(async () => await EditSettingsAsync());
            SettingsCommand = new Command(async () => await OpenSettingsAsync());
            HelpCommand = new Command(async () => await OpenHelpAsync());
            AboutCommand = new Command(async () => await OpenAboutAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());
            SignInCommand = new Command(async () => await SignInAsync());
            CreateAccountCommand = new Command(async () => await SignInAsync());
            SignOutCommand = new Command(async () => await LogoutAsync());
            PrivacyPolicyCommand = new Command(async () => await OpenPrivacyAsync());
            SetThemeCommand = new Command<string>(async theme => await SetThemeAsync(theme));
            DeleteAccountCommand = new Command(async () => await RequestAccountDeletionAsync());
        }

        public new bool HasError => !string.IsNullOrEmpty(base.ErrorMessage);

        public ICommand EditSettingsCommand { get; private set; } = null!;
        public ICommand SettingsCommand { get; private set; }
        public ICommand HelpCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
        public ICommand SignInCommand { get; private set; }
        public ICommand CreateAccountCommand { get; private set; }
        public ICommand SignOutCommand { get; private set; }
        public ICommand PrivacyPolicyCommand { get; private set; }
        public ICommand SetThemeCommand { get; private set; }
        public ICommand DeleteAccountCommand { get; private set; }

        private async Task EditSettingsAsync()
        {
            await ExecuteAsync(async () =>
            {

                await Task.Delay(500);
            });
        }

        private async Task OpenSettingsAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _navigationService?.NavigateToAsync("//Settings");
            }, "Failed to open settings");
        }

        private async Task OpenHelpAsync()
        {
            await ExecuteAsync(async () =>
            {
                var supportEmail = "support@vamoriq.app";
                var emailMessage = new EmailMessage
                {
                    Subject = "Vamoriq Support",
                    Body = "Hi Vamoriq team,\n\nI need help with...\n",
                    To = new List<string> { supportEmail }
                };

                if (MauiEmail.Default.IsComposeSupported)
                {
                    await MauiEmail.Default.ComposeAsync(emailMessage);
                }
                else
                {
                    var mailtoUri = new Uri($"mailto:{supportEmail}?subject=Vamoriq%20Support");
                    await Launcher.Default.OpenAsync(mailtoUri);
                }
            }, "Failed to open help");
        }

        private async Task OpenAboutAsync()
        {
            await ExecuteAsync(async () =>
            {
                var culture = _localizationService?.CurrentCulture ?? CultureInfo.CurrentUICulture;
                var locale = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                var aboutUrl = $"https://vamoriq.app/{locale}/about";
                await Launcher.Default.OpenAsync(aboutUrl);
            }, "Failed to open about");
        }

        private async Task SignInAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _navigationService?.NavigateToAsync("///Auth");
            }, "Failed to open sign in");
        }

        private async Task OpenPrivacyAsync()
        {
            await ExecuteAsync(async () =>
            {
                await Shell.Current.GoToAsync("///LegalConsent");
            }, "Failed to open privacy");
        }

        private async Task RequestAccountDeletionAsync()
        {
            await ExecuteAsync(async () =>
            {
                if (Shell.Current == null)
                {
                    return;
                }

                var confirm = await Shell.Current.DisplayAlert(
                    AppResources.Settings_DeleteAccountConfirmTitle,
                    AppResources.Settings_DeleteAccountConfirmBody,
                    AppResources.Delete,
                    AppResources.Cancel);

                if (!confirm)
                {
                    return;
                }

                const string recipient = "support@vamoriq.app";
                var subject = AppResources.Settings_DeleteAccountEmailSubject;
                var resolvedEmail = string.IsNullOrWhiteSpace(UserEmail) ? "your-email@example.com" : UserEmail;
                var body = string.Format(CultureInfo.CurrentCulture, AppResources.Settings_DeleteAccountEmailBody, resolvedEmail);

                if (MauiEmail.Default.IsComposeSupported)
                {
                    var emailMessage = new EmailMessage
                    {
                        Subject = subject,
                        Body = body,
                        To = new List<string> { recipient }
                    };
                    await MauiEmail.Default.ComposeAsync(emailMessage);
                }
                else
                {
                    var encodedSubject = Uri.EscapeDataString(subject);
                    var encodedBody = Uri.EscapeDataString(body);
                    var mailtoUri = new Uri($"mailto:{recipient}?subject={encodedSubject}&body={encodedBody}");
                    await Launcher.Default.OpenAsync(mailtoUri);
                }
            }, "Failed to start account deletion request");
        }

        private async Task SetThemeAsync(string theme)
        {
            await ExecuteAsync(async () =>
            {
                Logger?.LogInformation($"SetThemeAsync called with theme: {theme}");

                if (_themeManager != null)
                {

                    await _themeManager.SetThemeAsync(theme);

                    CurrentTheme = theme;
                    SelectedThemeIndex = ThemeOptions.IndexOf(theme);
                    Logger?.LogInformation($"Theme changed and saved: {theme}");

                    try
                    {
                        var appShell = Application.Current?.MainPage as AppShell;
                        appShell?.UpdateShellColors();
                        Logger?.LogInformation("AppShell colors updated after theme change");
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogWarning(ex, "Failed to update AppShell colors");
                    }
                }
                else
                {
                    Logger?.LogError("ThemeManager is NULL in SetThemeAsync!");
                }
            }, "Failed to set theme");
        }

        private async Task LogoutAsync()
        {
            await ExecuteAsync(async () =>
            {
                if (_authService == null)
                {
                    await Shell.Current.GoToAsync("///Welcome");
                    return;
                }

                var success = await _authService.LogoutAsync();

                IsLoggedIn = false;
                UserEmail = string.Empty;

                await Shell.Current.GoToAsync("///Welcome");
            }, "Failed to logout");
        }

        private async Task LoadAsync()
        {
            try
            {
                Logger.LogInformation("SettingsViewModel.LoadAsync: Starting...");
                _isLoadingSettings = true;
                IsBusy = true;

                Logger.LogInformation($"SettingsViewModel.LoadAsync: _authService is {(_authService == null ? "NULL" : "not null")}");
                var isAuth = _authService != null && await _authService.IsAuthenticatedAsync();
                IsLoggedIn = isAuth;
                Logger.LogInformation("SettingsViewModel.LoadAsync: IsLoggedIn={IsLoggedIn}", IsLoggedIn);

                var user = _authService != null ? await _authService.GetCurrentUserAsync() : null;
                UserEmail = user?.Email ?? string.Empty;

                City = _preferences?.Get("user_city", string.Empty) ?? string.Empty;
                var savedMinutes = _preferences?.Get("notification_time_minutes", 540) ?? 540;
                NotificationTime = TimeSpan.FromMinutes(savedMinutes);

                WeeklyNotifications = _preferences?.Get("weekly_notifications", false) ?? false;
                NewRecipeAlerts = _preferences?.Get("new_recipe_alerts", false) ?? false;
                AnalyticsEnabled = _preferences?.Get("gdpr_analytics_consent", false) ?? false;

                CurrentTheme = _themeManager?.CurrentTheme ?? "System";
                SelectedThemeIndex = ThemeOptions.IndexOf(CurrentTheme);
                if (SelectedThemeIndex < 0) SelectedThemeIndex = 0;
                Logger?.LogInformation($"SettingsViewModel.LoadAsync: Theme loaded - CurrentTheme={CurrentTheme}, SelectedThemeIndex={SelectedThemeIndex}");

                CurrentUnits = _preferences?.Get("units_of_measurement", "Metric") ?? "Metric";
                SelectedUnitsIndex = CurrentUnits == "Imperial" ? 1 : 0;

                var storedLanguageCode = _preferences?.Get("app_language_code", string.Empty) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(storedLanguageCode))
                {

                    storedLanguageCode = _localizationService?.CurrentCulture.TwoLetterISOLanguageName
                        ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                }

                storedLanguageCode = storedLanguageCode.Length > 2
                    ? storedLanguageCode.Substring(0, 2).ToLowerInvariant()
                    : storedLanguageCode.ToLowerInvariant();

                if (!_languageCodeMap.Values.Contains(storedLanguageCode))
                {
                    storedLanguageCode = "en";
                }

                var defaultLanguageName = "English";
                foreach (var kvp in _languageCodeMap)
                {
                    if (kvp.Value == storedLanguageCode)
                    {
                        defaultLanguageName = kvp.Key;
                        break;
                    }
                }

                var storedLanguageName = _preferences?.Get("app_language_name", string.Empty) ?? string.Empty;

                CurrentLanguageCode = storedLanguageCode;
                CurrentLanguage = string.IsNullOrWhiteSpace(storedLanguageName) ? defaultLanguageName : storedLanguageName;

                SelectedLanguageIndex = LanguageOptions.IndexOf(CurrentLanguage);
                if (SelectedLanguageIndex < 0)
                {
                    SelectedLanguageIndex = LanguageOptions.IndexOf(defaultLanguageName);
                    if (SelectedLanguageIndex < 0) SelectedLanguageIndex = 0;
                }

                var detectedVersion = AppInfo.Current.VersionString;
                if (string.IsNullOrWhiteSpace(detectedVersion) || detectedVersion.Contains("$("))
                {
                    detectedVersion = BuildInfo.ApplicationDisplayVersion;
                }
                if (string.IsNullOrWhiteSpace(detectedVersion))
                {
                    detectedVersion = BuildInfo.ApplicationVersion;
                }
                AppVersion = detectedVersion;

                Logger.LogInformation($"SettingsViewModel.LoadAsync: Completed successfully. AppVersion={AppVersion}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load settings");
                ErrorMessage = "Failed to load settings";
            }
            finally
            {
                _isLoadingSettings = false;
                IsBusy = false;
                Logger.LogInformation("SettingsViewModel.LoadAsync: IsBusy set to false");
            }
        }

        public new Task RefreshAsync()
        {
            Logger.LogInformation("SettingsViewModel.RefreshAsync: Called");
            return LoadAsync();
        }

        partial void OnWeeklyNotificationsChanged(bool value)
        {
            _preferences?.SetAsync("weekly_notifications", value);
        }

        partial void OnNewRecipeAlertsChanged(bool value)
        {
            _preferences?.SetAsync("new_recipe_alerts", value);
        }

        partial void OnAnalyticsEnabledChanged(bool value)
        {
            _preferences?.SetAsync("gdpr_analytics_consent", value);
            _preferences?.SetAsync("gdpr_crash_consent", value);
        }

        partial void OnSelectedThemeIndexChanged(int value)
        {
            if (value >= 0 && value < ThemeOptions.Count)
            {
                CurrentTheme = ThemeOptions[value];
                Logger?.LogInformation($"Theme selection changed to index {value}: {CurrentTheme}");

                if (_themeManager != null)
                {
                    Logger?.LogInformation($"Calling SetThemeAsync with theme: {CurrentTheme}");
                    _ = _themeManager.SetThemeAsync(CurrentTheme);
                }
                else
                {
                    Logger?.LogError("ThemeManager is NULL!");
                }
            }
            else
            {
                Logger?.LogWarning($"Invalid theme index: {value}");
            }
        }

        partial void OnSelectedUnitsIndexChanged(int value)
        {
            CurrentUnits = value == 1 ? "Imperial" : "Metric";
            _preferences?.SetAsync("units_of_measurement", CurrentUnits);
        }

        partial void OnSelectedLanguageIndexChanged(int value)
        {
            if (_isLoadingSettings)
            {
                return;
            }

            if (value >= 0 && value < LanguageOptions.Count)
            {
                var newLanguage = LanguageOptions[value];
                if (newLanguage == CurrentLanguage) return;

                CurrentLanguage = newLanguage;

                if (_languageCodeMap.TryGetValue(newLanguage, out var languageCode))
                {
                    CurrentLanguageCode = languageCode;

                    _preferences?.SetAsync("app_language_name", CurrentLanguage);
                    _preferences?.SetAsync("app_language_code", languageCode);

                    if (_localizationService != null)
                    {
                        _ = _localizationService.SetLanguageAsync(languageCode);
                    }

                    var culture = new System.Globalization.CultureInfo(languageCode);
                    Extensions.LocalizationResourceManager.Instance.SetCulture(culture);

                    Logger?.LogInformation($"Language changed to: {newLanguage} ({languageCode})");

                    _ = _preferences?.SetAsync("gdpr_consent_asked", false);

                    _ = _preferences?.SetAsync("gdpr_return_to_generator", true);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await Shell.Current.GoToAsync("///LegalConsent");
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, "Failed to navigate to LegalConsent after language change");
                        }
                    });
                }
            }
        }

        partial void OnCityChanged(string value)
        {
            if (!_isLoadingSettings)
                _preferences?.SetAsync("user_city", value);
        }

        partial void OnNotificationTimeChanged(TimeSpan value)
        {
            if (_isLoadingSettings) return;
            _preferences?.SetAsync("notification_time_minutes", (int)value.TotalMinutes);
            _ = _notificationService?.ScheduleDailyReminderAsync(value);
        }

        [RelayCommand]
        protected override async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
