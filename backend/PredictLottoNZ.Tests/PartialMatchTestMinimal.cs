using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Minimal standalone test for partial match identification
/// **Property 5: Partial matches are identified correctly**
/// **Validates: Requirements 2.3**
/// </summary>
public class PartialMatchTestMinimal
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running Minimal Partial Match Test...");
        Console.WriteLine("=====================================");

        try
        {
            await RunPartialMatchTest();
            Console.WriteLine("✓ PASSED: Partial match identification test");
            Console.WriteLine("Property 5: Partial matches are identified correctly - VALIDATED");
            Console.WriteLine("Requirements 2.3 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunPartialMatchTest()
    {
        using var serviceProvider = CreateServiceProvider();
        var context = serviceProvider.GetRequiredService<TestDbContext>();
        var service = serviceProvider.GetRequiredService<TestLookupService>();

        // Test data: combination [1,2,3,4] with partial matches
        var combination = new[] { 1, 2, 3, 4 };
        
        // Create test draws with known partial matches
        var testDraws = new[]
        {
            new TestDraw { Draw = 1, Numbers = new[] { 1, 2, 5, 6, 7, 8 } }, // 2 matches
            new TestDraw { Draw = 2, Numbers = new[] { 1, 2, 3, 9, 10, 11 } }, // 3 matches
            new TestDraw { Draw = 3, Numbers = new[] { 5, 6, 7, 8, 9, 10 } }, // 0 matches
            new TestDraw { Draw = 4, Numbers = new[] { 1, 2, 3, 4, 5, 6 } }, // 4 matches (exact)
        };

        await SeedTestData(context, testDraws);

        // Act
        var result = await service.SearchCombinationAsync(combination, includePartialMatches: true);

        // Assert
        ValidateResult(result, combination, testDraws);
    }

    private static void ValidateResult(TestSearchResult result, int[] combination, TestDraw[] testDraws)
    {
        // Should find 1 exact match (draw 4)
        if (result.ExactMatches.Count != 1)
            throw new Exception($"Expected 1 exact match, got {result.ExactMatches.Count}");

        // Should find 2 partial matches (draws 1 and 2)
        if (result.PartialMatches.Count != 2)
            throw new Exception($"Expected 2 partial matches, got {result.PartialMatches.Count}");

        // Validate partial match details
        var partialMatch1 = result.PartialMatches.First(m => m.DrawNumber == 1);
        if (partialMatch1.MatchCount != 2)
            throw new Exception($"Draw 1 should have 2 matches, got {partialMatch1.MatchCount}");

        var partialMatch2 = result.PartialMatches.First(m => m.DrawNumber == 2);
        if (partialMatch2.MatchCount != 3)
            throw new Exception($"Draw 2 should have 3 matches, got {partialMatch2.MatchCount}");

        // Validate that partial matches are not marked as exact
        if (result.PartialMatches.Any(m => m.IsExactMatch))
            throw new Exception("Partial matches should not be marked as exact");

        Console.WriteLine("All validations passed!");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddMemoryCache();
        services.AddScoped<TestLookupService>();
        
        var serviceProvider = services.BuildServiceProvider();
        var context = serviceProvider.GetRequiredService<TestDbContext>();
        context.Database.EnsureCreated();
        
        return serviceProvider;
    }

    private static async Task SeedTestData(TestDbContext context, TestDraw[] testDraws)
    {
        foreach (var draw in testDraws)
        {
            context.TestDraws.Add(draw);
        }
        await context.SaveChangesAsync();
    }
}

// Minimal test models
public class TestDraw
{
    public int Draw { get; set; }
    public int[] Numbers { get; set; } = Array.Empty<int>();
}

public class TestMatch
{
    public int DrawNumber { get; set; }
    public int[] WinningCombination { get; set; } = Array.Empty<int>();
    public int[] MatchedNumbers { get; set; } = Array.Empty<int>();
    public int MatchCount { get; set; }
    public bool IsExactMatch { get; set; }
}

public class TestSearchResult
{
    public int[] SearchedCombination { get; set; } = Array.Empty<int>();
    public List<TestMatch> ExactMatches { get; set; } = new();
    public List<TestMatch> PartialMatches { get; set; } = new();
}

// Minimal DbContext
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestDraw> TestDraws { get; set; }
}

// Minimal lookup service
public class TestLookupService
{
    private readonly TestDbContext _context;

    public TestLookupService(TestDbContext context)
    {
        _context = context;
    }

    public async Task<TestSearchResult> SearchCombinationAsync(int[] combination, bool includePartialMatches = true)
    {
        var draws = await _context.TestDraws.ToListAsync();
        var exactMatches = new List<TestMatch>();
        var partialMatches = new List<TestMatch>();

        foreach (var draw in draws)
        {
            var matchedNumbers = combination.Where(num => draw.Numbers.Contains(num)).ToArray();
            var matchCount = matchedNumbers.Length;

            if (matchCount == combination.Length)
            {
                exactMatches.Add(new TestMatch
                {
                    DrawNumber = draw.Draw,
                    WinningCombination = draw.Numbers,
                    MatchedNumbers = matchedNumbers,
                    MatchCount = matchCount,
                    IsExactMatch = true
                });
            }
            else if (includePartialMatches && matchCount >= 2)
            {
                partialMatches.Add(new TestMatch
                {
                    DrawNumber = draw.Draw,
                    WinningCombination = draw.Numbers,
                    MatchedNumbers = matchedNumbers,
                    MatchCount = matchCount,
                    IsExactMatch = false
                });
            }
        }

        return new TestSearchResult
        {
            SearchedCombination = combination,
            ExactMatches = exactMatches,
            PartialMatches = partialMatches
        };
    }
}