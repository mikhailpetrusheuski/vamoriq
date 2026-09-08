using System.Collections.ObjectModel;
using Vamoriq.Core.ViewModels;
using Vamoriq.Models;
using Vamoriq.Resources.Localization;
using Vamoriq.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Vamoriq.ViewModels
{
    public partial class ProgressViewModel : BaseViewModel
    {
        private readonly IMissionService _missionService;
        private readonly IStreakService _streakService;
        private readonly ICuratedMissionLibrary _curatedLibrary;

        [ObservableProperty]
        private int _currentStreak;

        [ObservableProperty]
        private int _longestStreak;

        [ObservableProperty]
        private int _totalCompleted;

        [ObservableProperty]
        private ObservableCollection<Mission> _completedMissions = new();

        public ProgressViewModel(
            IMissionService missionService,
            IStreakService streakService,
            ICuratedMissionLibrary curatedLibrary,
            ILogger<ProgressViewModel> logger) : base(logger)
        {
            _missionService = missionService ?? throw new ArgumentNullException(nameof(missionService));
            _streakService = streakService ?? throw new ArgumentNullException(nameof(streakService));
            _curatedLibrary = curatedLibrary ?? throw new ArgumentNullException(nameof(curatedLibrary));
            Title = AppResources.Progress_Title;
        }

        public override async Task OnAppearingAsync()
        {
            await ExecuteAsync(async () =>
            {
                var today = DateTime.Now.Date.ToString("yyyy-MM-dd");

                CurrentStreak = await _streakService.GetCurrentStreakAsync(today);
                LongestStreak = await _streakService.GetLongestStreakAsync();
                TotalCompleted = await _streakService.GetTotalCompletedAsync();

                var missions = await _missionService.GetCompletedMissionsAsync(0, 100);
                foreach (var m in missions)
                    await _curatedLibrary.LocalizeMissionAsync(m);
                CompletedMissions = new ObservableCollection<Mission>(missions);
            }, AppResources.Error);
        }

        [RelayCommand]
        private async Task ViewMission(string missionId)
        {
            await ExecuteAsync(async () =>
            {
                var mission = CompletedMissions.FirstOrDefault(m => m.Id == missionId);
                if (mission != null)
                {
                    MissionViewModel.CurrentMission = mission;
                    await Shell.Current.GoToAsync($"MissionDetail?missionId={missionId}");
                }
            }, AppResources.Error);
        }

        [RelayCommand]
        protected override async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
