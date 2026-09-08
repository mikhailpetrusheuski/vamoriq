using Vamoriq.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Vamoriq.ViewModels;

public partial class ConnectivityViewModel : ObservableObject, IDisposable
{
    private readonly IConnectivityService _connectivityService;
    private readonly ILogger<ConnectivityViewModel> _logger;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Checking...";

    [ObservableProperty]
    private bool _showOfflineMessage;

    public ConnectivityViewModel(IConnectivityService connectivityService, ILogger<ConnectivityViewModel> logger)
    {
        _connectivityService = connectivityService;
        _logger = logger;

        IsConnected = _connectivityService.IsConnected;
        UpdateConnectionStatus();

        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, Vamoriq.Services.Interfaces.ConnectivityChangedEventArgs e)
    {
        IsConnected = e.IsConnected;
        UpdateConnectionStatus();

        if (!IsConnected)
        {
            ShowOfflineMessage = true;
            _logger.LogInformation("App went offline");
        }
        else
        {
            ShowOfflineMessage = false;
            _logger.LogInformation("App came back online");
        }
    }

    private void UpdateConnectionStatus()
    {
        ConnectionStatus = IsConnected ? "Online" : "Offline";
    }

    public void Dispose()
    {
        _connectivityService.ConnectivityChanged -= OnConnectivityChanged;
    }
}
