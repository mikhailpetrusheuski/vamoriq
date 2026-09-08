namespace Vamoriq.Services.Interfaces;

public interface IDeviceInfoService
{

    Task<string> GetDeviceIdAsync();

    string GetPlatform();

    string GetDeviceModel();
}
