using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IModelRetrainingService
{
    /// <summary>
    /// Triggers the automated model retraining pipeline for all configured providers
    /// </summary>
    /// <param name="triggerReason">The reason for triggering retraining (e.g., "NewDataImport", "ScheduledRetraining")</param>
    /// <returns>List of training run IDs that were started</returns>
    Task<List<int>> TriggerRetrainingPipelineAsync(string triggerReason);

    /// <summary>
    /// Triggers retraining for a specific provider
    /// </summary>
    /// <param name="providerName">Name of the provider to retrain</param>
    /// <param name="triggerReason">The reason for triggering retraining</param>
    /// <returns>Training run ID if started, null if provider not configured</returns>
    Task<int?> TriggerProviderRetrainingAsync(string providerName, string triggerReason);

    /// <summary>
    /// Monitors the progress of active training runs
    /// </summary>
    /// <returns>List of active training runs with their current status</returns>
    Task<List<TrainingRunStatus>> GetActiveTrainingRunsAsync();

    /// <summary>
    /// Gets the status of a specific training run
    /// </summary>
    /// <param name="trainingRunId">ID of the training run</param>
    /// <returns>Training run status or null if not found</returns>
    Task<TrainingRunStatus?> GetTrainingRunStatusAsync(int trainingRunId);

    /// <summary>
    /// Handles completion of a training run and validates the new model
    /// </summary>
    /// <param name="trainingRunId">ID of the completed training run</param>
    /// <param name="success">Whether the training completed successfully</param>
    /// <param name="finalAccuracy">Final accuracy of the trained model</param>
    /// <param name="errorMessage">Error message if training failed</param>
    /// <returns>True if model was validated and deployed, false otherwise</returns>
    Task<bool> HandleTrainingCompletionAsync(int trainingRunId, bool success, double? finalAccuracy = null, string? errorMessage = null);

    /// <summary>
    /// Validates a newly trained model before deployment
    /// </summary>
    /// <param name="modelVersionId">ID of the model version to validate</param>
    /// <returns>Validation result with accuracy metrics</returns>
    Task<ModelValidationResult> ValidateModelAsync(int modelVersionId);

    /// <summary>
    /// Deploys a validated model to production
    /// </summary>
    /// <param name="modelVersionId">ID of the model version to deploy</param>
    /// <returns>True if deployment was successful</returns>
    Task<bool> DeployModelAsync(int modelVersionId);

    /// <summary>
    /// Rolls back to the previous model version if deployment fails
    /// </summary>
    /// <param name="providerName">Name of the provider to rollback</param>
    /// <returns>True if rollback was successful</returns>
    Task<bool> RollbackModelAsync(string providerName);

    /// <summary>
    /// Gets training history for a specific provider
    /// </summary>
    /// <param name="providerName">Name of the provider</param>
    /// <param name="limit">Maximum number of training runs to return</param>
    /// <returns>List of training runs ordered by most recent first</returns>
    Task<List<TrainingRun>> GetTrainingHistoryAsync(string providerName, int limit = 10);

    /// <summary>
    /// Checks if retraining should be triggered based on accuracy thresholds
    /// </summary>
    /// <param name="providerName">Name of the provider to check</param>
    /// <returns>True if retraining should be triggered</returns>
    Task<bool> ShouldTriggerRetrainingAsync(string providerName);

    /// <summary>
    /// Gets retraining configuration for all providers
    /// </summary>
    /// <returns>Dictionary of provider configurations</returns>
    Task<Dictionary<string, RetrainingConfiguration>> GetRetrainingConfigurationsAsync();
}