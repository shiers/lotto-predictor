using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

/// <summary>
/// Simple property-based test for combination search functionality
/// **Feature: lottery-lookup-navigation, Property 4: Combination search finds matching draws**
/// **Validates: Requirements 2.1**
/// </summary>
public static class CombinationSearchPropertyTestSimple
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running Combination Search Property Test...");
        Console.WriteLine("==========================================");

        try
        {
            Console.WriteLine("Property Test 4: Combination search finds matching draws...");
            await CombinationSearchFindsMatchingDraws_PropertyTest();
            Console.WriteLine("✓ PASSED: Combination search finds matching draws (100 iterations)");

            Console.WriteLine("\n==========================================");
            Console.WriteLine("Combination search property test PASSED!");
            Console.WriteLine("Property 4: Combination search finds matching draws - VALIDATED");
            Console.WriteLine("Requirements 2.1 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task CombinationSearchFindsMatchingDraws_PropertyTest()
    {
        const int iterations = 100;
        var random = new Random(42); // Fixed seed for reproducible tests

        for (int i = 0; i < iterations; i++)
        {
            using var serviceProvider = CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<LottoDbContext>();
            var numberLookupService = serviceProvider.GetRequiredService<INumberLookupService>();

            try
            {
                // Arrange - Generate test data
                var combination = GenerateValidCombination(random);
                var testDraws = GenerateTestDraws(random, 10);
                
                await ClearDatabase(context);
                await SeedTestData(context, testDraws);
                
                // Act - Search for the combination
                var result = await numberLookupService.SearchCombinationAsync(combination, includePartialMatches: true);
                
                // Assert - Validate the property
                if (!ValidateCombinationSearchResult(result, combination, testDraws))
                {
                    throw new Exception($"Combination search validation failed on iteration {i + 1}");
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
        
        // Add cache service
        services.AddSingleton<ICacheService, CacheService>();
        
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
        var length = random.Next(2, 7); // 2-6 numbers
        var numbers = new HashSet<int>();
        
        while (numbers.Count < length)
        {
            numbers.Add(random.Next(1, 41)); // Range [1, 40]
        }
        
        return numbers.OrderBy(x => x).ToArray();
    }

    private static List<LottoDraw> GenerateTestDraws(Random random, int count)
    {
        var draws = new List<LottoDraw>();
        
        for (int i = 0; i < count; i++)
        {
            var numbers = new HashSet<int>();
            while (numbers.Count < 6)
            {
                numbers.Add(random.Next(1, 41));
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

    private static async Task ClearDatabase(LottoDbContext context)
    {
        context.NumberOccurrences.RemoveRange(context.NumberOccurrences);
        context.LottoDraws.RemoveRange(context.LottoDraws);
        await context.SaveChangesAsync();
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

    private static bool ValidateCombinationSearchResult(CombinationSearchResult result, int[] combination, List<LottoDraw> testDraws)
    {
        // 1. Result should not be null
        if (result == null)
            return false;
        
        // 2. Searched combination should match input
        if (!result.SearchedCombination.SequenceEqual(combination))
            return false;
        
        // 3. Validate exact matches
        var expectedExactMatches = testDraws.Where(draw =>
        {
            var winningNumbers = new[] 
            { 
                draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 
            };
            return combination.All(num => winningNumbers.Contains(num));
        }).ToList();
        
        if (result.TotalExactMatches != expectedExactMatches.Count)
            return false;
        
        // 4. Validate partial matches (at least 2 numbers match but not all)
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
        
        if (result.TotalPartialMatches != expectedPartialMatches.Count)
            return false;
        
        // 5. Validate that exact matches are marked correctly
        foreach (var exactMatch in result.ExactMatches)
        {
            if (!exactMatch.IsExactMatch || exactMatch.MatchCount != combination.Length)
                return false;
        }
        
        // 6. Validate that partial matches are marked correctly
        foreach (var partialMatch in result.PartialMatches)
        {
            if (partialMatch.IsExactMatch || partialMatch.MatchCount < 2 || partialMatch.MatchCount >= combination.Length)
                return false;
        }
        
        // 7. Validate that all matches have valid draw numbers and dates
        var allMatches = result.ExactMatches.Concat(result.PartialMatches);
        foreach (var match in allMatches)
        {
            var correspondingDraw = testDraws.FirstOrDefault(d => d.Draw == match.DrawNumber);
            if (correspondingDraw == null || correspondingDraw.Date != match.DrawDate)
                return false;
        }
        
        return true;
    }
}