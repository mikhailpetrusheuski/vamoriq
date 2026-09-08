using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services;

public class DeviceInfoService : IDeviceInfoService
{
    private readonly IPreferencesService _preferences;
    private string? _cachedDeviceId;

    public DeviceInfoService(IPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public async Task<string> GetDeviceIdAsync()
    {

        if (!string.IsNullOrEmpty(_cachedDeviceId))
        {
            return _cachedDeviceId;
        }

        var storedDeviceId = _preferences.Get("device_id", string.Empty);
        if (!string.IsNullOrEmpty(storedDeviceId))
        {
            _cachedDeviceId = storedDeviceId;
            return storedDeviceId;
        }

        string deviceId;
        try
        {

            var platformDeviceId = DeviceInfo.Current?.Idiom.ToString() + DeviceInfo.Current?.Model;
            if (!string.IsNullOrEmpty(platformDeviceId))
            {

                using var sha = System.Security.Cryptography.SHA256.Create();
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(platformDeviceId));
                deviceId = Convert.ToHexString(hash)[..16];
            }
            else
            {

                deviceId = Guid.NewGuid().ToString("N")[..16];
            }
        }
        catch
        {

            deviceId = Guid.NewGuid().ToString("N")[..16];
        }

        await _preferences.SetAsync("device_id", deviceId);
        _cachedDeviceId = deviceId;

        return deviceId;
    }

    public string GetPlatform()
    {
        try
        {
            return DeviceInfo.Current?.Platform.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    public string GetDeviceModel()
    {
        try
        {
            return DeviceInfo.Current?.Model ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
