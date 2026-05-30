using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;

namespace PredictLottoNZ.Services;

public interface IDrawStatisticsService
{
    Task<NumberGapAnalysis> GetNumberGapAnalysisAsync();
    Task<SumStatistics> GetSumStatisticsAsync();
    Task<PairAnalysis> GetTopPairAnalysisAsync(int topCount = 20);
    Task<PowerballAnalysis> GetPowerballAnalysisAsync();
    Task<DrawPatternSummary> GetDrawPatternSummaryAsync();
}

public class NumberGapAnalysis
{
    public Dictionary<int, int> MainNumberGaps { get; set; } = new(); // number -> draws since last seen
    public Dictionary<int, int> PowerballGaps { get; set; } = new(); // powerball -> draws since last seen
    public Dictionary<int, double> MainNumberAverageGaps { get; set; } = new(); // number -> average gap
    public Dictionary<int, double> PowerballAverageGaps { get; set; } = new();
    public int TotalDrawsAnalyzed { get; set; }
}

public class SumStatistics
{
    public double MeanSum { get; set; }
    public double MedianSum { get; set; }
    public double StdDeviation { get; set; }
    public int MinSum { get; set; }
    public int MaxSum { get; set; }
    public int P10Sum { get; set; } // 10th percentile
    public int P90Sum { get; set; } // 90th percentile
    public Dictionary<string, int> SumRangeDistribution { get; set; } = new(); // "80-99" -> count
}

public class PairAnalysis
{
    public List<NumberPair> TopPairs { get; set; } = new();
    public List<NumberPair> RarestPairs { get; set; } = new();
}

public class NumberPair
{
    public int Number1 { get; set; }
    public int Number2 { get; set; }
    public int Count { get; set; }
    public double ExpectedCount { get; set; }
}

public class PowerballAnalysis
{
    public Dictionary<int, int> Frequencies { get; set; } = new(); // powerball -> total appearances
    public Dictionary<int, int> Last20Frequencies { get; set; } = new(); // powerball -> appearances in last 20
    public int MostFrequent { get; set; }
    public int LeastFrequent { get; set; }
}

public class DrawPatternSummary
{
    public double AvgOddCount { get; set; }
    public double AvgEvenCount { get; set; }
    public double AvgLowCount { get; set; } // numbers 1-20
    public double AvgHighCount { get; set; } // numbers 21-40
    public double AvgConsecutivePairs { get; set; }
    public double AvgRepeatFromPrevious { get; set; }
    public Dictionary<string, int> OddEvenDistribution { get; set; } = new(); // "3odd/3even" -> count
    public Dictionary<string, int> LowHighDistribution { get; set; } = new(); // "3low/3high" -> count
}

public class DrawStatisticsService : IDrawStatisticsService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<DrawStatisticsService> _logger;

    public DrawStatisticsService(LottoDbContext context, ILogger<DrawStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NumberGapAnalysis> GetNumberGapAnalysisAsync()
    {
        _logger.LogInformation("Computing number gap analysis");

        var draws = await _context.LottoDraws
            .OrderByDescending(d => d.Draw)
            .Select(d => new
            {
                d.Draw,
                Numbers = new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 },
                d.Powerball
            })
            .ToListAsync();

        var result = new NumberGapAnalysis { TotalDrawsAnalyzed = draws.Count };

        // Current gaps (draws since each number last appeared)
        for (int n = 1; n <= 40; n++)
        {
            var lastSeen = draws.FindIndex(d => d.Numbers.Contains(n));
            result.MainNumberGaps[n] = lastSeen >= 0 ? lastSeen : draws.Count;
        }

        for (int p = 1; p <= 10; p++)
        {
            var lastSeen = draws.FindIndex(d => d.Powerball == p);
            result.PowerballGaps[p] = lastSeen >= 0 ? lastSeen : draws.Count;
        }

        // Average gaps for main numbers
        for (int n = 1; n <= 40; n++)
        {
            var appearances = new List<int>();
            for (int i = 0; i < draws.Count; i++)
            {
                if (draws[i].Numbers.Contains(n))
                    appearances.Add(i);
            }

            if (appearances.Count > 1)
            {
                var gaps = new List<int>();
                for (int i = 1; i < appearances.Count; i++)
                    gaps.Add(appearances[i] - appearances[i - 1]);
                result.MainNumberAverageGaps[n] = gaps.Average();
            }
            else
            {
                result.MainNumberAverageGaps[n] = draws.Count;
            }
        }

        // Average gaps for powerball
        for (int p = 1; p <= 10; p++)
        {
            var appearances = new List<int>();
            for (int i = 0; i < draws.Count; i++)
            {
                if (draws[i].Powerball == p)
                    appearances.Add(i);
            }

            if (appearances.Count > 1)
            {
                var gaps = new List<int>();
                for (int i = 1; i < appearances.Count; i++)
                    gaps.Add(appearances[i] - appearances[i - 1]);
                result.PowerballAverageGaps[p] = gaps.Average();
            }
            else
            {
                result.PowerballAverageGaps[p] = draws.Count;
            }
        }

        return result;
    }

    public async Task<SumStatistics> GetSumStatisticsAsync()
    {
        _logger.LogInformation("Computing sum statistics");

        var sums = await _context.LottoDraws
            .Select(d => d.WinningNumber1 + d.WinningNumber2 + d.WinningNumber3 +
                         d.WinningNumber4 + d.WinningNumber5 + d.WinningNumber6)
            .ToListAsync();

        sums.Sort();

        var result = new SumStatistics
        {
            MeanSum = sums.Average(),
            MedianSum = sums[sums.Count / 2],
            MinSum = sums.Min(),
            MaxSum = sums.Max(),
            StdDeviation = Math.Sqrt(sums.Average(s => Math.Pow(s - sums.Average(), 2))),
            P10Sum = sums[(int)(sums.Count * 0.10)],
            P90Sum = sums[(int)(sums.Count * 0.90)]
        };

        // Distribution by range
        var ranges = new[] { (60, 79), (80, 99), (100, 119), (120, 139), (140, 159), (160, 179), (180, 200) };
        foreach (var (low, high) in ranges)
        {
            result.SumRangeDistribution[$"{low}-{high}"] = sums.Count(s => s >= low && s <= high);
        }

        return result;
    }

    public async Task<PairAnalysis> GetTopPairAnalysisAsync(int topCount = 20)
    {
        _logger.LogInformation("Computing pair co-occurrence analysis");

        var draws = await _context.LottoDraws
            .Select(d => new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 })
            .ToListAsync();

        var pairCounts = new Dictionary<(int, int), int>();

        foreach (var numbers in draws)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                for (int j = i + 1; j < numbers.Length; j++)
                {
                    var pair = (Math.Min(numbers[i], numbers[j]), Math.Max(numbers[i], numbers[j]));
                    pairCounts.TryGetValue(pair, out var count);
                    pairCounts[pair] = count + 1;
                }
            }
        }

        // Expected count for any pair: draws * C(4,38)/C(6,40) ≈ draws * 6*5/(40*39) * ... simplified:
        // P(both in draw) = C(38,4)/C(40,6) = (38!/(4!*34!)) / (40!/(6!*34!)) = (6*5)/(40*39) = 30/1560 ≈ 0.01923
        double expectedCount = draws.Count * (6.0 * 5.0) / (40.0 * 39.0);

        var topPairs = pairCounts
            .OrderByDescending(p => p.Value)
            .Take(topCount)
            .Select(p => new NumberPair { Number1 = p.Key.Item1, Number2 = p.Key.Item2, Count = p.Value, ExpectedCount = expectedCount })
            .ToList();

        var rarestPairs = pairCounts
            .Where(p => p.Value > 0) // Only pairs that have appeared at least once
            .OrderBy(p => p.Value)
            .Take(topCount)
            .Select(p => new NumberPair { Number1 = p.Key.Item1, Number2 = p.Key.Item2, Count = p.Value, ExpectedCount = expectedCount })
            .ToList();

        return new PairAnalysis { TopPairs = topPairs, RarestPairs = rarestPairs };
    }

    public async Task<PowerballAnalysis> GetPowerballAnalysisAsync()
    {
        _logger.LogInformation("Computing powerball analysis");

        var allPowerballs = await _context.LottoDraws
            .OrderByDescending(d => d.Draw)
            .Select(d => d.Powerball)
            .ToListAsync();

        var result = new PowerballAnalysis();

        for (int p = 1; p <= 10; p++)
        {
            result.Frequencies[p] = allPowerballs.Count(pb => pb == p);
            result.Last20Frequencies[p] = allPowerballs.Take(20).Count(pb => pb == p);
        }

        result.MostFrequent = result.Frequencies.OrderByDescending(f => f.Value).First().Key;
        result.LeastFrequent = result.Frequencies.OrderBy(f => f.Value).First().Key;

        return result;
    }

    public async Task<DrawPatternSummary> GetDrawPatternSummaryAsync()
    {
        _logger.LogInformation("Computing draw pattern summary");

        var draws = await _context.LottoDraws
            .OrderByDescending(d => d.Draw)
            .Select(d => new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 })
            .ToListAsync();

        var result = new DrawPatternSummary();
        var oddCounts = new List<int>();
        var lowCounts = new List<int>();
        var consecutiveCounts = new List<int>();
        var repeatCounts = new List<int>();

        for (int i = 0; i < draws.Count; i++)
        {
            var numbers = draws[i];
            var sorted = numbers.OrderBy(n => n).ToArray();

            // Odd/Even
            int oddCount = numbers.Count(n => n % 2 != 0);
            oddCounts.Add(oddCount);
            var oeKey = $"{oddCount}odd/{6 - oddCount}even";
            result.OddEvenDistribution.TryGetValue(oeKey, out var oeCount);
            result.OddEvenDistribution[oeKey] = oeCount + 1;

            // Low/High (1-20 vs 21-40)
            int lowCount = numbers.Count(n => n <= 20);
            lowCounts.Add(lowCount);
            var lhKey = $"{lowCount}low/{6 - lowCount}high";
            result.LowHighDistribution.TryGetValue(lhKey, out var lhCount);
            result.LowHighDistribution[lhKey] = lhCount + 1;

            // Consecutive pairs
            int consecutives = 0;
            for (int j = 1; j < sorted.Length; j++)
            {
                if (sorted[j] - sorted[j - 1] == 1)
                    consecutives++;
            }
            consecutiveCounts.Add(consecutives);

            // Repeat from previous draw
            if (i < draws.Count - 1)
            {
                var prevNumbers = draws[i + 1]; // Previous draw (list is desc order)
                int repeats = numbers.Count(n => prevNumbers.Contains(n));
                repeatCounts.Add(repeats);
            }
        }

        result.AvgOddCount = oddCounts.Average();
        result.AvgEvenCount = 6 - result.AvgOddCount;
        result.AvgLowCount = lowCounts.Average();
        result.AvgHighCount = 6 - result.AvgLowCount;
        result.AvgConsecutivePairs = consecutiveCounts.Average();
        result.AvgRepeatFromPrevious = repeatCounts.Count > 0 ? repeatCounts.Average() : 0;

        return result;
    }
}
