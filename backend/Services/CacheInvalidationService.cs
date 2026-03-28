using Microsoft.Extensions.Logging;

namespace PredictLottoNZ.Services;

/// <summary>
/// Service for handling cache invalidation when data changes
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvalidateOnNewDrawAsync(int drawNumber)
    {
        try
        {
            _logger.LogInformation("Invalidating cache for new draw {DrawNumber}", drawNumber);

            // Invalidate specific draw cache
            await _cacheService.RemoveAsync(CacheService.GetDrawNavigationKey(drawNumber));

            // Invalidate frequency data as new draw affects all frequencies
            await InvalidateFrequencyDataAsync();

            // Invalidate navigation data as draw sequence has changed
            await InvalidateNavigationDataAsync();

            // Invalidate search data as new draw affects search results
            await InvalidateSearchDataAsync();

            _logger.LogDebug("Cache invalidation completed for new draw {DrawNumber}", drawNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for new draw {DrawNumber}", drawNumber);
        }
    }

    public async Task InvalidateOnDrawUpdateAsync(int drawNumber)
    {
        try
        {
            _logger.LogInformation("Invalidating cache for updated draw {DrawNumber}", drawNumber);

            // Invalidate specific draw cache
            await _cacheService.RemoveAsync(CacheService.GetDrawNavigationKey(drawNumber));

            // Invalidate all number lookup caches as winning numbers may have changed
            await _cacheService.RemoveByPatternAsync("number_lookup_*");

            // Invalidate all combination search caches
            await _cacheService.RemoveByPatternAsync("combination_search_*");

            // Invalidate frequency data
            await InvalidateFrequencyDataAsync();

            _logger.LogDebug("Cache invalidation completed for updated draw {DrawNumber}", drawNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for updated draw {DrawNumber}", drawNumber);
        }
    }

    public async Task InvalidateOnDrawDeleteAsync(int drawNumber)
    {
        try
        {
            _logger.LogInformation("Invalidating cache for deleted draw {DrawNumber}", drawNumber);

            // Invalidate specific draw cache
            await _cacheService.RemoveAsync(CacheService.GetDrawNavigationKey(drawNumber));

            // Invalidate all related caches as draw deletion affects everything
            await InvalidateAllAsync();

            _logger.LogDebug("Cache invalidation completed for deleted draw {DrawNumber}", drawNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for deleted draw {DrawNumber}", drawNumber);
        }
    }

    public async Task InvalidateFrequencyDataAsync()
    {
        try
        {
            _logger.LogDebug("Invalidating frequency data cache");

            // Remove all frequency analysis cache entries
            await _cacheService.RemoveByPatternAsync("frequency_analysis_*");

            _logger.LogDebug("Frequency data cache invalidation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating frequency data cache");
        }
    }

    public async Task InvalidateSearchDataAsync()
    {
        try
        {
            _logger.LogDebug("Invalidating search data cache");

            // Remove all number lookup cache entries
            await _cacheService.RemoveByPatternAsync("number_lookup_*");

            // Remove all combination search cache entries
            await _cacheService.RemoveByPatternAsync("combination_search_*");

            // Remove all search suggestions cache entries
            await _cacheService.RemoveByPatternAsync("search_suggestions_*");

            _logger.LogDebug("Search data cache invalidation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating search data cache");
        }
    }

    public async Task InvalidateNavigationDataAsync()
    {
        try
        {
            _logger.LogDebug("Invalidating navigation data cache");

            // Remove all draw navigation cache entries
            await _cacheService.RemoveByPatternAsync("draw_navigation_*");

            _logger.LogDebug("Navigation data cache invalidation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating navigation data cache");
        }
    }

    public async Task InvalidateAllAsync()
    {
        try
        {
            _logger.LogWarning("Invalidating ALL cache entries");

            await _cacheService.ClearAllAsync();

            _logger.LogInformation("All cache entries invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all cache entries");
        }
    }
}