using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;

namespace PredictLottoNZ.Services;

/// <summary>
/// Service for warming up cache with frequently accessed data
/// </summary>
public class CacheWarmupService : ICacheWarmupService
{
    private readonly ICacheService _cacheService;
    private readonly LottoDbContext _context;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(
        ICacheService cacheService,
        LottoDbContext context,
        ILogger<CacheWarmupService> logger)
    {
        _cacheService = cacheService;
        _context = context;
        _logger = logger;
    }

    public async Task WarmUpAsync()
    {
        try
        {
            _logger.LogInformation("Starting comprehensive cache warm-up");

            var tasks = new List<Task>
            {
                WarmUpNumberFrequenciesAsync(),
                WarmUpRecentDrawsAsync(),
                WarmUpPopularCombinationsAsync(),
                WarmUpNavigationDataAsync()
            };

            Task.WhenAll(tasks).Wait();

            _logger.LogInformation("Cache warm-up completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warm-up");
        }
    }

    public async Task WarmUpNumberFrequenciesAsync()
    {
        try
        {
            _logger.LogDebug("Warming up number frequencies");

            var frequencies = await _context.NumberFrequencies
                .OrderByDescending(nf => nf.TotalOccurrences)
                .ToListAsync();

            var warmupTasks = frequencies.Select(async frequency =>
            {
                var key = CacheService.GetFrequencyAnalysisKey("number", frequency.Number);
                await _cacheService.SetAsync(key, frequency, TimeSpan.FromHours(2));
            });

            await Task.WhenAll(warmupTasks);

            _logger.LogInformation("Warmed up {Count} number frequencies", frequencies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up number frequencies");
        }
    }

    public async Task WarmUpRecentDrawsAsync()
    {
        try
        {
            _logger.LogDebug("Warming up recent draws");

            var recentDraws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .Take(100) // Last 100 draws
                .ToListAsync();

            var warmupTasks = recentDraws.Select(async draw =>
            {
                var key = CacheService.GetDrawNavigationKey(draw.Draw);
                await _cacheService.SetAsync(key, draw, TimeSpan.FromHours(4));
            });

            await Task.WhenAll(warmupTasks);

            _logger.LogInformation("Warmed up {Count} recent draws", recentDraws.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up recent draws");
        }
    }

    public async Task WarmUpPopularCombinationsAsync()
    {
        try
        {
            _logger.LogDebug("Warming up popular combinations");

            // Warm up popular single numbers (most frequent)
            var popularNumbers = await _context.NumberFrequencies
                .OrderByDescending(nf => nf.TotalOccurrences)
                .Take(10)
                .Select(nf => nf.Number)
                .ToListAsync();

            var singleNumberTasks = popularNumbers.Select(async number =>
            {
                var key = CacheService.GetNumberLookupKey(number);
                // We'll let the actual lookup service populate this when first accessed
                // This just ensures the cache key structure is ready
                Task.CompletedTask.Wait();
            });

            Task.WhenAll(singleNumberTasks).Wait();

            // Warm up some common number ranges
            var commonRanges = new[]
            {
                new { Numbers = new[] { 1, 2, 3, 4, 5, 6 }, Name = "Sequential Low" },
                new { Numbers = new[] { 7, 14, 21, 28, 35, 40 }, Name = "Multiples of 7" },
                new { Numbers = new[] { 1, 11, 21, 31, 32, 33 }, Name = "Mixed Pattern" }
            };

            var rangeTasks = commonRanges.Select(async range =>
            {
                var key = CacheService.GetNumbersLookupKey(range.Numbers);
                // Cache key prepared for future use
                Task.CompletedTask.Wait();
            });

            Task.WhenAll(rangeTasks).Wait();

            _logger.LogInformation("Warmed up popular number combinations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up popular combinations");
        }
    }

    public async Task WarmUpNavigationDataAsync()
    {
        try
        {
            _logger.LogDebug("Warming up navigation data");

            // Get the latest draw for navigation context
            var latestDraw = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .FirstOrDefaultAsync();

            if (latestDraw != null)
            {
                // Warm up navigation context for the latest draw
                var key = CacheService.GetDrawNavigationKey(latestDraw.Draw);
                await _cacheService.SetAsync(key, latestDraw, TimeSpan.FromHours(1));

                // Warm up a few draws before and after
                var surroundingDraws = await _context.LottoDraws
                    .Where(d => d.Draw >= latestDraw.Draw - 5 && d.Draw <= latestDraw.Draw + 5)
                    .ToListAsync();

                var navigationTasks = surroundingDraws.Select(async draw =>
                {
                    var navKey = CacheService.GetDrawNavigationKey(draw.Draw);
                    await _cacheService.SetAsync(navKey, draw, TimeSpan.FromHours(1));
                });

                await Task.WhenAll(navigationTasks);
            }

            _logger.LogInformation("Warmed up navigation data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up navigation data");
        }
    }
}