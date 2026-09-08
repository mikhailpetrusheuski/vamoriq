using Microsoft.Extensions.Logging;
using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services;

public partial class NotificationService : Interfaces.INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IAnalyticsService _analyticsService;
    private bool _permissionGranted;

    public bool IsPermissionGranted => _permissionGranted;

    public NotificationService(ILogger<NotificationService> logger, IAnalyticsService analyticsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    public async Task RequestPermissionAsync()
    {
        try
        {
            _permissionGranted = await RequestPlatformPermissionAsync();
            _logger.LogInformation("Notification permission result: {Granted}", _permissionGranted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to request notification permission");
            _permissionGranted = false;
        }
    }

    public async Task ScheduleDailyReminderAsync(TimeSpan timeOfDay)
    {
        try
        {
            await CancelDailyReminderAsync();
            await SchedulePlatformReminderAsync(timeOfDay);
            _logger.LogInformation("Daily reminder scheduled for {Time}", timeOfDay);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to schedule daily reminder");
        }
    }

    public async Task CancelDailyReminderAsync()
    {
        try
        {
            CancelPlatformReminder();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel daily reminder");
        }
    }

    public async Task TrackNotificationOpenedAsync()
    {
        await _analyticsService.TrackEventAsync("notification_opened");
    }

    partial void CancelPlatformReminder();
    private partial Task<bool> RequestPlatformPermissionAsync();
    private partial Task SchedulePlatformReminderAsync(TimeSpan timeOfDay);
}
