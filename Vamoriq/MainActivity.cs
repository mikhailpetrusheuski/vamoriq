#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Vamoriq;

[Activity(
    Label = "Vamoriq",
    MainLauncher = true,
    Theme = "@style/Maui.SplashTheme",
    LaunchMode = LaunchMode.SingleTop,
    Exported = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleNotificationIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleNotificationIntent(intent);
    }

    private static void HandleNotificationIntent(Intent? intent)
    {
        if (intent?.GetBooleanExtra("from_notification", false) == true)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500);
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("///Mission");

                var notifService = IPlatformApplication.Current?.Services
                    .GetService<Vamoriq.Services.Interfaces.INotificationService>();
                if (notifService is not null)
                    await notifService.TrackNotificationOpenedAsync();
            });
        }
    }
}
#endif
