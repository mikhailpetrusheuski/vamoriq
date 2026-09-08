using Vamoriq.Services.Interfaces;

namespace Vamoriq.Services
{
    public class PreferencesService : IPreferencesService
    {
        public string Get(string key, string defaultValue = null)
        {
            return Preferences.Get(key, defaultValue);
        }

        public bool Get(string key, bool defaultValue = false)
        {
            return Preferences.Get(key, defaultValue);
        }

        public int Get(string key, int defaultValue = 0)
        {
            return Preferences.Get(key, defaultValue);
        }

        public double Get(string key, double defaultValue = 0.0)
        {
            return Preferences.Get(key, defaultValue);
        }

        public async Task SetAsync(string key, string value)
        {
            Preferences.Set(key, value);
            await Task.CompletedTask;
        }

        public async Task SetAsync(string key, bool value)
        {
            Preferences.Set(key, value);
            await Task.CompletedTask;
        }

        public async Task SetAsync(string key, int value)
        {
            Preferences.Set(key, value);
            await Task.CompletedTask;
        }

        public async Task SetAsync(string key, double value)
        {
            Preferences.Set(key, value);
            await Task.CompletedTask;
        }

        public void Remove(string key)
        {
            Preferences.Remove(key);
        }

        public void Clear()
        {
            Preferences.Clear();
        }

        public bool ContainsKey(string key)
        {
            return Preferences.ContainsKey(key);
        }
    }
}
