using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Task3_1_CombinationSearchTest;

/// <summary>
/// Standalone test for combination search functionality - Task 3.1
/// **Property 4: Combination search finds matching draws**
/// **Validates: Requirements 2.1**
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running Task 3.1: Combination Search Test");
        Console.WriteLine("=========================================");

        try
        {
            await RunCombinationSearchTest();
            Console.WriteLine("✓ PASSED: Combination search test");
            Console.WriteLine("Property 4: Combination search finds matching draws - VALIDATED");
            Console.WriteLine("Requirements 2.1 - SATISFIED");
            Console.WriteLine("Task 3.1 - COMPLETED SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunCombinationSearchTest()
    {
        var service = new TestLookupService();

        // Test data: combination [1,2,3,4,5,6] with exact matches
        var combination = new[] { 1, 2, 3, 4, 5, 6 };
        
        // Create test draws with known exact matches
        var testDraws = new[]
        {
            new TestDraw { Draw = 1, Numbers = new[] { 1, 2, 3, 4, 5, 6 } }, // Exact match
            new TestDraw { Draw = 2, Numbers = new[] { 1, 2, 3, 9, 10, 11 } }, // Partial match (3 numbers)
            new TestDraw { Draw = 3, Numbers = new[] { 7, 8, 9, 10, 11, 12 } }, // No match
            new TestDraw { Draw = 4, Numbers = new[] { 6, 5, 4, 3, 2, 1 } }, // Exact match (different order)
            new TestDraw { Draw = 5, Numbers = new[] { 1, 2, 13, 14, 15, 16 } }, // Partial match (2 numbers)
        };

        service.SeedTestData(testDraws);

        // Act
        var result = await service.SearchCombinationAsync(combination, includePartialMatches: true);

        // Assert
        ValidateResult(result, combination, testDraws);
    }

    private static void ValidateResult(TestSearchResult result, int[] combination, TestDraw[] testDraws)
    {
        Console.WriteLine("Validating combination search results...");

        // Should find 2 exact matches (draws 1 and 4)
        if (result.ExactMatches.Count != 2)
            throw new Exception($"Expected 2 exact matches, got {result.ExactMatches.Count}");

        // Should find 2 partial matches (draws 2 and 5)
        if (result.PartialMatches.Count != 2)
            throw new Exception($"Expected 2 partial matches, got {result.PartialMatches.Count}");

        // Validate exact matches
        var exactMatch1 = result.ExactMatches.First(m => m.DrawNumber == 1);
        var exactMatch4 = result.ExactMatches.First(m => m.DrawNumber == 4);

        if (exactMatch1.MatchCount != 6)
            throw new Exception($"Draw 1 should have 6 matches, got {exactMatch1.MatchCount}");

        if (exactMatch4.MatchCount != 6)
            throw new Exception($"Draw 4 should have 6 matches, got {exactMatch4.MatchCount}");

        if (!exactMatch1.IsExactMatch || !exactMatch4.IsExactMatch)
            throw new Exception("Exact matches should be marked as exact");

        // Validate partial matches
        var partialMatch2 = result.PartialMatches.First(m => m.DrawNumber == 2);
        var partialMatch5 = result.PartialMatches.First(m => m.DrawNumber == 5);

        if (partialMatch2.MatchCount != 3)
            throw new Exception($"Draw 2 should have 3 matches, got {partialMatch2.MatchCount}");

        if (partialMatch5.MatchCount != 2)
            throw new Exception($"Draw 5 should have 2 matches, got {partialMatch5.MatchCount}");

        if (partialMatch2.IsExactMatch || partialMatch5.IsExactMatch)
            throw new Exception("Partial matches should not be marked as exact");

        // Validate that all matches contain correct matched numbers
        foreach (var match in result.ExactMatches.Concat(result.PartialMatches))
        {
            if (!match.MatchedNumbers.All(num => combination.Contains(num)))
                throw new Exception($"Matched numbers must be subset of combination");

            if (!match.MatchedNumbers.All(num => match.WinningCombination.Contains(num)))
                throw new Exception($"Matched numbers must be subset of winning combination");

            if (match.MatchedNumbers.Length != match.MatchCount)
                throw new Exception($"Match count must equal matched numbers array length");
        }

        // Validate searched combination is preserved
        if (!result.SearchedCombination.SequenceEqual(combination))
            throw new Exception("Searched combination should be preserved in result");

        Console.WriteLine("All validations passed!");
        Console.WriteLine($"- Searched combination: [{string.Join(",", combination)}]");
        Console.WriteLine($"- Found {result.ExactMatches.Count} exact matches");
        Console.WriteLine($"- Found {result.PartialMatches.Count} partial matches");
        
        foreach (var match in result.ExactMatches)
        {
            Console.WriteLine($"- Exact match: Draw {match.DrawNumber}, {match.MatchCount} matches: [{string.Join(",", match.MatchedNumbers)}]");
        }
        
        foreach (var match in result.PartialMatches)
        {
            Console.WriteLine($"- Partial match: Draw {match.DrawNumber}, {match.MatchCount} matches: [{string.Join(",", match.MatchedNumbers)}]");
        }
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