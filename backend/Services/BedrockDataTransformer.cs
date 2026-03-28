using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class BedrockDataTransformer : IBedrockDataTransformer
{
    private readonly ILogger<BedrockDataTransformer> _logger;

    public BedrockDataTransformer(ILogger<BedrockDataTransformer> logger)
    {
        _logger = logger;
    }

    public async Task<BedrockTrainingContext> TransformLottoDataAsync(
        IEnumerable<LottoDraw> draws, 
        IEnumerable<PredictionAccuracy> accuracyMetrics)
    {
        try
        {
            _logger.LogInformation("Starting Bedrock data transformation for {DrawCount} draws and {AccuracyCount} accuracy metrics",
                draws.Count(), accuracyMetrics.Count());

            var drawsList = draws.OrderBy(d => d.Date).ToList();
            var accuracyList = accuracyMetrics.ToList();

            var context = new BedrockTrainingContext
            {
                HistoricalDraws = drawsList,
                AccuracyMetrics = accuracyList,
                GeneratedAt = DateTime.UtcNow,
                TotalDraws = drawsList.Count,
                TotalPredictions = accuracyList.Count
            };

            // Calculate provider performance metrics
            context.ProviderPerformance = CalculateProviderPerformance(accuracyList);

            // Analyze temporal patterns
            context.TemporalPatterns = AnalyzeTemporalPatterns(drawsList);

            // Calculate number frequencies
            context.NumberFrequencies = CalculateNumberFrequencies(drawsList);

            // Analyze combination patterns
            context.CombinationPatterns = AnalyzeCombinationPatterns(drawsList);

            _logger.LogInformation("Bedrock data transformation completed successfully");

            return await Task.FromResult(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Bedrock data transformation");
            throw;
        }
    }

    public async Task<bool> ValidateBedrockDataAsync(BedrockTrainingContext context)
    {
        try
        {
            // Validate basic structure
            if (context == null)
            {
                _logger.LogError("BedrockTrainingContext is null");
                return false;
            }

            // Validate historical draws
            if (context.HistoricalDraws == null)
            {
                _logger.LogError("HistoricalDraws is null");
                return false;
            }

            // Validate each draw has valid numbers
            foreach (var draw in context.HistoricalDraws)
            {
                var numbers = new[] 
                { 
                    draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
                    draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6
                };

                if (numbers.Any(n => n < 1 || n > 40))
                {
                    _logger.LogError("Invalid number range in draw {DrawNumber}: {Numbers}", 
                        draw.Draw, string.Join(", ", numbers));
                    return false;
                }

                if (numbers.Distinct().Count() != 6)
                {
                    _logger.LogError("Duplicate numbers in draw {DrawNumber}: {Numbers}", 
                        draw.Draw, string.Join(", ", numbers));
                    return false;
                }
            }

            // Validate accuracy metrics
            if (context.AccuracyMetrics != null)
            {
                foreach (var accuracy in context.AccuracyMetrics)
                {
                    if (accuracy.OverallAccuracy < 0 || accuracy.OverallAccuracy > 1)
                    {
                        _logger.LogError("Invalid accuracy score: {Score}", accuracy.OverallAccuracy);
                        return false;
                    }

                    if (accuracy.ExactMatches < 0 || accuracy.ExactMatches > 6)
                    {
                        _logger.LogError("Invalid exact matches count: {Count}", accuracy.ExactMatches);
                        return false;
                    }
                }
            }

            // Validate number frequencies
            if (context.NumberFrequencies != null)
            {
                foreach (var freq in context.NumberFrequencies)
                {
                    if (freq.Key < 1 || freq.Key > 40)
                    {
                        _logger.LogError("Invalid number in frequency data: {Number}", freq.Key);
                        return false;
                    }

                    if (freq.Value < 0)
                    {
                        _logger.LogError("Invalid frequency count for number {Number}: {Count}", 
                            freq.Key, freq.Value);
                        return false;
                    }
                }
            }

            _logger.LogInformation("Bedrock data validation completed successfully");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Bedrock data validation");
            return false;
        }
    }

    public Dictionary<string, object> AnalyzeTemporalPatterns(IEnumerable<LottoDraw> draws)
    {
        var patterns = new Dictionary<string, object>();

        try
        {
            var drawsList = draws.OrderBy(d => d.Date).ToList();

            if (!drawsList.Any())
            {
                return patterns;
            }

            // Analyze day of week patterns
            var dayOfWeekFrequency = drawsList
                .GroupBy(d => d.Date.DayOfWeek)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            patterns["day_of_week_frequency"] = dayOfWeekFrequency;

            // Analyze monthly patterns
            var monthlyFrequency = drawsList
                .GroupBy(d => d.Date.Month)
                .ToDictionary(g => $"Month_{g.Key}", g => g.Count());

            patterns["monthly_frequency"] = monthlyFrequency;

            // Analyze yearly trends
            var yearlyFrequency = drawsList
                .GroupBy(d => d.Date.Year)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            patterns["yearly_frequency"] = yearlyFrequency;

            // Calculate time gaps between draws
            var timeGaps = new List<double>();
            for (int i = 1; i < drawsList.Count; i++)
            {
                var gap = (drawsList[i].Date - drawsList[i - 1].Date).TotalDays;
                timeGaps.Add(gap);
            }

            if (timeGaps.Any())
            {
                patterns["average_gap_days"] = timeGaps.Average();
                patterns["min_gap_days"] = timeGaps.Min();
                patterns["max_gap_days"] = timeGaps.Max();
            }

            // Analyze recent vs historical patterns (last 3 months vs all time)
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
            var recentDraws = drawsList.Where(d => d.Date >= threeMonthsAgo).ToList();
            var historicalDraws = drawsList.Where(d => d.Date < threeMonthsAgo).ToList();

            patterns["recent_draws_count"] = recentDraws.Count;
            patterns["historical_draws_count"] = historicalDraws.Count;

            _logger.LogDebug("Analyzed temporal patterns: {PatternCount} patterns identified", patterns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing temporal patterns");
        }

        return patterns;
    }

    public Dictionary<int, int> CalculateNumberFrequencies(IEnumerable<LottoDraw> draws)
    {
        var frequencies = new Dictionary<int, int>();

        try
        {
            // Initialize all numbers 1-40 with zero frequency
            for (int i = 1; i <= 40; i++)
            {
                frequencies[i] = 0;
            }

            // Count occurrences of each number
            foreach (var draw in draws)
            {
                var numbers = new[] 
                { 
                    draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
                    draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6
                };

                foreach (var number in numbers)
                {
                    if (number >= 1 && number <= 40)
                    {
                        frequencies[number]++;
                    }
                }
            }

            _logger.LogDebug("Calculated number frequencies for {DrawCount} draws", draws.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating number frequencies");
        }

        return frequencies;
    }

    public Dictionary<string, int> AnalyzeCombinationPatterns(IEnumerable<LottoDraw> draws)
    {
        var patterns = new Dictionary<string, int>();

        try
        {
            foreach (var draw in draws)
            {
                var numbers = new[] 
                { 
                    draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
                    draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6
                }.OrderBy(n => n).ToArray();

                // Analyze odd/even patterns
                var oddCount = numbers.Count(n => n % 2 == 1);
                var evenCount = 6 - oddCount;
                var oddEvenPattern = $"odd_{oddCount}_even_{evenCount}";
                
                patterns[oddEvenPattern] = patterns.GetValueOrDefault(oddEvenPattern, 0) + 1;

                // Analyze low/high patterns (1-20 vs 21-40)
                var lowCount = numbers.Count(n => n <= 20);
                var highCount = 6 - lowCount;
                var lowHighPattern = $"low_{lowCount}_high_{highCount}";
                
                patterns[lowHighPattern] = patterns.GetValueOrDefault(lowHighPattern, 0) + 1;

                // Analyze consecutive number patterns
                var consecutiveCount = 0;
                for (int i = 1; i < numbers.Length; i++)
                {
                    if (numbers[i] == numbers[i - 1] + 1)
                    {
                        consecutiveCount++;
                    }
                }
                var consecutivePattern = $"consecutive_{consecutiveCount}";
                
                patterns[consecutivePattern] = patterns.GetValueOrDefault(consecutivePattern, 0) + 1;

                // Analyze sum ranges
                var sum = numbers.Sum();
                var sumRange = sum switch
                {
                    <= 90 => "sum_very_low",
                    <= 120 => "sum_low",
                    <= 150 => "sum_medium",
                    <= 180 => "sum_high",
                    _ => "sum_very_high"
                };
                
                patterns[sumRange] = patterns.GetValueOrDefault(sumRange, 0) + 1;
            }

            _logger.LogDebug("Analyzed combination patterns: {PatternCount} unique patterns found", patterns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing combination patterns");
        }

        return patterns;
    }

    private Dictionary<string, double> CalculateProviderPerformance(IEnumerable<PredictionAccuracy> accuracyMetrics)
    {
        var performance = new Dictionary<string, double>();

        try
        {
            var providerGroups = accuracyMetrics.GroupBy(a => a.ProviderName);

            foreach (var group in providerGroups)
            {
                var accuracyScores = group.Select(a => a.OverallAccuracy).ToList();
                
                if (accuracyScores.Any())
                {
                    performance[group.Key] = accuracyScores.Average();
                }
            }

            _logger.LogDebug("Calculated provider performance for {ProviderCount} providers", performance.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating provider performance");
        }

        return performance;
    }
}