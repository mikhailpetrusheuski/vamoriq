namespace Vamoriq.Services.Interfaces;

public interface ILocalPreferences
{
    int GetInt(string key, int defaultValue = 0);
    Task SetIntAsync(string key, int value);
}
