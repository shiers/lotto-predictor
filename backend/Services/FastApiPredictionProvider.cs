using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class FastApiPredictionProviderOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8001";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class FastApiPredictionProvider : IPredictionProvider
{
    private readonly HttpClient _httpClient;
    private readonly FastApiPredictionProviderOptions _options;
    private readonly ILogger<FastApiPredictionProvider> _logger;
    private readonly ITrainingDataService _trainingDataService;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public string ProviderName => "FastAPI";
    public int Priority => 50; // Medium priority - between AWS LLM and Frequency
    
    public FastApiPredictionProvider(
        HttpClient httpClient,
        IOptions<FastApiPredictionProviderOptions> options,
        ILogger<FastApiPredictionProvider> logger,
        ITrainingDataService trainingDataService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _trainingDataService = trainingDataService;
        
        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Use CamelCase instead of SnakeCaseLower
            WriteIndented = false
        };
    }
    
    public async Task<IEnumerable<PredictionResult>> PredictAsync(int count)
    {
        _logger.LogInformation("Generating {Count} predictions using FastAPI service", count);
        
        try
        {
            // Get recent historical data for prediction context
            var weeklyNumbers = await GetRecentWeeklyNumbersAsync();
            
            var predictions = new List<PredictionResult>();
            
            // Generate the requested number of predictions
            for (int i = 0; i < count; i++)
            {
                var prediction = await GenerateSinglePredictionWithRetryAsync(weeklyNumbers);
                if (prediction != null)
                {
                    predictions.Add(prediction);
                }
            }
            
            if (predictions.Any())
            {
                _logger.LogInformation("Successfully generated {Count} FastAPI predictions", predictions.Count);
                return predictions;
            }
            
            _logger.LogWarning("FastAPI service failed to generate any predictions");
            throw new InvalidOperationException("FastAPI service failed to generate predictions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate FastAPI predictions");
            throw;
        }
    }
    
    private async Task<PredictionResult?> GenerateSinglePredictionWithRetryAsync(double[] weeklyNumbers)
    {
        var request = new FastApiPredictRequest
        {
            WeeklyNumbers = weeklyNumbers
        };
        
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        
        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Attempting FastAPI prediction (attempt {Attempt}/{MaxRetries})", 
                    attempt, _options.MaxRetries);
                
                var response = await CallFastApiServiceAsync(requestJson);
                
                if (response != null)
                {
                    // Convert the single prediction values to a 6-number combination
                    var numbers = GenerateNumbersFromPredictions(response);
                    var powerball = GeneratePowerballFromPredictions(response);
                    
                    return new PredictionResult
                    {
                        Numbers = numbers,
                        Powerball = powerball,
                        Score = response.BlendedPrediction,
                        Source = ProviderName,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FastAPI prediction attempt {Attempt} failed", attempt);
                
                if (attempt < _options.MaxRetries)
                {
                    var delay = _options.RetryDelayMs * attempt; // Exponential backoff
                    _logger.LogDebug("Retrying in {Delay}ms", delay);
                    await Task.Delay(delay);
                }
            }
        }
        
        _logger.LogError("All {MaxRetries} FastAPI prediction attempts failed", _options.MaxRetries);
        return null;
    }
    
    private async Task<FastApiPredictResponse?> CallFastApiServiceAsync(string requestJson)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var endpoint = "/predict";
        string responseJson = string.Empty;
        bool success = false;
        string? errorMessage = null;
        
        try
        {
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Calling FastAPI service at {BaseUrl}/predict", _options.BaseUrl);
            
            using var response = await _httpClient.PostAsync(endpoint, content);
            
            responseJson = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                errorMessage = $"HTTP {response.StatusCode}: {responseJson}";
                _logger.LogError("FastAPI service returned error {StatusCode}: {Error}", 
                    response.StatusCode, responseJson);
                throw new HttpRequestException($"FastAPI service returned {response.StatusCode}: {responseJson}");
            }
            
            _logger.LogDebug("FastAPI service response: {Response}", responseJson);
            
            var result = JsonSerializer.Deserialize<FastApiPredictResponse>(responseJson, _jsonOptions);
            
            if (result == null)
            {
                errorMessage = "Failed to deserialize response";
                _logger.LogError("Failed to deserialize FastAPI response");
                throw new InvalidOperationException("Failed to deserialize FastAPI response");
            }
            
            success = true;
            return result;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Log the external service call for training data
            try
            {
                await _trainingDataService.LogExternalServiceCallAsync(
                    ProviderName, 
                    $"{_options.BaseUrl}{endpoint}", 
                    requestJson, 
                    responseJson, 
                    success, 
                    stopwatch.Elapsed, 
                    errorMessage);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to log external service call for training data");
            }
        }
    }
    
    private async Task<double[]> GetRecentWeeklyNumbersAsync()
    {
        // For now, return a default set of numbers
        // In a real implementation, this would fetch recent lottery data
        // and calculate weekly averages or use other statistical methods
        
        _logger.LogDebug("Using default weekly numbers for FastAPI prediction");
        
        // Generate some variation based on current time to avoid identical predictions
        var random = new Random(DateTime.UtcNow.Millisecond);
        var baseNumbers = new[] { 5.0, 12.0, 18.0, 25.0, 33.0, 38.0 };
        
        return baseNumbers.Select(n => n + random.NextDouble() * 2 - 1).ToArray(); // Add small random variation
    }
    
    private int[] GenerateNumbersFromPredictions(FastApiPredictResponse response)
    {
        // Convert the prediction values to lottery numbers
        // This is a simplified approach - in practice, you might use more sophisticated methods
        
        var predictions = new[] 
        { 
            response.MlPrediction, 
            response.GptPrediction, 
            response.BlendedPrediction 
        };
        
        var numbers = new List<int>();
        var random = new Random((int)(response.BlendedPrediction * 1000));
        
        // Generate 6 unique numbers based on the predictions
        while (numbers.Count < 6)
        {
            // Use predictions to influence number generation
            var baseValue = predictions[numbers.Count % predictions.Length];
            var number = (int)(Math.Abs(baseValue) % 40) + 1;
            
            // Add some randomness to avoid identical combinations
            number = ((number + random.Next(1, 40)) % 40) + 1;
            
            if (!numbers.Contains(number))
            {
                numbers.Add(number);
            }
        }
        
        return numbers.OrderBy(n => n).ToArray();
    }
    
    private int GeneratePowerballFromPredictions(FastApiPredictResponse response)
    {
        // Generate Powerball number (1-10) based on predictions
        var random = new Random((int)(response.BlendedPrediction * 10000));
        
        // Use the blended prediction to influence Powerball selection
        var baseValue = Math.Abs(response.BlendedPrediction) % 10;
        var powerball = (int)baseValue + 1;
        
        // Add some randomness while keeping it in valid range
        powerball = ((powerball + random.Next(1, 10)) % 10) + 1;
        
        return Math.Max(1, Math.Min(10, powerball));
    }
}

// Request/Response models for FastAPI service
public class FastApiPredictRequest
{
    public double[] WeeklyNumbers { get; set; } = Array.Empty<double>();
}

public class FastApiPredictResponse
{
    public double MlPrediction { get; set; }
    public double GptPrediction { get; set; }
    public double BlendedPrediction { get; set; }
}