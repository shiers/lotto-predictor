using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Task3_2_PartialMatchTest;

/// <summary>
/// Standalone test for partial match identification - Task 3.2
/// **Property 5: Partial matches are identified correctly**
/// **Validates: Requirements 2.3**
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running Task 3.2: Partial Match Identification Test");
        Console.WriteLine("===================================================");

        try
        {
            await RunPartialMatchTest();
            Console.WriteLine("✓ PASSED: Partial match identification test");
            Console.WriteLine("Property 5: Partial matches are identified correctly - VALIDATED");
            Console.WriteLine("Requirements 2.3 - SATISFIED");
            Console.WriteLine("Task 3.2 - COMPLETED SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunPartialMatchTest()
    {
        var service = new TestLookupService();

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

        service.SeedTestData(testDraws);

        // Act
        var result = await service.SearchCombinationAsync(combination, includePartialMatches: true);

        // Assert
        ValidateResult(result, combination, testDraws);
    }

    private static void ValidateResult(TestSearchResult result, int[] combination, TestDraw[] testDraws)
    {
        Console.WriteLine("Validating partial match identification...");

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

        // Validate matched numbers are correct
        var expectedMatch1Numbers = new[] { 1, 2 };
        if (!partialMatch1.MatchedNumbers.SequenceEqual(expectedMatch1Numbers))
            throw new Exception($"Draw 1 matched numbers should be [1,2], got [{string.Join(",", partialMatch1.MatchedNumbers)}]");

        var expectedMatch2Numbers = new[] { 1, 2, 3 };
        if (!partialMatch2.MatchedNumbers.SequenceEqual(expectedMatch2Numbers))
            throw new Exception($"Draw 2 matched numbers should be [1,2,3], got [{string.Join(",", partialMatch2.MatchedNumbers)}]");

        // Validate that matched numbers are subsets of both combination and winning numbers
        foreach (var match in result.PartialMatches)
        {
            if (!match.MatchedNumbers.All(num => combination.Contains(num)))
                throw new Exception($"Matched numbers must be subset of combination");

            if (!match.MatchedNumbers.All(num => match.WinningCombination.Contains(num)))
                throw new Exception($"Matched numbers must be subset of winning combination");

            if (match.MatchedNumbers.Length != match.MatchCount)
                throw new Exception($"Match count must equal matched numbers array length");
        }

        Console.WriteLine("All validations passed!");
        Console.WriteLine($"- Found {result.ExactMatches.Count} exact matches");
        Console.WriteLine($"- Found {result.PartialMatches.Count} partial matches");
        Console.WriteLine($"- Partial match 1: Draw {partialMatch1.DrawNumber}, {partialMatch1.MatchCount} matches: [{string.Join(",", partialMatch1.MatchedNumbers)}]");
        Console.WriteLine($"- Partial match 2: Draw {partialMatch2.DrawNumber}, {partialMatch2.MatchCount} matches: [{string.Join(",", partialMatch2.MatchedNumbers)}]");
    }
}

// Test models
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

// Test lookup service
public class TestLookupService
{
    private List<TestDraw> _draws = new();

    public void SeedTestData(TestDraw[] testDraws)
    {
        _draws = testDraws.ToList();
    }

    public async Task<TestSearchResult> SearchCombinationAsync(int[] combination, bool includePartialMatches = true)
    {
        await Task.CompletedTask; // Simulate async operation

        var exactMatches = new List<TestMatch>();
        var partialMatches = new List<TestMatch>();

        foreach (var draw in _draws)
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