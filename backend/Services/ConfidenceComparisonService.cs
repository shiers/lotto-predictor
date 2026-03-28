using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class ConfidenceComparisonService : IConfidenceComparisonService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<ConfidenceComparisonService> _logger;
    
    // Configuration constants
    private const double DEFAULT_PROVIDER_WEIGHT = 0.5;
    private const double HIGH_CONFIDENCE_THRESHOLD = 0.7;
    private const double MEDIUM_CONFIDENCE_THRESHOLD = 0.4;
    private const int PERFORMANCE_HISTORY_DAYS = 90;
    
    public ConfidenceComparisonService(
        LottoDbContext context,
        ILogger<ConfidenceComparisonService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<IEnumerable<ConfidenceRankedPrediction>> RankPredictionsByConfidenceAsync(
        IEnumerable<PredictionResult> predictions)
    {
        var predictionList = predictions.ToList();
        if (!predictionList.Any())
        {
            return Enumerable.Empty<ConfidenceRankedPrediction>();
        }
        
        _logger.LogInformation("Ranking {Count} predictions by confidence", predictionList.Count);
        
        // Get provider performance weights
        var providerWeights = await GetProviderPerformanceWeightsAsync();
        
        // Calculate confidence rankings
        var rankedPredictions = new List<ConfidenceRankedPrediction>();
        var maxConfidence = predictionList.Max(p => p.ConfidenceScore ?? 0);
        
        foreach (var prediction in predictionList)
        {
            var providerWeight = providerWeights.GetValueOrDefault(prediction.Source, DEFAULT_PROVIDER_WEIGHT);
            var confidence = prediction.ConfidenceScore ?? 0;
            var weightedConfidence = confidence * providerWeight;
            
            var rankedPrediction = new ConfidenceRankedPrediction
            {
                Prediction = prediction,
                ConfidenceRank = confidence,
                ProviderWeight = providerWeight,
                WeightedConfidence = weightedConfidence,
                IsHighestConfidence = Math.Abs(confidence - maxConfidence) < 0.001,
                ConfidenceCategory = GetConfidenceCategory(confidence)
            };
            
            rankedPredictions.Add(rankedPrediction);
        }
        
        // Sort by weighted confidence (highest first)
        var sortedPredictions = rankedPredictions
            .OrderByDescending(p => p.WeightedConfidence)
            .ThenByDescending(p => p.ConfidenceRank)
            .ToList();
        
        _logger.LogInformation("Ranked predictions: Top confidence = {TopConfidence:F3}, Provider = {TopProvider}", 
            sortedPredictions.FirstOrDefault()?.ConfidenceRank ?? 0,
            sortedPredictions.FirstOrDefault()?.Prediction.Source ?? "Unknown");
        
        return sortedPredictions;
    }
    
    public async Task<Dictionary<string, double>> GetProviderPerformanceWeightsAsync()
    {
        _logger.LogDebug("Calculating provider performance weights");
        
        var cutoffDate = DateTime.UtcNow.AddDays(-PERFORMANCE_HISTORY_DAYS);
        
        // Get accuracy data for each provider from the last 90 days
        var providerMetrics = await _context.PredictionAccuracies
            .Include(pa => pa.Prediction)
            .Where(pa => pa.AnalyzedAt >= cutoffDate)
            .GroupBy(pa => pa.ProviderName)
            .Select(g => new
            {
                ProviderName = g.Key,
                AverageAccuracy = g.Average(pa => pa.OverallAccuracy),
                TotalPredictions = g.Count(),
                SuccessfulPredictions = g.Count(pa => pa.ExactMatches > 0 || pa.PartialMatches >= 3)
            })
            .ToListAsync();
        
        var weights = new Dictionary<string, double>();
        
        if (!providerMetrics.Any())
        {
            _logger.LogWarning("No historical accuracy data found, using default weights");
            return weights;
        }
        
        // Calculate weights based on accuracy and success rate
        var maxAccuracy = providerMetrics.Max(m => m.AverageAccuracy);
        var totalPredictions = providerMetrics.Sum(m => m.TotalPredictions);
        
        foreach (var metric in providerMetrics)
        {
            var successRate = metric.TotalPredictions > 0 
                ? (double)metric.SuccessfulPredictions / metric.TotalPredictions 
                : 0;
            
            var volumeWeight = Math.Min(1.0, (double)metric.TotalPredictions / Math.Max(1, totalPredictions * 0.1));
            var accuracyWeight = maxAccuracy > 0 ? metric.AverageAccuracy / maxAccuracy : 0.5;
            
            // Combine accuracy, success rate, and volume into final weight
            var finalWeight = (accuracyWeight * 0.5) + (successRate * 0.3) + (volumeWeight * 0.2);
            finalWeight = Math.Max(0.1, Math.Min(1.0, finalWeight)); // Clamp between 0.1 and 1.0
            
            weights[metric.ProviderName] = finalWeight;
            
            _logger.LogDebug("Provider {Provider}: Accuracy={Accuracy:F3}, Success Rate={SuccessRate:F3}, Weight={Weight:F3}",
                metric.ProviderName, metric.AverageAccuracy, successRate, finalWeight);
        }
        
        return weights;
    }
    
    public Task<IEnumerable<PredictionResult>> FilterByConfidenceThresholdAsync(
        IEnumerable<PredictionResult> predictions, 
        double threshold)
    {
        if (threshold < 0 || threshold > 1)
        {
            throw new ArgumentException("Confidence threshold must be between 0 and 1", nameof(threshold));
        }
        
        var predictionList = predictions.ToList();
        _logger.LogInformation("Filtering {Count} predictions with confidence threshold {Threshold:F2}", 
            predictionList.Count, threshold);
        
        var filtered = predictionList
            .Where(p => (p.ConfidenceScore ?? 0) >= threshold)
            .OrderByDescending(p => p.ConfidenceScore)
            .ToList();
        
        _logger.LogInformation("Filtered to {Count} predictions above threshold", filtered.Count);
        
        return Task.FromResult<IEnumerable<PredictionResult>>(filtered);
    }
    
    public async Task<IEnumerable<WeightedPredictionResult>> GetWeightedPredictionsAsync(
        IEnumerable<PredictionResult> predictions)
    {
        var predictionList = predictions.ToList();
        if (!predictionList.Any())
        {
            return Enumerable.Empty<WeightedPredictionResult>();
        }
        
        _logger.LogInformation("Calculating weighted predictions for {Count} predictions", predictionList.Count);
        
        var providerWeights = await GetProviderPerformanceWeightsAsync();
        var weightedPredictions = new List<WeightedPredictionResult>();
        
        foreach (var prediction in predictionList)
        {
            var providerWeight = providerWeights.GetValueOrDefault(prediction.Source, DEFAULT_PROVIDER_WEIGHT);
            var confidence = prediction.ConfidenceScore ?? 0;
            var score = prediction.Score;
            
            var weightedScore = score * providerWeight;
            var weightedConfidence = confidence * providerWeight;
            var overallRating = (weightedScore * 0.3) + (weightedConfidence * 0.7); // Confidence weighted more heavily
            
            var weightedPrediction = new WeightedPredictionResult
            {
                Prediction = prediction,
                ProviderWeight = providerWeight,
                WeightedScore = weightedScore,
                WeightedConfidence = weightedConfidence,
                OverallRating = overallRating
            };
            
            weightedPredictions.Add(weightedPrediction);
        }
        
        return weightedPredictions.OrderByDescending(wp => wp.OverallRating);
    }
    
    public async Task<PredictionRecommendations> GetPredictionRecommendationsAsync(
        IEnumerable<PredictionResult> predictions)
    {
        var predictionList = predictions.ToList();
        if (!predictionList.Any())
        {
            return new PredictionRecommendations
            {
                RecommendationReasoning = "No predictions available for analysis"
            };
        }
        
        _logger.LogInformation("Generating recommendations for {Count} predictions", predictionList.Count);
        
        var providerWeights = await GetProviderPerformanceWeightsAsync();
        var weightedPredictions = await GetWeightedPredictionsAsync(predictionList);
        var rankedPredictions = await RankPredictionsByConfidenceAsync(predictionList);
        
        // Calculate provider confidence averages
        var providerConfidenceAverages = predictionList
            .GroupBy(p => p.Source)
            .ToDictionary(
                g => g.Key,
                g => g.Average(p => p.ConfidenceScore ?? 0)
            );
        
        // Get high confidence predictions
        var highConfidencePredictions = predictionList
            .Where(p => (p.ConfidenceScore ?? 0) >= HIGH_CONFIDENCE_THRESHOLD)
            .OrderByDescending(p => p.ConfidenceScore)
            .ToList();
        
        // Get best performing provider predictions
        var bestProvider = providerWeights.OrderByDescending(kv => kv.Value).FirstOrDefault();
        var bestPerformingProviderPredictions = predictionList
            .Where(p => p.Source == bestProvider.Key)
            .OrderByDescending(p => p.ConfidenceScore)
            .ToList();
        
        // Calculate recommended confidence threshold
        var allConfidences = predictionList
            .Select(p => p.ConfidenceScore ?? 0)
            .ToList();
        
        var recommendedThreshold = allConfidences.Any() 
            ? Math.Max(0.3, allConfidences.Average() - (allConfidences.DefaultIfEmpty(0).Max() - allConfidences.DefaultIfEmpty(0).Min()) * 0.2)
            : 0.5;
        
        // Generate reasoning
        var reasoning = GenerateRecommendationReasoning(
            predictionList, 
            highConfidencePredictions, 
            providerWeights, 
            recommendedThreshold);
        
        return new PredictionRecommendations
        {
            TopRecommendation = weightedPredictions.FirstOrDefault()?.Prediction,
            HighConfidencePredictions = highConfidencePredictions,
            BestPerformingProviderPredictions = bestPerformingProviderPredictions,
            ProviderConfidenceAverages = providerConfidenceAverages,
            ProviderPerformanceWeights = providerWeights,
            RecommendedConfidenceThreshold = recommendedThreshold,
            RecommendationReasoning = reasoning
        };
    }
    
    private string GetConfidenceCategory(double confidence)
    {
        return confidence switch
        {
            >= HIGH_CONFIDENCE_THRESHOLD => "High",
            >= MEDIUM_CONFIDENCE_THRESHOLD => "Medium",
            _ => "Low"
        };
    }
    
    private string GenerateRecommendationReasoning(
        List<PredictionResult> allPredictions,
        List<PredictionResult> highConfidencePredictions,
        Dictionary<string, double> providerWeights,
        double recommendedThreshold)
    {
        var reasoning = new List<string>();
        
        if (highConfidencePredictions.Any())
        {
            reasoning.Add($"Found {highConfidencePredictions.Count} high-confidence predictions (≥70%)");
        }
        
        if (providerWeights.Any())
        {
            var bestProvider = providerWeights.OrderByDescending(kv => kv.Value).First();
            reasoning.Add($"Best performing provider: {bestProvider.Key} (weight: {bestProvider.Value:F2})");
        }
        
        var avgConfidence = allPredictions
            .Select(p => p.ConfidenceScore ?? 0)
            .DefaultIfEmpty(0)
            .Average();
        
        reasoning.Add($"Average confidence: {avgConfidence:P1}");
        reasoning.Add($"Recommended threshold: {recommendedThreshold:P1}");
        
        if (avgConfidence >= HIGH_CONFIDENCE_THRESHOLD)
        {
            reasoning.Add("Overall confidence is high - strong prediction quality");
        }
        else if (avgConfidence >= MEDIUM_CONFIDENCE_THRESHOLD)
        {
            reasoning.Add("Overall confidence is moderate - consider multiple predictions");
        }
        else
        {
            reasoning.Add("Overall confidence is low - use with caution");
        }
        
        return string.Join(". ", reasoning) + ".";
    }
}