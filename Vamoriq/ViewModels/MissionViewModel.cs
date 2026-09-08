using Vamoriq.Core.ViewModels;
using Vamoriq.Models;
using Vamoriq.Resources.Localization;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vamoriq.ViewModels
{
    public enum MissionDayState { Loading, Overdue, GetMission, Active, Completed }

    public partial class MissionViewModel : BaseViewModel
    {

        public static Mission? CurrentMission { get; set; }

        private readonly IMissionService _missionService;
        private readonly IMissionAIService _missionAIService;
        private readonly ICuratedMissionLibrary _curatedLibrary;
        private readonly IStreakService _streakService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IPreferencesService _preferencesService;
        private readonly INavigationService _navigationService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowGetMissionButton))]
        [NotifyPropertyChangedFor(nameof(ShowActiveState))]
        [NotifyPropertyChangedFor(nameof(ShowCompletedState))]
        [NotifyPropertyChangedFor(nameof(ShowOverdueState))]
        [NotifyPropertyChangedFor(nameof(ShowLoading))]
        [NotifyPropertyChangedFor(nameof(ShowStreak))]
        private MissionDayState _dayState = MissionDayState.Loading;

        [ObservableProperty]
        private Mission? _todayMission;

        [ObservableProperty]
        private Mission? _overdueMission;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowStreak))]
        [NotifyPropertyChangedFor(nameof(StreakText))]
        private int _currentStreak;

        [ObservableProperty]
        private int _totalCompleted;

        private string? _lastShownMissionDate;

        [ObservableProperty]
        private bool _isGenerating;

        public bool ShowGetMissionButton => DayState == MissionDayState.GetMission;
        public bool ShowActiveState => DayState == MissionDayState.Active;
        public bool ShowCompletedState => DayState == MissionDayState.Completed;
        public bool ShowOverdueState => DayState == MissionDayState.Overdue;
        public bool ShowLoading => DayState == MissionDayState.Loading;
        public bool ShowStreak => CurrentStreak > 0 &&
            (DayState == MissionDayState.Active || DayState == MissionDayState.GetMission);
        public string StreakText => string.Format(AppResources.Mission_StreakDays, CurrentStreak);

        public MissionViewModel(
            IMissionService missionService,
            IMissionAIService missionAIService,
            ICuratedMissionLibrary curatedLibrary,
            IStreakService streakService,
            IAnalyticsService analyticsService,
            IPreferencesService preferencesService,
            INavigationService navigationService,
            INotificationService notificationService,
            ILogger<MissionViewModel> logger) : base(logger)
        {
            _missionService = missionService ?? throw new ArgumentNullException(nameof(missionService));
            _missionAIService = missionAIService ?? throw new ArgumentNullException(nameof(missionAIService));
            _curatedLibrary = curatedLibrary ?? throw new ArgumentNullException(nameof(curatedLibrary));
            _streakService = streakService ?? throw new ArgumentNullException(nameof(streakService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            Title = AppResources.Mission_Title;
        }

        public override async Task OnAppearingAsync()
        {
            await ExecuteAsync(async () =>
            {
                DayState = MissionDayState.Loading;
                var today = DateTime.Now.Date.ToString("yyyy-MM-dd");

                var expired = await _missionService.AutoExpireOldMissionsAsync(today);
                if (expired > 0)
                {
                    await _analyticsService.TrackEventAsync("streak_broken",
                        new Dictionary<string, object> { ["expired_count"] = expired });
                }

                var todayMission = await _missionService.GetMissionByLocalDateAsync(today);
                if (todayMission is not null)
                    await _curatedLibrary.LocalizeMissionAsync(todayMission);

                if (todayMission is not null)
                {
                    if (todayMission.Status == MissionStatus.Assigned)
                    {
                        TodayMission = todayMission;
                        CurrentMission = todayMission;
                        DayState = MissionDayState.Active;
                        var shownKey = $"{todayMission.Id}_{today}";
                        if (_lastShownMissionDate != shownKey)
                        {
                            _lastShownMissionDate = shownKey;
                            await _analyticsService.TrackEventAsync("mission_shown",
                                new Dictionary<string, object> { ["mission_id"] = todayMission.Id });
                        }
                    }
                    else if (todayMission.Status == MissionStatus.Completed)
                    {
                        TodayMission = todayMission;
                        CurrentMission = todayMission;
                        DayState = MissionDayState.Completed;
                    }
                    else
                    {

                        DayState = MissionDayState.GetMission;
                    }
                }
                else
                {
                    var overdue = await _missionService.GetOverdueMissionAsync(today);
                    if (overdue is not null)
                        await _curatedLibrary.LocalizeMissionAsync(overdue);
                    if (overdue is not null)
                    {
                        OverdueMission = overdue;
                        CurrentMission = overdue;
                        DayState = MissionDayState.Overdue;
                        var shownKey = $"{overdue.Id}_{today}";
                        if (_lastShownMissionDate != shownKey)
                        {
                            _lastShownMissionDate = shownKey;
                            await _analyticsService.TrackEventAsync("mission_shown",
                                new Dictionary<string, object> { ["mission_id"] = overdue.Id, ["is_overdue"] = true });
                        }
                    }
                    else
                    {
                        DayState = MissionDayState.GetMission;
                    }
                }

                CurrentStreak = await _streakService.GetCurrentStreakAsync(today);
                TotalCompleted = await _streakService.GetTotalCompletedAsync();

                await EnsureNotificationsSetupAsync();
            }, AppResources.Error);
        }

        private async Task EnsureNotificationsSetupAsync()
        {
            try
            {
                var permRequested = _preferencesService.Get("notification_permission_requested", false);
                if (!permRequested)
                {
                    await _notificationService.RequestPermissionAsync();
                    await _preferencesService.SetAsync("notification_permission_requested", true);
                }

                var savedMinutes = _preferencesService.Get("notification_time_minutes", 540);
                await _notificationService.ScheduleDailyReminderAsync(TimeSpan.FromMinutes(savedMinutes));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Notification setup failed");
            }
        }

        [RelayCommand]
        private async Task GetMissionAsync()
        {
            await ExecuteAsync(async () =>
            {
                IsGenerating = true;
                try
                {
                    var completedCuratedIds = await _missionService.GetCompletedCuratedIdsAsync();
                    var dayNumber = TotalCompleted + 1;

                    var curated = await _curatedLibrary.GetNextMissionAsync(completedCuratedIds, dayNumber);
                    if (curated is null)
                    {
                        SetError(AppResources.Mission_NoMissionsAvailable);
                        await _analyticsService.TrackEventAsync("mission_generation_failed",
                            new Dictionary<string, object> { ["error_type"] = "no_candidates" });
                        return;
                    }

                    await _analyticsService.TrackEventAsync("mission_generation_started",
                        new Dictionary<string, object>
                        {
                            ["day_number"] = dayNumber,
                            ["curated_id"] = curated.Id
                        });

                    var city = _preferencesService.Get("user_city", "");
                    var result = await _missionAIService.PersonalizeMissionAsync(curated, city, dayNumber);

                    result.Mission.MissionLocalDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
                    var saved = await _missionService.SaveMissionAsync(result.Mission);

                    TodayMission = saved;
                    CurrentMission = saved;
                    DayState = MissionDayState.Active;

                    await _analyticsService.TrackEventAsync(
                        result.WasPersonalized ? "ai_personalization_used" : "ai_fallback_used",
                        new Dictionary<string, object>
                        {
                            ["mission_id"] = saved.Id,
                            ["curated_id"] = saved.CuratedId
                        });

                    await _analyticsService.TrackEventAsync("mission_shown",
                        new Dictionary<string, object> { ["mission_id"] = saved.Id });
                }
                finally
                {
                    IsGenerating = false;
                }
            }, AppResources.Error);
        }

        [RelayCommand]
        private async Task SkipOverdueAsync()
        {
            await ExecuteAsync(async () =>
            {
                if (OverdueMission is null) return;

                var reason = await PromptSkipReasonAsync();
                if (reason is null) return;

                await _missionService.SkipMissionAsync(OverdueMission.Id, reason.Value);
                await _analyticsService.TrackEventAsync("streak_broken",
                    new Dictionary<string, object>
                    {
                        ["reason"] = "skipped_overdue",
                        ["skip_reason"] = reason.Value.ToString()
                    });

                OverdueMission = null;
                DayState = MissionDayState.GetMission;
            }, AppResources.Error);
        }

        private static async Task<SkipReason?> PromptSkipReasonAsync()
        {
            var action = await Shell.Current.DisplayActionSheet(
                AppResources.Mission_SkipReason,
                AppResources.Cancel,
                null,
                AppResources.Mission_SkipTooBusy,
                AppResources.Mission_SkipNotInterested,
                AppResources.Mission_SkipNotSafe,
                AppResources.Mission_SkipTechnical,
                AppResources.Mission_SkipOther);

            if (action == AppResources.Mission_SkipTooBusy) return SkipReason.TooBusy;
            if (action == AppResources.Mission_SkipNotInterested) return SkipReason.NotInterested;
            if (action == AppResources.Mission_SkipNotSafe) return SkipReason.NotSafeFeeling;
            if (action == AppResources.Mission_SkipTechnical) return SkipReason.TechnicalIssue;
            if (action == AppResources.Mission_SkipOther) return SkipReason.Other;
            return null;
        }

        [RelayCommand]
        private async Task ViewMissionAsync()
        {
            var mission = TodayMission ?? OverdueMission;
            if (mission is null) return;

            CurrentMission = mission;
            await _navigationService.NavigateToAsync("MissionDetail",
                new Dictionary<string, object> { ["missionId"] = mission.Id });
        }

        [RelayCommand]
        private async Task FinishOverdueAsync()
        {
            if (OverdueMission is null) return;

            CurrentMission = OverdueMission;
            await _navigationService.NavigateToAsync("MissionDetail",
                new Dictionary<string, object> { ["missionId"] = OverdueMission.Id });
        }

        [RelayCommand]
        protected override async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("//Mission");
        }
    }
}
