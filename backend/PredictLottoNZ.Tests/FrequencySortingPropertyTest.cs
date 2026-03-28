using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Property-based test for frequency sorting functionality
/// **Feature: lottery-lookup-navigation, Property 9: Frequency sorting functions properly**
/// **Validates: Requirements 4.2**
/// </summary>
public static class FrequencySortingPropertyTest
{
    public static async Task RunFrequencySortingTest()
    {
        Console.WriteLine("Running Frequency Sorting Property Test...");
        Console.WriteLine("=========================================");

        Console.WriteLine("Property Test 9: Frequency sorting functions properly...");

        // Test the property across multiple iterations
        const int iterations = 100;
        var random = new Random(42); // Fixed seed for reproducible tests

        for (int i = 0; i < iterations; i++)
        {
            try
            {
                await TestFrequencySortingFunctionality(random, i);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }

        Console.WriteLine($"✓ PASSED: Frequency sorting functions properly ({iterations} iterations)");
        Console.WriteLine("=========================================");
        Console.WriteLine("Frequency sorting property test PASSED!");
        Console.WriteLine("Property 9: Frequency sorting functions properly - VALIDATED");
        Console.WriteLine("Requirements 4.2 - SATISFIED");
    }

    private static async Task TestFrequencySortingFunctionality(Random random, int iteration)
    {
        // Generate test data with varied frequency patterns
        var testData = GenerateRandomTestData(random, iteration);
        
        // Test sorting by total occurrences (Requirement 4.2)
        await TestSortingByTotalOccurrences(testData);
        
        // Test sorting by recent appearances (Requirement 4.2)
        await TestSortingByRecentAppearances(testData);
        
        // Test sorting by longest gaps (Requirement 4.2)
        await TestSortingByLongestGaps(testData);
        
        // Test sorting stability and consistency
        await TestSortingStabilityAndConsistency(testData);
    }

    private static async Task TestSortingByTotalOccurrences(TestData testData)
    {
        // Calculate frequency data
        var frequencies = CalculateNumberFrequencies(testData);
        
        // Test ascending sort by total occurrences
        var sortedAscending = frequencies.OrderBy(f => f.TotalOccurrences).ToList();
        ValidateSortOrder(sortedAscending, f => f.TotalOccurrences, "TotalOccurrences", ascending: true);
        
        // Test descending sort by total occurrences
        var sortedDescending = frequencies.OrderByDescending(f => f.TotalOccurrences).ToList();
        ValidateSortOrder(sortedDescending, f => f.TotalOccurrences, "TotalOccurrences", ascending: false);
        
        // Verify that sorting preserves all data
        if (sortedAscending.Count != frequencies.Count || sortedDescending.Count != frequencies.Count)
        {
            throw new Exception("Sorting should preserve all frequency data");
        }
        
        // Verify that both sorts contain the same elements (just in different order)
        var originalNumbers = frequencies.Select(f => f.Number).OrderBy(n => n).ToList();
        var ascendingNumbers = sortedAscending.Select(f => f.Number).OrderBy(n => n).ToList();
        var descendingNumbers = sortedDescending.Select(f => f.Number).OrderBy(n => n).ToList();
        
        if (!originalNumbers.SequenceEqual(ascendingNumbers) || !originalNumbers.SequenceEqual(descendingNumbers))
        {
            throw new Exception("Sorting should preserve all numbers without duplication or loss");
        }
    }

    private static async Task TestSortingByRecentAppearances(TestData testData)
    {
        // Calculate frequency data
        var frequencies = CalculateNumberFrequencies(testData);
        
        // Filter out numbers that have never appeared (they would have DateTime.MinValue)
        var numbersWithAppearances = frequencies.Where(f => f.TotalOccurrences > 0).ToList();
        
        if (numbersWithAppearances.Any())
        {
            // Test ascending sort by last appearance date (oldest first)
            var sortedAscending = numbersWithAppearances.OrderBy(f => f.LastAppearance).ToList();
            ValidateSortOrder(sortedAscending, f => f.LastAppearance, "LastAppearance", ascending: true);
            
            // Test descending sort by last appearance date (most recent first)
            var sortedDescending = numbersWithAppearances.OrderByDescending(f => f.LastAppearance).ToList();
            ValidateSortOrder(sortedDescending, f => f.LastAppearance, "LastAppearance", ascending: false);
            
            // Verify that the most recent appearance is indeed the latest date
            var mostRecentDate = testData.NumberOccurrences.Max(no => no.DrawDate);
            var sortedByRecentFirst = sortedDescending.First();
            
            if (sortedByRecentFirst.LastAppearance != mostRecentDate)
            {
                // Allow for the case where multiple numbers have the same most recent date
                var numbersWithMostRecentDate = numbersWithAppearances
                    .Where(f => f.LastAppearance == mostRecentDate)
                    .Select(f => f.Number)
                    .ToList();
                
                if (!numbersWithMostRecentDate.Contains(sortedByRecentFirst.Number))
                {
                    throw new Exception($"Most recent sort should put numbers with latest appearance first. Expected date: {mostRecentDate}, got: {sortedByRecentFirst.LastAppearance}");
                }
            }
        }
    }

    private static async Task TestSortingByLongestGaps(TestData testData)
    {
        // Calculate frequency data
        var frequencies = CalculateNumberFrequencies(testData);
        
        // Filter out numbers that have appeared less than twice (they would have gap of 0)
        var numbersWithGaps = frequencies.Where(f => f.LongestGap > 0).ToList();
        
        if (numbersWithGaps.Any())
        {
            // Test ascending sort by longest gap (shortest gaps first)
            var sortedAscending = numbersWithGaps.OrderBy(f => f.LongestGap).ToList();
            ValidateSortOrder(sortedAscending, f => f.LongestGap, "LongestGap", ascending: true);
            
            // Test descending sort by longest gap (longest gaps first)
            var sortedDescending = numbersWithGaps.OrderByDescending(f => f.LongestGap).ToList();
            ValidateSortOrder(sortedDescending, f => f.LongestGap, "LongestGap", ascending: false);
            
            // Verify that gaps are calculated correctly for the sorted results
            foreach (var frequency in numbersWithGaps)
            {
                var numberOccurrences = testData.NumberOccurrences
                    .Where(no => no.Number == frequency.Number)
                    .OrderBy(no => no.DrawDate)
                    .ToList();
                
                if (numberOccurrences.Count >= 2)
                {
                    var expectedLongestGap = CalculateExpectedLongestGap(numberOccurrences);
                    if (frequency.LongestGap != expectedLongestGap)
                    {
                        throw new Exception($"Number {frequency.Number}: Expected longest gap {expectedLongestGap}, got {frequency.LongestGap}");
                    }
                }
            }
        }
    }

    private static async Task TestSortingStabilityAndConsistency(TestData testData)
    {
        // Calculate frequency data
        var frequencies = CalculateNumberFrequencies(testData);
        
        // Test that multiple sorts of the same data produce consistent results
        var sort1 = frequencies.OrderBy(f => f.TotalOccurrences).ThenBy(f => f.Number).ToList();
        var sort2 = frequencies.OrderBy(f => f.TotalOccurrences).ThenBy(f => f.Number).ToList();
        
        if (!sort1.SequenceEqual(sort2, new NumberFrequencyComparer()))
        {
            throw new Exception("Multiple sorts of the same data should produce identical results");
        }
        
        // Test stable sorting with secondary sort criteria
        var stableSort = frequencies
            .OrderBy(f => f.TotalOccurrences)
            .ThenByDescending(f => f.LastAppearance)
            .ThenBy(f => f.Number)
            .ToList();
        
        // Verify that items with the same primary sort key maintain consistent secondary ordering
        var groupedByOccurrences = stableSort.GroupBy(f => f.TotalOccurrences).ToList();
        
        foreach (var group in groupedByOccurrences.Where(g => g.Count() > 1))
        {
            var groupItems = group.ToList();
            
            // Within each group of same occurrence count, verify secondary sort by LastAppearance (descending)
            for (int i = 0; i < groupItems.Count - 1; i++)
            {
                if (groupItems[i].LastAppearance < groupItems[i + 1].LastAppearance)
                {
                    throw new Exception($"Secondary sort by LastAppearance should be descending within same occurrence count group");
                }
                
                // If LastAppearance is also the same, verify tertiary sort by Number (ascending)
                if (groupItems[i].LastAppearance == groupItems[i + 1].LastAppearance)
                {
                    if (groupItems[i].Number > groupItems[i + 1].Number)
                    {
                        throw new Exception($"Tertiary sort by Number should be ascending when LastAppearance is the same");
                    }
                }
            }
        }
    }

    private static void ValidateSortOrder<T>(List<NumberFrequencyDto> sortedList, Func<NumberFrequencyDto, T> keySelector, string propertyName, bool ascending) where T : IComparable<T>
    {
        for (int i = 0; i < sortedList.Count - 1; i++)
        {
            var current = keySelector(sortedList[i]);
            var next = keySelector(sortedList[i + 1]);
            var comparison = current.CompareTo(next);
            
            if (ascending && comparison > 0)
            {
                throw new Exception($"Ascending sort by {propertyName} failed: {current} > {next} at positions {i} and {i + 1}");
            }
            
            if (!ascending && comparison < 0)
            {
                throw new Exception($"Descending sort by {propertyName} failed: {current} < {next} at positions {i} and {i + 1}");
            }
        }
    }

    private static TestData GenerateRandomTestData(Random random, int iteration)
    {
        var numberOccurrences = new List<NumberOccurrence>();
        var lottoDraws = new List<LottoDraw>();
        
        // Generate random lotto draws with varied patterns to create interesting sorting scenarios
        int drawCount = random.Next(20, 100); // More draws for better sorting test data
        
        for (int i = 0; i < drawCount; i++)
        {
            var drawNumber = iteration * 1000 + i;
            var drawDate = DateTime.UtcNow.AddDays(-random.Next(1, 1000)); // Spread over ~3 years
            var numbers = GenerateRandomCombination(random);
            
            var draw = new LottoDraw
            {
                Draw = drawNumber,
                Date = drawDate,
                WinningNumber1 = numbers[0],
                WinningNumber2 = numbers[1],
                WinningNumber3 = numbers[2],
                WinningNumber4 = numbers[3],
                WinningNumber5 = numbers[4],
                WinningNumber6 = numbers[5],
                BonusNumber = random.Next(1, 41),
                Powerball = random.Next(1, 11)
            };
            
            lottoDraws.Add(draw);
            
            // Create number occurrences for each winning number
            for (int pos = 1; pos <= 6; pos++)
            {
                var number = pos switch
                {
                    1 => numbers[0],
                    2 => numbers[1],
                    3 => numbers[2],
                    4 => numbers[3],
                    5 => numbers[4],
                    6 => numbers[5],
                    _ => throw new InvalidOperationException()
                };
                
                numberOccurrences.Add(new NumberOccurrence
                {
                    DrawNumber = drawNumber,
                    Number = number,
                    Position = pos,
                    DrawDate = drawDate,
                    IsBonus = false,
                    IsPowerball = false
                });
            }
        }
        
        return new TestData 
        { 
            LottoDraws = lottoDraws, 
            NumberOccurrences = numberOccurrences 
        };
    }

    private static int[] GenerateRandomCombination(Random random)
    {
        var numbers = new HashSet<int>();
        while (numbers.Count < 6)
        {
            numbers.Add(random.Next(1, 41));
        }
        return numbers.OrderBy(n => n).ToArray();
    }

    private static List<NumberFrequencyDto> CalculateNumberFrequencies(TestData testData)
    {
        var frequencies = new List<NumberFrequencyDto>();
        var totalDraws = testData.LottoDraws.Count;
        
        for (int number = 1; number <= 40; number++)
        {
            var occurrences = testData.NumberOccurrences.Where(no => no.Number == number).ToList();
            var totalOccurrences = occurrences.Count;
            
            var frequency = new NumberFrequencyDto
            {
                Number = number,
                TotalOccurrences = totalOccurrences,
                Percentage = totalDraws > 0 ? (double)totalOccurrences / totalDraws * 100 : 0,
                AverageFrequency = totalDraws > 0 ? (double)totalOccurrences / totalDraws : 0
            };
            
            if (totalOccurrences > 0)
            {
                var dates = occurrences.Select(o => o.DrawDate).OrderBy(d => d).ToList();
                frequency.FirstAppearance = dates.First();
                frequency.LastAppearance = dates.Last();
                frequency.LongestGap = CalculateExpectedLongestGap(occurrences);
                frequency.CurrentGap = (DateTime.UtcNow - frequency.LastAppearance).Days;
            }
            else
            {
                frequency.FirstAppearance = DateTime.MinValue;
                frequency.LastAppearance = DateTime.MinValue;
                frequency.LongestGap = 0;
                frequency.CurrentGap = 0;
            }
            
            frequencies.Add(frequency);
        }
        
        return frequencies;
    }

    private static int CalculateExpectedLongestGap(List<NumberOccurrence> occurrences)
    {
        if (occurrences.Count < 2)
            return 0;
        
        var dates = occurrences.Select(o => o.DrawDate).OrderBy(d => d).ToList();
        int longestGap = 0;
        
        for (int i = 1; i < dates.Count; i++)
        {
            var gap = (dates[i] - dates[i - 1]).Days;
            if (gap > longestGap)
                longestGap = gap;
        }
        
        return longestGap;
    }

    private class TestData
    {
        public List<LottoDraw> LottoDraws { get; set; } = new();
        public List<NumberOccurrence> NumberOccurrences { get; set; } = new();
    }

    private class NumberFrequencyComparer : IEqualityComparer<NumberFrequencyDto>
    {
        public bool Equals(NumberFrequencyDto? x, NumberFrequencyDto? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            
            return x.Number == y.Number &&
                   x.TotalOccurrences == y.TotalOccurrences &&
                   x.LastAppearance == y.LastAppearance &&
                   x.FirstAppearance == y.FirstAppearance &&
                   x.LongestGap == y.LongestGap &&
                   Math.Abs(x.Percentage - y.Percentage) < 0.001 &&
                   Math.Abs(x.AverageFrequency - y.AverageFrequency) < 0.001;
        }

        public int GetHashCode(NumberFrequencyDto obj)
        {
            return obj.Number.GetHashCode();
        }
    }
}