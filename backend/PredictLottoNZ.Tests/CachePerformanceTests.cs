using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using StackExchange.Redis;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace PredictLottoNZ.Tests;

public class CachePerformanceTests : IDisposable
{
    private readonly LottoDbContext _context;
    private readonly INumberLookupService _lookupService;
    private readonly IFrequencyAnalysisService _frequencyService;
    private readonly ICacheService _cacheService;
    private readonly IPerformanceMonitoringService _performanceMonitoring;
    private readonly ITestOutputHelper _output;

    public CachePerformanceTests(ITestOutputHelper output)
    {
        _output = output;

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<LottoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LottoDbContext(options);

        // Setup logging
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var lookupLogger = loggerFactory.CreateLogger<NumberLookupService>();
        var frequencyLogger = loggerFactory.CreateLogger<FrequencyAnalysisService>();
        var cacheLogger = loggerFactory.CreateLogger<CacheService>();
        var perfLogger = loggerFactory.CreateLogger<PerformanceMonitoringService>();

        // Setup cache services
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        
        // Mock Redis connection (for testing)
        var mockRedis = ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
        
        _cacheService = new CacheService(memoryCache, distributedCache, mockRedis, cacheLogger, _context);
        _performanceMonitoring = new PerformanceMonitoringService(perfLogger);
        
        _lookupService = new NumberLookupService(_context, _cacheService, _performanceMonitoring, lookupLogger);
        _frequencyService = new FrequencyAnalysisService(_context, distributedCache, frequencyLogger);

        // Seed test data
        SeedTestData();
    }

    [Fact]
    public async Task CacheHitRate_AchievesTargetPerformance()
    {
        // Arrange
        var testNumbers = new int[] { 7, 14, 21, 28, 35 };
        await _cacheService.ClearAllAsync();

        // Act - First round (cache misses)
        var firstRoundTimes = new List<long>();
        foreach (var number in testNumbers)
        {
            var stopwatch = Stopwatch.StartNew();
            await _lookupService.LookupNumberAsync(number);
            stopwatch.Stop();
            firstRoundTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        var firstRoundStats = await _cacheService.GetStatisticsAsync();

        // Act - Second round (cache hits)
        var secondRoundTimes = new List<long>();
        foreach (var number in testNumbers)
        {
            var stopwatch = Stopwatch.StartNew();
            await _lookupService.LookupNumberAsync(number);
            stopwatch.Stop();
            secondRoundTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        var secondRoundStats = await _cacheService.GetStatisticsAsync();

        // Assert
        var firstRoundAvg = firstRoundTimes.Average();
        var secondRoundAvg = secondRoundTimes.Average();
        var improvementRatio = firstRoundAvg / secondRoundAvg;

        Assert.True(improvementRatio >= 3.0, $"Cache should provide at least 3x improvement, got {improvementRatio:F2}x");

        var firstRoundHitRate = CalculateHitRate(firstRoundStats);
        var secondRoundHitRate = CalculateHitRate(secondRoundStats);

        Assert.True(secondRoundHitRate >= 85.0, $"Cache hit rate should be at least 85%, got {secondRoundHitRate:F2}%");

        _output.WriteLine($"First round average: {firstRoundAvg:F2}ms, Hit rate: {firstRoundHitRate:F2}%");
        _output.WriteLine($"Second round average: {secondRoundAvg:F2}ms, Hit rate: {secondRoundHitRate:F2}%");
        _output.WriteLine($"Performance improvement: {improvementRatio:F2}x");
    }

    [Fact]
    public async Task CacheWarmup_ImprovesInitialPerformance()
    {
        // Arrange
        await _cacheService.ClearAllAsync();
        var testNumbers = new int[] { 1, 7, 13, 21, 28, 35, 40 };

        // Act - Cold cache performance
        var coldCacheTimes = new List<long>();
        foreach (var number in testNumbers)
        {
            var stopwatch = Stopwatch.StartNew();
            await _lookupService.LookupNumberAsync(number);
            stopwatch.Stop();
            coldCacheTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Clear cache and warm it up
        await _cacheService.ClearAllAsync();
        await _cacheService.WarmUpCacheAsync();

        // Act - Warm cache performance
        var warmCacheTimes = new List<long>();
        foreach (var number in testNumbers)
        {
            var stopwatch = Stopwatch.StartNew();
            await _lookupService.LookupNumberAsync(number);
            stopwatch.Stop();
            warmCacheTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var coldCacheAvg = coldCacheTimes.Average();
        var warmCacheAvg = warmCacheTimes.Average();
        var warmupImprovement = coldCacheAvg / warmCacheAvg;

        Assert.True(warmupImprovement >= 2.0, $"Cache warmup should provide at least 2x improvement, got {warmupImprovement:F2}x");

        _output.WriteLine($"Cold cache average: {coldCacheAvg:F2}ms");
        _output.WriteLine($"Warm cache average: {warmCacheAvg:F2}ms");
        _output.WriteLine($"Warmup improvement: {warmupImprovement:F2}x");
    }

    [Fact]
    public async Task CacheEviction_MaintainsPerformance()
    {
        // Arrange
        await _cacheService.ClearAllAsync();
        const int cacheLimit = 100; // Assume cache has a limit
        const int testNumbers = 150; // Exceed cache limit

        // Act - Fill cache beyond limit
        var evictionTimes = new List<long>();
        for (int i = 1; i <= testNumbers; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await _lookupService.LookupNumberAsync((i % 40) + 1);
            stopwatch.Stop();
            evictionTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Performance should remain stable even with eviction
        var firstHalf = evictionTimes.Take(testNumbers / 2).Average();
        var secondHalf = evictionTimes.Skip(testNumbers / 2).Average();
        var performanceDegradation = secondHalf / firstHalf;

        Assert.True(performanceDegradation <= 1.5, $"Performance degradation due to eviction should be <= 50%, got {(performanceDegradation - 1) * 100:F1}%");

        var cacheStats = await _cacheService.GetStatisticsAsync();
        _output.WriteLine($"First half average: {firstHalf:F2}ms");
        _output.WriteLine($"Second half average: {secondHalf:F2}ms");
        _output.WriteLine($"Performance degradation: {(performanceDegradation - 1) * 100:F1}%");
        _output.WriteLine($"Final cache entries: Memory={cacheStats.MemoryCacheEntries}, Distributed={cacheStats.DistributedCacheEntries}");
    }

    [Fact]
    public async Task CacheConcurrency_HandlesHighLoad()
    {
        // Arrange
        await _cacheService.ClearAllAsync();
        const int concurrentRequests = 50;
        const int maxTotalTimeMs = 10000; // 10 seconds

        // Act - Concurrent cache operations
        var tasks = new List<Task<long>>();
        for (int i = 0; i < concurrentRequests; i++)
        {
            var number = (i % 40) + 1;
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                await _lookupService.LookupNumberAsync(number);
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }));
        }

        var overallStopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        overallStopwatch.Stop();

        // Assert
        Assert.True(overallStopwatch.ElapsedMilliseconds < maxTotalTimeMs,
            $"Concurrent cache operations took {overallStopwatch.ElapsedMilliseconds}ms, expected < {maxTotalTimeMs}ms");

        var averageTime = results.Average();
        var maxTime = results.Max();
        var minTime = results.Min();

        Assert.True(averageTime < 500, $"Average response time should be < 500ms, got {averageTime:F2}ms");
        Assert.True(maxTime < 2000, $"Max response time should be < 2000ms, got {maxTime}ms");

        _output.WriteLine($"Concurrent requests: {concurrentRequests}");
        _output.WriteLine($"Total time: {overallStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time: {averageTime:F2}ms");
        _output.WriteLine($"Min time: {minTime}ms, Max time: {maxTime}ms");
    }

    [Fact]
    public async Task CacheInvalidation_MaintainsDataConsistency()
    {
        // Arrange
        const int testNumber = 15;
        await _cacheService.ClearAllAsync();

        // Act - Initial lookup (populate cache)
        var initialResult = await _lookupService.LookupNumberAsync(testNumber);
        var initialCount = initialResult.Count();

        // Simulate data change (add new draw)
        await AddNewDrawWithNumber(testNumber);

        // Invalidate cache
        await _cacheService.RemoveByPatternAsync($"number_{testNumber}*");

        // Lookup again (should get fresh data)
        var updatedResult = await _lookupService.LookupNumberAsync(testNumber);
        var updatedCount = updatedResult.Count();

        // Assert
        Assert.True(updatedCount > initialCount, $"Updated result should have more occurrences: initial={initialCount}, updated={updatedCount}");

        _output.WriteLine($"Initial count: {initialCount}");
        _output.WriteLine($"Updated count: {updatedCount}");
        _output.WriteLine($"Cache invalidation successful");
    }

    [Fact]
    public async Task CacheMemoryUsage_StaysWithinLimits()
    {
        // Arrange
        await _cacheService.ClearAllAsync();
        var initialMemory = GC.GetTotalMemory(true);
        const long maxMemoryIncreaseMB = 50; // 50MB limit

        // Act - Perform many cache operations
        for (int i = 0; i < 200; i++)
        {
            var number = (i % 40) + 1;
            await _lookupService.LookupNumberAsync(number);
            
            // Also test frequency analysis caching
            if (i % 10 == 0)
            {
                await _frequencyService.GetNumberFrequenciesAsync();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024 * 1024);

        // Assert
        Assert.True(memoryIncreaseMB < maxMemoryIncreaseMB,
            $"Memory usage increased by {memoryIncreaseMB}MB, expected < {maxMemoryIncreaseMB}MB");

        var cacheStats = await _cacheService.GetStatisticsAsync();
        _output.WriteLine($"Initial memory: {initialMemory / (1024 * 1024)}MB");
        _output.WriteLine($"Final memory: {finalMemory / (1024 * 1024)}MB");
        _output.WriteLine($"Memory increase: {memoryIncreaseMB}MB");
        _output.WriteLine($"Cache entries: Memory={cacheStats.MemoryCacheEntries}, Distributed={cacheStats.DistributedCacheEntries}");
    }

    [Theory]
    [InlineData(CacheLevel.Memory)]
    [InlineData(CacheLevel.Distributed)]
    [InlineData(CacheLevel.Both)]
    public async Task CacheLevelPerformance_MeetsTargets(CacheLevel cacheLevel)
    {
        // Arrange
        await _cacheService.ClearAllAsync();
        const int testNumber = 25;
        const int iterations = 10;

        // Act - Test performance for specific cache level
        var times = new List<long>();
        
        for (int i = 0; i < iterations; i++)
        {
            // Clear specific cache level
            await _cacheService.ClearAllAsync(cacheLevel);
            
            // First call (cache miss)
            var stopwatch = Stopwatch.StartNew();
            await _lookupService.LookupNumberAsync(testNumber);
            stopwatch.Stop();
            var missTime = stopwatch.ElapsedMilliseconds;

            // Second call (cache hit)
            stopwatch.Restart();
            await _lookupService.LookupNumberAsync(testNumber);
            stopwatch.Stop();
            var hitTime = stopwatch.ElapsedMilliseconds;

            times.Add(hitTime);
        }

        var averageHitTime = times.Average();
        var maxHitTime = times.Max();

        // Assert - Cache hits should be fast regardless of level
        var expectedMaxTime = cacheLevel switch
        {
            CacheLevel.Memory => 10, // Memory cache should be very fast
            CacheLevel.Distributed => 50, // Distributed cache can be slower
            CacheLevel.Both => 10, // Should use memory cache first
            _ => 50
        };

        Assert.True(averageHitTime < expectedMaxTime, 
            $"Average hit time for {cacheLevel} should be < {expectedMaxTime}ms, got {averageHitTime:F2}ms");

        _output.WriteLine($"Cache level: {cacheLevel}");
        _output.WriteLine($"Average hit time: {averageHitTime:F2}ms");
        _output.WriteLine($"Max hit time: {maxHitTime}ms");
    }

    [Fact]
    public async Task CacheStatistics_ProvideAccurateMetrics()
    {
        // Arrange
        await _cacheService.ClearAllAsync();
        var testNumbers = new int[] { 5, 10, 15, 20, 25 };

        // Act - Generate cache activity
        // First round - cache misses
        foreach (var number in testNumbers)
        {
            await _lookupService.LookupNumberAsync(number);
        }

        // Second round - cache hits
        foreach (var number in testNumbers)
        {
            await _lookupService.LookupNumberAsync(number);
        }

        // Third round - mix of hits and misses
        foreach (var number in testNumbers.Concat(new[] { 30, 35 }))
        {
            await _lookupService.LookupNumberAsync(number);
        }

        var finalStats = await _cacheService.GetStatisticsAsync();

        // Assert
        var totalRequests = finalStats.MemoryCacheHits + finalStats.MemoryCacheMisses + 
                           finalStats.DistributedCacheHits + finalStats.DistributedCacheMisses;
        
        Assert.True(totalRequests > 0, "Should have recorded cache requests");
        Assert.True(finalStats.MemoryCacheHits > 0 || finalStats.DistributedCacheHits > 0, "Should have recorded cache hits");
        Assert.True(finalStats.MemoryCacheMisses > 0 || finalStats.DistributedCacheMisses > 0, "Should have recorded cache misses");

        var hitRate = CalculateHitRate(finalStats);
        Assert.True(hitRate > 50.0, $"Hit rate should be > 50%, got {hitRate:F2}%");

        _output.WriteLine($"Total requests: {totalRequests}");
        _output.WriteLine($"Memory hits: {finalStats.MemoryCacheHits}, misses: {finalStats.MemoryCacheMisses}");
        _output.WriteLine($"Distributed hits: {finalStats.DistributedCacheHits}, misses: {finalStats.DistributedCacheMisses}");
        _output.WriteLine($"Overall hit rate: {hitRate:F2}%");
    }

    private void SeedTestData()
    {
        // Create test draws
        var draws = new List<LottoDraw>();
        var random = new Random(42); // Fixed seed for consistent tests

        for (int i = 1; i <= 500; i++)
        {
            var numbers = Enumerable.Range(1, 40)
                .OrderBy(x => random.Next())
                .Take(6)
                .OrderBy(x => x)
                .ToArray();

            var draw = new LottoDraw
            {
                Draw = i,
                Date = DateTime.UtcNow.AddDays(-i),
                WinningNumber1 = numbers[0],
                WinningNumber2 = numbers[1],
                WinningNumber3 = numbers[2],
                WinningNumber4 = numbers[3],
                WinningNumber5 = numbers[4],
                WinningNumber6 = numbers[5],
                BonusNumber = random.Next(1, 41),
                Powerball = random.Next(1, 11)
            };

            draws.Add(draw);
        }

        _context.LottoDraws.AddRange(draws);

        // Create number occurrences
        var occurrences = new List<NumberOccurrence>();
        foreach (var draw in draws)
        {
            var numbers = new int[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                                    draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 };
            
            for (int pos = 0; pos < numbers.Length; pos++)
            {
                occurrences.Add(new NumberOccurrence
                {
                    DrawNumber = draw.Draw,
                    Number = numbers[pos],
                    Position = pos + 1,
                    DrawDate = draw.Date,
                    IsBonus = false,
                    IsPowerball = false,
                    Draw = draw
                });
            }

            // Add bonus number occurrence
            occurrences.Add(new NumberOccurrence
            {
                DrawNumber = draw.Draw,
                Number = draw.BonusNumber,
                Position = 7,
                DrawDate = draw.Date,
                IsBonus = true,
                IsPowerball = false,
                Draw = draw
            });
        }

        _context.NumberOccurrences.AddRange(occurrences);

        // Create number frequencies
        var frequencies = new List<NumberFrequency>();
        for (int number = 1; number <= 40; number++)
        {
            var numberOccurrences = occurrences.Where(o => o.Number == number && !o.IsBonus).ToList();
            
            frequencies.Add(new NumberFrequency
            {
                Number = number,
                TotalOccurrences = numberOccurrences.Count,
                FirstAppearance = numberOccurrences.Any() ? numberOccurrences.Min(o => o.DrawDate) : DateTime.UtcNow,
                LastAppearance = numberOccurrences.Any() ? numberOccurrences.Max(o => o.DrawDate) : DateTime.UtcNow,
                AverageFrequency = numberOccurrences.Count / 500.0,
                LongestGap = random.Next(1, 50),
                LastCalculated = DateTime.UtcNow
            });
        }

        _context.NumberFrequencies.AddRange(frequencies);
        _context.SaveChanges();
    }

    private async Task AddNewDrawWithNumber(int number)
    {
        var random = new Random();
        var numbers = new List<int> { number };
        
        // Add 5 more random numbers
        while (numbers.Count < 6)
        {
            var newNumber = random.Next(1, 41);
            if (!numbers.Contains(newNumber))
            {
                numbers.Add(newNumber);
            }
        }
        
        numbers.Sort();

        var newDraw = new LottoDraw
        {
            Draw = _context.LottoDraws.Max(d => d.Draw) + 1,
            Date = DateTime.UtcNow,
            WinningNumber1 = numbers[0],
            WinningNumber2 = numbers[1],
            WinningNumber3 = numbers[2],
            WinningNumber4 = numbers[3],
            WinningNumber5 = numbers[4],
            WinningNumber6 = numbers[5],
            BonusNumber = random.Next(1, 41),
            Powerball = random.Next(1, 11)
        };

        _context.LottoDraws.Add(newDraw);

        // Add corresponding occurrences
        for (int pos = 0; pos < numbers.Count; pos++)
        {
            _context.NumberOccurrences.Add(new NumberOccurrence
            {
                DrawNumber = newDraw.Draw,
                Number = numbers[pos],
                Position = pos + 1,
                DrawDate = newDraw.Date,
                IsBonus = false,
                IsPowerball = false,
                Draw = newDraw
            });
        }

        await _context.SaveChangesAsync();
    }

    private double CalculateHitRate(CacheStatistics stats)
    {
        var totalRequests = stats.MemoryCacheHits + stats.MemoryCacheMisses + 
                           stats.DistributedCacheHits + stats.DistributedCacheMisses;
        
        if (totalRequests == 0) return 0;
        
        var totalHits = stats.MemoryCacheHits + stats.DistributedCacheHits;
        return (double)totalHits / totalRequests * 100;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
