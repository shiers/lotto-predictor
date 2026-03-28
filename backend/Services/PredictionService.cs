using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public interface IPredictionService
{
    Task<IEnumerable<PredictionResult>> GeneratePredictionsAsync(int count);
    Task<IEnumerable<PredictionResult>> GetStoredPredictionsAsync(int count);
    Task<PaginatedResponse<PredictionResult>> GetStoredPredictionsPaginatedAsync(PredictionPaginationRequest request);
    Task StorePredictionsAsync(IEnumerable<PredictionResult> predictions);
}

public class CircuitBreakerState
{
    public bool IsOpen { get; set; }
    public DateTime LastFailureTime { get; set; }
    public int FailureCount { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public int FailureThreshold { get; set; } = 3;
}

public class PredictionService : IPredictionService
{
    private readonly IEnumerable<IPredictionProvider> _providers;
    private readonly LottoDbContext _context;
    private readonly ILogger<PredictionService> _logger;
    private readonly Dictionary<string, CircuitBreakerState> _circuitBreakers;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public PredictionService(
        IEnumerable<IPredictionProvider> providers,
        LottoDbContext context,
        ILogger<PredictionService> logger)
    {
        _providers = providers.OrderBy(p => p.Priority); // Order by priority (lower = higher priority)
        _context = context;
        _logger = logger;
        _circuitBreakers = new Dictionary<string, CircuitBreakerState>();
        
        // Initialize circuit breakers for each provider
        foreach (var provider in _providers)
        {
            _circuitBreakers[provider.ProviderName] = new CircuitBreakerState();
        }
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    public async Task<IEnumerable<PredictionResult>> GeneratePredictionsAsync(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));
            
        _logger.LogInformation("Generating {Count} predictions using provider chain", count);
        
        // Try providers in priority order
        foreach (var provider in _providers)
        {
            var circuitBreaker = _circuitBreakers[provider.ProviderName];
            
            // Check circuit breaker state
            if (IsCircuitBreakerOpen(circuitBreaker))
            {
                _logger.LogWarning("Circuit breaker is open for provider {ProviderName}, skipping", provider.ProviderName);
                continue;
            }
            
            try
            {
                _logger.LogInformation("Attempting to generate predictions using provider: {ProviderName}", provider.ProviderName);
                
                var predictions = await provider.PredictAsync(count);
                var predictionList = predictions.ToList();
                
                if (predictionList.Any())
                {
                    _logger.LogInformation("Successfully generated {Count} predictions using provider: {ProviderName}", 
                        predictionList.Count, provider.ProviderName);
                    
                    // Reset circuit breaker on success
                    ResetCircuitBreaker(circuitBreaker);
                    
                    // Store predictions with comprehensive logging for training data
                    await StorePredictionsWithPayloadAsync(predictionList, provider.ProviderName, count);
                    
                    return predictionList;
                }
                
                _logger.LogWarning("Provider {ProviderName} returned no predictions", provider.ProviderName);
                RecordProviderFailure(circuitBreaker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {ProviderName} failed to generate predictions", provider.ProviderName);
                RecordProviderFailure(circuitBreaker);
                // Continue to next provider
            }
        }
        
        _logger.LogError("All prediction providers failed to generate predictions");
        throw new InvalidOperationException("No prediction providers were able to generate predictions");
    }
    
    public async Task<IEnumerable<PredictionResult>> GetStoredPredictionsAsync(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));
            
        _logger.LogInformation("Retrieving {Count} stored predictions", count);
        
        try
        {
            var predictions = await _context.Predictions
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
                
            return predictions.Select(PredictionResult.FromEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve stored predictions from database");
            // Return empty list if database query fails
            return new List<PredictionResult>();
        }
    }
    
    public async Task<PaginatedResponse<PredictionResult>> GetStoredPredictionsPaginatedAsync(PredictionPaginationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
            
        _logger.LogInformation("Retrieving paginated stored predictions - Page: {Page}, PageSize: {PageSize}", 
            request.Page, request.PageSize);
        
        try
        {
            var query = _context.Predictions.AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(request.Source))
            {
                query = query.Where(p => p.Source.Contains(request.Source));
            }
            
            if (request.StartDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= request.StartDate.Value);
            }
            
            if (request.EndDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= request.EndDate.Value);
            }
            
            if (request.TargetDrawDate.HasValue)
            {
                query = query.Where(p => p.TargetDrawDate.HasValue && 
                    p.TargetDrawDate.Value.Date == request.TargetDrawDate.Value.Date);
            }
            
            if (request.MinConfidenceScore.HasValue)
            {
                query = query.Where(p => p.ConfidenceScore.HasValue && 
                    p.ConfidenceScore.Value >= request.MinConfidenceScore.Value);
            }
            
            if (request.MaxConfidenceScore.HasValue)
            {
                query = query.Where(p => p.ConfidenceScore.HasValue && 
                    p.ConfidenceScore.Value <= request.MaxConfidenceScore.Value);
            }
            
            if (request.HasReasoningExplanation.HasValue)
            {
                if (request.HasReasoningExplanation.Value)
                {
                    query = query.Where(p => !string.IsNullOrEmpty(p.ReasoningExplanation));
                }
                else
                {
                    query = query.Where(p => string.IsNullOrEmpty(p.ReasoningExplanation));
                }
            }
            
            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = request.SortBy.ToLowerInvariant() switch
                {
                    "createdat" => request.IsDescending 
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt),
                    "source" => request.IsDescending 
                        ? query.OrderByDescending(p => p.Source)
                        : query.OrderBy(p => p.Source),
                    "score" => request.IsDescending 
                        ? query.OrderByDescending(p => p.Score)
                        : query.OrderBy(p => p.Score),
                    "confidencescore" => request.IsDescending 
                        ? query.OrderByDescending(p => p.ConfidenceScore)
                        : query.OrderBy(p => p.ConfidenceScore),
                    "targetdrawdate" => request.IsDescending 
                        ? query.OrderByDescending(p => p.TargetDrawDate)
                        : query.OrderBy(p => p.TargetDrawDate),
                    _ => query.OrderByDescending(p => p.CreatedAt) // Default sort
                };
            }
            else
            {
                // Default sort by creation date (newest first)
                query = query.OrderByDescending(p => p.CreatedAt);
            }
            
            // Get total count before pagination
            var totalItems = await query.CountAsync();
            
            // Apply pagination
            var predictions = await query
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync();
            
            var predictionResults = predictions.Select(PredictionResult.FromEntity).ToList();
            
            return new PaginatedResponse<PredictionResult>
            {
                Items = predictionResults,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated stored predictions from database");
            
            // Return empty paginated response on error
            return new PaginatedResponse<PredictionResult>
            {
                Items = new List<PredictionResult>(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = 0
            };
        }
    }
    
    public async Task StorePredictionsAsync(IEnumerable<PredictionResult> predictions)
    {
        await StorePredictionsWithPayloadAsync(predictions, "Unknown", 0);
    }
    
    private async Task StorePredictionsWithPayloadAsync(IEnumerable<PredictionResult> predictions, string providerName, int requestedCount)
    {
        var predictionList = predictions.ToList();
        
        if (!predictionList.Any())
        {
            _logger.LogWarning("No predictions to store");
            return;
        }
        
        _logger.LogInformation("Storing {Count} predictions from provider {ProviderName}", predictionList.Count, providerName);
        
        // Create request payload for training data
        var requestPayload = new
        {
            provider = providerName,
            requestedCount = requestedCount,
            timestamp = DateTime.UtcNow,
            requestId = Guid.NewGuid()
        };
        
        // Create response payload for training data
        var responsePayload = new
        {
            provider = providerName,
            predictionsGenerated = predictionList.Count,
            predictions = predictionList.Select(p => new
            {
                numbers = p.Numbers,
                powerball = p.Powerball,
                score = p.Score,
                source = p.Source,
                createdAt = p.CreatedAt
            }),
            timestamp = DateTime.UtcNow
        };
        
        var requestJson = JsonSerializer.Serialize(requestPayload, _jsonOptions);
        var responseJson = JsonSerializer.Serialize(responsePayload, _jsonOptions);
        
        var entities = predictionList.Select(p => new Prediction
        {
            CreatedAt = p.CreatedAt,
            Source = p.Source,
            Score = p.Score,
            Number1 = p.Numbers[0],
            Number2 = p.Numbers[1],
            Number3 = p.Numbers[2],
            Number4 = p.Numbers[3],
            Number5 = p.Numbers[4],
            Number6 = p.Numbers[5],
            Powerball = p.Powerball,
            RawRequestPayload = requestJson,
            RawResponsePayload = responseJson
        }).ToList();
        
        await _context.Predictions.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Successfully stored {Count} predictions with training data payloads", entities.Count);
    }
    
    private bool IsCircuitBreakerOpen(CircuitBreakerState circuitBreaker)
    {
        if (!circuitBreaker.IsOpen)
            return false;
            
        // Check if timeout has elapsed
        if (DateTime.UtcNow - circuitBreaker.LastFailureTime > circuitBreaker.Timeout)
        {
            _logger.LogInformation("Circuit breaker timeout elapsed, attempting to close");
            circuitBreaker.IsOpen = false;
            circuitBreaker.FailureCount = 0;
            return false;
        }
        
        return true;
    }
    
    private void RecordProviderFailure(CircuitBreakerState circuitBreaker)
    {
        circuitBreaker.FailureCount++;
        circuitBreaker.LastFailureTime = DateTime.UtcNow;
        
        if (circuitBreaker.FailureCount >= circuitBreaker.FailureThreshold)
        {
            circuitBreaker.IsOpen = true;
            _logger.LogWarning("Circuit breaker opened due to {FailureCount} consecutive failures", 
                circuitBreaker.FailureCount);
        }
    }
    
    private void ResetCircuitBreaker(CircuitBreakerState circuitBreaker)
    {
        if (circuitBreaker.FailureCount > 0 || circuitBreaker.IsOpen)
        {
            _logger.LogInformation("Resetting circuit breaker after successful operation");
            circuitBreaker.FailureCount = 0;
            circuitBreaker.IsOpen = false;
        }
    }
}