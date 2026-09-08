using Vamoriq.ViewModels;

namespace Vamoriq.Views;

public partial class SettingsPage : ContentPage
{
    private SettingsViewModel? _currentViewModel;

    public SettingsPage()
    {
        InitializeComponent();

        this.Loaded += OnPageLoaded;

        _currentViewModel = BindingContext as SettingsViewModel;
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("SettingsPage.OnPageLoaded called");

        if (_currentViewModel == null)
        {
            if (BindingContext is SettingsViewModel vm)
            {
                _currentViewModel = vm;
                System.Diagnostics.Debug.WriteLine("✅ SettingsViewModel initialized from BindingContext in OnPageLoaded");
            }
            else if (Handler?.MauiContext?.Services != null)
            {
                _currentViewModel = Handler.MauiContext.Services.GetService<SettingsViewModel>();
                if (_currentViewModel != null)
                {
                    BindingContext = _currentViewModel;
                    System.Diagnostics.Debug.WriteLine("✅ SettingsViewModel resolved from DI in OnPageLoaded");
                }
            }
        }

        if (_currentViewModel != null)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsPage: Calling RefreshAsync for ViewModel with Title={_currentViewModel.Title}");
            await _currentViewModel.RefreshAsync();
            System.Diagnostics.Debug.WriteLine($"SettingsPage: RefreshAsync completed. IsLoggedIn={_currentViewModel.IsLoggedIn}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("⚠️ SettingsViewModel is still null in OnPageLoaded!");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("SettingsPage.OnAppearing called");

        if (_currentViewModel != null)
        {
            await _currentViewModel.RefreshAsync();
            System.Diagnostics.Debug.WriteLine($"SettingsPage.OnAppearing: RefreshAsync completed. IsLoggedIn={_currentViewModel.IsLoggedIn}");
        }
    }
}
