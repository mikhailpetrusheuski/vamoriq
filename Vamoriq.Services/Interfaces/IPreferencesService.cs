namespace Vamoriq.Services.Interfaces
{

    public interface IPreferencesService
    {

        string Get(string key, string defaultValue = null);

        bool Get(string key, bool defaultValue = false);

        int Get(string key, int defaultValue = 0);

        double Get(string key, double defaultValue = 0.0);

        Task SetAsync(string key, string value);

        Task SetAsync(string key, bool value);

        Task SetAsync(string key, int value);

        Task SetAsync(string key, double value);

        void Remove(string key);

        void Clear();

        bool ContainsKey(string key);
    }
}
