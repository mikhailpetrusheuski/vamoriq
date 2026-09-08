using Vamoriq.Models;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Vamoriq.Services.Utilities;

namespace Vamoriq.Services.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly StructuredLogger _structuredLogger;
    private readonly IKeycloakService _keycloakService;
    private readonly ISecureStorageService _secureStorage;
    private readonly IConnectivityService _connectivityService;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private readonly TimeSpan _minimumRefreshBackoff = TimeSpan.FromSeconds(60);
    private DateTimeOffset _lastRefreshUtc = DateTimeOffset.MinValue;

    private readonly SemaphoreSlim _serverValidationSemaphore = new(1, 1);
    private readonly TimeSpan _serverValidationInterval = TimeSpan.FromMinutes(5);
    private DateTimeOffset _lastServerValidationUtc = DateTimeOffset.MinValue;
    private string? _lastServerValidatedToken;

    public AuthService(
        ILogger<AuthService> logger,
        IKeycloakService keycloakService,
        ISecureStorageService secureStorage,
        IConnectivityService connectivityService)
    {
        _logger = logger;
        _structuredLogger = new StructuredLogger(_logger, "AuthService");
        _keycloakService = keycloakService;
        _secureStorage = secureStorage;
        _connectivityService = connectivityService;

        _structuredLogger.LogInformation("AuthService initialized successfully");
    }

    public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await GetStoredRefreshTokenAsync();
            bool remoteOk = true;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    remoteOk = await _keycloakService.LogoutAsync(refreshToken, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Remote logout failed; proceeding with local sign-out");
                    remoteOk = false;
                }
            }

            await RemoveStoredTokensAsync();
            await RemoveStoredUserAsync();

            _logger.LogInformation("Logout completed - user is now signed out");

            return remoteOk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            try
            {
                await RemoveStoredTokensAsync();
                await RemoveStoredUserAsync();
            }
            catch { }
            return false;
        }
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetStoredTokenAsync();
            if (string.IsNullOrEmpty(token))
            {

                var refreshToken = await GetStoredRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshResult = await RefreshTokenAsync(cancellationToken);
                    return refreshResult.IsSuccess;
                }
                return false;
            }

            if (JwtUtility.IsTokenExpired(token))
            {
                if (!_connectivityService.IsConnected)
                {
                    _logger.LogWarning("Token is expired and no internet connection available");
                    return false;
                }
                _logger.LogInformation("Token is expired, attempting to refresh");
                var refreshResult = await RefreshTokenAsync(cancellationToken);
                if (!refreshResult.IsSuccess)
                {
                    await RemoveStoredTokensAsync();
                    await RemoveStoredUserAsync();
                }
                return refreshResult.IsSuccess;
            }

            if (_connectivityService.IsConnected)
            {

                if (JwtUtility.IsTokenExpiringWithin(token, TimeSpan.FromMinutes(5)))
                {
                    _logger.LogInformation("Token is expiring soon, refreshing proactively");
                    var refreshResult = await RefreshTokenAsync(cancellationToken);
                    if (!refreshResult.IsSuccess)
                    {
                        await RemoveStoredTokensAsync();
                        await RemoveStoredUserAsync();
                        return false;
                    }

                    token = refreshResult.Token ?? await GetStoredTokenAsync();
                    _lastServerValidationUtc = DateTimeOffset.UtcNow;
                    _lastServerValidatedToken = token;
                }

                if (ShouldValidateTokenWithServer(token))
                {
                    await _serverValidationSemaphore.WaitAsync(cancellationToken);
                    try
                    {

                        if (ShouldValidateTokenWithServer(token))
                        {
                            var isServerValid = await _keycloakService.ValidateTokenAsync(token, cancellationToken);
                            _lastServerValidationUtc = DateTimeOffset.UtcNow;
                            _lastServerValidatedToken = token;

                            if (!isServerValid)
                            {
                                _logger.LogWarning("Access token rejected by server. Trying to refresh");
                                var refreshResult = await RefreshTokenAsync(cancellationToken);
                                if (!refreshResult.IsSuccess)
                                {
                                    await RemoveStoredTokensAsync();
                                    await RemoveStoredUserAsync();
                                    return false;
                                }
                            }
                        }
                    }
                    finally
                    {
                        _serverValidationSemaphore.Release();
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication");
            return false;
        }
    }

    private bool ShouldValidateTokenWithServer(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (!string.Equals(_lastServerValidatedToken, token, StringComparison.Ordinal))
        {
            return true;
        }

        return now - _lastServerValidationUtc >= _serverValidationInterval;
    }

    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {

            var cachedUser = await GetStoredUserAsync();
            if (cachedUser != null)
            {
                return cachedUser;
            }

            var token = await GetStoredTokenAsync();
            if (string.IsNullOrEmpty(token))
                return null;

            if (!JwtUtility.IsTokenExpired(token))
            {
                var userFromJwt = new User
                {
                    Id = JwtUtility.GetUserId(token) ?? Guid.NewGuid().ToString(),
                    Email = JwtUtility.GetUserEmail(token) ?? "unknown@email.com",
                    Username = JwtUtility.GetUserName(token) ?? "Unknown",
                    FirstName = JwtUtility.GetFirstName(token) ?? string.Empty,
                    LastName = JwtUtility.GetLastName(token) ?? string.Empty,
                    IsEmailVerified = JwtUtility.GetEmailVerified(token) ?? false
                };

                await StoreUserAsync(userFromJwt);
                return userFromJwt;
            }

            if (!_connectivityService.IsConnected)
            {
                return null;
            }

            var refresh = await RefreshTokenAsync(cancellationToken);
            if (!refresh.IsSuccess || string.IsNullOrWhiteSpace(refresh.Token))
            {
                return null;
            }

            if (refresh.User != null)
            {
                await StoreUserAsync(refresh.User);
                return refresh.User;
            }

            var refreshedUser = new User
            {
                Id = JwtUtility.GetUserId(refresh.Token) ?? Guid.NewGuid().ToString(),
                Email = JwtUtility.GetUserEmail(refresh.Token) ?? "unknown@email.com",
                Username = JwtUtility.GetUserName(refresh.Token) ?? "Unknown",
                FirstName = JwtUtility.GetFirstName(refresh.Token) ?? string.Empty,
                LastName = JwtUtility.GetLastName(refresh.Token) ?? string.Empty,
                IsEmailVerified = JwtUtility.GetEmailVerified(refresh.Token) ?? false
            };
            await StoreUserAsync(refreshedUser);
            return refreshedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    public async Task<AuthResult> HandleKeycloakCallbackAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _keycloakService.HandleCallbackAsync(code, state, cancellationToken);

            if (result.IsSuccess && result.Token != null)
            {
                await StoreTokenAsync(result.Token);

                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    await StoreRefreshTokenAsync(result.RefreshToken);
                }

                if (result.User != null)
                {
                    await StoreUserAsync(result.User);
                }

                _logger.LogInformation("Keycloak authentication completed successfully");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Keycloak callback");
            return new AuthResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _refreshSemaphore.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastRefreshUtc < _minimumRefreshBackoff)
            {
                _logger.LogDebug("Skipping token refresh: last refresh {LastRefresh} (<{Backoff})", _lastRefreshUtc, _minimumRefreshBackoff);
                return new AuthResult
                {
                    IsSuccess = true,
                    Token = await GetStoredTokenAsync(),
                    RefreshToken = await GetStoredRefreshTokenAsync(),
                    User = await GetStoredUserAsync()
                };
            }

            var refreshToken = await GetStoredRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthResult { IsSuccess = false, ErrorMessage = "No refresh token available" };
            }

            var result = await _keycloakService.RefreshTokenAsync(refreshToken, cancellationToken);

            if (result.IsSuccess && result.Token != null)
            {
                await StoreTokenAsync(result.Token);

                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    await StoreRefreshTokenAsync(result.RefreshToken);
                }

                if (result.User != null)
                {
                    await StoreUserAsync(result.User);
                }

                _logger.LogInformation("Token refreshed successfully");
                _lastRefreshUtc = now;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    private async Task StoreTokenAsync(string token)
    {
        try
        {
            await _secureStorage.SetAsync("access_token", token);

            try
            {
                var exp = JwtUtility.GetExpiryUnixSeconds(token);
                if (exp.HasValue)
                {
                    await _secureStorage.SetAsync("access_token_exp", exp.Value.ToString());
                }
            }
            catch { }
            _logger.LogInformation("Access token stored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing access token");
        }
    }

    public async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _secureStorage.GetAsync("access_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return null;
        }
    }

    private async Task StoreRefreshTokenAsync(string refreshToken)
    {
        try
        {
            await _secureStorage.SetAsync("refresh_token", refreshToken);
            _logger.LogInformation("Refresh token stored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token");
        }
    }

    private async Task<string?> GetStoredRefreshTokenAsync()
    {
        try
        {
            return await _secureStorage.GetAsync("refresh_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh token");
            return null;
        }
    }

    private async Task RemoveStoredTokensAsync()
    {
        try
        {
            await _secureStorage.RemoveAsync("access_token");
            await _secureStorage.RemoveAsync("refresh_token");
            _logger.LogInformation("Tokens removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tokens");
        }
    }

    private async Task RemoveStoredUserAsync()
    {
        try
        {
            await _secureStorage.RemoveAsync("user_data");
            _logger.LogInformation("User data removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user data");
        }
    }

    private async Task StoreUserAsync(User user)
    {
        try
        {
            var userJson = JsonConvert.SerializeObject(user);
            await _secureStorage.SetAsync("user_data", userJson);
            _logger.LogInformation("User data stored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing user data");
        }
    }

    private async Task<User?> GetStoredUserAsync()
    {
        try
        {
            var userJson = await _secureStorage.GetAsync("user_data");
            if (string.IsNullOrEmpty(userJson))
                return null;

            return JsonConvert.DeserializeObject<User>(userJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user data");
            return null;
        }
    }

}
