using Vamoriq.Extensions;
using Vamoriq.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Vamoriq.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    private readonly IKeycloakService _keycloakService;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthViewModel> _logger;
    private readonly LocalizationResourceManager _localizer = LocalizationResourceManager.Instance;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _loadingMessage = string.Empty;

    [ObservableProperty]
    private double _loadingProgress = 0.0;

    public AuthViewModel(IKeycloakService keycloakService, IAuthService authService, ILogger<AuthViewModel> logger)
    {
        _keycloakService = keycloakService;
        _authService = authService;
        _logger = logger;

        _logger.LogInformation("AuthViewModel constructor called");

        IsLoading = true;
        HasError = false;
        LoadingMessage = L("Auth_LoadingOpenBrowser");
    }

    private bool _authInProgress;

    private string L(string key) => _localizer[key];

    public bool IsReady => !IsLoading && !HasError;

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsReady));
    }

    partial void OnHasErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(IsReady));
    }

    [RelayCommand]
    private async Task StartAuthentication()
    {
        await StartAuthenticationAsync(null);
    }

    public async Task StartAuthenticationAsync(string? idpHint)
    {
        try
        {
            if (_authInProgress)
                return;

            _authInProgress = true;

            _logger.LogInformation("Starting system browser authentication (idpHint={IdpHint})...", idpHint ?? "(default)");

            IsLoading = true;
            HasError = false;
            LoadingProgress = 0.0;
            LoadingMessage = L("Auth_LoadingOpenBrowser");

            LoadingProgress = 0.4;
            var resolvedIdp = string.IsNullOrWhiteSpace(idpHint) ? null : idpHint.Trim();
            var startUrl = _keycloakService.GetAuthorizationUrl(resolvedIdp);
            var redirectUri = _keycloakService.GetRedirectUri();

            if (string.IsNullOrWhiteSpace(startUrl))
                throw new InvalidOperationException("Authorization URL is empty.");
            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new InvalidOperationException("Redirect URI is empty.");

            var result = await WebAuthenticator.Default.AuthenticateAsync(new Uri(startUrl), new Uri(redirectUri));

            if (result?.Properties == null)
                throw new InvalidOperationException("Authentication returned no result.");

            if (result.Properties.TryGetValue("error", out var error) && !string.IsNullOrWhiteSpace(error))
            {
                HasError = true;
                ErrorMessage = string.Format(L("Auth_Error_Provider"), error);
                return;
            }

            if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
            {
                HasError = true;
                ErrorMessage = L("Auth_Error_NoCode");
                return;
            }

            result.Properties.TryGetValue("state", out var state);

            LoadingProgress = 0.8;
            LoadingMessage = L("Auth_LoadingCompleting");

            await CompleteAuthenticationAsync(code, state);
        }
        catch (TaskCanceledException)
        {
            HasError = true;
            ErrorMessage = L("Auth_Error_Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed: {Message}", ex.Message);
            HasError = true;
            ErrorMessage = L("Auth_Error_Generic");
        }
        finally
        {
            IsLoading = false;
            _authInProgress = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("///Welcome");
    }

    [RelayCommand]
    private async Task Retry()
    {
        await StartAuthenticationAsync(null);
    }

    private async Task CompleteAuthenticationAsync(string code, string? state)
    {
        try
        {
            _logger.LogInformation("Received authorization code from Keycloak");

            var result = await _authService.HandleKeycloakCallbackAsync(code, state);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Keycloak authentication successful");

                try
                {
                    var tokenRefreshService = Application.Current?.Handler?.MauiContext?.Services
                        .GetService<ITokenRefreshService>();
                    if (tokenRefreshService != null && !tokenRefreshService.IsRunning)
                    {
                        await tokenRefreshService.StartBackgroundRefreshAsync();
                        _logger.LogInformation("Background token refresh started after authentication");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start background token refresh");
                }

                try
                {
                    var settingsVm = Application.Current?.Handler?.MauiContext?.Services?.GetService<SettingsViewModel>();
                    if (settingsVm != null)
                    {
                        await settingsVm.RefreshAsync();
                    }
                }
                catch { }

                await Shell.Current.GoToAsync("///Mission");
            }
            else
            {
                _logger.LogError("Keycloak authentication failed: {Error}", result.ErrorMessage);
                HasError = true;
                ErrorMessage = L("Auth_Error_Generic");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Keycloak callback");
            HasError = true;
            ErrorMessage = L("Auth_Error_Processing");
        }
        finally
        {

        }
    }
}
