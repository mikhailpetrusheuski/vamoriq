using Vamoriq.Services.Interfaces;
using Vamoriq.Services.Services;
using Vamoriq.ViewModels;
using Vamoriq.Views;
using Vamoriq.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vamoriq.Services.Providers;
using Microsoft.Maui.Handlers;

#if ANDROID
using Android.Views;
#endif

namespace Vamoriq;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {

        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();

#if ANDROID

        WebViewHandler.Mapper.AppendToMapping("WebViewSoftwareRenderingWorkaround", (handler, view) =>
        {
            try
            {
                handler?.PlatformView?.SetLayerType(LayerType.Software, null);
            }
            catch
            {

            }
        });
#endif

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddEnvironmentVariables();

        try
        {
            using var pkgStream = FileSystem.OpenAppPackageFileAsync("config/appsettings.json").GetAwaiter().GetResult();
            if (pkgStream != null)
            {
                var memory = new MemoryStream();
                pkgStream.CopyTo(memory);
                memory.Position = 0;
                configBuilder.AddJsonStream(memory);
            }
        }
        catch {  }

        try
        {
            var overridePath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");
            if (File.Exists(overridePath))
            {
                configBuilder.AddJsonFile(overridePath, optional: true, reloadOnChange: false);
            }
        }
        catch { }

        var configuration = configBuilder.Build();
        builder.Services.AddSingleton<IConfiguration>(configuration);

        builder.Services.AddLogging(logging =>
        {

            var logLevel = configuration.GetValue<string>("Logging:MinimumLevel") ?? "Information";
            var minimumLevel = Enum.TryParse<LogLevel>(logLevel, true, out var level) ? level : LogLevel.Information;

            logging.SetMinimumLevel(minimumLevel);

            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
            logging.AddFilter("System", LogLevel.Warning);
            logging.AddFilter("Vamoriq", LogLevel.Debug);
            logging.AddFilter("Vamoriq.Services", LogLevel.Debug);
            logging.AddFilter("Vamoriq.ViewModels", LogLevel.Information);

            logging.AddDebug();
            logging.AddConsole();

            var enableFileLogging = configuration.GetValue<bool>("Logging:EnableFileLogging", false);
            if (enableFileLogging)
            {
                var logPath = configuration.GetValue<string>("Logging:LogPath") ?? "logs/app.log";
                var retainDays = configuration.GetValue<int>("Logging:RetainLogsDays", 7);

                logging.AddFilter("FileLogger", LogLevel.Information);
            }
        });

        builder.Services.AddHttpClient();

        builder.Services.AddHttpClient("api-gateway")
            .AddPolicyHandler((sp, req) => PollyPolicies.GetResiliencePolicy(sp));

        builder.Services.AddSingleton<ICacheService, CacheService>();
        builder.Services.AddSingleton<IConsentProvider, ConsentProvider>();
        builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
        builder.Services.AddSingleton<IPromptProvider, PromptProvider>();

        builder.Services.AddSingleton<IThemeManager, Vamoriq.Services.ThemeManager>();

        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ILocalPreferences, LocalPreferencesAdapter>();

        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IDeviceInfoService, DeviceInfoService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

        builder.Services.AddSingleton<IKeycloakService, KeycloakService>();
        builder.Services.AddSingleton<ITokenRefreshService, TokenRefreshService>();

        builder.Services.AddSingleton<IGraphQLService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var authService = sp.GetRequiredService<IAuthService>();
            var logger = sp.GetRequiredService<ILogger<GraphQLService>>();
            return new GraphQLService(httpClientFactory.CreateClient("api-gateway"), configuration, logger, authService);
        });

        builder.Services.AddSingleton<IMissionService, MissionService>();
        builder.Services.AddSingleton<IStreakService, StreakService>();
        builder.Services.AddSingleton<ICuratedMissionLibrary, CuratedMissionLibrary>();
        builder.Services.AddSingleton<IMissionAIService, MissionAIService>();
        builder.Services.AddSingleton<IPhotoStorageService>(sp =>
            new PhotoStorageService(
                sp.GetRequiredService<ILogger<PhotoStorageService>>(),
                FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<Vamoriq.Services.Interfaces.INotificationService, Vamoriq.Services.NotificationService>();

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<AuthViewModel>(sp => new AuthViewModel(
            sp.GetRequiredService<IKeycloakService>(),
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ILogger<AuthViewModel>>()
        ));
        builder.Services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ILogger<SettingsViewModel>>(),
            sp.GetService<IPreferencesService>(),
            sp.GetService<IThemeManager>(),
            sp.GetService<ILocalizationService>(),
            sp.GetService<Vamoriq.Services.Interfaces.INotificationService>()
        ));
        builder.Services.AddTransient<LegalConsentViewModel>(sp => new LegalConsentViewModel(
            sp.GetRequiredService<IPreferencesService>(),
            sp.GetRequiredService<ILogger<LegalConsentViewModel>>(),
            sp.GetRequiredService<ILocalizationService>()
        ));

        builder.Services.AddTransient<MissionViewModel>(sp => new MissionViewModel(
            sp.GetRequiredService<IMissionService>(),
            sp.GetRequiredService<IMissionAIService>(),
            sp.GetRequiredService<ICuratedMissionLibrary>(),
            sp.GetRequiredService<IStreakService>(),
            sp.GetRequiredService<IAnalyticsService>(),
            sp.GetRequiredService<IPreferencesService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<Vamoriq.Services.Interfaces.INotificationService>(),
            sp.GetRequiredService<ILogger<MissionViewModel>>()
        ));
        builder.Services.AddTransient<MissionDetailViewModel>(sp => new MissionDetailViewModel(
            sp.GetRequiredService<IMissionService>(),
            sp.GetRequiredService<IStreakService>(),
            sp.GetRequiredService<IPhotoStorageService>(),
            sp.GetRequiredService<IAnalyticsService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<ICuratedMissionLibrary>(),
            sp.GetRequiredService<ILogger<MissionDetailViewModel>>()
        ));
        builder.Services.AddTransient<ProgressViewModel>(sp => new ProgressViewModel(
            sp.GetRequiredService<IMissionService>(),
            sp.GetRequiredService<IStreakService>(),
            sp.GetRequiredService<ICuratedMissionLibrary>(),
            sp.GetRequiredService<ILogger<ProgressViewModel>>()
        ));

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<LegalConsentPage>();
        builder.Services.AddTransient<MissionPage>();
        builder.Services.AddTransient<MissionDetailPage>();
        builder.Services.AddTransient<ProgressPage>();

        var app = builder.Build();

        try
        {
            var loggerFactory = app.Services.GetService<ILoggerFactory>();
            var startupLogger = loggerFactory?.CreateLogger("Startup");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    startupLogger?.LogCritical(ex, "UnhandledException (AppDomain) terminating={Terminating}", e.IsTerminating);
                    Console.WriteLine($"UnhandledException: {ex}");
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try
                {
                    startupLogger?.LogCritical(e.Exception, "UnobservedTaskException");
                    Console.WriteLine($"UnobservedTaskException: {e.Exception}");
                    e.SetObserved();
                }
                catch { }
            };

#if ANDROID
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                try
                {
                    startupLogger?.LogCritical(e.Exception, "Android UnhandledExceptionRaiser");
                    Console.WriteLine($"AndroidUnhandled: {e.Exception}");
                }
                catch { }
            };
#endif
        }
        catch { }

        return app;
    }
}
