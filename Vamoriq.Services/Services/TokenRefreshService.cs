using Vamoriq.Services.Interfaces;
using Vamoriq.Services.Utilities;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services.Services;

public class TokenRefreshService : ITokenRefreshService
{
    private readonly IAuthService _authService;
    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<TokenRefreshService> _logger;
    private Timer? _refreshTimer;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(10);

    public bool IsRunning => _refreshTimer != null;

    public event EventHandler<TokenRefreshEventArgs>? TokenRefreshed;
    public event EventHandler<TokenRefreshEventArgs>? TokenRefreshFailed;

    public TokenRefreshService(
        IAuthService authService,
        ISecureStorageService secureStorage,
        ILogger<TokenRefreshService> logger)
    {
        _authService = authService;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    public async Task StartBackgroundRefreshAsync()
    {
        try
        {
            if (_refreshTimer != null)
            {
                _logger.LogInformation("Background token refresh is already running");
                return;
            }

            _logger.LogInformation("Starting background token refresh service");

            _refreshTimer = new Timer(
                callback: async _ => await RefreshTokenIfNeededAsync(),
                state: null,
                dueTime: TimeSpan.Zero,
                period: _refreshInterval
            );

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting background token refresh");
        }
    }

    public async Task StopBackgroundRefreshAsync()
    {
        try
        {
            if (_refreshTimer != null)
            {
                _logger.LogInformation("Stopping background token refresh service");

                await _refreshTimer.DisposeAsync();
                _refreshTimer = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping background token refresh");
        }
    }

    private async Task RefreshTokenIfNeededAsync()
    {
        try
        {
            var token = await _secureStorage.GetAsync("access_token");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("No access token found, skipping refresh");
                return;
            }

            if (JwtUtility.IsTokenExpiringWithin(token, _refreshThreshold))
            {
                _logger.LogInformation("Token is expiring within threshold, attempting refresh");

                var result = await _authService.RefreshTokenAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Background token refresh successful");
                    TokenRefreshed?.Invoke(this, new TokenRefreshEventArgs { IsSuccess = true });
                }
                else
                {
                    _logger.LogWarning("Background token refresh failed: {Error}", result.ErrorMessage);
                    TokenRefreshFailed?.Invoke(this, new TokenRefreshEventArgs
                    {
                        IsSuccess = false,
                        ErrorMessage = result.ErrorMessage
                    });
                }
            }
            else
            {
                _logger.LogDebug("Token is still valid, no refresh needed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during background token refresh");
            TokenRefreshFailed?.Invoke(this, new TokenRefreshEventArgs
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
