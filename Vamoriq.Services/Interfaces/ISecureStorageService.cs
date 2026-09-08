namespace Vamoriq.Services.Interfaces;

public interface ISecureStorageService
{
    Task SetAsync(string key, string? value);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
}
