using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Vamoriq.Resources.Localization;
using Vamoriq.Services;
using Microsoft.Extensions.Logging;

namespace Vamoriq.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<MainViewModel>? _logger;
        private string _title = AppResources.AppName;
        private bool _isBusy = false;

        public MainViewModel()
        {
            ContinueWithGoogleCommand = new Command(async () => await OnContinueWithGoogle());
            ContinueWithAppleCommand = new Command(async () => await OnContinueWithApple());
            TryDemoCommand = new Command(async () => await OnTryDemo());
        }

        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger;
            ContinueWithGoogleCommand = new Command(async () => await OnContinueWithGoogle());
            ContinueWithAppleCommand = new Command(async () => await OnContinueWithApple());
            TryDemoCommand = new Command(async () => await OnTryDemo());
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ContinueWithGoogleCommand { get; }
        public ICommand ContinueWithAppleCommand { get; }
        public ICommand TryDemoCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task OnContinueWithGoogle()
        {
            IsBusy = true;
            try
            {
                _logger?.LogInformation("User clicked CONTINUE WITH GOOGLE, navigating to Auth page");

                if (Shell.Current == null)
                    throw new InvalidOperationException("Shell.Current is null");

                AuthNavigationContext.SetNextIdpHint("google");
                await Shell.Current.GoToAsync("///Auth");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to navigate to Auth page: {Message}", ex.Message);
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error, ex.Message, AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnContinueWithApple()
        {
            IsBusy = true;
            try
            {
                _logger?.LogInformation("User clicked CONTINUE WITH APPLE, navigating to Auth page");

                if (Shell.Current == null)
                    throw new InvalidOperationException("Shell.Current is null");

                AuthNavigationContext.SetNextIdpHint("apple");
                await Shell.Current.GoToAsync("///Auth");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to navigate to Auth page (Apple): {Message}", ex.Message);
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error, ex.Message, AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnTryDemo()
        {
            IsBusy = true;
            try
            {
                _logger?.LogInformation("User clicked TRY DEMO, navigating to Mission page");

                if (Shell.Current == null)
                    throw new InvalidOperationException("Shell.Current is null");

                await Shell.Current.GoToAsync("///Mission");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to navigate to Mission page: {Message}", ex.Message);
                await Application.Current.MainPage.DisplayAlert(
                    AppResources.Error, ex.Message, AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
