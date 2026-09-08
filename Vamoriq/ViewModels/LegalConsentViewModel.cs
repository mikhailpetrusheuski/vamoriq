using System.Globalization;
using Vamoriq.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Vamoriq.ViewModels
{
	public partial class LegalConsentViewModel : ObservableObject
	{
		private readonly IPreferencesService? _preferences;
		private readonly ILogger<LegalConsentViewModel>? _logger;
		private readonly ILocalizationService? _localizationService;

		[ObservableProperty]
		private string _legalUrl = BuildLegalUrl("en");

		[ObservableProperty]
		private bool _analyticsConsent;

		[ObservableProperty]
		private bool _crashConsent;

		public LegalConsentViewModel()
		{
		}

		public LegalConsentViewModel(
			IPreferencesService preferences,
			ILogger<LegalConsentViewModel> logger,
			ILocalizationService localizationService)
		{
			_preferences = preferences;
			_logger = logger;
			_localizationService = localizationService;
			_analyticsConsent = _preferences.Get("gdpr_analytics_consent", false);
			_crashConsent = _preferences.Get("gdpr_crash_consent", false);

			var locale = _localizationService?.CurrentCulture.TwoLetterISOLanguageName ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			_legalUrl = BuildLegalUrl(locale);
		}

		[RelayCommand]
		private async Task SaveAsync()
		{
			if (_preferences == null)
			{
				_logger?.LogWarning("Preferences service not available yet. Delaying consent save.");
				return;
			}

			try
			{
				var returnToGenerator = _preferences.Get("gdpr_return_to_generator", false);
				await _preferences.SetAsync("gdpr_analytics_consent", AnalyticsConsent);
				await _preferences.SetAsync("gdpr_crash_consent", CrashConsent);
				await _preferences.SetAsync("gdpr_consent_asked", true);
				if (returnToGenerator)
				{
					await Shell.Current.GoToAsync("///Mission");
					await _preferences.SetAsync("gdpr_return_to_generator", false);
				}
				else
				{
					await Shell.Current.GoToAsync("///Welcome");
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to save consent");
			}
		}

		[RelayCommand]
		private async Task OpenPrivacyAsync()
		{
			await Launcher.Default.OpenAsync($"{LegalUrl}/privacy");
		}

		[RelayCommand]
		private async Task OpenTermsAsync()
		{
			await Launcher.Default.OpenAsync($"{LegalUrl}/terms");
		}

		[RelayCommand]
		private async Task OpenAboutAsync()
		{
			await Launcher.Default.OpenAsync($"{LegalUrl}/about");
		}

		private static string BuildLegalUrl(string locale)
		{
			var normalized = string.IsNullOrWhiteSpace(locale) ? "en" : locale.ToLowerInvariant();
			return $"https://vamoriq.app/{normalized}";
		}
	}
}
