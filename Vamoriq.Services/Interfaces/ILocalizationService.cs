using System.Globalization;

namespace Vamoriq.Services.Interfaces
{

    public interface ILocalizationService
    {

        CultureInfo CurrentCulture { get; }

        Task SetLanguageAsync(string cultureName);

        string GetString(string key);

        List<string> GetAvailableLanguages();

        event EventHandler? LanguageChanged;
    }
}
