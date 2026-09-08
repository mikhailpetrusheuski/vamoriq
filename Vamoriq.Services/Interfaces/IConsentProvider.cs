namespace Vamoriq.Services.Interfaces
{
    public interface IConsentProvider
    {
        bool IsAnalyticsAllowed();
        bool IsCrashReportingAllowed();
    }
}
