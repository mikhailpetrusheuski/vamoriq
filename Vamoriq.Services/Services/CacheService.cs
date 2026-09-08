using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Vamoriq.Services.Services;

public class CacheService : ICacheService, IDisposable
{
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, CacheItem> _cache;
    private readonly Timer _cleanupTimer;
    private readonly object _statsLock = new();

    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly int _maxCacheSize = 10000;
    private readonly long _maxMemoryUsage = 100 * 1024 * 1024;

    private int _hitCount;
    private int _missCount;
    private bool _disposed;

    public CacheService(ILogger<CacheService> logger)
    {
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CacheItem>();

        _cleanupTimer = new Timer(CleanupExpiredItems, null, _cleanupInterval, _cleanupInterval);
        _logger.LogInformation("Cache service initialized with cleanup interval {Interval}", _cleanupInterval);
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
            return null;

        try
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                if (cacheItem.ExpiresAt > DateTime.UtcNow)
                {
                    cacheItem.LastAccessed = DateTime.UtcNow;

                    Interlocked.Increment(ref _hitCount);

                    var result = JsonConvert.DeserializeObject<T>(cacheItem.Value);
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return result;
                }
                else
                {
                    _cache.TryRemove(key, out _);
                    _logger.LogDebug("Cache item expired for key: {Key}", key);
                }
            }

            Interlocked.Increment(ref _missCount);
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from cache for key: {Key}", key);
            Interlocked.Increment(ref _missCount);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return;

        try
        {
            var exp = expiration ?? _defaultExpiration;
            var expiresAt = DateTime.UtcNow.Add(exp);

            var serializedValue = JsonConvert.SerializeObject(value);
            var size = System.Text.Encoding.UTF8.GetByteCount(serializedValue);

            var cacheItem = new CacheItem
            {
                Value = serializedValue,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                Size = size,
                Type = typeof(T).Name
            };

            if (await CheckCacheLimitsAsync(size))
            {
                _cache.AddOrUpdate(key, cacheItem, (k, oldItem) => cacheItem);
                _logger.LogDebug("Cached item for key: {Key}, expires at: {ExpiresAt}, size: {Size} bytes",
                    key, expiresAt, size);
            }
            else
            {
                _logger.LogWarning("Cache limit exceeded, item not cached for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        try
        {
            if (_cache.TryRemove(key, out var removedItem))
            {
                _logger.LogDebug("Removed item from cache for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cache for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                if (cacheItem.ExpiresAt > DateTime.UtcNow)
                {
                    return true;
                }
                else
                {

                _cache.TryRemove(key, out _);
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in cache for key: {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var count = _cache.Count;
            _cache.Clear();

            lock (_statsLock)
            {
                _hitCount = 0;
                _missCount = 0;
            }

            _logger.LogInformation("Cleared entire cache, removed {Count} items", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    public async Task ClearByPatternAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return;

        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = _cache.Keys.Where(key => regex.IsMatch(key)).ToList();

            int removedCount = 0;
            foreach (var key in keysToRemove)
            {
                if (_cache.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            _logger.LogInformation("Cleared cache by pattern '{Pattern}', removed {Count} items", pattern, removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class
    {
        var cachedItem = await GetAsync<T>(key);
        if (cachedItem != null)
        {
            return cachedItem;
        }

        try
        {
            var item = await getItem();
            if (item != null)
            {
                await SetAsync(key, item, expiration);
            }
            return item!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSet for key: {Key}", key);
            throw;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var totalSize = _cache.Values.Sum(item => item.Size);

            lock (_statsLock)
            {
                return new CacheStatistics
                {
                    TotalKeys = _cache.Count,
                    MemoryUsage = totalSize,
                    HitCount = _hitCount,
                    MissCount = _missCount,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    public async Task RefreshAsync(string key, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(key))
            return;

        try
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                var exp = expiration ?? _defaultExpiration;
                cacheItem.ExpiresAt = DateTime.UtcNow.Add(exp);
                cacheItem.LastAccessed = DateTime.UtcNow;

                _logger.LogDebug("Refreshed cache item for key: {Key}, new expiration: {ExpiresAt}",
                    key, cacheItem.ExpiresAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache item for key: {Key}", key);
        }
    }

    #region Private Methods

    private async Task<bool> CheckCacheLimitsAsync(int newItemSize)
    {
        if (_cache.Count >= _maxCacheSize)
        {
            await EvictLeastRecentlyUsedAsync();
        }

        var currentMemoryUsage = _cache.Values.Sum(item => item.Size);
        if (currentMemoryUsage + newItemSize > _maxMemoryUsage)
        {
            await EvictLeastRecentlyUsedAsync();
            return _cache.Values.Sum(item => item.Size) + newItemSize <= _maxMemoryUsage;
        }

        return true;
    }

    private async Task EvictLeastRecentlyUsedAsync()
    {
        try
        {
            var itemsToEvict = _cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(Math.Max(1, _cache.Count / 10))
                .ToList();

            int evictedCount = 0;
            foreach (var item in itemsToEvict)
            {
                if (_cache.TryRemove(item.Key, out _))
                {
                    evictedCount++;
                }
            }

            _logger.LogInformation("Evicted {Count} least recently used cache items", evictedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache eviction");
        }
    }

    private void CleanupExpiredItems(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            int removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogDebug("Cleanup removed {Count} expired cache items", removedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _cache?.Clear();
            _disposed = true;

            _logger.LogInformation("Cache service disposed");
        }
    }

    #endregion

    #region Nested Classes

    private class CacheItem
    {
        public string Value { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public int Size { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    #endregion
}
