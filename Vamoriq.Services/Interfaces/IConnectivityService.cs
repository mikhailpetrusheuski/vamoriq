namespace Vamoriq.Services.Interfaces;

public interface IConnectivityService
{
    bool IsConnected { get; }
    event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
}

public class ConnectivityChangedEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
