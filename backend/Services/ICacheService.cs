using Microsoft.Extensions.Caching.Distributed;

namespace PredictLottoNZ.Services;

/// <summary>
/// Interface for multi-level caching service with memory and distributed (Redis) caching
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached value or execute factory function and cache the result
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both);
    
    /// <summary>
    /// Get cached value
    /// </summary>
    Task<T?> GetAsync<T>(string key, CacheLevel level = CacheLevel.Both);
    
    /// <summary>
    /// Set cached value
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both);
    
    /// <summary>
    /// Remove cached value
    /// </summary>
    Task RemoveAsync(string key, CacheLevel level = CacheLevel.Both);
    
    /// <summary>
    /// Remove cached values by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CacheLevel level = CacheLevel.Both);
    
    /// <summary>
    /// Warm up cache with frequently accessed data
    /// </summary>
    Task WarmUpCacheAsync();
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();
    
    /// <summary>
    /// Clear all cache entries
    /// </summary>
    Task ClearAllAsync(CacheLevel level = CacheLevel.Both);
}

/// <summary>
/// Cache level enumeration
/// </summary>
public enum CacheLevel
{
    /// <summary>
    /// Memory cache only (L1)
    /// </summary>
    Memory,
    
    /// <summary>
    /// Distributed cache only (L2 - Redis)
    /// </summary>
    Distributed,
    
    /// <summary>
    /// Both memory and distributed cache
    /// </summary>
    Both
}

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStatistics
{
    public long MemoryCacheHits { get; set; }
    public long MemoryCacheMisses { get; set; }
    public long DistributedCacheHits { get; set; }
    public long DistributedCacheMisses { get; set; }
    public int MemoryCacheEntries { get; set; }
    public long DistributedCacheEntries { get; set; }
    public double MemoryHitRate => MemoryCacheHits + MemoryCacheMisses > 0 ? 
        (double)MemoryCacheHits / (MemoryCacheHits + MemoryCacheMisses) * 100 : 0;
    public double DistributedHitRate => DistributedCacheHits + DistributedCacheMisses > 0 ? 
        (double)DistributedCacheHits / (DistributedCacheHits + DistributedCacheMisses) * 100 : 0;
}