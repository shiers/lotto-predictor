using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

/// <summary>
/// Controller for cache management and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly ICacheWarmupService _cacheWarmupService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        ICacheInvalidationService cacheInvalidationService,
        ICacheWarmupService cacheWarmupService,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService;
        _cacheInvalidationService = cacheInvalidationService;
        _cacheWarmupService = cacheWarmupService;
        _logger = logger;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<CacheStatistics>> GetStatistics()
    {
        try
        {
            var statistics = await _cacheService.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, new { error = "Failed to get cache statistics" });
        }
    }

    /// <summary>
    /// Warm up cache with frequently accessed data
    /// </summary>
    [HttpPost("warmup")]
    public async Task<ActionResult> WarmUp()
    {
        try
        {
            await _cacheWarmupService.WarmUpAsync();
            _logger.LogInformation("Cache warm-up initiated via API");
            return Ok(new { message = "Cache warm-up completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warm-up via API");
            return StatusCode(500, new { error = "Cache warm-up failed" });
        }
    }

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    [HttpDelete("clear")]
    public async Task<ActionResult> ClearAll([FromQuery] string level = "both")
    {
        try
        {
            var cacheLevel = level.ToLowerInvariant() switch
            {
                "memory" => CacheLevel.Memory,
                "distributed" => CacheLevel.Distributed,
                "both" => CacheLevel.Both,
                _ => CacheLevel.Both
            };

            await _cacheService.ClearAllAsync(cacheLevel);
            _logger.LogWarning("Cache cleared via API for level: {Level}", level);
            return Ok(new { message = $"Cache cleared successfully for level: {level}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache via API");
            return StatusCode(500, new { error = "Failed to clear cache" });
        }
    }

    /// <summary>
    /// Invalidate frequency data cache
    /// </summary>
    [HttpDelete("invalidate/frequency")]
    public async Task<ActionResult> InvalidateFrequencyData()
    {
        try
        {
            await _cacheInvalidationService.InvalidateFrequencyDataAsync();
            _logger.LogInformation("Frequency data cache invalidated via API");
            return Ok(new { message = "Frequency data cache invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating frequency data cache via API");
            return StatusCode(500, new { error = "Failed to invalidate frequency data cache" });
        }
    }

    /// <summary>
    /// Invalidate search data cache
    /// </summary>
    [HttpDelete("invalidate/search")]
    public async Task<ActionResult> InvalidateSearchData()
    {
        try
        {
            await _cacheInvalidationService.InvalidateSearchDataAsync();
            _logger.LogInformation("Search data cache invalidated via API");
            return Ok(new { message = "Search data cache invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating search data cache via API");
            return StatusCode(500, new { error = "Failed to invalidate search data cache" });
        }
    }

    /// <summary>
    /// Invalidate navigation data cache
    /// </summary>
    [HttpDelete("invalidate/navigation")]
    public async Task<ActionResult> InvalidateNavigationData()
    {
        try
        {
            await _cacheInvalidationService.InvalidateNavigationDataAsync();
            _logger.LogInformation("Navigation data cache invalidated via API");
            return Ok(new { message = "Navigation data cache invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating navigation data cache via API");
            return StatusCode(500, new { error = "Failed to invalidate navigation data cache" });
        }
    }

    /// <summary>
    /// Invalidate cache for a specific draw
    /// </summary>
    [HttpDelete("invalidate/draw/{drawNumber}")]
    public async Task<ActionResult> InvalidateDrawData(int drawNumber)
    {
        try
        {
            await _cacheInvalidationService.InvalidateOnDrawUpdateAsync(drawNumber);
            _logger.LogInformation("Cache invalidated for draw {DrawNumber} via API", drawNumber);
            return Ok(new { message = $"Cache invalidated successfully for draw {drawNumber}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for draw {DrawNumber} via API", drawNumber);
            return StatusCode(500, new { error = $"Failed to invalidate cache for draw {drawNumber}" });
        }
    }

    /// <summary>
    /// Get cache health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            var statistics = await _cacheService.GetStatisticsAsync();
            
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                memoryCache = new
                {
                    entries = statistics.MemoryCacheEntries,
                    hitRate = statistics.MemoryHitRate,
                    hits = statistics.MemoryCacheHits,
                    misses = statistics.MemoryCacheMisses
                },
                distributedCache = new
                {
                    entries = statistics.DistributedCacheEntries,
                    hitRate = statistics.DistributedHitRate,
                    hits = statistics.DistributedCacheHits,
                    misses = statistics.DistributedCacheMisses
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache health status");
            return StatusCode(500, new { 
                status = "unhealthy", 
                timestamp = DateTime.UtcNow,
                error = "Failed to get cache health status" 
            });
        }
    }
}