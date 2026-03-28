namespace PredictLottoNZ.Services;

/// <summary>
/// Interface for cache warmup service
/// </summary>
public interface ICacheWarmupService
{
    /// <summary>
    /// Warm up cache with frequently accessed data during application startup
    /// </summary>
    Task WarmUpAsync();
    
    /// <summary>
    /// Warm up specific data types
    /// </summary>
    Task WarmUpNumberFrequenciesAsync();
    
    /// <summary>
    /// Warm up recent draws
    /// </summary>
    Task WarmUpRecentDrawsAsync();
    
    /// <summary>
    /// Warm up popular number combinations
    /// </summary>
    Task WarmUpPopularCombinationsAsync();
    
    /// <summary>
    /// Warm up navigation data
    /// </summary>
    Task WarmUpNavigationDataAsync();
}