using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Vamoriq.Services;

public class ConnectivityService : IConnectivityService, IDisposable
{
    private readonly ILogger<ConnectivityService> _logger;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public event EventHandler<Vamoriq.Services.Interfaces.ConnectivityChangedEventArgs>? ConnectivityChanged;

    public ConnectivityService(ILogger<ConnectivityService> logger)
    {
        _logger = logger;
        _isConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, Microsoft.Maui.Networking.ConnectivityChangedEventArgs e)
    {
        var wasConnected = _isConnected;
        _isConnected = e.NetworkAccess == NetworkAccess.Internet;

        if (wasConnected != _isConnected)
        {
            _logger.LogInformation("Connectivity changed: {Status}", _isConnected ? "Connected" : "Disconnected");

            ConnectivityChanged?.Invoke(this, new Vamoriq.Services.Interfaces.ConnectivityChangedEventArgs
            {
                IsConnected = _isConnected
            });
        }
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            var current = Connectivity.Current.NetworkAccess;
            _isConnected = current == NetworkAccess.Internet;

            if (_isConnected)
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                try
                {
                    var response = await httpClient.GetAsync("https://www.google.com");
                    _isConnected = response.IsSuccessStatusCode;
                }
                catch
                {
                    _isConnected = false;
                }
            }

            return _isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connectivity");
            return false;
        }
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }
}
