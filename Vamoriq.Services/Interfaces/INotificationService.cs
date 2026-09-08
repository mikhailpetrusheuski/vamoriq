namespace Vamoriq.Services.Interfaces;

public interface INotificationService
{
    Task RequestPermissionAsync();
    Task ScheduleDailyReminderAsync(TimeSpan timeOfDay);
    Task CancelDailyReminderAsync();
    Task TrackNotificationOpenedAsync();
    bool IsPermissionGranted { get; }
}
