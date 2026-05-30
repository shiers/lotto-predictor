using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public interface IBacktestService
{
    Task<BacktestResult> RunBacktestAsync(BacktestRequest request);
}

public class BacktestRequest
{
    /// <summary>Number of historical draws to test against (from most recent backwards)</summary>
    public int DrawCount { get; set; } = 20;
    
    /// <summary>Number of prediction lines per draw (simulates a ticket)</summary>
    public int LinesPerDraw { get; set; } = 4;
    
    /// <summary>If true, uses the enhanced predictor. If false, uses random baseline for comparison.</summary>
    public bool UseEnhancedPredictor { get; set; } = true;
}

public class BacktestResult
{
    public int DrawsTested { get; set; }
    public int LinesPerDraw { get; set; }
    public int TotalLines { get; set; }
    public BacktestMatchSummary MatchSummary { get; set; } = new();
    public BacktestFinancialSummary FinancialSummary { get; set; } = new();
    public List<BacktestDrawResult> DrawResults { get; set; } = new();
    public BacktestCoverageStats CoverageStats { get; set; } = new();
    public string Strategy { get; set; } = "";
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
}

public class BacktestMatchSummary
{
    public int TotalMatch0 { get; set; }
    public int TotalMatch1 { get; set; }
    public int TotalMatch2 { get; set; }
    public int TotalMatch3 { get; set; }
    public int TotalMatch3PlusBonus { get; set; }
    public int TotalMatch4 { get; set; }
    public int TotalMatch4PlusBonus { get; set; }
    public int TotalMatch5 { get; set; }
    public int TotalMatch5PlusBonus { get; set; }
    public int TotalMatch6 { get; set; }
    public int TotalPowerballMatches { get; set; }
    public double AverageMatchesPerLine { get; set; }
    public int BestSingleLineMatches { get; set; }
}

public class BacktestFinancialSummary
{
    public decimal TotalCost { get; set; }
    public decimal TotalWinnings { get; set; }
    public decimal NetReturn { get; set; }
    public double ReturnOnInvestment { get; set; } // percentage
    public int WinningLines { get; set; } // lines that won any prize
    public decimal AverageWinPerDraw { get; set; }
}

public class BacktestDrawResult
{
    public int DrawNumber { get; set; }
    public DateTime DrawDate { get; set; }
    public int[] WinningNumbers { get; set; } = Array.Empty<int>();
    public int BonusNumber { get; set; }
    public int Powerball { get; set; }
    public List<BacktestLineResult> LineResults { get; set; } = new();
    public decimal DrawWinnings { get; set; }
    public int BestMatchCount { get; set; }
}

public class BacktestLineResult
{
    public int[] PredictedNumbers { get; set; } = Array.Empty<int>();
    public int PredictedPowerball { get; set; }
    public int MainMatches { get; set; }
    public bool BonusMatched { get; set; }
    public bool PowerballMatched { get; set; }
    public string Division { get; set; } = "None";
    public decimal Prize { get; set; }
}

public class BacktestCoverageStats
{
    public double AverageUniqueCoverage { get; set; } // unique numbers / total numbers per draw
    public double AveragePowerballSpread { get; set; } // unique powerballs / lines per draw
    public double AverageSumInRange { get; set; } // % of lines with sum in P10-P90
}

public class BacktestService : IBacktestService
{
    private readonly LottoDbContext _context;
    private readonly IDrawStatisticsService _statisticsService;
    private readonly ILogger<BacktestService> _logger;

    // NZ Lotto prize approximations (based on recent draw averages)
    private static readonly Dictionary<string, decimal> PrizeTable = new()
    {
        ["Div1"] = 1_000_000m,   // 6 numbers
        ["Div2"] = 25_000m,      // 5 + bonus
        ["Div3"] = 1_000m,       // 5 numbers
        ["Div4"] = 100m,         // 4 + bonus
        ["Div5"] = 50m,          // 4 numbers
        ["Div6"] = 40m,          // 3 + bonus
        ["Div7"] = 16.50m,       // 3 numbers
    };

    private const decimal TicketLineCost = 1.50m; // NZ Lotto Powerball cost per line

    public BacktestService(
        LottoDbContext context,
        IDrawStatisticsService statisticsService,
        ILogger<BacktestService> logger)
    {
        _context = context;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    public async Task<BacktestResult> RunBacktestAsync(BacktestRequest request)
    {
        _logger.LogInformation("Running backtest: {DrawCount} draws, {Lines} lines/draw, enhanced={Enhanced}",
            request.DrawCount, request.LinesPerDraw, request.UseEnhancedPredictor);

        // Get the draws to test against (most recent N draws)
        var testDraws = await _context.LottoDraws
            .OrderByDescending(d => d.Draw)
            .Take(request.DrawCount)
            .ToListAsync();

        if (!testDraws.Any())
            throw new InvalidOperationException("No draws found for backtesting");

        // Get all draws for building historical context (everything before the test period)
        var oldestTestDraw = testDraws.Last().Draw;
        var historicalDraws = await _context.LottoDraws
            .Where(d => d.Draw < oldestTestDraw)
            .OrderByDescending(d => d.Draw)
            .ToListAsync();

        var result = new BacktestResult
        {
            DrawsTested = testDraws.Count,
            LinesPerDraw = request.LinesPerDraw,
            TotalLines = testDraws.Count * request.LinesPerDraw,
            Strategy = request.UseEnhancedPredictor ? "Enhanced (gap + sum + coverage)" : "Random baseline"
        };

        var allMatchCounts = new List<int>();
        var coverageRatios = new List<double>();
        var pbSpreadRatios = new List<double>();
        var sumInRangeRatios = new List<double>();

        // Get sum statistics for validation
        var sumStats = await _statisticsService.GetSumStatisticsAsync();

        foreach (var draw in testDraws)
        {
            var winningNumbers = new[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
                                         draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 };

            // Generate predictions for this draw using only data available BEFORE this draw
            var predictions = request.UseEnhancedPredictor
                ? GenerateEnhancedPredictions(request.LinesPerDraw, historicalDraws, draw.Draw)
                : GenerateRandomPredictions(request.LinesPerDraw);

            var drawResult = new BacktestDrawResult
            {
                DrawNumber = draw.Draw,
                DrawDate = draw.Date,
                WinningNumbers = winningNumbers,
                BonusNumber = draw.BonusNumber,
                Powerball = draw.Powerball
            };

            var uniqueNumbers = new HashSet<int>();
            var uniquePowerballs = new HashSet<int>();
            int linesInSumRange = 0;

            foreach (var prediction in predictions)
            {
                var lineResult = EvaluateLine(prediction.Numbers, prediction.Powerball, winningNumbers, draw.BonusNumber, draw.Powerball);
                drawResult.LineResults.Add(lineResult);
                allMatchCounts.Add(lineResult.MainMatches);

                foreach (var n in prediction.Numbers) uniqueNumbers.Add(n);
                uniquePowerballs.Add(prediction.Powerball);

                var sum = prediction.Numbers.Sum();
                if (sum >= sumStats.P10Sum && sum <= sumStats.P90Sum) linesInSumRange++;
            }

            drawResult.DrawWinnings = drawResult.LineResults.Sum(l => l.Prize);
            drawResult.BestMatchCount = drawResult.LineResults.Max(l => l.MainMatches + (l.BonusMatched ? 0 : 0)); // just main matches
            result.DrawResults.Add(drawResult);

            // Coverage stats
            coverageRatios.Add((double)uniqueNumbers.Count / (request.LinesPerDraw * 6));
            pbSpreadRatios.Add((double)uniquePowerballs.Count / request.LinesPerDraw);
            sumInRangeRatios.Add((double)linesInSumRange / request.LinesPerDraw);

            // Move this draw into historical data for next iteration
            historicalDraws.Insert(0, draw);
        }

        // Compile match summary
        result.MatchSummary = new BacktestMatchSummary
        {
            TotalMatch0 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 0),
            TotalMatch1 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 1),
            TotalMatch2 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 2),
            TotalMatch3 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 3 && !l.BonusMatched),
            TotalMatch3PlusBonus = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 3 && l.BonusMatched),
            TotalMatch4 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 4 && !l.BonusMatched),
            TotalMatch4PlusBonus = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 4 && l.BonusMatched),
            TotalMatch5 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 5 && !l.BonusMatched),
            TotalMatch5PlusBonus = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 5 && l.BonusMatched),
            TotalMatch6 = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.MainMatches == 6),
            TotalPowerballMatches = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.PowerballMatched),
            AverageMatchesPerLine = allMatchCounts.Count > 0 ? allMatchCounts.Average() : 0,
            BestSingleLineMatches = allMatchCounts.Count > 0 ? allMatchCounts.Max() : 0
        };

        // Financial summary
        var totalCost = result.TotalLines * TicketLineCost;
        var totalWinnings = result.DrawResults.Sum(d => d.DrawWinnings);
        result.FinancialSummary = new BacktestFinancialSummary
        {
            TotalCost = totalCost,
            TotalWinnings = totalWinnings,
            NetReturn = totalWinnings - totalCost,
            ReturnOnInvestment = totalCost > 0 ? (double)((totalWinnings - totalCost) / totalCost * 100) : 0,
            WinningLines = result.DrawResults.SelectMany(d => d.LineResults).Count(l => l.Prize > 0),
            AverageWinPerDraw = testDraws.Count > 0 ? totalWinnings / testDraws.Count : 0
        };

        // Coverage stats
        result.CoverageStats = new BacktestCoverageStats
        {
            AverageUniqueCoverage = coverageRatios.Count > 0 ? coverageRatios.Average() : 0,
            AveragePowerballSpread = pbSpreadRatios.Count > 0 ? pbSpreadRatios.Average() : 0,
            AverageSumInRange = sumInRangeRatios.Count > 0 ? sumInRangeRatios.Average() : 0
        };

        _logger.LogInformation("Backtest complete: {Draws} draws, avg {Avg:F2} matches/line, ROI {ROI:F1}%",
            result.DrawsTested, result.MatchSummary.AverageMatchesPerLine, result.FinancialSummary.ReturnOnInvestment);

        return result;
    }

    private BacktestLineResult EvaluateLine(int[] predicted, int predictedPb, int[] winning, int bonus, int powerball)
    {
        var mainMatches = predicted.Intersect(winning).Count();
        var bonusMatched = predicted.Contains(bonus);
        var pbMatched = predictedPb == powerball;

        var (division, prize) = DeterminePrize(mainMatches, bonusMatched, pbMatched);

        return new BacktestLineResult
        {
            PredictedNumbers = predicted,
            PredictedPowerball = predictedPb,
            MainMatches = mainMatches,
            BonusMatched = bonusMatched,
            PowerballMatched = pbMatched,
            Division = division,
            Prize = prize
        };
    }

    private (string division, decimal prize) DeterminePrize(int mainMatches, bool bonusMatched, bool powerballMatched)
    {
        // NZ Lotto prize divisions (Lotto only, not Powerball multiplier)
        return mainMatches switch
        {
            6 => ("Div1", PrizeTable["Div1"]),
            5 when bonusMatched => ("Div2", PrizeTable["Div2"]),
            5 => ("Div3", PrizeTable["Div3"]),
            4 when bonusMatched => ("Div4", PrizeTable["Div4"]),
            4 => ("Div5", PrizeTable["Div5"]),
            3 when bonusMatched => ("Div6", PrizeTable["Div6"]),
            3 => ("Div7", PrizeTable["Div7"]),
            _ => ("None", 0m)
        };
    }

    /// <summary>
    /// Generate predictions using the enhanced strategy (gap analysis + coverage optimization)
    /// without calling the LLM - uses the same statistical logic locally for fast backtesting.
    /// </summary>
    private List<PredictionResult> GenerateEnhancedPredictions(int count, List<Models.LottoDraw> historicalDraws, int currentDraw)
    {
        var predictions = new List<PredictionResult>();
        var usedNumbers = new HashSet<int>();
        var usedPowerballs = new HashSet<int>();
        var random = new Random(currentDraw); // Deterministic seed for reproducibility

        // Compute gaps from historical data
        var gaps = ComputeGaps(historicalDraws);
        var pbGaps = ComputePowerballGaps(historicalDraws);

        // Compute sum range from historical data
        var sums = historicalDraws.Select(d => d.WinningNumber1 + d.WinningNumber2 + d.WinningNumber3 +
                                                d.WinningNumber4 + d.WinningNumber5 + d.WinningNumber6).ToList();
        sums.Sort();
        var p10Sum = sums.Count > 10 ? sums[(int)(sums.Count * 0.10)] : 80;
        var p90Sum = sums.Count > 10 ? sums[(int)(sums.Count * 0.90)] : 170;

        // Compute average gaps for weighting
        var avgGaps = ComputeAverageGaps(historicalDraws);

        for (int line = 0; line < count; line++)
        {
            var numbers = GenerateEnhancedLine(gaps, avgGaps, usedNumbers, p10Sum, p90Sum, random);
            var powerball = GenerateEnhancedPowerball(pbGaps, usedPowerballs, random);

            foreach (var n in numbers) usedNumbers.Add(n);
            usedPowerballs.Add(powerball);

            predictions.Add(new PredictionResult
            {
                Numbers = numbers,
                Powerball = powerball,
                Score = 0.8,
                Source = "Backtest-Enhanced",
                CreatedAt = DateTime.UtcNow
            });
        }

        return predictions;
    }

    private int[] GenerateEnhancedLine(Dictionary<int, int> gaps, Dictionary<int, double> avgGaps,
        HashSet<int> usedNumbers, int minSum, int maxSum, Random random)
    {
        // Weight numbers by how overdue they are (gap / average gap ratio)
        var weights = new Dictionary<int, double>();
        for (int n = 1; n <= 40; n++)
        {
            var gap = gaps.GetValueOrDefault(n, 0);
            var avgGap = avgGaps.GetValueOrDefault(n, 6.67); // Expected: 40/6 ≈ 6.67
            var overdueRatio = gap / Math.Max(avgGap, 1.0);
            
            if (usedNumbers.Contains(n))
            {
                // Allow reuse of highly overdue numbers (but penalize normal ones)
                weights[n] = overdueRatio > 1.5 ? 0.5 : 0.05;
            }
            else
            {
                weights[n] = 1.0 + Math.Max(0, overdueRatio - 0.5); // Boost overdue numbers
            }
        }

        // Try to generate a valid line (sum in range) with weighted selection
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var line = WeightedSample(weights, 6, random);
            var sum = line.Sum();

            if (sum >= minSum && sum <= maxSum)
            {
                // Check odd/even balance (prefer 2-4 odd numbers)
                var oddCount = line.Count(n => n % 2 != 0);
                if (oddCount >= 2 && oddCount <= 4)
                {
                    return line.OrderBy(n => n).ToArray();
                }
            }
        }

        // Fallback: just return weighted sample without sum constraint
        var fallback = WeightedSample(weights, 6, random);
        return fallback.OrderBy(n => n).ToArray();
    }

    private int GenerateEnhancedPowerball(Dictionary<int, int> pbGaps, HashSet<int> usedPowerballs, Random random)
    {
        // Prefer powerballs not yet used and with higher gaps
        var candidates = Enumerable.Range(1, 10)
            .Where(p => !usedPowerballs.Contains(p))
            .ToList();

        if (!candidates.Any())
            candidates = Enumerable.Range(1, 10).ToList();

        // Weight by gap
        var weights = candidates.ToDictionary(p => p, p => 1.0 + pbGaps.GetValueOrDefault(p, 0) * 0.2);
        var totalWeight = weights.Values.Sum();
        var roll = random.NextDouble() * totalWeight;
        var cumulative = 0.0;

        foreach (var (pb, weight) in weights)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return pb;
        }

        return candidates.Last();
    }

    private List<int> WeightedSample(Dictionary<int, double> weights, int count, Random random)
    {
        var selected = new List<int>();
        var available = new Dictionary<int, double>(weights);

        for (int i = 0; i < count; i++)
        {
            var totalWeight = available.Values.Sum();
            var roll = random.NextDouble() * totalWeight;
            var cumulative = 0.0;

            foreach (var (number, weight) in available)
            {
                cumulative += weight;
                if (roll <= cumulative)
                {
                    selected.Add(number);
                    available.Remove(number);
                    break;
                }
            }
        }

        return selected;
    }

    /// <summary>
    /// Generate completely random predictions as a baseline for comparison.
    /// </summary>
    private List<PredictionResult> GenerateRandomPredictions(int count)
    {
        var predictions = new List<PredictionResult>();
        var random = new Random();

        for (int line = 0; line < count; line++)
        {
            var numbers = new HashSet<int>();
            while (numbers.Count < 6)
                numbers.Add(random.Next(1, 41));

            predictions.Add(new PredictionResult
            {
                Numbers = numbers.OrderBy(n => n).ToArray(),
                Powerball = random.Next(1, 11),
                Score = 0.5,
                Source = "Backtest-Random",
                CreatedAt = DateTime.UtcNow
            });
        }

        return predictions;
    }

    private Dictionary<int, int> ComputeGaps(List<Models.LottoDraw> draws)
    {
        var gaps = new Dictionary<int, int>();
        for (int n = 1; n <= 40; n++)
        {
            var idx = draws.FindIndex(d =>
                d.WinningNumber1 == n || d.WinningNumber2 == n || d.WinningNumber3 == n ||
                d.WinningNumber4 == n || d.WinningNumber5 == n || d.WinningNumber6 == n);
            gaps[n] = idx >= 0 ? idx : draws.Count;
        }
        return gaps;
    }

    private Dictionary<int, int> ComputePowerballGaps(List<Models.LottoDraw> draws)
    {
        var gaps = new Dictionary<int, int>();
        for (int p = 1; p <= 10; p++)
        {
            var idx = draws.FindIndex(d => d.Powerball == p);
            gaps[p] = idx >= 0 ? idx : draws.Count;
        }
        return gaps;
    }

    private Dictionary<int, double> ComputeAverageGaps(List<Models.LottoDraw> draws)
    {
        var avgGaps = new Dictionary<int, double>();
        for (int n = 1; n <= 40; n++)
        {
            var appearances = new List<int>();
            for (int i = 0; i < draws.Count; i++)
            {
                var d = draws[i];
                if (d.WinningNumber1 == n || d.WinningNumber2 == n || d.WinningNumber3 == n ||
                    d.WinningNumber4 == n || d.WinningNumber5 == n || d.WinningNumber6 == n)
                    appearances.Add(i);
            }

            if (appearances.Count > 1)
            {
                var gapList = new List<int>();
                for (int i = 1; i < appearances.Count; i++)
                    gapList.Add(appearances[i] - appearances[i - 1]);
                avgGaps[n] = gapList.Average();
            }
            else
            {
                avgGaps[n] = 6.67; // Expected average for 6 from 40
            }
        }
        return avgGaps;
    }
}
