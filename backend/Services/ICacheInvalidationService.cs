namespace PredictLottoNZ.Services;

/// <summary>
/// Interface for cache invalidation service
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidate cache when new lottery draw is added
    /// </summary>
    Task InvalidateOnNewDrawAsync(int drawNumber);
    
    /// <summary>
    /// Invalidate cache when lottery draw is updated
    /// </summary>
    Task InvalidateOnDrawUpdateAsync(int drawNumber);
    
    /// <summary>
    /// Invalidate cache when lottery draw is deleted
    /// </summary>
    Task InvalidateOnDrawDeleteAsync(int drawNumber);
    
    /// <summary>
    /// Invalidate frequency-related cache entries
    /// </summary>
    Task InvalidateFrequencyDataAsync();
    
    /// <summary>
    /// Invalidate search-related cache entries
    /// </summary>
    Task InvalidateSearchDataAsync();
    
    /// <summary>
    /// Invalidate navigation-related cache entries
    /// </summary>
    Task InvalidateNavigationDataAsync();
    
    /// <summary>
    /// Invalidate all cache entries (use with caution)
    /// </summary>
    Task InvalidateAllAsync();
}