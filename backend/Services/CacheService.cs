using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Concurrent;
using PredictLottoNZ.Data;
using Microsoft.EntityFrameworkCore;

namespace PredictLottoNZ.Services;

/// <summary>
/// Multi-level caching service implementation with memory (L1) and Redis (L2) caching
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheService> _logger;
    private readonly LottoDbContext _context;
    
    // Cache statistics
    private readonly ConcurrentDictionary<string, long> _statistics = new();
    
    // Default cache expiration times
    private readonly TimeSpan _defaultMemoryExpiration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _defaultDistributedExpiration = TimeSpan.FromMinutes(30);
    
    // Cache key prefixes for different data types
    private const string NUMBER_LOOKUP_PREFIX = "number_lookup";
    private const string COMBINATION_SEARCH_PREFIX = "combination_search";
    private const string FREQUENCY_ANALYSIS_PREFIX = "frequency_analysis";
    private const string DRAW_NAVIGATION_PREFIX = "draw_navigation";
    private const string SEARCH_SUGGESTIONS_PREFIX = "search_suggestions";
    
    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<CacheService> logger,
        LottoDbContext context)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _context = context;
        
        // Initialize statistics
        _statistics["memory_hits"] = 0;
        _statistics["memory_misses"] = 0;
        _statistics["distributed_hits"] = 0;
        _statistics["distributed_misses"] = 0;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both)
    {
        try
        {
            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key, level);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // Execute factory function to get fresh data
            _logger.LogDebug("Cache miss for key {Key}, executing factory function", key);
            var freshValue = await factory();
            
            if (freshValue != null)
            {
                // Cache the fresh value
                await SetAsync(key, freshValue, expiration, level);
            }

            return freshValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key {Key}", key);
            // If caching fails, still try to get fresh data
            return await factory();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CacheLevel level = CacheLevel.Both)
    {
        try
        {
            // L1: Try memory cache first (if enabled)
            if (level == CacheLevel.Memory || level == CacheLevel.Both)
            {
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    _statistics.AddOrUpdate("memory_hits", 1, (key, value) => value + 1);
                    _logger.LogTrace("Memory cache hit for key {Key}", key);
                    return memoryValue;
                }
                
                _statistics.AddOrUpdate("memory_misses", 1, (key, value) => value + 1);
                _logger.LogTrace("Memory cache miss for key {Key}", key);
            }

            // L2: Try distributed cache (Redis) if memory cache missed or not enabled
            if (level == CacheLevel.Distributed || level == CacheLevel.Both)
            {
                var distributedValue = await _distributedCache.GetStringAsync(key);
                if (distributedValue != null)
                {
                    _statistics.AddOrUpdate("distributed_hits", 1, (key, value) => value + 1);
                    _logger.LogTrace("Distributed cache hit for key {Key}", key);
                    
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);
                    
                    // Populate memory cache if we're using both levels
                    if (level == CacheLevel.Both && deserializedValue != null)
                    {
                        _memoryCache.Set(key, deserializedValue, _defaultMemoryExpiration);
                    }
                    
                    return deserializedValue;
                }
                
                _statistics.AddOrUpdate("distributed_misses", 1, (key, value) => value + 1);
                _logger.LogTrace("Distributed cache miss for key {Key}", key);
            }

            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key {Key}", key);
            return default(T);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both)
    {
        try
        {
            if (value == null) return;

            var memoryExpiration = expiration ?? _defaultMemoryExpiration;
            var distributedExpiration = expiration ?? _defaultDistributedExpiration;

            // Set in memory cache (L1)
            if (level == CacheLevel.Memory || level == CacheLevel.Both)
            {
                _memoryCache.Set(key, value, memoryExpiration);
                _logger.LogTrace("Set memory cache for key {Key} with expiration {Expiration}", key, memoryExpiration);
            }

            // Set in distributed cache (L2)
            if (level == CacheLevel.Distributed || level == CacheLevel.Both)
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = distributedExpiration
                };
                
                await _distributedCache.SetStringAsync(key, serializedValue, options);
                _logger.LogTrace("Set distributed cache for key {Key} with expiration {Expiration}", key, distributedExpiration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CacheLevel level = CacheLevel.Both)
    {
        try
        {
            // Remove from memory cache
            if (level == CacheLevel.Memory || level == CacheLevel.Both)
            {
                _memoryCache.Remove(key);
                _logger.LogTrace("Removed from memory cache: {Key}", key);
            }

            // Remove from distributed cache
            if (level == CacheLevel.Distributed || level == CacheLevel.Both)
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogTrace("Removed from distributed cache: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CacheLevel level = CacheLevel.Both)
    {
        try
        {
            // For memory cache, we can't easily remove by pattern, so we'll skip it
            // In a production system, you might want to track keys separately
            
            // For Redis, we can use SCAN with pattern matching
            if (level == CacheLevel.Distributed || level == CacheLevel.Both)
            {
                var database = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                
                await foreach (var key in server.KeysAsync(pattern: pattern))
                {
                    await database.KeyDeleteAsync(key);
                }
                
                _logger.LogDebug("Removed keys matching pattern {Pattern} from distributed cache", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern {Pattern}", pattern);
        }
    }

    public async Task WarmUpCacheAsync()
    {
        try
        {
            _logger.LogInformation("Starting cache warm-up process");
            
            // Warm up frequently accessed number frequencies
            await WarmUpNumberFrequencies();
            
            // Warm up recent draw data
            await WarmUpRecentDraws();
            
            // Warm up popular number combinations
            await WarmUpPopularCombinations();
            
            _logger.LogInformation("Cache warm-up process completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warm-up");
        }
    }

    private async Task WarmUpNumberFrequencies()
    {
        try
        {
            var frequencies = await _context.NumberFrequencies
                .OrderByDescending(nf => nf.TotalOccurrences)
                .Take(40) // All numbers 1-40
                .ToListAsync();

            foreach (var frequency in frequencies)
            {
                var key = $"{FREQUENCY_ANALYSIS_PREFIX}_number_{frequency.Number}";
                await SetAsync(key, frequency, TimeSpan.FromHours(1), CacheLevel.Both);
            }
            
            _logger.LogDebug("Warmed up {Count} number frequencies", frequencies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up number frequencies");
        }
    }

    private async Task WarmUpRecentDraws()
    {
        try
        {
            var recentDraws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .Take(50) // Last 50 draws
                .ToListAsync();

            foreach (var draw in recentDraws)
            {
                var key = $"{DRAW_NAVIGATION_PREFIX}_draw_{draw.Draw}";
                await SetAsync(key, draw, TimeSpan.FromHours(2), CacheLevel.Both);
            }
            
            _logger.LogDebug("Warmed up {Count} recent draws", recentDraws.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up recent draws");
        }
    }

    private async Task WarmUpPopularCombinations()
    {
        try
        {
            // Warm up some popular number combinations (1-10, 11-20, etc.)
            var popularRanges = new[]
            {
                new { Start = 1, End = 10 },
                new { Start = 11, End = 20 },
                new { Start = 21, End = 30 },
                new { Start = 31, End = 40 }
            };

            foreach (var range in popularRanges)
            {
                var numbers = Enumerable.Range(range.Start, range.End - range.Start + 1).ToArray();
                var key = $"{NUMBER_LOOKUP_PREFIX}_range_{range.Start}_{range.End}";
                
                // We'll cache a placeholder for now - in a real implementation,
                // you'd calculate the actual lookup results
                await SetAsync(key, $"Range {range.Start}-{range.End}", TimeSpan.FromMinutes(30), CacheLevel.Distributed);
            }
            
            _logger.LogDebug("Warmed up {Count} popular number ranges", popularRanges.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up popular combinations");
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var memoryHits = _statistics.GetOrAdd("memory_hits", 0);
            var memoryMisses = _statistics.GetOrAdd("memory_misses", 0);
            var distributedHits = _statistics.GetOrAdd("distributed_hits", 0);
            var distributedMisses = _statistics.GetOrAdd("distributed_misses", 0);

            // Get memory cache entry count (this is an approximation)
            var memoryCacheField = _memoryCache.GetType().GetField("_coherentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var memoryCacheEntries = 0;
            if (memoryCacheField?.GetValue(_memoryCache) is System.Collections.IDictionary coherentState)
            {
                memoryCacheEntries = coherentState.Count;
            }

            // Get Redis cache entry count
            long distributedCacheEntries = 0;
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                distributedCacheEntries = await server.DatabaseSizeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get distributed cache size");
            }

            return new CacheStatistics
            {
                MemoryCacheHits = memoryHits,
                MemoryCacheMisses = memoryMisses,
                DistributedCacheHits = distributedHits,
                DistributedCacheMisses = distributedMisses,
                MemoryCacheEntries = memoryCacheEntries,
                DistributedCacheEntries = distributedCacheEntries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    public async Task ClearAllAsync(CacheLevel level = CacheLevel.Both)
    {
        try
        {
            // Clear memory cache
            if (level == CacheLevel.Memory || level == CacheLevel.Both)
            {
                if (_memoryCache is MemoryCache mc)
                {
                    mc.Clear();
                }
                _logger.LogInformation("Cleared memory cache");
            }

            // Clear distributed cache
            if (level == CacheLevel.Distributed || level == CacheLevel.Both)
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                await server.FlushDatabaseAsync();
                _logger.LogInformation("Cleared distributed cache");
            }

            // Reset statistics
            _statistics.Clear();
            _statistics["memory_hits"] = 0;
            _statistics["memory_misses"] = 0;
            _statistics["distributed_hits"] = 0;
            _statistics["distributed_misses"] = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    /// <summary>
    /// Generate cache key for number lookup
    /// </summary>
    public static string GetNumberLookupKey(int number) => $"{NUMBER_LOOKUP_PREFIX}_{number}";

    /// <summary>
    /// Generate cache key for multiple number lookup
    /// </summary>
    public static string GetNumbersLookupKey(int[] numbers) => 
        $"{NUMBER_LOOKUP_PREFIX}_{string.Join("_", numbers.OrderBy(n => n))}";

    /// <summary>
    /// Generate cache key for combination search
    /// </summary>
    public static string GetCombinationSearchKey(int[] combination, bool includePartialMatches) => 
        $"{COMBINATION_SEARCH_PREFIX}_{string.Join("_", combination.OrderBy(n => n))}_{includePartialMatches}";

    /// <summary>
    /// Generate cache key for frequency analysis
    /// </summary>
    public static string GetFrequencyAnalysisKey(string analysisType, params object[] parameters) => 
        $"{FREQUENCY_ANALYSIS_PREFIX}_{analysisType}_{string.Join("_", parameters)}";

    /// <summary>
    /// Generate cache key for draw navigation
    /// </summary>
    public static string GetDrawNavigationKey(int drawNumber) => $"{DRAW_NAVIGATION_PREFIX}_{drawNumber}";

    /// <summary>
    /// Generate cache key for search suggestions
    /// </summary>
    public static string GetSearchSuggestionsKey(string query, string searchType) => 
        $"{SEARCH_SUGGESTIONS_PREFIX}_{searchType}_{query.ToLowerInvariant()}";
}