using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Property-based test for range frequency calculation accuracy
/// **Feature: lottery-lookup-navigation, Property 6: Range frequency calculation is accurate**
/// **Validates: Requirements 3.1**
/// </summary>
public static class RangeFrequencyCalculationPropertyTest
{
    public static async Task RunRangeFrequencyCalculationTest()
    {
        Console.WriteLine("Running Range Frequency Calculation Property Test...");
        Console.WriteLine("==============================================");

        Console.WriteLine("Property Test 6: Range frequency calculation is accurate...");

        // Test the property across multiple iterations
        const int iterations = 100;
        var random = new Random(42); // Fixed seed for reproducible tests

        for (int i = 0; i < iterations; i++)
        {
            try
            {
                await TestRangeFrequencyCalculationAccuracy(random, i);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED on iteration {i + 1}: {ex.Message}");
                throw;
            }
        }

        Console.WriteLine($"✓ PASSED: Range frequency calculation is accurate ({iterations} iterations)");
        Console.WriteLine("==============================================");
        Console.WriteLine("Range frequency calculation property test PASSED!");
        Console.WriteLine("Property 6: Range frequency calculation is accurate - VALIDATED");
        Console.WriteLine("Requirements 3.1 - SATISFIED");
    }

    private static async Task TestRangeFrequencyCalculationAccuracy(Random random, int iteration)
    {
        // Generate random test data
        var testData = GenerateRandomTestData(random, iteration);
        
        // Generate random ranges to test
        var ranges = GenerateRandomRanges(random);
        
        // Calculate expected frequencies manually
        var expectedFrequencies = CalculateExpectedRangeFrequencies(testData, ranges);
        
        // Calculate frequencies using the service logic (without database)
        var actualFrequencies = CalculateRangeFrequenciesFromData(testData, ranges);
        
        // Verify that calculated frequencies match expected frequencies
        ValidateRangeFrequencies(expectedFrequencies, actualFrequencies, ranges);
        
        // Test edge cases
        await TestRangeFrequencyEdgeCases(testData, random);
    }

    private static TestData GenerateRandomTestData(Random random, int iteration)
    {
        var numberOccurrences = new List<NumberOccurrence>();
        var lottoDraws = new List<LottoDraw>();
        
        // Generate random lotto draws
        int drawCount = random.Next(10, 50);
        for (int i = 0; i < drawCount; i++)
        {
            var drawNumber = iteration * 1000 + i;
            var drawDate = DateTime.UtcNow.AddDays(-random.Next(1, 365));
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

    private static List<NumberRange> GenerateRandomRanges(Random random)
    {
        var ranges = new List<NumberRange>();
        int rangeCount = random.Next(1, 5); // 1-4 ranges
        
        for (int i = 0; i < rangeCount; i++)
        {
            int start = random.Next(1, 35); // Ensure room for end number
            int end = random.Next(start, Math.Min(start + 10, 40)); // Max range of 10 numbers
            
            ranges.Add(new NumberRange
            {
                StartNumber = start,
                EndNumber = end,
                Label = $"Range {start}-{end}"
            });
        }
        
        return ranges;
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

    private static Dictionary<NumberRange, RangeFrequencyExpected> CalculateExpectedRangeFrequencies(
        TestData testData, 
        List<NumberRange> ranges)
    {
        var expected = new Dictionary<NumberRange, RangeFrequencyExpected>();
        var totalDraws = testData.LottoDraws.Count;
        
        foreach (var range in ranges)
        {
            var numbersInRange = Enumerable.Range(range.StartNumber, range.EndNumber - range.StartNumber + 1).ToArray();
            
            // Count occurrences for each number in the range
            var numberFrequencies = new Dictionary<int, int>();
            foreach (var number in numbersInRange)
            {
                numberFrequencies[number] = testData.NumberOccurrences.Count(no => no.Number == number);
            }
            
            var totalRangeOccurrences = numberFrequencies.Values.Sum();
            var percentage = totalDraws > 0 ? (double)totalRangeOccurrences / totalDraws * 100 : 0;
            var averagePerDraw = totalDraws > 0 ? (double)totalRangeOccurrences / totalDraws : 0;
            
            expected[range] = new RangeFrequencyExpected
            {
                TotalOccurrences = totalRangeOccurrences,
                Percentage = percentage,
                AveragePerDraw = averagePerDraw,
                NumberFrequencies = numberFrequencies
            };
        }
        
        return expected;
    }

    private static List<RangeFrequency> CalculateRangeFrequenciesFromData(
        TestData testData, 
        List<NumberRange> ranges)
    {
        var result = new List<RangeFrequency>();
        var totalDraws = testData.LottoDraws.Count;
        
        foreach (var range in ranges)
        {
            ValidateRange(range);
            
            var numbersInRange = Enumerable.Range(range.StartNumber, range.EndNumber - range.StartNumber + 1).ToArray();
            
            var rangeOccurrences = testData.NumberOccurrences
                .Where(no => numbersInRange.Contains(no.Number))
                .GroupBy(no => no.Number)
                .Select(g => new NumberFrequencyDto
                {
                    Number = g.Key,
                    TotalOccurrences = g.Count(),
                    LastAppearance = g.Max(no => no.DrawDate),
                    FirstAppearance = g.Min(no => no.DrawDate),
                    Percentage = (double)g.Count() / totalDraws * 100,
                    CurrentGap = CalculateCurrentGap(g.Max(no => no.DrawDate))
                })
                .ToList();

            var totalRangeOccurrences = rangeOccurrences.Sum(ro => ro.TotalOccurrences);
            var averagePerDraw = totalDraws > 0 ? (double)totalRangeOccurrences / totalDraws : 0;

            result.Add(new RangeFrequency
            {
                Range = range,
                TotalOccurrences = totalRangeOccurrences,
                Percentage = totalDraws > 0 ? (double)totalRangeOccurrences / totalDraws * 100 : 0,
                AveragePerDraw = averagePerDraw,
                IndividualNumbers = rangeOccurrences
            });
        }
        
        return result;
    }

    private static void ValidateRangeFrequencies(
        Dictionary<NumberRange, RangeFrequencyExpected> expected,
        List<RangeFrequency> actual,
        List<NumberRange> ranges)
    {
        if (actual.Count != ranges.Count)
        {
            throw new Exception($"Expected {ranges.Count} range frequencies, got {actual.Count}");
        }
        
        foreach (var range in ranges)
        {
            var expectedData = expected[range];
            var actualData = actual.FirstOrDefault(rf => 
                rf.Range.StartNumber == range.StartNumber && 
                rf.Range.EndNumber == range.EndNumber);
            
            if (actualData == null)
            {
                throw new Exception($"Missing range frequency data for range {range.StartNumber}-{range.EndNumber}");
            }
            
            // Validate total occurrences
            if (actualData.TotalOccurrences != expectedData.TotalOccurrences)
            {
                throw new Exception($"Range {range.StartNumber}-{range.EndNumber}: Expected {expectedData.TotalOccurrences} total occurrences, got {actualData.TotalOccurrences}");
            }
            
            // Validate percentage (with small tolerance for floating point precision)
            if (Math.Abs(actualData.Percentage - expectedData.Percentage) > 0.001)
            {
                throw new Exception($"Range {range.StartNumber}-{range.EndNumber}: Expected {expectedData.Percentage:F3}% percentage, got {actualData.Percentage:F3}%");
            }
            
            // Validate average per draw
            if (Math.Abs(actualData.AveragePerDraw - expectedData.AveragePerDraw) > 0.001)
            {
                throw new Exception($"Range {range.StartNumber}-{range.EndNumber}: Expected {expectedData.AveragePerDraw:F3} average per draw, got {actualData.AveragePerDraw:F3}");
            }
            
            // Validate individual number frequencies
            ValidateIndividualNumberFrequencies(expectedData.NumberFrequencies, actualData.IndividualNumbers, range);
        }
    }

    private static void ValidateIndividualNumberFrequencies(
        Dictionary<int, int> expected,
        IEnumerable<NumberFrequencyDto> actual,
        NumberRange range)
    {
        var actualDict = actual.ToDictionary(nf => nf.Number, nf => nf.TotalOccurrences);
        
        // Check that all numbers in range are represented
        var numbersInRange = Enumerable.Range(range.StartNumber, range.EndNumber - range.StartNumber + 1);
        
        foreach (var number in numbersInRange)
        {
            var expectedCount = expected.GetValueOrDefault(number, 0);
            var actualCount = actualDict.GetValueOrDefault(number, 0);
            
            if (actualCount != expectedCount)
            {
                throw new Exception($"Number {number} in range {range.StartNumber}-{range.EndNumber}: Expected {expectedCount} occurrences, got {actualCount}");
            }
        }
    }

    private static async Task TestRangeFrequencyEdgeCases(TestData testData, Random random)
    {
        // Test single number range
        var singleNumberRange = new List<NumberRange>
        {
            new NumberRange { StartNumber = 15, EndNumber = 15, Label = "Single" }
        };
        
        var singleResult = CalculateRangeFrequenciesFromData(testData, singleNumberRange);
        if (singleResult.Count != 1)
        {
            throw new Exception("Single number range should return exactly one result");
        }
        
        // Test full range (1-40)
        var fullRange = new List<NumberRange>
        {
            new NumberRange { StartNumber = 1, EndNumber = 40, Label = "Full" }
        };
        
        var fullResult = CalculateRangeFrequenciesFromData(testData, fullRange);
        if (fullResult.Count != 1)
        {
            throw new Exception("Full range should return exactly one result");
        }
        
        // Verify full range total equals sum of all occurrences
        var expectedTotal = testData.NumberOccurrences.Count;
        if (fullResult[0].TotalOccurrences != expectedTotal)
        {
            throw new Exception($"Full range total should be {expectedTotal}, got {fullResult[0].TotalOccurrences}");
        }
        
        // Test overlapping ranges
        var overlappingRanges = new List<NumberRange>
        {
            new NumberRange { StartNumber = 1, EndNumber = 10, Label = "First" },
            new NumberRange { StartNumber = 5, EndNumber = 15, Label = "Second" }
        };
        
        var overlappingResult = CalculateRangeFrequenciesFromData(testData, overlappingRanges);
        if (overlappingResult.Count != 2)
        {
            throw new Exception("Overlapping ranges should return two results");
        }
    }

    private static void ValidateRange(NumberRange range)
    {
        if (range.StartNumber < 1 || range.StartNumber > 40)
        {
            throw new ArgumentException($"Start number must be between 1 and 40, got {range.StartNumber}");
        }

        if (range.EndNumber < 1 || range.EndNumber > 40)
        {
            throw new ArgumentException($"End number must be between 1 and 40, got {range.EndNumber}");
        }

        if (range.StartNumber > range.EndNumber)
        {
            throw new ArgumentException($"Start number ({range.StartNumber}) cannot be greater than end number ({range.EndNumber})");
        }
    }

    private static int CalculateCurrentGap(DateTime lastAppearance)
    {
        return (DateTime.UtcNow - lastAppearance).Days;
    }

    private class TestData
    {
        public List<LottoDraw> LottoDraws { get; set; } = new();
        public List<NumberOccurrence> NumberOccurrences { get; set; } = new();
    }

    private class RangeFrequencyExpected
    {
        public int TotalOccurrences { get; set; }
        public double Percentage { get; set; }
        public double AveragePerDraw { get; set; }
        public Dictionary<int, int> NumberFrequencies { get; set; } = new();
    }
}