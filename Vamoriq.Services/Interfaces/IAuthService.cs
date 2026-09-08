using Vamoriq.Models;

namespace Vamoriq.Services.Interfaces;

public interface IAuthService
{
    Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<AuthResult> HandleKeycloakCallbackAsync(string code, string state, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(CancellationToken cancellationToken = default);
    Task<string?> GetStoredTokenAsync();
}

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }
}
