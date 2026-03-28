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

public class PerformanceTests : IDisposable
{
    private readonly LottoDbContext _context;
    private readonly INumberLookupService _lookupService;
    private readonly IFrequencyAnalysisService _frequencyService;
    private readonly ICacheService _cacheService;
    private readonly IPerformanceMonitoringService _performanceMonitoring;
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
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
    public async Task NumberLookup_WithLargeDataset_CompletesWithinTimeLimit()
    {
        // Arrange
        const int timeoutMs = 500; // 500ms timeout
        const int testNumber = 7;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _lookupService.LookupNumberAsync(testNumber);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs, 
            $"Number lookup took {stopwatch.ElapsedMilliseconds}ms, expected < {timeoutMs}ms");
        Assert.NotEmpty(result);
        
        _output.WriteLine($"Number lookup completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task PaginatedNumberLookup_PerformsBetterThanFullLookup()
    {
        // Arrange
        const int testNumber = 13;
        var pagination = new PaginationRequest { Page = 1, PageSize = 50 };

        // Act - Full lookup
        var fullStopwatch = Stopwatch.StartNew();
        var fullResult = await _lookupService.LookupNumberAsync(testNumber);
        fullStopwatch.Stop();

        // Act - Paginated lookup
        var paginatedStopwatch = Stopwatch.StartNew();
        var paginatedResult = await _lookupService.LookupNumberPaginatedAsync(testNumber, pagination);
        paginatedStopwatch.Stop();

        // Assert
        Assert.True(paginatedStopwatch.ElapsedMilliseconds <= fullStopwatch.ElapsedMilliseconds,
            $"Paginated lookup ({paginatedStopwatch.ElapsedMilliseconds}ms) should be faster than or equal to full lookup ({fullStopwatch.ElapsedMilliseconds}ms)");
        
        Assert.True(paginatedResult.ItemCount <= pagination.PageSize,
            "Paginated result should respect page size limit");

        _output.WriteLine($"Full lookup: {fullStopwatch.ElapsedMilliseconds}ms, Paginated: {paginatedStopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CachePerformance_ShowsSignificantImprovement()
    {
        // Arrange
        const int testNumber = 21;
        
        // Clear cache to ensure clean test
        await _cacheService.ClearAllAsync();

        // Act - First call (cache miss)
        var firstCallStopwatch = Stopwatch.StartNew();
        var firstResult = await _lookupService.LookupNumberAsync(testNumber);
        firstCallStopwatch.Stop();

        // Act - Second call (cache hit)
        var secondCallStopwatch = Stopwatch.StartNew();
        var secondResult = await _lookupService.LookupNumberAsync(testNumber);
        secondCallStopwatch.Stop();

        // Assert
        Assert.True(secondCallStopwatch.ElapsedMilliseconds < firstCallStopwatch.ElapsedMilliseconds,
            $"Cached call ({secondCallStopwatch.ElapsedMilliseconds}ms) should be faster than uncached call ({firstCallStopwatch.ElapsedMilliseconds}ms)");
        
        Assert.Equal(firstResult.Count(), secondResult.Count());

        var improvementRatio = (double)firstCallStopwatch.ElapsedMilliseconds / secondCallStopwatch.ElapsedMilliseconds;
        Assert.True(improvementRatio > 1.5, $"Cache should provide at least 50% improvement, got {improvementRatio:F2}x");

        _output.WriteLine($"Cache miss: {firstCallStopwatch.ElapsedMilliseconds}ms, Cache hit: {secondCallStopwatch.ElapsedMilliseconds}ms, Improvement: {improvementRatio:F2}x");
    }

    [Fact]
    public async Task CombinationSearch_WithLargeDataset_CompletesWithinTimeLimit()
    {
        // Arrange
        const int timeoutMs = 2000; // 2 second timeout for combination search
        var combination = new int[] { 1, 7, 13, 21, 28, 35 };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _lookupService.SearchCombinationAsync(combination, true);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs,
            $"Combination search took {stopwatch.ElapsedMilliseconds}ms, expected < {timeoutMs}ms");
        Assert.NotNull(result);

        _output.WriteLine($"Combination search completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task FrequencyAnalysis_CompletesWithinTimeLimit()
    {
        // Arrange
        const int timeoutMs = 1000; // 1 second timeout
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _frequencyService.GetNumberFrequenciesAsync();
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs,
            $"Frequency analysis took {stopwatch.ElapsedMilliseconds}ms, expected < {timeoutMs}ms");
        Assert.NotEmpty(result);

        _output.WriteLine($"Frequency analysis completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentLookups_HandleMultipleRequestsEfficiently()
    {
        // Arrange
        const int concurrentRequests = 10;
        const int timeoutMs = 3000; // 3 seconds for all concurrent requests
        var numbers = Enumerable.Range(1, concurrentRequests).ToArray();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = numbers.Select(num => _lookupService.LookupNumberAsync(num)).ToArray();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs,
            $"Concurrent lookups took {stopwatch.ElapsedMilliseconds}ms, expected < {timeoutMs}ms");
        Assert.Equal(concurrentRequests, results.Length);
        Assert.All(results, result => Assert.NotNull(result));

        var averageTimePerRequest = (double)stopwatch.ElapsedMilliseconds / concurrentRequests;
        _output.WriteLine($"Concurrent lookups: {stopwatch.ElapsedMilliseconds}ms total, {averageTimePerRequest:F2}ms average per request");
    }

    [Fact]
    public async Task PaginationPerformance_ScalesLinearlyWithPageSize()
    {
        // Arrange
        const int testNumber = 7;
        var pageSizes = new int[] { 10, 25, 50, 100 };
        var timings = new List<(int PageSize, long ElapsedMs)>();

        // Act
        foreach (var pageSize in pageSizes)
        {
            var pagination = new PaginationRequest { Page = 1, PageSize = pageSize };
            
            var stopwatch = Stopwatch.StartNew();
            var result = await _lookupService.LookupNumberPaginatedAsync(testNumber, pagination);
            stopwatch.Stop();

            timings.Add((pageSize, stopwatch.ElapsedMilliseconds));
            
            Assert.True(result.ItemCount <= pageSize, $"Result count {result.ItemCount} should not exceed page size {pageSize}");
        }

        // Assert - Performance should scale reasonably with page size
        for (int i = 1; i < timings.Count; i++)
        {
            var current = timings[i];
            var previous = timings[i - 1];
            
            // Allow for some variance, but performance shouldn't degrade exponentially
            var scalingFactor = (double)current.ElapsedMs / previous.ElapsedMs;
            var pageSizeRatio = (double)current.PageSize / previous.PageSize;
            
            Assert.True(scalingFactor <= pageSizeRatio * 2, 
                $"Performance scaling factor {scalingFactor:F2} is too high for page size ratio {pageSizeRatio:F2}");
        }

        _output.WriteLine("Pagination performance scaling:");
        foreach (var (pageSize, elapsedMs) in timings)
        {
            _output.WriteLine($"  Page size {pageSize}: {elapsedMs}ms");
        }
    }

    [Fact]
    public async Task PerformanceMonitoring_TracksOperationMetrics()
    {
        // Arrange
        const int testNumber = 15;

        // Act
        var initialMetrics = await _performanceMonitoring.GetCurrentMetricsAsync();
        
        // Perform some operations
        await _lookupService.LookupNumberAsync(testNumber);
        await _lookupService.LookupNumberAsync(testNumber + 1);
        await _lookupService.LookupNumberAsync(testNumber + 2);

        var finalMetrics = await _performanceMonitoring.GetCurrentMetricsAsync();

        // Assert
        Assert.True(finalMetrics.OperationsPerSecond >= initialMetrics.OperationsPerSecond,
            "Operations per second should increase or stay the same");
        
        Assert.True(finalMetrics.OperationCounts.ContainsKey("number_lookup"),
            "Should track number lookup operations");

        _output.WriteLine($"Initial ops/sec: {initialMetrics.OperationsPerSecond:F2}, Final ops/sec: {finalMetrics.OperationsPerSecond:F2}");
        _output.WriteLine($"Operation counts: {string.Join(", ", finalMetrics.OperationCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
    }

    [Fact]
    public async Task CacheHitRate_ImprovesWithRepeatedAccess()
    {
        // Arrange
        var testNumbers = new int[] { 5, 10, 15, 20, 25 };
        
        // Clear cache
        await _cacheService.ClearAllAsync();

        // Act - First round (cache misses)
        foreach (var number in testNumbers)
        {
            await _lookupService.LookupNumberAsync(number);
        }

        var firstRoundStats = await _cacheService.GetStatisticsAsync();

        // Act - Second round (cache hits)
        foreach (var number in testNumbers)
        {
            await _lookupService.LookupNumberAsync(number);
        }

        var secondRoundStats = await _cacheService.GetStatisticsAsync();

        // Assert
        var firstRoundHitRate = CalculateHitRate(firstRoundStats);
        var secondRoundHitRate = CalculateHitRate(secondRoundStats);

        Assert.True(secondRoundHitRate > firstRoundHitRate,
            $"Second round hit rate ({secondRoundHitRate:F2}%) should be higher than first round ({firstRoundHitRate:F2}%)");

        _output.WriteLine($"First round hit rate: {firstRoundHitRate:F2}%, Second round hit rate: {secondRoundHitRate:F2}%");
    }

    [Fact]
    public async Task LazyLoading_PerformsBetterThanEagerLoading()
    {
        // Arrange
        const int testNumber = 12;
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act - Paginated (lazy) loading
        var lazyStopwatch = Stopwatch.StartNew();
        var lazyResult = await _lookupService.LookupNumberPaginatedAsync(testNumber, pagination);
        lazyStopwatch.Stop();

        // Act - Full (eager) loading
        var eagerStopwatch = Stopwatch.StartNew();
        var eagerResult = await _lookupService.LookupNumberAsync(testNumber);
        eagerStopwatch.Stop();

        // Assert
        Assert.True(lazyStopwatch.ElapsedMilliseconds <= eagerStopwatch.ElapsedMilliseconds,
            $"Lazy loading ({lazyStopwatch.ElapsedMilliseconds}ms) should be faster than or equal to eager loading ({eagerStopwatch.ElapsedMilliseconds}ms)");

        // Verify we got the expected subset
        Assert.True(lazyResult.ItemCount <= pagination.PageSize);
        Assert.True(lazyResult.ItemCount <= eagerResult.Count());

        _output.WriteLine($"Lazy loading: {lazyStopwatch.ElapsedMilliseconds}ms ({lazyResult.ItemCount} items), Eager loading: {eagerStopwatch.ElapsedMilliseconds}ms ({eagerResult.Count()} items)");
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 25)]
    [InlineData(1, 50)]
    [InlineData(1, 100)]
    public async Task PaginationResponseTime_StaysWithinLimits(int page, int pageSize)
    {
        // Arrange
        const int maxResponseTimeMs = 300; // 300ms limit
        const int testNumber = 8;
        var pagination = new PaginationRequest { Page = page, PageSize = pageSize };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _lookupService.LookupNumberPaginatedAsync(testNumber, pagination);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTimeMs,
            $"Pagination (page {page}, size {pageSize}) took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTimeMs}ms");
        
        Assert.NotNull(result);
        Assert.True(result.ItemCount <= pageSize);

        _output.WriteLine($"Page {page}, Size {pageSize}: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task LargeDatasetSearch_CompletesWithinTimeLimit()
    {
        // Arrange - Create a large dataset
        await SeedLargeDataset(10000); // 10,000 draws
        const int timeoutMs = 2000; // 2 second timeout
        const int testNumber = 15;

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _lookupService.LookupNumberAsync(testNumber);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < timeoutMs,
            $"Large dataset search took {stopwatch.ElapsedMilliseconds}ms, expected < {timeoutMs}ms");
        Assert.NotEmpty(result);

        _output.WriteLine($"Large dataset search ({result.Count()} results) completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task PaginationWithLargeDataset_PerformsBetterThanFullQuery()
    {
        // Arrange - Create large dataset
        await SeedLargeDataset(5000);
        const int testNumber = 7;
        var pagination = new PaginationRequest { Page = 1, PageSize = 50 };

        // Act - Full query
        var fullStopwatch = Stopwatch.StartNew();
        var fullResult = await _lookupService.LookupNumberAsync(testNumber);
        fullStopwatch.Stop();

        // Act - Paginated query
        var paginatedStopwatch = Stopwatch.StartNew();
        var paginatedResult = await _lookupService.LookupNumberPaginatedAsync(testNumber, pagination);
        paginatedStopwatch.Stop();

        // Assert
        Assert.True(paginatedStopwatch.ElapsedMilliseconds <= fullStopwatch.ElapsedMilliseconds,
            $"Paginated query ({paginatedStopwatch.ElapsedMilliseconds}ms) should be faster than or equal to full query ({fullStopwatch.ElapsedMilliseconds}ms)");

        var performanceImprovement = (double)fullStopwatch.ElapsedMilliseconds / paginatedStopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Pagination performance improvement: {performanceImprovement:F2}x faster");
        _output.WriteLine($"Full query: {fullResult.Count()} results in {fullStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Paginated query: {paginatedResult.ItemCount} results in {paginatedStopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task LazyLoadingPerformance_ShowsSignificantImprovement()
    {
        // Arrange
        await SeedLargeDataset(2000);
        const int testNumber = 12;
        var smallPage = new PaginationRequest { Page = 1, PageSize = 10 };
        var largePage = new PaginationRequest { Page = 1, PageSize = 100 };

        // Act - Small page (lazy loading)
        var lazyStopwatch = Stopwatch.StartNew();
        var lazyResult = await _lookupService.LookupNumberPaginatedAsync(testNumber, smallPage);
        lazyStopwatch.Stop();

        // Act - Large page (more eager loading)
        var eagerStopwatch = Stopwatch.StartNew();
        var eagerResult = await _lookupService.LookupNumberPaginatedAsync(testNumber, largePage);
        eagerStopwatch.Stop();

        // Assert
        Assert.True(lazyStopwatch.ElapsedMilliseconds <= eagerStopwatch.ElapsedMilliseconds,
            $"Lazy loading ({lazyStopwatch.ElapsedMilliseconds}ms) should be faster than or equal to eager loading ({eagerStopwatch.ElapsedMilliseconds}ms)");

        _output.WriteLine($"Lazy loading (10 items): {lazyStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Eager loading (100 items): {eagerStopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CacheHitRateImprovement_MeetsPerformanceTargets()
    {
        // Arrange
        var testNumbers = new int[] { 5, 10, 15, 20, 25, 30, 35, 40 };
        await _cacheService.ClearAllAsync();

        // Act - First round (populate cache)
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

        Assert.True(improvementRatio >= 2.0, $"Cache should provide at least 2x improvement, got {improvementRatio:F2}x");

        var firstRoundHitRate = CalculateHitRate(firstRoundStats);
        var secondRoundHitRate = CalculateHitRate(secondRoundStats);

        Assert.True(secondRoundHitRate >= 80.0, $"Cache hit rate should be at least 80%, got {secondRoundHitRate:F2}%");

        _output.WriteLine($"First round average: {firstRoundAvg:F2}ms, Hit rate: {firstRoundHitRate:F2}%");
        _output.WriteLine($"Second round average: {secondRoundAvg:F2}ms, Hit rate: {secondRoundHitRate:F2}%");
        _output.WriteLine($"Performance improvement: {improvementRatio:F2}x");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task PaginationScalability_MaintainsPerformanceAcrossPageSizes(int pageSize)
    {
        // Arrange
        await SeedLargeDataset(1000);
        const int testNumber = 7;
        const int maxResponseTimeMs = 500; // 500ms limit regardless of page size
        var pagination = new PaginationRequest { Page = 1, PageSize = pageSize };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _lookupService.LookupNumberPaginatedAsync(testNumber, pagination);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTimeMs,
            $"Pagination with page size {pageSize} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTimeMs}ms");

        Assert.True(result.ItemCount <= pageSize);
        Assert.True(result.ItemCount >= 0);

        var timePerItem = result.ItemCount > 0 ? (double)stopwatch.ElapsedMilliseconds / result.ItemCount : 0;
        _output.WriteLine($"Page size {pageSize}: {stopwatch.ElapsedMilliseconds}ms total, {timePerItem:F2}ms per item");
    }

    [Fact]
    public async Task ConcurrentPaginatedRequests_HandleLoadEfficiently()
    {
        // Arrange
        await SeedLargeDataset(2000);
        const int concurrentRequests = 20;
        const int maxTotalTimeMs = 5000; // 5 seconds for all requests
        var numbers = Enumerable.Range(1, concurrentRequests).ToArray();
        var pagination = new PaginationRequest { Page = 1, PageSize = 25 };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = numbers.Select(num => _lookupService.LookupNumberPaginatedAsync(num, pagination)).ToArray();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxTotalTimeMs,
            $"Concurrent paginated requests took {stopwatch.ElapsedMilliseconds}ms, expected < {maxTotalTimeMs}ms");

        Assert.Equal(concurrentRequests, results.Length);
        Assert.All(results, result => Assert.NotNull(result));

        var averageTimePerRequest = (double)stopwatch.ElapsedMilliseconds / concurrentRequests;
        var totalResults = results.Sum(r => r.ItemCount);

        _output.WriteLine($"Concurrent requests: {stopwatch.ElapsedMilliseconds}ms total, {averageTimePerRequest:F2}ms average per request");
        _output.WriteLine($"Total results returned: {totalResults}");
    }

    [Fact]
    public async Task MemoryUsageUnderLoad_StaysWithinLimits()
    {
        // Arrange
        await SeedLargeDataset(3000);
        var initialMemory = GC.GetTotalMemory(true);
        const long maxMemoryIncreaseMB = 100; // 100MB limit

        // Act - Perform multiple operations
        var tasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            var number = (i % 40) + 1;
            var pagination = new PaginationRequest { Page = 1, PageSize = 50 };
            tasks.Add(_lookupService.LookupNumberPaginatedAsync(number, pagination));
        }

        await Task.WhenAll(tasks);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024 * 1024);

        // Assert
        Assert.True(memoryIncreaseMB < maxMemoryIncreaseMB,
            $"Memory usage increased by {memoryIncreaseMB}MB, expected < {maxMemoryIncreaseMB}MB");

        _output.WriteLine($"Initial memory: {initialMemory / (1024 * 1024)}MB");
        _output.WriteLine($"Final memory: {finalMemory / (1024 * 1024)}MB");
        _output.WriteLine($"Memory increase: {memoryIncreaseMB}MB");
    }

    [Fact]
    public async Task DatabaseQueryOptimization_MeetsPerformanceTargets()
    {
        // Arrange
        await SeedLargeDataset(5000);
        const int testNumber = 13;
        const int maxQueryTimeMs = 100; // 100ms for database query

        // Act - Measure database query time specifically
        using var perfContext = _performanceMonitoring.StartOperation("database_query_test");
        
        var stopwatch = Stopwatch.StartNew();
        var result = await _lookupService.LookupNumberAsync(testNumber);
        stopwatch.Stop();

        _performanceMonitoring.CompleteOperation(perfContext, result.Count());

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxQueryTimeMs,
            $"Database query took {stopwatch.ElapsedMilliseconds}ms, expected < {maxQueryTimeMs}ms");

        var metrics = await _performanceMonitoring.GetCurrentMetricsAsync();
        Assert.True(metrics.AverageResponseTime.TotalMilliseconds < maxQueryTimeMs);

        _output.WriteLine($"Database query time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Results returned: {result.Count()}");
        _output.WriteLine($"Average response time: {metrics.AverageResponseTime.TotalMilliseconds:F2}ms");
    }

    private async Task SeedLargeDataset(int drawCount)
    {
        // Clear existing data
        _context.NumberOccurrences.RemoveRange(_context.NumberOccurrences);
        _context.NumberFrequencies.RemoveRange(_context.NumberFrequencies);
        _context.LottoDraws.RemoveRange(_context.LottoDraws);
        await _context.SaveChangesAsync();

        // Create large dataset
        var draws = new List<LottoDraw>();
        var occurrences = new List<NumberOccurrence>();
        var random = new Random(42); // Fixed seed for consistent tests

        for (int i = 1; i <= drawCount; i++)
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

            // Create occurrences for this draw
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

        // Batch insert for better performance
        _context.LottoDraws.AddRange(draws);
        await _context.SaveChangesAsync();

        _context.NumberOccurrences.AddRange(occurrences);
        await _context.SaveChangesAsync();

        // Update frequencies
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
                AverageFrequency = numberOccurrences.Count / (double)drawCount,
                LongestGap = random.Next(1, 50),
                LastCalculated = DateTime.UtcNow
            });
        }

        _context.NumberFrequencies.AddRange(frequencies);
        await _context.SaveChangesAsync();

        _output.WriteLine($"Seeded large dataset with {drawCount} draws and {occurrences.Count} occurrences");
    }

    private void SeedTestData()
    {
        // Create test draws
        var draws = new List<LottoDraw>();
        var random = new Random(42); // Fixed seed for consistent tests

        for (int i = 1; i <= 1000; i++)
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
                AverageFrequency = numberOccurrences.Count / 1000.0,
                LongestGap = random.Next(1, 50),
                LastCalculated = DateTime.UtcNow
            });
        }

        _context.NumberFrequencies.AddRange(frequencies);
        _context.SaveChanges();
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