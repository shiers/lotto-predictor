using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Services;
using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceMonitoringService _performanceMonitoring;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(
        IPerformanceMonitoringService performanceMonitoring,
        ICacheService cacheService,
        ILogger<PerformanceController> logger)
    {
        _performanceMonitoring = performanceMonitoring;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get current performance metrics
    /// </summary>
    /// <returns>Real-time performance metrics</returns>
    [HttpGet("metrics")]
    public async Task<ActionResult<PerformanceMetrics>> GetCurrentMetrics()
    {
        try
        {
            _logger.LogInformation("Retrieving current performance metrics");

            var metrics = await _performanceMonitoring.GetCurrentMetricsAsync();
            
            // Add cache statistics
            var cacheStats = await _cacheService.GetStatisticsAsync();
            metrics.CacheHitRate = CalculateCacheHitRate(cacheStats);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current performance metrics");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving metrics" });
        }
    }

    /// <summary>
    /// Get performance statistics for a time period
    /// </summary>
    /// <param name="startTime">Start of time period (ISO 8601 format)</param>
    /// <param name="endTime">End of time period (ISO 8601 format)</param>
    /// <returns>Performance statistics for the specified period</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<PerformanceStatistics>> GetStatistics(
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            var start = startTime ?? DateTime.UtcNow.AddHours(-24); // Default to last 24 hours
            var end = endTime ?? DateTime.UtcNow;

            if (start >= end)
            {
                return BadRequest(new { error = "Start time must be before end time" });
            }

            if ((end - start).TotalDays > 30)
            {
                return BadRequest(new { error = "Time period cannot exceed 30 days" });
            }

            _logger.LogInformation("Retrieving performance statistics from {StartTime} to {EndTime}", start, end);

            var statistics = await _performanceMonitoring.GetStatisticsAsync(start, end);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance statistics");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <returns>Current cache performance statistics</returns>
    [HttpGet("cache-statistics")]
    public async Task<ActionResult<CacheStatistics>> GetCacheStatistics()
    {
        try
        {
            _logger.LogInformation("Retrieving cache statistics");

            var statistics = await _cacheService.GetStatisticsAsync();

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving cache statistics" });
        }
    }

    /// <summary>
    /// Warm up the cache with frequently accessed data
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("cache/warmup")]
    public async Task<ActionResult> WarmUpCache()
    {
        try
        {
            _logger.LogInformation("Starting cache warm-up process");

            await _cacheService.WarmUpCacheAsync();

            _logger.LogInformation("Cache warm-up completed successfully");

            return Ok(new { message = "Cache warm-up completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warm-up");
            return StatusCode(500, new { error = "Internal server error occurred during cache warm-up" });
        }
    }

    /// <summary>
    /// Clear cache (use with caution)
    /// </summary>
    /// <param name="level">Cache level to clear (memory, distributed, or both)</param>
    /// <returns>Success status</returns>
    [HttpDelete("cache")]
    public async Task<ActionResult> ClearCache([FromQuery] string level = "both")
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

            _logger.LogWarning("Clearing cache at level: {CacheLevel}", cacheLevel);

            await _cacheService.ClearAllAsync(cacheLevel);

            _logger.LogInformation("Cache cleared successfully at level: {CacheLevel}", cacheLevel);

            return Ok(new { message = $"Cache cleared successfully at level: {cacheLevel}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { error = "Internal server error occurred while clearing cache" });
        }
    }

    /// <summary>
    /// Get health check information
    /// </summary>
    /// <returns>System health status</returns>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealthStatus()
    {
        try
        {
            var metrics = await _performanceMonitoring.GetCurrentMetricsAsync();
            var cacheStats = await _cacheService.GetStatisticsAsync();

            var health = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Performance = new
                {
                    ActiveOperations = metrics.ActiveOperations,
                    OperationsPerSecond = metrics.OperationsPerSecond,
                    AverageResponseTime = metrics.AverageResponseTime.TotalMilliseconds,
                    MemoryUsage = metrics.MemoryUsage
                },
                Cache = new
                {
                    HitRate = CalculateCacheHitRate(cacheStats),
                    MemoryEntries = cacheStats.MemoryCacheEntries,
                    DistributedEntries = cacheStats.DistributedCacheEntries
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health status");
            
            var unhealthyStatus = new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = "Unable to retrieve system metrics"
            };

            return StatusCode(500, unhealthyStatus);
        }
    }

    /// <summary>
    /// Get slow query analysis
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="limit">Maximum number of slow queries to return (default: 50)</param>
    /// <returns>List of slow queries with analysis</returns>
    [HttpGet("slow-queries")]
    public async Task<ActionResult<object>> GetSlowQueries(
        [FromQuery] [Range(1, 168)] int hours = 24,
        [FromQuery] [Range(1, 100)] int limit = 50)
    {
        try
        {
            var startTime = DateTime.UtcNow.AddHours(-hours);
            var endTime = DateTime.UtcNow;

            _logger.LogInformation("Retrieving slow queries from last {Hours} hours", hours);

            var statistics = await _performanceMonitoring.GetStatisticsAsync(startTime, endTime);

            var slowQueries = statistics.SlowQueries
                .Take(limit)
                .Select(sq => new
                {
                    sq.OperationId,
                    sq.OperationType,
                    Duration = sq.Duration.TotalMilliseconds,
                    sq.Timestamp,
                    sq.Parameters,
                    CheckpointCount = sq.Checkpoints.Count,
                    Checkpoints = sq.Checkpoints.Select(cp => new
                    {
                        cp.Name,
                        ElapsedTime = cp.ElapsedTime.TotalMilliseconds,
                        cp.Timestamp
                    })
                })
                .ToList();

            var analysis = new
            {
                TotalSlowQueries = statistics.SlowQueries.Count,
                TimeRange = new { StartTime = startTime, EndTime = endTime },
                SlowQueries = slowQueries,
                Summary = new
                {
                    AverageSlowQueryDuration = slowQueries.Any() 
                        ? slowQueries.Average(sq => sq.Duration) 
                        : 0,
                    SlowestQuery = slowQueries.FirstOrDefault(),
                    MostCommonSlowOperationType = slowQueries
                        .GroupBy(sq => sq.OperationType)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key
                }
            };

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slow query analysis");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving slow queries" });
        }
    }

    private double CalculateCacheHitRate(CacheStatistics stats)
    {
        var totalRequests = stats.MemoryCacheHits + stats.MemoryCacheMisses + 
                           stats.DistributedCacheHits + stats.DistributedCacheMisses;
        
        if (totalRequests == 0) return 0;
        
        var totalHits = stats.MemoryCacheHits + stats.DistributedCacheHits;
        return (double)totalHits / totalRequests * 100;
    }
}