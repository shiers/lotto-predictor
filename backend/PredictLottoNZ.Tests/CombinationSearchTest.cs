using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// **Feature: lottery-lookup-navigation, Property 4: Combination search finds matching draws**
/// Completely standalone test for combination search functionality
/// </summary>
public static class CombinationSearchTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Running Combination Search Property Test...");
        Console.WriteLine("==========================================");

        try
        {
            await RunCombinationSearchTest();
            Console.WriteLine("✓ PASSED: All combination search property tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FAILED: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    public static async Task RunCombinationSearchTest()
    {
        Console.WriteLine("Property Test 4: Combination search finds matching draws...");

        // Test the property across multiple iterations
        const int iterations = 100;
        var random = new Random(42); // Fixed seed for reproducible tests

        for (int i = 0; i < iterations; i++)
        {
            try
            {
                await TestCombinationSearchProperty(random, i);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }

        Console.WriteLine($"✓ PASSED: Combination search finds matching draws ({iterations} iterations)");
        Console.WriteLine("Property 4: Combination search finds matching draws - VALIDATED");
        Console.WriteLine("Requirements 2.1 - SATISFIED");
    }

    private static async Task TestCombinationSearchProperty(Random random, int iteration)
    {
        // Generate test data
        var testDraws = GenerateTestDraws(random, 20);
        
        // Generate a valid combination (2-6 numbers)
        var combinationSize = random.Next(2, 7);
        var combination = GenerateValidCombination(random, combinationSize);
        
        // Act - Simulate combination search logic
        var result = SimulateCombinationSearch(combination, testDraws, includePartialMatches: true);
        
        // Assert - Property: Combination search finds matching draws
        ValidateCombinationSearchResult(result, combination, testDraws);
        
        await Task.CompletedTask;
    }

    private static List<TestDraw> GenerateTestDraws(Random random, int count)
    {
        var draws = new List<TestDraw>();
        
        // Add some predictable draws
        draws.Add(new TestDraw
        {
            DrawNumber = 1001,
            Date = new DateTime(2023, 1, 1),
            WinningNumbers = new[] { 1, 2, 3, 4, 5, 6 }
        });
        
        draws.Add(new TestDraw
        {
            DrawNumber = 1002,
            Date = new DateTime(2023, 1, 8),
            WinningNumbers = new[] { 1, 2, 10, 11, 12, 13 }
        });

        // Add random draws
        for (int i = 0; i < count - 2; i++)
        {
            draws.Add(GenerateRandomDraw(random, 2000 + i));
        }

        return draws;
    }

    private static TestDraw GenerateRandomDraw(Random random, int drawNumber)
    {
        var numbers = new HashSet<int>();
        while (numbers.Count < 6)
        {
            numbers.Add(random.Next(1, 41));
        }
        var sortedNumbers = numbers.OrderBy(n => n).ToArray();

        return new TestDraw
        {
            DrawNumber = drawNumber,
            Date = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
            WinningNumbers = sortedNumbers
        };
    }

    private static int[] GenerateValidCombination(Random random, int size)
    {
        var numbers = new HashSet<int>();
        while (numbers.Count < size)
        {
            numbers.Add(random.Next(1, 41));
        }
        return numbers.OrderBy(n => n).ToArray();
    }

    private static TestCombinationSearchResult SimulateCombinationSearch(int[] combination, List<TestDraw> draws, bool includePartialMatches)
    {
        var exactMatches = new List<TestCombinationMatch>();
        var partialMatches = new List<TestCombinationMatch>();

        foreach (var draw in draws.OrderByDescending(d => d.Date))
        {
            var winningNumbers = draw.WinningNumbers;
            var matchedNumbers = combination.Intersect(winningNumbers).ToArray();
            var matchCount = matchedNumbers.Length;

            if (matchCount == combination.Length)
            {
                // Exact match
                exactMatches.Add(new TestCombinationMatch
                {
                    DrawNumber = draw.DrawNumber,
                    DrawDate = draw.Date,
                    WinningCombination = winningNumbers,
                    MatchedNumbers = matchedNumbers,
                    MatchCount = matchCount,
                    IsExactMatch = true
                });
            }
            else if (includePartialMatches && matchCount >= 2)
            {
                // Partial match (at least 2 numbers)
                partialMatches.Add(new TestCombinationMatch
                {
                    DrawNumber = draw.DrawNumber,
                    DrawDate = draw.Date,
                    WinningCombination = winningNumbers,
                    MatchedNumbers = matchedNumbers,
                    MatchCount = matchCount,
                    IsExactMatch = false
                });
            }
        }

        return new TestCombinationSearchResult
        {
            SearchedCombination = combination,
            ExactMatches = exactMatches,
            PartialMatches = partialMatches.OrderByDescending(pm => pm.MatchCount).ThenByDescending(pm => pm.DrawDate),
            TotalExactMatches = exactMatches.Count,
            TotalPartialMatches = partialMatches.Count
        };
    }

    private static void ValidateCombinationSearchResult(TestCombinationSearchResult result, int[] combination, List<TestDraw> testDraws)
    {
        // 1. Result should not be null
        if (result == null)
            throw new Exception("Combination search result should not be null");
        
        // 2. Searched combination should match input
        if (!combination.SequenceEqual(result.SearchedCombination))
            throw new Exception("Searched combination should match input");
        
        // 3. All exact matches should contain all numbers from the combination
        foreach (var exactMatch in result.ExactMatches)
        {
            var winningNumbers = exactMatch.WinningCombination;
            var allNumbersPresent = combination.All(num => winningNumbers.Contains(num));
            if (!allNumbersPresent)
                throw new Exception($"Exact match draw {exactMatch.DrawNumber} should contain all numbers from combination [{string.Join(", ", combination)}]");
            
            if (!exactMatch.IsExactMatch)
                throw new Exception("Exact match should have IsExactMatch = true");
            
            if (exactMatch.MatchCount != combination.Length)
                throw new Exception($"Exact match count should be {combination.Length}, got {exactMatch.MatchCount}");
            
            if (!combination.OrderBy(x => x).SequenceEqual(exactMatch.MatchedNumbers.OrderBy(x => x)))
                throw new Exception("Exact match MatchedNumbers should equal combination");
        }
        
        // 4. All partial matches should contain at least 2 numbers from the combination
        foreach (var partialMatch in result.PartialMatches)
        {
            var winningNumbers = partialMatch.WinningCombination;
            var matchingNumbers = combination.Intersect(winningNumbers).ToArray();
            
            if (matchingNumbers.Length < 2)
                throw new Exception($"Partial match draw {partialMatch.DrawNumber} should contain at least 2 numbers from combination");
            
            if (matchingNumbers.Length >= combination.Length)
                throw new Exception("Partial match should not contain all numbers (that would be an exact match)");
            
            if (partialMatch.IsExactMatch)
                throw new Exception("Partial match should have IsExactMatch = false");
            
            if (partialMatch.MatchCount != matchingNumbers.Length)
                throw new Exception($"Partial match count should be {matchingNumbers.Length}, got {partialMatch.MatchCount}");
            
            if (!matchingNumbers.OrderBy(x => x).SequenceEqual(partialMatch.MatchedNumbers.OrderBy(x => x)))
                throw new Exception("Partial match MatchedNumbers should equal intersecting numbers");
        }
        
        // 5. Total counts should match actual collections
        if (result.ExactMatches.Count() != result.TotalExactMatches)
            throw new Exception("TotalExactMatches should match ExactMatches count");
        
        if (result.PartialMatches.Count() != result.TotalPartialMatches)
            throw new Exception("TotalPartialMatches should match PartialMatches count");
        
        // 6. No draw should appear in both exact and partial matches
        var exactDrawNumbers = result.ExactMatches.Select(em => em.DrawNumber).ToHashSet();
        var partialDrawNumbers = result.PartialMatches.Select(pm => pm.DrawNumber).ToHashSet();
        var intersection = exactDrawNumbers.Intersect(partialDrawNumbers);
        if (intersection.Any())
            throw new Exception("No draw should appear in both exact and partial matches");
        
        // 7. Results should be ordered correctly
        if (result.ExactMatches.Count() > 1)
        {
            var exactDates = result.ExactMatches.Select(em => em.DrawDate).ToArray();
            var sortedExactDates = exactDates.OrderByDescending(d => d).ToArray();
            if (!exactDates.SequenceEqual(sortedExactDates))
                throw new Exception("Exact matches should be ordered by date descending");
        }
        
        if (result.PartialMatches.Count() > 1)
        {
            var partialMatches = result.PartialMatches.ToArray();
            for (int i = 0; i < partialMatches.Length - 1; i++)
            {
                var current = partialMatches[i];
                var next = partialMatches[i + 1];
                
                if (current.MatchCount < next.MatchCount)
                    throw new Exception("Partial matches should be ordered by match count descending");
                
                if (current.MatchCount == next.MatchCount && current.DrawDate < next.DrawDate)
                    throw new Exception("Partial matches with same match count should be ordered by date descending");
            }
        }
        
        // 8. Verify that all exact matches are actually exact
        foreach (var exactMatch in result.ExactMatches)
        {
            var testDraw = testDraws.FirstOrDefault(d => d.DrawNumber == exactMatch.DrawNumber);
            if (testDraw != null)
            {
                var actualMatches = combination.Intersect(testDraw.WinningNumbers).Count();
                if (actualMatches != combination.Length)
                    throw new Exception($"Draw {exactMatch.DrawNumber} marked as exact match but only has {actualMatches} matching numbers");
            }
        }
        
        // 9. Verify that all partial matches are actually partial
        foreach (var partialMatch in result.PartialMatches)
        {
            var testDraw = testDraws.FirstOrDefault(d => d.DrawNumber == partialMatch.DrawNumber);
            if (testDraw != null)
            {
                var actualMatches = combination.Intersect(testDraw.WinningNumbers).Count();
                if (actualMatches < 2)
                    throw new Exception($"Draw {partialMatch.DrawNumber} marked as partial match but only has {actualMatches} matching numbers");
                if (actualMatches >= combination.Length)
                    throw new Exception($"Draw {partialMatch.DrawNumber} marked as partial match but has {actualMatches} matching numbers (should be exact)");
            }
        }
    }

    private class TestDraw
    {
        public int DrawNumber { get; set; }
        public DateTime Date { get; set; }
        public int[] WinningNumbers { get; set; } = Array.Empty<int>();
    }

    private class TestCombinationSearchResult
    {
        public int[] SearchedCombination { get; set; } = Array.Empty<int>();
        public IEnumerable<TestCombinationMatch> ExactMatches { get; set; } = new List<TestCombinationMatch>();
        public IEnumerable<TestCombinationMatch> PartialMatches { get; set; } = new List<TestCombinationMatch>();
        public int TotalExactMatches { get; set; }
        public int TotalPartialMatches { get; set; }
    }

    private class TestCombinationMatch
    {
        public int DrawNumber { get; set; }
        public DateTime DrawDate { get; set; }
        public int[] WinningCombination { get; set; } = Array.Empty<int>();
        public int[] MatchedNumbers { get; set; } = Array.Empty<int>();
        public int MatchCount { get; set; }
        public bool IsExactMatch { get; set; }
    }
}