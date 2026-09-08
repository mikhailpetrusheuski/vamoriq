using Vamoriq.Models;

namespace Vamoriq.Services.Interfaces;

public interface IKeycloakService
{

    string GetAuthorizationUrl(string? idpHint = null);

    string GetRedirectUri();

    Task<AuthResult> HandleCallbackAsync(string code, string state, CancellationToken cancellationToken = default);

    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<User?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<bool> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}

public class KeycloakConfig
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid profile email";
    public string ResponseType { get; set; } = "code";
}
