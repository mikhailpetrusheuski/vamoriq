using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services
{
    public class ConsentProvider : IConsentProvider
    {
        private readonly IPreferencesService _preferences;

        public ConsentProvider(IPreferencesService preferences)
        {
            _preferences = preferences;
        }

        public bool IsAnalyticsAllowed()
        {
            try { return _preferences.Get("gdpr_analytics_consent", false); } catch { return false; }
        }

        public bool IsCrashReportingAllowed()
        {
            try { return _preferences.Get("gdpr_crash_consent", false); } catch { return false; }
        }
    }
}
