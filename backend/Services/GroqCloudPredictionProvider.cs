using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;
using System.Text;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public class GroqCloudPredictionProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

public class GroqCloudPredictionProvider : IPredictionProvider
{
    private readonly HttpClient _httpClient;
    private readonly GroqCloudPredictionProviderOptions _options;
    private readonly ILogger<GroqCloudPredictionProvider> _logger;
    private readonly LottoDbContext _context;
    private readonly IDrawStatisticsService _statisticsService;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderName => "GroqCloud";
    public int Priority => 20; // High priority - between AWS LLM (10) and FastAPI (50)

    public GroqCloudPredictionProvider(
        HttpClient httpClient,
        IOptions<GroqCloudPredictionProviderOptions> options,
        ILogger<GroqCloudPredictionProvider> logger,
        LottoDbContext context,
        IDrawStatisticsService statisticsService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _context = context;
        _statisticsService = statisticsService;

        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public Task<bool> IsAvailableAsync()
    {
        // Only available if API key is configured
        return Task.FromResult(!string.IsNullOrEmpty(_options.ApiKey));
    }

    public async Task<IEnumerable<PredictionResult>> PredictAsync(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        _logger.LogInformation("Generating {Count} predictions using GroqCloud with model {Model}", count, _options.Model);

        try
        {
            // Validate API key
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                throw new InvalidOperationException("GroqCloud API key is not configured");
            }

            // Get historical data for context
            var historicalData = await GetRecentHistoricalDataAsync();
            
            // Get statistical analysis for richer context
            var gapAnalysis = await _statisticsService.GetNumberGapAnalysisAsync();
            var sumStats = await _statisticsService.GetSumStatisticsAsync();
            var pairAnalysis = await _statisticsService.GetTopPairAnalysisAsync(15);
            var powerballAnalysis = await _statisticsService.GetPowerballAnalysisAsync();
            var patternSummary = await _statisticsService.GetDrawPatternSummaryAsync();
            
            // Build the prediction prompt with full statistical context
            var prompt = BuildEnhancedPredictionPrompt(historicalData, count, gapAnalysis, sumStats, pairAnalysis, powerballAnalysis, patternSummary);
            
            // Make request to GroqCloud
            var predictions = await CallGroqCloudApiAsync(prompt, count);
            
            // Validate predictions against statistical constraints
            var validatedPredictions = ValidatePredictions(predictions, sumStats, count);
            
            _logger.LogInformation("Successfully generated {Count} predictions using GroqCloud", validatedPredictions.Count());
            return validatedPredictions;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw argument exceptions as-is
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Configuration error in GroqCloud provider: {Message}", ex.Message);
            throw new InvalidOperationException($"GroqCloud provider configuration error: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling GroqCloud API: {Message}", ex.Message);
            throw new InvalidOperationException($"GroqCloud API communication error: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from GroqCloud response: {Message}", ex.Message);
            throw new InvalidOperationException($"GroqCloud response parsing error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GroqCloud provider: {Message}", ex.Message);
            throw new InvalidOperationException($"GroqCloud provider unexpected error: {ex.Message}", ex);
        }
    }

    private async Task<List<LottoDrawDto>> GetRecentHistoricalDataAsync()
    {
        try
        {
            if (_context == null)
            {
                _logger.LogWarning("Database context is null, proceeding without historical data");
                return new List<LottoDrawDto>();
            }

            var recentDraws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .Take(20) // Last 20 draws for context
                .Select(d => new LottoDrawDto
                {
                    Draw = d.Draw,
                    Date = d.Date,
                    WinningNumbers = new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 },
                    BonusNumber = d.BonusNumber,
                    Powerball = d.Powerball
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} recent draws for context", recentDraws.Count);
            return recentDraws ?? new List<LottoDrawDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve historical data, proceeding without context");
            return new List<LottoDrawDto>();
        }
    }

    private string BuildEnhancedPredictionPrompt(
        List<LottoDrawDto> historicalData, int count,
        NumberGapAnalysis gapAnalysis, SumStatistics sumStats,
        PairAnalysis pairAnalysis, PowerballAnalysis powerballAnalysis,
        DrawPatternSummary patternSummary)
    {
        var systemPrompt = @"You are an expert lottery analyst for New Zealand Lotto Powerball. You use statistical analysis to generate optimized number selections.

Game rules:
- 6 numbers drawn from 1-40 (main draw)
- 1 Powerball drawn from 1-10 (separate machine)
- Each draw is mechanically random (air-mix machines)
- Draws are independent events

Your strategy:
- Use number gap analysis to identify numbers that are statistically overdue relative to their average gap
- Ensure the sum of 6 numbers falls within the historically normal range
- Balance odd/even and low/high distributions to match historical patterns
- Avoid generating lines with too many consecutive numbers (rare in real draws)
- When generating multiple lines, MAXIMIZE COVERAGE - spread numbers across lines so different winning combinations are captured
- Each line should cover different regions of the number space
- Powerball selections should be spread across different values for multi-line tickets

Prize structure (optimize for expected value):
- Division 7: Match 3 numbers = ~$16.50
- Division 6: Match 3 + bonus = ~$40
- Division 5: Match 4 numbers = ~$49-61
- Division 4: Match 4 + bonus = ~$99-117
- Division 3: Match 5 numbers = ~$600-1,900
- Division 2: Match 5 + bonus = ~$15,000-47,000
- Division 1: Match 6 numbers = $200K-$1M+

Format response as valid JSON only.";

        var historicalDataJson = JsonSerializer.Serialize(historicalData.Take(10), _jsonOptions);

        // Build gap summary - top overdue numbers
        var overdueMain = gapAnalysis.MainNumberGaps
            .Where(g => g.Value > gapAnalysis.MainNumberAverageGaps.GetValueOrDefault(g.Key, 7))
            .OrderByDescending(g => g.Value)
            .Take(15)
            .Select(g => $"{g.Key}(gap:{g.Value}, avg:{gapAnalysis.MainNumberAverageGaps[g.Key]:F1})")
            .ToList();

        var overduePowerball = gapAnalysis.PowerballGaps
            .OrderByDescending(g => g.Value)
            .Take(5)
            .Select(g => $"PB{g.Key}(gap:{g.Value})")
            .ToList();

        // Top co-occurring pairs
        var topPairsStr = string.Join(", ", pairAnalysis.TopPairs.Take(10)
            .Select(p => $"({p.Number1},{p.Number2})x{p.Count}"));

        // Odd/even distribution
        var oeDistStr = string.Join(", ", patternSummary.OddEvenDistribution
            .OrderByDescending(d => d.Value)
            .Take(4)
            .Select(d => $"{d.Key}={d.Value}draws"));

        // Low/high distribution
        var lhDistStr = string.Join(", ", patternSummary.LowHighDistribution
            .OrderByDescending(d => d.Value)
            .Take(4)
            .Select(d => $"{d.Key}={d.Value}draws"));

        // Powerball frequency in last 20
        var pbLast20 = string.Join(", ", powerballAnalysis.Last20Frequencies
            .OrderByDescending(f => f.Value)
            .Select(f => $"PB{f.Key}:{f.Value}"));

        var userPrompt = $@"Generate {count} lottery predictions as a COORDINATED SET that maximizes coverage.

=== STATISTICAL CONTEXT (from {gapAnalysis.TotalDrawsAnalyzed} historical draws) ===

RECENT DRAWS (last 10):
{historicalDataJson}

SUM CONSTRAINTS:
- Historical mean sum: {sumStats.MeanSum:F1}, std dev: {sumStats.StdDeviation:F1}
- Target range (P10-P90): {sumStats.P10Sum} to {sumStats.P90Sum}
- Each line's 6 numbers should sum to between {sumStats.P10Sum} and {sumStats.P90Sum}

OVERDUE NUMBERS (gap exceeds average):
Main: {string.Join(", ", overdueMain)}
Powerball: {string.Join(", ", overduePowerball)}

PATTERN DISTRIBUTIONS (most common):
- Odd/Even splits: {oeDistStr}
- Low(1-20)/High(21-40) splits: {lhDistStr}
- Average consecutive pairs per draw: {patternSummary.AvgConsecutivePairs:F2}
- Average numbers repeated from previous draw: {patternSummary.AvgRepeatFromPrevious:F2}

TOP CO-OCCURRING PAIRS: {topPairsStr}

POWERBALL (last 20 draws): {pbLast20}

=== MULTI-LINE OPTIMIZATION RULES ===
- Spread numbers across lines but ALLOW highly overdue numbers to appear on 2 lines
- Each line should have a different odd/even balance
- Use different Powerball values on each line
- Cover different number ranges (some lines favor low, some high, some mixed)
- Numbers that are significantly overdue (gap > 1.5x average) may appear on multiple lines

=== REQUIREMENTS ===
- Generate exactly {count} predictions
- Each prediction: 6 unique numbers (1-40) + 1 Powerball (1-10)
- Sum of each line's 6 numbers must be between {sumStats.P10Sum} and {sumStats.P90Sum}
- Include confidence score (0.0-1.0) and brief reasoning
- Prioritize overdue numbers but maintain statistical balance

Response format (JSON only):
{{
    ""predictions"": [
        {{
            ""numbers"": [1, 2, 3, 4, 5, 6],
            ""powerball"": 7,
            ""confidence"": 0.85,
            ""reasoning"": ""Brief statistical explanation""
        }}
    ]
}}";

        return JsonSerializer.Serialize(new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens,
            response_format = new { type = "json_object" }
        }, _jsonOptions);
    }

    private IEnumerable<PredictionResult> ValidatePredictions(IEnumerable<PredictionResult> predictions, SumStatistics sumStats, int requestedCount)
    {
        var validated = new List<PredictionResult>();

        foreach (var prediction in predictions)
        {
            var sum = prediction.Numbers.Sum();

            // Warn but don't reject if sum is outside P10-P90 range
            if (sum < sumStats.P10Sum || sum > sumStats.P90Sum)
            {
                _logger.LogWarning("Prediction sum {Sum} is outside normal range ({P10}-{P90}), keeping but noting",
                    sum, sumStats.P10Sum, sumStats.P90Sum);
            }

            validated.Add(prediction);
        }

        // Check multi-line coverage
        if (validated.Count > 1)
        {
            var allNumbers = validated.SelectMany(p => p.Numbers).ToList();
            var uniqueNumbers = allNumbers.Distinct().Count();
            var totalNumbers = allNumbers.Count;
            var coverageRatio = (double)uniqueNumbers / totalNumbers;

            _logger.LogInformation("Multi-line coverage: {Unique}/{Total} unique numbers ({Coverage:P0})",
                uniqueNumbers, totalNumbers, coverageRatio);
        }

        return validated;
    }

    private async Task<IEnumerable<PredictionResult>> CallGroqCloudApiAsync(string prompt, int count)
    {
        var retryCount = 0;
        Exception? lastException = null;

        _logger.LogDebug("Starting GroqCloud API call with prompt length: {Length}", prompt.Length);

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                _logger.LogDebug("Making GroqCloud API request (attempt {Attempt}) to {BaseUrl}/chat/completions", retryCount + 1, _httpClient.BaseAddress);

                var content = new StringContent(prompt, Encoding.UTF8, "application/json");
                _logger.LogDebug("Request payload: {Payload}", prompt);
                var fullUrl = $"{_options.BaseUrl}/chat/completions";
                _logger.LogDebug("Full URL: {FullUrl}", fullUrl);
                var response = await _httpClient.PostAsync(fullUrl, content);

                _logger.LogDebug("GroqCloud API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("GroqCloud API response length: {Length}", responseContent.Length);
                    return ParseGroqCloudResponse(responseContent, count);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GroqCloud API error response: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"GroqCloud API error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (HttpRequestException)
            {
                throw; // Don't retry HTTP errors, they're likely permanent
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                _logger.LogWarning(ex, "GroqCloud API request failed (attempt {Attempt}): {Message}", retryCount, ex.Message);

                if (retryCount <= _options.MaxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(1000 * Math.Pow(2, retryCount - 1)); // Exponential backoff
                    _logger.LogInformation("Retrying GroqCloud API request in {Delay}ms", delay.TotalMilliseconds);
                    await Task.Delay(delay);
                }
            }
        }

        throw new InvalidOperationException($"GroqCloud API failed after {_options.MaxRetries + 1} attempts", lastException);
    }

    private IEnumerable<PredictionResult> ParseGroqCloudResponse(string responseContent, int expectedCount)
    {
        try
        {
            _logger.LogDebug("Parsing GroqCloud response: {Response}", responseContent);

            var apiResponse = JsonSerializer.Deserialize<GroqCloudApiResponse>(responseContent, _jsonOptions);
            
            if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                throw new InvalidOperationException("Invalid GroqCloud API response structure");
            }

            var predictionContent = apiResponse.Choices.First().Message.Content;
            if (string.IsNullOrEmpty(predictionContent))
            {
                throw new InvalidOperationException("Empty prediction content from GroqCloud");
            }
            
            var predictionResponse = JsonSerializer.Deserialize<GroqCloudPredictionResponse>(predictionContent, _jsonOptions);

            if (predictionResponse?.Predictions == null || !predictionResponse.Predictions.Any())
            {
                throw new InvalidOperationException("No predictions found in GroqCloud response");
            }

            var results = new List<PredictionResult>();
            var timestamp = DateTime.UtcNow;

            foreach (var prediction in predictionResponse.Predictions.Take(expectedCount))
            {
                // Validate prediction
                if (prediction.Numbers == null || prediction.Numbers.Length != 6)
                {
                    _logger.LogWarning("Invalid prediction: must have exactly 6 numbers");
                    continue;
                }

                if (prediction.Numbers.Any(n => n < 1 || n > 40))
                {
                    _logger.LogWarning("Invalid prediction: numbers must be between 1-40");
                    continue;
                }

                if (prediction.Numbers.Distinct().Count() != 6)
                {
                    _logger.LogWarning("Invalid prediction: numbers must be unique");
                    continue;
                }

                // Validate Powerball
                var powerball = prediction.Powerball ?? new Random().Next(1, 11); // Default random if not provided
                if (powerball < 1 || powerball > 10)
                {
                    _logger.LogWarning("Invalid Powerball: must be between 1-10, using random value");
                    powerball = new Random().Next(1, 11);
                }

                // Create prediction result
                var result = new PredictionResult
                {
                    Numbers = prediction.Numbers.OrderBy(n => n).ToArray(),
                    Powerball = powerball,
                    Score = Math.Max(0.0, Math.Min(1.0, prediction.Confidence)), // Clamp between 0-1
                    Source = $"GroqCloud-{_options.Model}",
                    CreatedAt = timestamp,
                    ConfidenceScore = Math.Max(0.0, Math.Min(1.0, prediction.Confidence)),
                    ReasoningExplanation = prediction.Reasoning ?? "Statistical analysis based on historical patterns"
                };

                results.Add(result);
            }

            if (!results.Any())
            {
                throw new InvalidOperationException("No valid predictions could be parsed from GroqCloud response");
            }

            _logger.LogInformation("Successfully parsed {Count} valid predictions from GroqCloud", results.Count);
            return results;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse GroqCloud response as JSON: {Response}", responseContent);
            throw new InvalidOperationException("Invalid JSON response from GroqCloud", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse GroqCloud response");
            throw;
        }
    }

    // Response DTOs for GroqCloud API
    private class GroqCloudApiResponse
    {
        public GroqCloudChoice[]? Choices { get; set; }
    }

    private class GroqCloudChoice
    {
        public GroqCloudMessage? Message { get; set; }
    }

    private class GroqCloudMessage
    {
        public string? Content { get; set; }
    }

    private class GroqCloudPredictionResponse
    {
        public GroqCloudPrediction[]? Predictions { get; set; }
    }

    private class GroqCloudPrediction
    {
        public int[]? Numbers { get; set; }
        public int? Powerball { get; set; }
        public double Confidence { get; set; }
        public string? Reasoning { get; set; }
    }
}