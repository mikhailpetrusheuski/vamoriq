namespace Vamoriq.Services.Interfaces;

public interface ITokenRefreshService
{
    Task StartBackgroundRefreshAsync();
    Task StopBackgroundRefreshAsync();
    bool IsRunning { get; }
    event EventHandler<TokenRefreshEventArgs>? TokenRefreshed;
    event EventHandler<TokenRefreshEventArgs>? TokenRefreshFailed;
}

public class TokenRefreshEventArgs : EventArgs
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime RefreshTime { get; set; } = DateTime.UtcNow;
}
