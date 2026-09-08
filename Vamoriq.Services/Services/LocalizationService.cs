using System.Globalization;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services.Services
{

    public class LocalizationService : ILocalizationService
    {
        private readonly IPreferencesService _preferencesService;
        private readonly ILogger<LocalizationService> _logger;
        private CultureInfo _currentCulture;

        private static readonly Dictionary<string, string> SupportedLanguages = new()
        {
            { "en", "English" },
            { "ru", "Русский" },
            { "es", "Español" },
            { "it", "Italiano" },
            { "de", "Deutsch" },
            { "fr", "Français" },
            { "pl", "Polski" }
        };

        public event EventHandler? LanguageChanged;

        public LocalizationService(
            IPreferencesService preferencesService,
            ILogger<LocalizationService> logger)
        {
            _preferencesService = preferencesService;
            _logger = logger;

            var savedLanguage = _preferencesService.Get("app_language_code", string.Empty);

            if (string.IsNullOrEmpty(savedLanguage))
            {
                savedLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                if (!SupportedLanguages.ContainsKey(savedLanguage))
                {
                    savedLanguage = "en";
                }
            }

            _currentCulture = new CultureInfo(savedLanguage);
            ApplyCulture(_currentCulture);
        }

        public CultureInfo CurrentCulture => _currentCulture;

        public async Task SetLanguageAsync(string cultureName)
        {
            try
            {
                if (string.IsNullOrEmpty(cultureName))
                {
                    _logger.LogWarning("Attempted to set empty culture name");
                    return;
                }

                var cultureCode = cultureName.Length > 2
                    ? cultureName.Substring(0, 2).ToLower()
                    : cultureName.ToLower();

                if (!SupportedLanguages.ContainsKey(cultureCode))
                {
                    _logger.LogWarning($"Unsupported language: {cultureName}");
                    return;
                }

                var newCulture = new CultureInfo(cultureCode);

                if (_currentCulture.Name == newCulture.Name)
                {
                    _logger.LogInformation($"Language already set to {cultureCode}");
                    return;
                }

                _currentCulture = newCulture;
                ApplyCulture(_currentCulture);

                await _preferencesService.SetAsync("app_language_code", cultureCode);
                await _preferencesService.SetAsync("app_language_name", SupportedLanguages[cultureCode]);

                _logger.LogInformation($"Language changed to: {cultureCode}");

                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting language to {cultureName}");
            }
        }

        public string GetString(string key)
        {
            try
            {

                return key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting localized string for key: {key}");
                return key;
            }
        }

        public List<string> GetAvailableLanguages()
        {
            return SupportedLanguages.Keys.ToList();
        }

        private void ApplyCulture(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            _logger.LogInformation($"Applied culture: {culture.Name}");
        }
    }
}
