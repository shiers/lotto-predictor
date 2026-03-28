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
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderName => "GroqCloud";
    public int Priority => 20; // High priority - between AWS LLM (10) and FastAPI (50)

    public GroqCloudPredictionProvider(
        HttpClient httpClient,
        IOptions<GroqCloudPredictionProviderOptions> options,
        ILogger<GroqCloudPredictionProvider> logger,
        LottoDbContext context)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _context = context;

        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
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
            
            // Build the prediction prompt
            var prompt = BuildPredictionPrompt(historicalData, count);
            
            // Make request to GroqCloud
            var predictions = await CallGroqCloudApiAsync(prompt, count);
            
            _logger.LogInformation("Successfully generated {Count} predictions using GroqCloud", predictions.Count());
            return predictions;
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

    private string BuildPredictionPrompt(List<LottoDrawDto> historicalData, int count)
    {
        var systemPrompt = @"You are a professional statistician specializing in probability theory. You analyze historical lottery data to explain frequency, variance, and randomness.

Your task is to analyze New Zealand Lotto data and generate lottery predictions. The lottery uses 6 numbers from 1-40 plus a Powerball number from 1-10.

Guidelines:
- Use statistical analysis of historical patterns
- Consider frequency distributions and trends
- Apply probability theory principles
- Provide confidence scores based on statistical evidence
- Each prediction must contain exactly 6 unique numbers between 1-40 plus 1 Powerball number between 1-10
- Format response as valid JSON only";

        var historicalDataJson = JsonSerializer.Serialize(historicalData.Take(10), _jsonOptions);

        var userPrompt = $@"Based on the following historical New Zealand Lotto data, generate {count} lottery predictions:

Historical Data (last 10 draws):
{historicalDataJson}

Requirements:
- Generate exactly {count} predictions
- Each prediction must have exactly 6 unique numbers between 1-40 plus 1 Powerball number between 1-10
- Include confidence score (0.0-1.0) for each prediction
- Provide brief statistical reasoning for each prediction
- Consider frequency patterns, gaps, and statistical trends for both main numbers and Powerball

Response format (JSON only):
{{
    ""predictions"": [
        {{
            ""numbers"": [1, 2, 3, 4, 5, 6],
            ""powerball"": 7,
            ""confidence"": 0.85,
            ""reasoning"": ""Statistical explanation""
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