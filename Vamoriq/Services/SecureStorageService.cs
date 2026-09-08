using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services;

public class SecureStorageService : ISecureStorageService
{
    public async Task SetAsync(string key, string? value)
    {
        if (value == null)
        {
            await SecureStorage.Default.SetAsync(key, null);
        }
        else
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        return await SecureStorage.Default.GetAsync(key);
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch
        {

            await SecureStorage.Default.SetAsync(key, string.Empty);
        }
    }
}
