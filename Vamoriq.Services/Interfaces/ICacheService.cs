namespace Vamoriq.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;

        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        Task RemoveAsync(string key);

        Task<bool> ExistsAsync(string key);

        Task ClearAsync();

        Task ClearByPatternAsync(string pattern);

        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;

        Task<CacheStatistics> GetStatisticsAsync();

        Task RefreshAsync(string key, TimeSpan? expiration = null);
    }

    public class CacheStatistics
    {
        public int TotalKeys { get; set; }
        public long MemoryUsage { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public int TotalRequests => HitCount + MissCount;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
