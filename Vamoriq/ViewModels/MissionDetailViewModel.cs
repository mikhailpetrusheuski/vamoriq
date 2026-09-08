using Vamoriq.Core.ViewModels;
using Vamoriq.Models;
using Vamoriq.Resources.Localization;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vamoriq.ViewModels
{
    [QueryProperty(nameof(MissionId), "missionId")]
    public partial class MissionDetailViewModel : BaseViewModel
    {
        private readonly IMissionService _missionService;
        private readonly IStreakService _streakService;
        private readonly IPhotoStorageService _photoStorageService;
        private readonly IAnalyticsService _analyticsService;
        private readonly INavigationService _navigationService;
        private readonly ICuratedMissionLibrary _curatedLibrary;

        [ObservableProperty]
        private string _missionId = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowProofSection))]
        [NotifyPropertyChangedFor(nameof(CanComplete))]
        [NotifyPropertyChangedFor(nameof(InstructionItems))]
        [NotifyCanExecuteChangedFor(nameof(CompleteMissionCommand))]
        private Mission? _mission;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanComplete))]
        [NotifyPropertyChangedFor(nameof(ProofCharCount))]
        [NotifyCanExecuteChangedFor(nameof(CompleteMissionCommand))]
        private string _proofText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProofPhoto))]
        private string? _proofPhotoPath;

        [ObservableProperty]
        private bool _showCelebration;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CelebrationStreakText))]
        private int _celebrationStreak;

        public bool HasProofPhoto => !string.IsNullOrEmpty(ProofPhotoPath);
        public bool ShowProofSection => Mission?.Status == MissionStatus.Assigned;
        public bool CanComplete => Mission?.Status == MissionStatus.Assigned && (ProofText?.Length ?? 0) >= 10;
        public string AttachPhotoText => $"📷 {AppResources.Mission_AttachPhoto}";
        public string ProofCharCount => $"{ProofText?.Length ?? 0}/10";
        public string CelebrationStreakText => string.Format(AppResources.Mission_StreakDays, CelebrationStreak);

        public List<InstructionItem> InstructionItems =>
            Mission?.Instructions?.Select((text, i) => new InstructionItem { Index = i + 1, Text = text }).ToList()
            ?? new();

        public class InstructionItem
        {
            public int Index { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private static readonly HashSet<string> _tappedMissionIds = new();

        public MissionDetailViewModel(
            IMissionService missionService,
            IStreakService streakService,
            IPhotoStorageService photoStorageService,
            IAnalyticsService analyticsService,
            INavigationService navigationService,
            ICuratedMissionLibrary curatedLibrary,
            ILogger<MissionDetailViewModel> logger) : base(logger)
        {
            _missionService = missionService ?? throw new ArgumentNullException(nameof(missionService));
            _streakService = streakService ?? throw new ArgumentNullException(nameof(streakService));
            _photoStorageService = photoStorageService ?? throw new ArgumentNullException(nameof(photoStorageService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _curatedLibrary = curatedLibrary ?? throw new ArgumentNullException(nameof(curatedLibrary));
            Title = AppResources.Mission_Title;
        }

        partial void OnMissionIdChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = LoadMissionAsync(value);
            }
        }

        private async Task LoadMissionAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            await ExecuteAsync(async () =>
            {

                var mission = MissionViewModel.CurrentMission;
                if (mission is null || mission.Id != id)
                {

                    var today = DateTime.Now.Date.ToString("yyyy-MM-dd");
                    mission = await _missionService.GetMissionByLocalDateAsync(today);

                    if (mission is null || mission.Id != id)
                    {
                        mission = await _missionService.GetOverdueMissionAsync(today);
                    }
                }

                if (mission is not null && mission.Id == id)
                {
                    await _curatedLibrary.LocalizeMissionAsync(mission);
                    Mission = mission;
                    Title = mission.Title;
                    if (_tappedMissionIds.Add(id))
                    {
                        await _analyticsService.TrackEventAsync("mission_tapped",
                            new Dictionary<string, object> { ["mission_id"] = id });
                    }
                }
                else
                {
                    Logger.LogWarning("MissionDetail: Could not resolve mission {MissionId}", id);
                    SetError(AppResources.Error);
                }
            }, AppResources.Error);
        }

        [RelayCommand]
        private async Task AttachPhotoAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _analyticsService.TrackEventAsync("proof_attach_started");

                try
                {
                    var takePhotoOption = AppResources.Mission_TakePhoto;
                    var chooseGalleryOption = AppResources.Mission_ChooseFromGallery;

                    var action = await Shell.Current.DisplayActionSheet(
                        AppResources.Mission_AddPhoto,
                        AppResources.Cancel,
                        null,
                        takePhotoOption,
                        chooseGalleryOption);

                    FileResult? photo = null;
                    if (action == takePhotoOption)
                    {
                        photo = await MediaPicker.Default.CapturePhotoAsync(
                            new MediaPickerOptions { Title = takePhotoOption });
                    }
                    else if (action == chooseGalleryOption)
                    {
                        photo = await MediaPicker.Default.PickPhotoAsync(
                            new MediaPickerOptions { Title = chooseGalleryOption });
                    }

                    if (photo is null) return;

                    var savedPath = await _photoStorageService.SaveProofPhotoAsync(
                        MissionId, photo.FullPath);

                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        ProofPhotoPath = savedPath;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Photo attachment failed for mission {MissionId}", MissionId);
                    await _analyticsService.TrackEventAsync("proof_attach_failed",
                        new Dictionary<string, object> { ["error"] = ex.Message });
                }
            }, AppResources.Error);
        }

        [RelayCommand(CanExecute = nameof(CanComplete))]
        private async Task CompleteMissionAsync()
        {
            await ExecuteAsync(async () =>
            {
                if (Mission is null || ProofText.Length < 10) return;

                var proof = new Proof
                {
                    Id = Guid.NewGuid().ToString(),
                    MissionId = MissionId,
                    TextContent = ProofText,
                    PhotoPath = ProofPhotoPath,
                    CompletedAtUtc = DateTime.UtcNow
                };

                var success = await _missionService.CompleteMissionAsync(MissionId, proof);
                if (!success)
                {
                    SetError(AppResources.Error);
                    return;
                }

                await _analyticsService.TrackEventAsync("mission_completed",
                    new Dictionary<string, object>
                    {
                        ["mission_id"] = MissionId,
                        ["has_photo"] = HasProofPhoto
                    });

                var today = DateTime.Now.Date.ToString("yyyy-MM-dd");
                var streak = await _streakService.GetCurrentStreakAsync(today);

                if (streak > 1)
                {
                    await _analyticsService.TrackEventAsync("streak_continued",
                        new Dictionary<string, object> { ["streak"] = streak });
                }

                CelebrationStreak = streak;
                ShowCelebration = true;

                Mission.Status = MissionStatus.Completed;
                OnPropertyChanged(nameof(Mission));
                OnPropertyChanged(nameof(ShowProofSection));
                OnPropertyChanged(nameof(CanComplete));
                CompleteMissionCommand.NotifyCanExecuteChanged();

                await Task.Delay(2000);
                await Shell.Current.GoToAsync("///Mission");
            }, AppResources.Error);
        }

        [RelayCommand]
        private async Task ShareMissionAsync()
        {
            if (Mission is null) return;

            await ExecuteAsync(async () =>
            {
                var shareText = $"{Mission.Title}\n\n{Mission.Description}";
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Title = Mission.Title,
                    Text = shareText
                });
            }, AppResources.Error);
        }

        [RelayCommand]
        private async Task SeeProgressAsync()
        {
            await Shell.Current.GoToAsync("//Progress");
        }

        [RelayCommand]
        protected override async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
