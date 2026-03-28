using Microsoft.Extensions.Options;
using PredictLottoNZ.Models.DTOs;
using System.Text;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public class LocalLlmPredictionProviderOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8002";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 2000;
    public string ModelVersion { get; set; } = "v1.0";
}

public class LocalLlmPredictionProvider : IPredictionProvider
{
    private readonly HttpClient _httpClient;
    private readonly LocalLlmPredictionProviderOptions _options;
    private readonly ILogger<LocalLlmPredictionProvider> _logger;
    private readonly ITrainingDataService _trainingDataService;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public string ProviderName => "Local LLM";
    public int Priority => 20; // Second highest priority after Bedrock
    public bool SupportsConfidenceScores => true; // Local LLM provides confidence scores
    public bool SupportsReasoningChains => true; // Local LLM provides detailed reasoning
    
    public LocalLlmPredictionProvider(
        HttpClient httpClient,
        IOptions<LocalLlmPredictionProviderOptions> options,
        ILogger<LocalLlmPredictionProvider> logger,
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    public async Task<IEnumerable<PredictionResult>> PredictAsync(int count)
    {
        _logger.LogInformation("Generating {Count} predictions using Local LLM service", count);
        
        try
        {
            // Check service health first
            var isHealthy = await CheckServiceHealthAsync();
            if (!isHealthy)
            {
                throw new InvalidOperationException("Local LLM service is not healthy");
            }
            
            var predictions = new List<PredictionResult>();
            
            // Generate the requested number of predictions
            for (int i = 0; i < count; i++)
            {
                var prediction = await GenerateSinglePredictionWithRetryAsync(i + 1, count);
                if (prediction != null)
                {
                    predictions.Add(prediction);
                }
            }
            
            if (predictions.Any())
            {
                _logger.LogInformation("Successfully generated {Count} Local LLM predictions", predictions.Count);
                return predictions;
            }
            
            _logger.LogWarning("Local LLM service failed to generate any predictions");
            throw new InvalidOperationException("Local LLM service failed to generate predictions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Local LLM predictions");
            throw;
        }
    }
    
    private async Task<bool> CheckServiceHealthAsync()
    {
        try
        {
            _logger.LogDebug("Checking Local LLM service health");
            
            using var response = await _httpClient.GetAsync("/health");
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogDebug("Local LLM service health check: {Status}", 
                isHealthy ? "Healthy" : $"Unhealthy ({response.StatusCode})");
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local LLM service health check failed");
            return false;
        }
    }
    
    private async Task<PredictionResult?> GenerateSinglePredictionWithRetryAsync(int predictionIndex, int totalCount)
    {
        var request = new LocalLlmPredictRequest
        {
            PredictionCount = 1,
            ModelVersion = _options.ModelVersion,
            RequestId = Guid.NewGuid().ToString(),
            Context = new LocalLlmContext
            {
                PredictionIndex = predictionIndex,
                TotalPredictions = totalCount,
                Timestamp = DateTime.UtcNow
            }
        };
        
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        
        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Attempting Local LLM prediction {Index}/{Total} (attempt {Attempt}/{MaxRetries})", 
                    predictionIndex, totalCount, attempt, _options.MaxRetries);
                
                var response = await CallLocalLlmServiceAsync(requestJson);
                
                if (response != null && response.Predictions.Any())
                {
                    var llmPrediction = response.Predictions.First();
                    
                    return new PredictionResult
                    {
                        Numbers = llmPrediction.Numbers,
                        Score = llmPrediction.Score,
                        Source = $"{ProviderName} ({response.ModelVersion})",
                        CreatedAt = DateTime.UtcNow,
                        ConfidenceScore = llmPrediction.ConfidenceScore,
                        ReasoningExplanation = llmPrediction.Explanation
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Local LLM prediction attempt {Attempt} failed", attempt);
                
                if (attempt < _options.MaxRetries)
                {
                    var delay = _options.RetryDelayMs * attempt; // Exponential backoff
                    _logger.LogDebug("Retrying in {Delay}ms", delay);
                    await Task.Delay(delay);
                }
            }
        }
        
        _logger.LogError("All {MaxRetries} Local LLM prediction attempts failed", _options.MaxRetries);
        return null;
    }
    
    private async Task<LocalLlmPredictResponse?> CallLocalLlmServiceAsync(string requestJson)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var endpoint = "/predict";
        string responseJson = string.Empty;
        bool success = false;
        string? errorMessage = null;
        
        try
        {
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Calling Local LLM service at {BaseUrl}/predict", _options.BaseUrl);
            
            using var response = await _httpClient.PostAsync(endpoint, content);
            
            responseJson = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                errorMessage = $"HTTP {response.StatusCode}: {responseJson}";
                _logger.LogError("Local LLM service returned error {StatusCode}: {Error}", 
                    response.StatusCode, responseJson);
                throw new HttpRequestException($"Local LLM service returned {response.StatusCode}: {responseJson}");
            }
            
            _logger.LogDebug("Local LLM service response: {Response}", responseJson);
            
            var result = JsonSerializer.Deserialize<LocalLlmPredictResponse>(responseJson, _jsonOptions);
            
            if (result == null)
            {
                errorMessage = "Failed to deserialize response";
                _logger.LogError("Failed to deserialize Local LLM response");
                throw new InvalidOperationException("Failed to deserialize Local LLM response");
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
}

// Request/Response models for Local LLM service
public class LocalLlmPredictRequest
{
    public int PredictionCount { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public LocalLlmContext Context { get; set; } = new();
}

public class LocalLlmContext
{
    public int PredictionIndex { get; set; }
    public int TotalPredictions { get; set; }
    public DateTime Timestamp { get; set; }
}

public class LocalLlmPredictResponse
{
    public List<LocalLlmPrediction> Predictions { get; set; } = new();
    public string ModelVersion { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class LocalLlmPrediction
{
    public int[] Numbers { get; set; } = Array.Empty<int>();
    public double Score { get; set; }
    public double ConfidenceScore { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public List<string> KeyFactors { get; set; } = new();
}