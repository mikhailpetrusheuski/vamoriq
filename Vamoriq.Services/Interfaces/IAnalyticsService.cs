namespace Vamoriq.Services.Interfaces;

public interface IAnalyticsService
{
    Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null);
}
