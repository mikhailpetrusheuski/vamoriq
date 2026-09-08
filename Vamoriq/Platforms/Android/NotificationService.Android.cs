using Android;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace Vamoriq.Services;

public partial class NotificationService
{
    private const string ChannelId = "vamoriq_daily_mission";
    private const int ReminderNotificationId = 1001;
    private const int AlarmRequestCode = 2001;

    private partial Task<bool> RequestPlatformPermissionAsync()
    {
        EnsureNotificationChannel();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var status = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                Platform.CurrentActivity ?? Platform.AppContext,
                Manifest.Permission.PostNotifications);
            if (status != Android.Content.PM.Permission.Granted)
            {
                Platform.CurrentActivity?.RequestPermissions(
                    new[] { Manifest.Permission.PostNotifications }, 0);
                return Task.FromResult(false);
            }
        }
        return Task.FromResult(true);
    }

    private partial Task SchedulePlatformReminderAsync(TimeSpan timeOfDay)
    {
        EnsureNotificationChannel();

        var context = Platform.AppContext;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager is null) return Task.CompletedTask;

        var intent = new Intent(context, typeof(NotificationReceiver));
        intent.SetAction("com.vamoriq.DAILY_MISSION_REMINDER");
        var pendingIntent = PendingIntent.GetBroadcast(
            context, AlarmRequestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        if (pendingIntent is null) return Task.CompletedTask;

        var now = Java.Util.Calendar.Instance!;
        var trigger = Java.Util.Calendar.Instance!;
        trigger.Set(Java.Util.CalendarField.HourOfDay, timeOfDay.Hours);
        trigger.Set(Java.Util.CalendarField.Minute, timeOfDay.Minutes);
        trigger.Set(Java.Util.CalendarField.Second, 0);

        if (trigger.Before(now))
            trigger.Add(Java.Util.CalendarField.DayOfMonth, 1);

        alarmManager.SetInexactRepeating(
            AlarmType.RtcWakeup,
            trigger.TimeInMillis,
            AlarmManager.IntervalDay,
            pendingIntent);

        return Task.CompletedTask;
    }

    partial void CancelPlatformReminder()
    {
        var context = Platform.AppContext;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        var intent = new Intent(context, typeof(NotificationReceiver));
        intent.SetAction("com.vamoriq.DAILY_MISSION_REMINDER");
        var pendingIntent = PendingIntent.GetBroadcast(
            context, AlarmRequestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        if (pendingIntent is not null)
            alarmManager?.Cancel(pendingIntent);
    }

    private static void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channel = new NotificationChannel(
            ChannelId,
            "Daily Mission",
            NotificationImportance.Default)
        {
            Description = "Daily mission reminders"
        };

        var notificationManager = (NotificationManager?)Platform.AppContext
            .GetSystemService(Context.NotificationService);
        notificationManager?.CreateNotificationChannel(channel);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { "com.vamoriq.DAILY_MISSION_REMINDER" })]
public class NotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null) return;

        var tapIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
        tapIntent?.PutExtra("from_notification", true);
        tapIntent?.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

        var pendingIntent = PendingIntent.GetActivity(
            context, 0, tapIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notification = new NotificationCompat.Builder(context, "vamoriq_daily_mission")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle("Vamoriq")
            .SetContentText(Vamoriq.Resources.Localization.AppResources.Mission_NotificationText)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent)
            .SetPriority(NotificationCompat.PriorityDefault)
            .Build();

        var notificationManager = NotificationManagerCompat.From(context);
        notificationManager.Notify(1001, notification);
    }
}
