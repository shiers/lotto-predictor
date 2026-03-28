using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Property-based test for partial match identification functionality
/// **Feature: lottery-lookup-navigation, Property 5: Partial matches are identified correctly**
/// **Validates: Requirements 2.3**
/// 
/// This test validates that for any combination search, partial matches are shown 
/// with accurate match counts when a combination has appeared partially.
/// </summary>
public static class PartialMatchIdentificationPropertyTest
{
    public static async Task RunPartialMatchIdentificationPropertyTest()
    {
        Console.WriteLine("Running Partial Match Identification Property Test...");
        Console.WriteLine("====================================================");

        try
        {
            Console.WriteLine("Property Test 5: Partial matches are identified correctly...");
            await PartialMatchesAreIdentifiedCorrectly_PropertyTest();
            Console.WriteLine("✓ PASSED: Partial matches are identified correctly (100 iterations)");

            Console.WriteLine("\nProperty Test 5a: Partial matches show accurate match counts...");
            await PartialMatchesShowAccurateMatchCounts_PropertyTest();
            Console.WriteLine("✓ PASSED: Partial matches show accurate match counts (50 iterations)");

            Console.WriteLine("\nProperty Test 5b: Partial matches contain only matched numbers...");
            await PartialMatchesContainOnlyMatchedNumbers_PropertyTest();
            Console.WriteLine("✓ PASSED: Partial matches contain only matched numbers (50 iterations)");

            Console.WriteLine("\nProperty Test 5c: Partial matches are not marked as exact matches...");
            await PartialMatchesAreNotMarkedAsExactMatches_PropertyTest();
            Console.WriteLine("✓ PASSED: Partial matches are not marked as exact matches (50 iterations)");

            Console.WriteLine("\n====================================================");
            Console.WriteLine("Partial match identification property test PASSED!");
            Console.WriteLine("Property 5: Partial matches are identified correctly - VALIDATED");
            Console.WriteLine("Requirements 2.3 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            throw;
        }
    }

    private static async Task PartialMatchesAreIdentifiedCorrectly_PropertyTest()
    {
        const int iterations = 100;
        var random = new Random(52); // Fixed seed for reproducible tests

        for (int i = 0; i < iterations; i++)
        {
            using var serviceProvider = CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<LottoDbContext>();
            var numberLookupService = serviceProvider.GetRequiredService<INumberLookupService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            try
            {
                // Arrange - Generate test data with guaranteed partial matches
                var combination = GenerateValidCombination(random);
                var testDraws = GenerateTestDrawsWithPartialMatches(random, combination, 15);
                
                await ClearDatabase(context, cacheService);
                await SeedTestData(context, testDraws);
                
                // Act - Search for the combination with partial matches enabled
                var result = await numberLookupService.SearchCombinationAsync(combination, includePartialMatches: true);
                
                // Assert - Validate that partial matches are identified correctly
                if (!ValidatePartialMatchIdentification(result, combination, testDraws))
                {
                    throw new Exception($"Partial match identification validation failed on iteration {i + 1}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }
    }

    private static async Task PartialMatchesShowAccurateMatchCounts_PropertyTest()
    {
        const int iterations = 50;
        var random = new Random(53);

        for (int i = 0; i < iterations; i++)
        {
            using var serviceProvider = CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<LottoDbContext>();
            var numberLookupService = serviceProvider.GetRequiredService<INumberLookupService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            try
            {
                var combination = GenerateValidCombination(random);
                var testDraws = GenerateTestDrawsWithPartialMatches(random, combination, 10);
                
                await ClearDatabase(context, cacheService);
                await SeedTestData(context, testDraws);
                
                var result = await numberLookupService.SearchCombinationAsync(combination, includePartialMatches: true);
                
                // Assert - Every partial match should have accurate match count
                var isValid = result.PartialMatches.All(match => 
                {
                    var actualMatchCount = combination.Count(num => match.WinningCombination.Contains(num));
                    return match.MatchCount == actualMatchCount && 
                           match.MatchCount >= 2 && 
                           match.MatchCount < combination.Length &&
                           match.MatchedNumbers.Length == actualMatchCount;
                });
                
                if (!isValid)
                {
                    throw new Exception($"Partial match count accuracy validation failed on iteration {i + 1}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }
    }

    private static async Task PartialMatchesContainOnlyMatchedNumbers_PropertyTest()
    {
        const int iterations = 50;
        var random = new Random(54);

        for (int i = 0; i < iterations; i++)
        {
            using var serviceProvider = CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<LottoDbContext>();
            var numberLookupService = serviceProvider.GetRequiredService<INumberLookupService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            try
            {
                var combination = GenerateValidCombination(random);
                var testDraws = GenerateTestDrawsWithPartialMatches(random, combination, 10);
                
                await ClearDatabase(context, cacheService);
                await SeedTestData(context, testDraws);
                
                var result = await numberLookupService.SearchCombinationAsync(combination, includePartialMatches: true);
                
                // Assert - Every partial match should contain only numbers that are in the original combination
                var isValid = result.PartialMatches.All(match => 
                    match.MatchedNumbers.All(num => combination.Contains(num)) &&
                    match.MatchedNumbers.All(num => match.WinningCombination.Contains(num)));
                
                if (!isValid)
                {
                    throw new Exception($"Partial match matched numbers validation failed on iteration {i + 1}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }
    }

    private static async Task PartialMatchesAreNotMarkedAsExactMatches_PropertyTest()
    {
        const int iterations = 50;
        var random = new Random(55);

        for (int i = 0; i < iterations; i++)
        {
            using var serviceProvider = CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<LottoDbContext>();
            var numberLookupService = serviceProvider.GetRequiredService<INumberLookupService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            try
            {
                var combination = GenerateValidCombination(random);
                var testDraws = GenerateTestDrawsWithPartialMatches(random, combination, 10);
                
                await ClearDatabase(context, cacheService);
                await SeedTestData(context, testDraws);
                
                var result = await numberLookupService.SearchCombinationAsync(combination, includePartialMatches: true);
                
                // Assert - No partial match should be marked as an exact match
                var isValid = result.PartialMatches.All(match => !match.IsExactMatch);
                
                if (!isValid)
                {
                    throw new Exception($"Partial match exact match flag validation failed on iteration {i + 1}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add in-memory database
        services.AddDbContext<LottoDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Add distributed cache (in-memory for tests)
        services.AddDistributedMemoryCache();
        
        // Add cache service (test implementation)
        services.AddSingleton<ICacheService, TestCacheService>();
        
        // Add number lookup service
        services.AddScoped<INumberLookupService, NumberLookupService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<LottoDbContext>();
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        return serviceProvider;
    }

    private static int[] GenerateValidCombination(Random random)
    {
        var length = random.Next(3, 7); // 3-6 numbers to ensure partial matches are possible
        var numbers = new HashSet<int>();
        
        while (numbers.Count < length)
        {
            numbers.Add(random.Next(1, 41)); // Range [1, 40]
        }
        
        return numbers.OrderBy(x => x).ToArray();
    }

    private static List<LottoDraw> GenerateTestDrawsWithPartialMatches(Random random, int[] combination, int count)
    {
        var draws = new List<LottoDraw>();
        
        for (int i = 0; i < count; i++)
        {
            var numbers = new HashSet<int>();
            
            // Ensure some draws have partial matches by including some numbers from the combination
            if (i < count / 2) // First half will have partial matches
            {
                // Include 2 to (combination.Length - 1) numbers from the combination
                var numbersToInclude = random.Next(2, combination.Length);
                var selectedNumbers = combination.OrderBy(x => random.Next()).Take(numbersToInclude);
                
                foreach (var num in selectedNumbers)
                {
                    numbers.Add(num);
                }
            }
            
            // Fill remaining slots with random numbers
            while (numbers.Count < 6)
            {
                var randomNum = random.Next(1, 41);
                numbers.Add(randomNum);
            }
            
            var winningNumbers = numbers.OrderBy(x => x).ToArray();
            
            draws.Add(new LottoDraw
            {
                Draw = i + 1,
                Date = DateTime.Today.AddDays(-count + i),
                WinningNumber1 = winningNumbers[0],
                WinningNumber2 = winningNumbers[1],
                WinningNumber3 = winningNumbers[2],
                WinningNumber4 = winningNumbers[3],
                WinningNumber5 = winningNumbers[4],
                WinningNumber6 = winningNumbers[5],
                BonusNumber = random.Next(1, 41),
                Powerball = random.Next(1, 11)
            });
        }
        
        return draws;
    }

    private static async Task ClearDatabase(LottoDbContext context, ICacheService cacheService)
    {
        context.NumberOccurrences.RemoveRange(context.NumberOccurrences);
        context.LottoDraws.RemoveRange(context.LottoDraws);
        await context.SaveChangesAsync();
        
        // Clear cache
        await cacheService.ClearAllAsync();
    }

    private static async Task SeedTestData(LottoDbContext context, List<LottoDraw> testDraws)
    {
        // Add draws to database
        context.LottoDraws.AddRange(testDraws);
        await context.SaveChangesAsync();
        
        // Create number occurrences for each draw
        var occurrences = new List<NumberOccurrence>();
        
        foreach (var draw in testDraws)
        {
            var winningNumbers = new[] 
            { 
                draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 
            };
            
            for (int i = 0; i < winningNumbers.Length; i++)
            {
                occurrences.Add(new NumberOccurrence
                {
                    DrawNumber = draw.Draw,
                    Number = winningNumbers[i],
                    Position = i + 1,
                    DrawDate = draw.Date,
                    IsBonus = false,
                    IsPowerball = false
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
                IsPowerball = false
            });
            
            // Add powerball occurrence
            occurrences.Add(new NumberOccurrence
            {
                DrawNumber = draw.Draw,
                Number = draw.Powerball,
                Position = 8,
                DrawDate = draw.Date,
                IsBonus = false,
                IsPowerball = true
            });
        }
        
        context.NumberOccurrences.AddRange(occurrences);
        await context.SaveChangesAsync();
    }

    private static bool ValidatePartialMatchIdentification(CombinationSearchResult result, int[] combination, List<LottoDraw> testDraws)
    {
        // 1. Result should not be null
        if (result == null)
            return false;
        
        // 2. Searched combination should match input
        if (!result.SearchedCombination.SequenceEqual(combination))
            return false;
        
        // 3. Calculate expected partial matches from test data
        var expectedPartialMatches = testDraws.Where(draw =>
        {
            var winningNumbers = new[] 
            { 
                draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 
            };
            var matchCount = combination.Count(num => winningNumbers.Contains(num));
            return matchCount >= 2 && matchCount < combination.Length;
        }).ToList();
        
        // 4. Validate that the correct number of partial matches are found
        if (result.TotalPartialMatches != expectedPartialMatches.Count)
            return false;
        
        // 5. Validate each partial match
        foreach (var partialMatch in result.PartialMatches)
        {
            // Should not be marked as exact match
            if (partialMatch.IsExactMatch)
                return false;
            
            // Should have at least 2 matches but less than combination length
            if (partialMatch.MatchCount < 2 || partialMatch.MatchCount >= combination.Length)
                return false;
            
            // Matched numbers should be subset of both combination and winning numbers
            if (!partialMatch.MatchedNumbers.All(num => combination.Contains(num)))
                return false;
            
            if (!partialMatch.MatchedNumbers.All(num => partialMatch.WinningCombination.Contains(num)))
                return false;
            
            // Match count should equal the length of matched numbers array
            if (partialMatch.MatchCount != partialMatch.MatchedNumbers.Length)
                return false;
            
            // Verify the match count is accurate
            var actualMatchCount = combination.Count(num => partialMatch.WinningCombination.Contains(num));
            if (partialMatch.MatchCount != actualMatchCount)
                return false;
        }
        
        // 6. Validate that partial matches are ordered correctly (by match count desc, then by date desc)
        var orderedPartialMatches = result.PartialMatches.ToList();
        for (int i = 0; i < orderedPartialMatches.Count - 1; i++)
        {
            var current = orderedPartialMatches[i];
            var next = orderedPartialMatches[i + 1];
            
            if (current.MatchCount < next.MatchCount)
                return false;
            
            if (current.MatchCount == next.MatchCount && current.DrawDate < next.DrawDate)
                return false;
        }
        
        return true;
    }

    // Test cache service implementation
    public class TestCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _cache = new();

        public async Task<T?> GetAsync<T>(string key, CacheLevel level = CacheLevel.Both)
        {
            await Task.CompletedTask;
            return _cache.TryGetValue(key, out var value) ? (T?)value : default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both)
        {
            await Task.CompletedTask;
            _cache[key] = value!;
        }

        public async Task RemoveAsync(string key, CacheLevel level = CacheLevel.Both)
        {
            await Task.CompletedTask;
            _cache.Remove(key);
        }

        public async Task ClearAllAsync(CacheLevel level = CacheLevel.Both)
        {
            await Task.CompletedTask;
            _cache.Clear();
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CacheLevel level = CacheLevel.Both)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return (T?)value;
            }

            var newValue = await factory();
            _cache[key] = newValue!;
            return newValue;
        }

        public async Task RemoveByPatternAsync(string pattern, CacheLevel level = CacheLevel.Both)
        {
            await Task.CompletedTask;
            var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        public async Task WarmUpCacheAsync()
        {
            await Task.CompletedTask;
            // No-op for tests
        }

        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            await Task.CompletedTask;
            return new CacheStatistics
            {
                MemoryCacheEntries = _cache.Count,
                MemoryCacheHits = 0,
                MemoryCacheMisses = 0,
                DistributedCacheEntries = 0,
                DistributedCacheHits = 0,
                DistributedCacheMisses = 0
            };
        }
    }
}

