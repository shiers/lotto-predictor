using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IConfidenceComparisonService
{
    /// <summary>
    /// Compare confidence scores across multiple providers and rank predictions
    /// </summary>
    Task<IEnumerable<ConfidenceRankedPrediction>> RankPredictionsByConfidenceAsync(
        IEnumerable<PredictionResult> predictions);
    
    /// <summary>
    /// Get provider performance weights based on historical accuracy
    /// </summary>
    Task<Dictionary<string, double>> GetProviderPerformanceWeightsAsync();
    
    /// <summary>
    /// Filter predictions by confidence threshold
    /// </summary>
    Task<IEnumerable<PredictionResult>> FilterByConfidenceThresholdAsync(
        IEnumerable<PredictionResult> predictions, 
        double threshold);
    
    /// <summary>
    /// Get confidence-weighted predictions with provider performance factored in
    /// </summary>
    Task<IEnumerable<WeightedPredictionResult>> GetWeightedPredictionsAsync(
        IEnumerable<PredictionResult> predictions);
    
    /// <summary>
    /// Get recommendations based on confidence scores and provider performance
    /// </summary>
    Task<PredictionRecommendations> GetPredictionRecommendationsAsync(
        IEnumerable<PredictionResult> predictions);
}