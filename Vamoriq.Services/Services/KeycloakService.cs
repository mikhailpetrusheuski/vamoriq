using Vamoriq.Models;
using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Vamoriq.Services.Utilities;

namespace Vamoriq.Services.Services;

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakService> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly KeycloakConfig _config;
    private string? _currentCodeVerifier;

    public KeycloakService(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakService> logger, ILocalizationService localizationService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _localizationService = localizationService;

        var authority = _configuration["Keycloak:Authority"];
        var clientId = _configuration["Keycloak:ClientId"];
        var clientSecret = _configuration["Keycloak:ClientSecret"];
        var redirectUri = _configuration["Keycloak:RedirectUri"];
        var scope = _configuration["Keycloak:Scope"];
        var responseType = _configuration["Keycloak:ResponseType"];

        _logger.LogInformation("Raw configuration values: Authority={Authority}, ClientId={ClientId}, RedirectUri={RedirectUri}",
            authority, clientId, redirectUri);

        _config = new KeycloakConfig
        {
            Authority = authority ?? "https://auth.example.com/realms/vamoriq",
            ClientId = clientId ?? "vamoriq",
            ClientSecret = clientSecret ?? "",
            RedirectUri = redirectUri ?? "vamoriq://callback",
            Scope = scope ?? "openid profile email offline_access",
            ResponseType = responseType ?? "code"
        };

        _logger.LogInformation("KeycloakService initialized with config: Authority={Authority}, ClientId={ClientId}, RedirectUri={RedirectUri}",
            _config.Authority, _config.ClientId, _config.RedirectUri);

        if (string.IsNullOrEmpty(_config.Authority))
        {
            _logger.LogError("Keycloak Authority is not configured!");
        }

        if (string.IsNullOrEmpty(_config.ClientId))
        {
            _logger.LogError("Keycloak ClientId is not configured!");
        }

        if (string.IsNullOrEmpty(_config.RedirectUri))
        {
            _logger.LogError("Keycloak RedirectUri is not configured!");
        }
    }

    private string GetBaseUrl()
    {

        var uri = new Uri(_config.Authority);
        return $"{uri.Scheme}://{uri.Host}";
    }

    public string GetAuthorizationUrl(string? idpHint = null)
    {
        try
        {
            _logger.LogInformation("Generating authorization URL with config: Authority={Authority}, ClientId={ClientId}, RedirectUri={RedirectUri}",
                _config.Authority, _config.ClientId, _config.RedirectUri);

            if (string.IsNullOrEmpty(_config.Authority))
            {
                throw new InvalidOperationException("Keycloak Authority is not configured");
            }

            if (string.IsNullOrEmpty(_config.ClientId))
            {
                throw new InvalidOperationException("Keycloak ClientId is not configured");
            }

            if (string.IsNullOrEmpty(_config.RedirectUri))
            {
                throw new InvalidOperationException("Keycloak RedirectUri is not configured");
            }

            var authUrl = $"{_config.Authority}/protocol/openid-connect/auth";
            _logger.LogInformation("Keycloak authorization base URL: {AuthUrl}", authUrl);

            var state = GenerateRandomState();
            var nonce = GenerateRandomNonce();

            var codeVerifier = GenerateRandomCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            _currentCodeVerifier = codeVerifier;

            var currentCulture = _localizationService.CurrentCulture;
            var languageCode = currentCulture.TwoLetterISOLanguageName;

            _logger.LogInformation("Setting Keycloak UI language to: {Language}", languageCode);

            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["response_type"] = _config.ResponseType,
                ["scope"] = _config.Scope,
                ["redirect_uri"] = _config.RedirectUri,
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["ui_locales"] = languageCode
            };

            if (!string.IsNullOrWhiteSpace(idpHint))
            {
                queryParams["kc_idp_hint"] = idpHint.Trim();
            }
            else
            {

                queryParams["kc_idp_hint"] = "google";
            }

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            authUrl = $"{authUrl}?{queryString}";

            _logger.LogInformation("Generated authorization URL: {Url}", authUrl);

            return authUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization URL");
            throw;
        }
    }

    public string GetRedirectUri()
    {
        return _config.RedirectUri;
    }

    public async Task<AuthResult> HandleCallbackAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Handling Keycloak callback with code: {Code}", code);

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _config.RedirectUri,
                ["code_verifier"] = _currentCodeVerifier ?? ""
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var tokenUrl = $"{GetBaseUrl()}/realms/vamoriq/protocol/openid-connect/token";
            _logger.LogInformation("Token endpoint URL: {Url}", tokenUrl);
            var response = await _httpClient.PostAsync(tokenUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonConvert.DeserializeObject<KeycloakTokenResponse>(responseContent);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {

                    var user = CreateUserFromAccessToken(tokenResponse.AccessToken);

                    _currentCodeVerifier = null;

                    return new AuthResult
                    {
                        IsSuccess = true,
                        Token = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken,
                        User = user,
                        ErrorMessage = null
                    };
                }
            }

            _logger.LogError("Failed to exchange code for token. Status: {Status}", response.StatusCode);
            return new AuthResult { IsSuccess = false, ErrorMessage = "Failed to exchange authorization code for token" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Keycloak callback");
            return new AuthResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(tokenRequest);
            var tokenUrl = $"{GetBaseUrl()}/realms/vamoriq/protocol/openid-connect/token";
            _logger.LogInformation("Token endpoint URL: {Url}", tokenUrl);
            var response = await _httpClient.PostAsync(tokenUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonConvert.DeserializeObject<KeycloakTokenResponse>(responseContent);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {

                    var user = CreateUserFromAccessToken(tokenResponse.AccessToken);

                    return new AuthResult
                    {
                        IsSuccess = true,
                        Token = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken,
                        User = user,
                        ErrorMessage = null
                    };
                }
            }

            return new AuthResult { IsSuccess = false, ErrorMessage = "Failed to refresh token" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private static User CreateUserFromAccessToken(string accessToken)
    {
        return new User
        {
            Id = JwtUtility.GetUserId(accessToken) ?? Guid.NewGuid().ToString(),
            Email = JwtUtility.GetUserEmail(accessToken) ?? string.Empty,
            Username = JwtUtility.GetUserName(accessToken) ?? "Unknown",
            FirstName = JwtUtility.GetFirstName(accessToken) ?? string.Empty,
            LastName = JwtUtility.GetLastName(accessToken) ?? string.Empty,
            IsEmailVerified = JwtUtility.GetEmailVerified(accessToken) ?? false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var logoutRequest = new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(logoutRequest);
            var logoutUrl = $"{GetBaseUrl()}/realms/vamoriq/protocol/openid-connect/logout";
            _logger.LogInformation("Logout endpoint URL: {Url}", logoutUrl);
            var response = await _httpClient.PostAsync(logoutUrl, content, cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    public async Task<User?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var userInfoUrl = $"{GetBaseUrl()}/realms/vamoriq/protocol/openid-connect/userinfo";
            _logger.LogInformation("UserInfo endpoint URL: {Url}", userInfoUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var userInfo = JsonConvert.DeserializeObject<KeycloakUserInfo>(content);

                if (userInfo != null)
                {
                    return new User
                    {
                        Id = userInfo.Sub,
                        Username = userInfo.PreferredUsername ?? userInfo.Name,
                        Email = userInfo.Email,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName,
                        IsEmailVerified = userInfo.EmailVerified ?? false,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info from Keycloak");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_config.ClientSecret))
            {
                var introspectUrl = $"{GetBaseUrl()}/realms/vamoriq/protocol/openid-connect/token/introspect";
                var tokenRequest = new Dictionary<string, string>
                {
                    ["token"] = accessToken,
                    ["client_id"] = _config.ClientId,
                    ["client_secret"] = _config.ClientSecret
                };

                var content = new FormUrlEncodedContent(tokenRequest);
                var response = await _httpClient.PostAsync(introspectUrl, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var payload = JsonConvert.DeserializeObject<KeycloakIntrospectionResponse>(responseContent);
                return payload?.Active == true;
            }

            var userInfoUrl = $"{GetBaseUrl()}/realms/vamoriq/protocol/openid-connect/userinfo";
            var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await _httpClient.SendAsync(request, cancellationToken);
            return userInfoResponse.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    private string GenerateRandomState()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateRandomNonce()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateRandomCodeVerifier()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var challengeBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(challengeBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }

    private string ExtractRealmFromAuthority(string authority)
    {
        try
        {

            var uri = new Uri(authority);
            var segments = uri.Segments;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i].TrimEnd('/') == "realms")
                {
                    return segments[i + 1].TrimEnd('/');
                }
            }

            return "vamoriq";
        }
        catch
        {
            return "vamoriq";
        }
    }
}

public class KeycloakTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonProperty("scope")]
    public string Scope { get; set; } = string.Empty;
}

public class KeycloakUserInfo
{
    [JsonProperty("sub")]
    public string Sub { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("given_name")]
    public string GivenName { get; set; } = string.Empty;

    [JsonProperty("family_name")]
    public string FamilyName { get; set; } = string.Empty;

    [JsonProperty("preferred_username")]
    public string PreferredUsername { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("email_verified")]
    public bool? EmailVerified { get; set; }
}

public class KeycloakIntrospectionResponse
{
    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("exp")]
    public long? Exp { get; set; }
}
