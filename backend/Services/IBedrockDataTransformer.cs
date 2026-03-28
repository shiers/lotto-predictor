using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IBedrockDataTransformer
{
    Task<BedrockTrainingContext> TransformLottoDataAsync(
        IEnumerable<LottoDraw> draws, 
        IEnumerable<PredictionAccuracy> accuracyMetrics);
    
    Task<bool> ValidateBedrockDataAsync(BedrockTrainingContext context);
    
    Dictionary<string, object> AnalyzeTemporalPatterns(IEnumerable<LottoDraw> draws);
    
    Dictionary<int, int> CalculateNumberFrequencies(IEnumerable<LottoDraw> draws);
    
    Dictionary<string, int> AnalyzeCombinationPatterns(IEnumerable<LottoDraw> draws);
}