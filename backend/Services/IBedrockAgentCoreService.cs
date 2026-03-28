using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IBedrockAgentCoreService
{
    Task<BedrockPredictionResult> PredictAsync(BedrockTrainingContext context);
    Task<bool> IsAvailableAsync();
    Task<bool> ValidateConfigurationAsync();
    
    // Model retraining methods
    Task<BedrockTrainingJobResult> StartTrainingJobAsync(TrainingDataset trainingDataset);
    Task<BedrockTrainingJobStatus> GetTrainingJobStatusAsync(string jobId);
    Task<bool> CancelTrainingJobAsync(string jobId);
    Task<BedrockModelValidationResult> ValidateTrainedModelAsync(string modelId);
    Task<bool> DeployModelAsync(string modelId, string deploymentName);
    
    string ServiceName { get; }
}