using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services;

public class LocalPreferencesAdapter : ILocalPreferences
{
    private readonly IPreferencesService _preferences;

    public LocalPreferencesAdapter(IPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return _preferences.Get(key, defaultValue);
    }

    public Task SetIntAsync(string key, int value)
    {
        return _preferences.SetAsync(key, value);
    }
}
