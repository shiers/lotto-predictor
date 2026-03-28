using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class BedrockPredictionProvider : IPredictionProvider
{
    private readonly IBedrockAgentCoreService _bedrockService;
    private readonly IBedrockDataTransformer _dataTransformer;
    private readonly LottoDbContext _context;
    private readonly ILogger<BedrockPredictionProvider> _logger;

    public string ProviderName => "Bedrock AgentCore";
    public int Priority => 1; // Highest priority
    public bool SupportsConfidenceScores => true; // Bedrock provides confidence scores
    public bool SupportsReasoningChains => true; // Bedrock can provide detailed reasoning chains

    public BedrockPredictionProvider(
        IBedrockAgentCoreService bedrockService,
        IBedrockDataTransformer dataTransformer,
        LottoDbContext context,
        ILogger<BedrockPredictionProvider> logger)
    {
        _bedrockService = bedrockService;
        _dataTransformer = dataTransformer;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<PredictionResult>> PredictAsync(int count)
    {
        try
        {
            _logger.LogInformation("Starting Bedrock prediction for {Count} predictions", count);

            // Check if Bedrock service is available
            var isAvailable = await _bedrockService.IsAvailableAsync();
            if (!isAvailable)
            {
                _logger.LogWarning("Bedrock service is not available");
                throw new InvalidOperationException("Bedrock AgentCore service is not available");
            }

            // Get historical data for context
            var historicalDraws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .Take(100) // Last 100 draws for context
                .ToListAsync();

            if (!historicalDraws.Any())
            {
                _logger.LogWarning("No historical draws found for Bedrock prediction");
                throw new InvalidOperationException("No historical lottery data available for prediction");
            }

            // Get accuracy metrics for context
            var accuracyMetrics = await _context.PredictionAccuracies
                .Include(pa => pa.Prediction)
                .OrderByDescending(pa => pa.AnalyzedAt)
                .Take(50) // Last 50 accuracy measurements
                .ToListAsync();

            // Transform data for Bedrock
            var trainingContext = await _dataTransformer.TransformLottoDataAsync(historicalDraws, accuracyMetrics);

            // Validate the training context
            var isValid = await _dataTransformer.ValidateBedrockDataAsync(trainingContext);
            if (!isValid)
            {
                throw new InvalidOperationException("Invalid training context for Bedrock prediction");
            }

            // Generate predictions using Bedrock
            var results = new List<PredictionResult>();
            
            for (int i = 0; i < count; i++)
            {
                try
                {
                    _logger.LogDebug("Generating Bedrock prediction {Index} of {Count}", i + 1, count);
                    
                    var bedrockResult = await _bedrockService.PredictAsync(trainingContext);
                    
                    var predictionResult = new PredictionResult
                    {
                        Numbers = bedrockResult.Numbers,
                        Score = bedrockResult.ConfidenceScore * 100, // Convert to 0-100 scale
                        Source = $"{ProviderName} (Model: {bedrockResult.ModelId})",
                        CreatedAt = DateTime.UtcNow,
                        ConfidenceScore = bedrockResult.ConfidenceScore,
                        ReasoningExplanation = bedrockResult.ReasoningChain
                    };

                    results.Add(predictionResult);
                    
                    _logger.LogDebug("Generated Bedrock prediction: {Numbers} with confidence {Confidence}", 
                        string.Join(", ", bedrockResult.Numbers), bedrockResult.ConfidenceScore);

                    // Add small delay between requests to avoid rate limiting
                    if (i < count - 1)
                    {
                        await Task.Delay(1000); // 1 second delay
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate Bedrock prediction {Index}", i + 1);
                    
                    // If we have some predictions, return them; otherwise rethrow
                    if (results.Any())
                    {
                        _logger.LogWarning("Returning {Count} partial predictions due to error", results.Count);
                        break;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (!results.Any())
            {
                throw new InvalidOperationException("No predictions were generated by Bedrock service");
            }

            _logger.LogInformation("Successfully generated {Count} Bedrock predictions", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Bedrock prediction generation");
            throw;
        }
    }
}