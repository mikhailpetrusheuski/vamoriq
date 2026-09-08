using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ILogger<AnalyticsService> _logger;
    private readonly IConsentProvider _consentProvider;

    public AnalyticsService(ILogger<AnalyticsService> logger, IConsentProvider consentProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _consentProvider = consentProvider ?? throw new ArgumentNullException(nameof(consentProvider));
    }

    public Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null)
    {
        if (!_consentProvider.IsAnalyticsAllowed())
            return Task.CompletedTask;

        _logger.LogInformation("Event tracked: {EventName}", eventName);
        return Task.CompletedTask;
    }
}
