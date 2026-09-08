using Foundation;
using UserNotifications;

namespace Vamoriq.Services;

public partial class NotificationService
{
    private const string DailyReminderIdentifier = "vamoriq_daily_mission";

    private async partial Task<bool> RequestPlatformPermissionAsync()
    {
        var center = UNUserNotificationCenter.Current;
        var (granted, _) = await center.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
        return granted;
    }

    private partial Task SchedulePlatformReminderAsync(TimeSpan timeOfDay)
    {
        var center = UNUserNotificationCenter.Current;

        var content = new UNMutableNotificationContent
        {
            Title = "Vamoriq",
            Body = Vamoriq.Resources.Localization.AppResources.Mission_NotificationText,
            Sound = UNNotificationSound.Default
        };

        var dateComponents = new NSDateComponents
        {
            Hour = timeOfDay.Hours,
            Minute = timeOfDay.Minutes
        };

        var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, repeats: true);
        var request = UNNotificationRequest.FromIdentifier(DailyReminderIdentifier, content, trigger);

        center.AddNotificationRequest(request, null);
        return Task.CompletedTask;
    }

    partial void CancelPlatformReminder()
    {
        var center = UNUserNotificationCenter.Current;
        center.RemovePendingNotificationRequests(new[] { DailyReminderIdentifier });
    }
}
