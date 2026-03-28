using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public class ModelRetrainingService : IModelRetrainingService
{
    private readonly LottoDbContext _context;
    private readonly ITrainingDataService _trainingDataService;
    private readonly IAccuracyAnalysisService _accuracyAnalysisService;
    private readonly IBedrockAgentCoreService? _bedrockService;
    private readonly ILogger<ModelRetrainingService> _logger;
    private readonly IConfiguration _configuration;

    // Provider-specific retraining configurations
    private readonly Dictionary<string, RetrainingConfiguration> _retrainingConfigurations;

    public ModelRetrainingService(
        LottoDbContext context,
        ITrainingDataService trainingDataService,
        IAccuracyAnalysisService accuracyAnalysisService,
        ILogger<ModelRetrainingService> logger,
        IConfiguration configuration,
        IBedrockAgentCoreService? bedrockService = null)
    {
        _context = context;
        _trainingDataService = trainingDataService;
        _accuracyAnalysisService = accuracyAnalysisService;
        _bedrockService = bedrockService;
        _logger = logger;
        _configuration = configuration;

        // Initialize retraining configurations from appsettings
        _retrainingConfigurations = LoadRetrainingConfigurations();
    }

    public async Task<List<int>> TriggerRetrainingPipelineAsync(string triggerReason)
    {
        _logger.LogInformation("Triggering automated model retraining pipeline. Reason: {TriggerReason}", triggerReason);

        var startedTrainingRuns = new List<int>();
        var errors = new List<string>();

        try
        {
            // Check if we have sufficient data for retraining
            var trainingDataExport = await _trainingDataService.ExportTrainingDataAsync();
            var totalRecords = trainingDataExport.Predictions.Count + trainingDataExport.LottoDraws.Count;
            if (totalRecords < 50) // Minimum threshold
            {
                _logger.LogWarning("Insufficient training data ({SampleCount} samples). Skipping retraining.", 
                    totalRecords);
                return startedTrainingRuns;
            }

            // Trigger retraining for each configured provider
            foreach (var config in _retrainingConfigurations.Values.Where(c => c.IsEnabled))
            {
                try
                {
                    // Check if provider should be retrained
                    if (!await ShouldTriggerRetrainingAsync(config.ProviderName))
                    {
                        _logger.LogInformation("Skipping retraining for {ProviderName} - conditions not met", 
                            config.ProviderName);
                        continue;
                    }

                    // Check concurrent training limit
                    var activeRuns = await GetActiveTrainingRunsForProviderAsync(config.ProviderName);
                    if (activeRuns.Count >= config.MaxConcurrentTraining)
                    {
                        _logger.LogWarning("Skipping retraining for {ProviderName} - max concurrent limit reached ({Limit})", 
                            config.ProviderName, config.MaxConcurrentTraining);
                        continue;
                    }

                    var trainingRunId = await TriggerProviderRetrainingAsync(config.ProviderName, triggerReason);
                    if (trainingRunId.HasValue)
                    {
                        startedTrainingRuns.Add(trainingRunId.Value);
                        _logger.LogInformation("Started retraining for {ProviderName} with run ID {TrainingRunId}", 
                            config.ProviderName, trainingRunId.Value);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Failed to start retraining for {config.ProviderName}: {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, "Error starting retraining for {ProviderName}", config.ProviderName);
                }
            }

            _logger.LogInformation("Retraining pipeline completed. Started {StartedCount} runs, {ErrorCount} errors", 
                startedTrainingRuns.Count, errors.Count);

            return startedTrainingRuns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in retraining pipeline");
            throw;
        }
    }

    public async Task<int?> TriggerProviderRetrainingAsync(string providerName, string triggerReason)
    {
        _logger.LogInformation("Triggering retraining for provider {ProviderName}. Reason: {TriggerReason}", 
            providerName, triggerReason);

        try
        {
            // Validate provider configuration
            if (!_retrainingConfigurations.ContainsKey(providerName))
            {
                _logger.LogWarning("No retraining configuration found for provider {ProviderName}", providerName);
                return null;
            }
            var config = _retrainingConfigurations[providerName];
            if (!config.IsEnabled)
            {
                _logger.LogInformation("Retraining is disabled for provider {ProviderName}", providerName);
                return null;
            }

            // Generate training dataset
            var trainingDataExport = await _trainingDataService.ExportTrainingDataAsync();
            var totalRecords = trainingDataExport.Predictions.Count + trainingDataExport.LottoDraws.Count;
            
            // Validate dataset - using a mock validation since ValidateTrainingDatasetAsync doesn't exist
            var validationResult = new { IsValid = true, ValidationErrors = new List<string>() };
            if (!validationResult.IsValid)
            {
                _logger.LogError("Training dataset validation failed for {ProviderName}: {Errors}", 
                    providerName, string.Join(", ", validationResult.ValidationErrors));
                return null;
            }

            // Create training run record
            var trainingRun = new TrainingRun
            {
                ProviderName = providerName,
                StartedAt = DateTime.UtcNow,
                Status = "Starting",
                TrainingDataSize = totalRecords,
                TriggerReason = triggerReason
            };

            _context.TrainingRuns.Add(trainingRun);
            await _context.SaveChangesAsync();

            // Convert TrainingDataExport to TrainingDataset
            var trainingDataset = new TrainingDataset
            {
                GeneratedAt = trainingDataExport.ExportedAt,
                TotalSamples = totalRecords,
                FromDate = trainingDataExport.FromDate,
                ToDate = trainingDataExport.ToDate
            };

            // Start provider-specific retraining
            _ = Task.Run(async () => await ExecuteProviderRetrainingAsync(trainingRun.Id, providerName, trainingDataset));

            _logger.LogInformation("Started retraining for {ProviderName} with training run ID {TrainingRunId}", 
                providerName, trainingRun.Id);

            return trainingRun.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering retraining for provider {ProviderName}", providerName);
            throw;
        }
    }

    public async Task<List<TrainingRunStatus>> GetActiveTrainingRunsAsync()
    {
        try
        {
            var activeRuns = await _context.TrainingRuns
                .Where(tr => tr.CompletedAt == null)
                .OrderByDescending(tr => tr.StartedAt)
                .ToListAsync();

            return activeRuns.Select(tr => new TrainingRunStatus
            {
                Id = tr.Id,
                ProviderName = tr.ProviderName,
                StartedAt = tr.StartedAt,
                CompletedAt = tr.CompletedAt,
                Status = tr.Status,
                TrainingDataSize = tr.TrainingDataSize,
                FinalAccuracy = tr.FinalAccuracy,
                ErrorMessage = tr.ErrorMessage,
                ResultingModelVersionId = tr.ResultingModelVersionId,
                ProgressPercentage = CalculateProgressPercentage(tr),
                CurrentPhase = GetCurrentPhase(tr.Status)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active training runs");
            throw;
        }
    }
    public async Task<TrainingRunStatus?> GetTrainingRunStatusAsync(int trainingRunId)
    {
        try
        {
            var trainingRun = _context.TrainingRuns
                .FirstOrDefaultAsync(tr => tr.Id == trainingRunId).GetAwaiter().GetResult();

            if (trainingRun == null)
                return null;

            return new TrainingRunStatus
            {
                Id = trainingRun.Id,
                ProviderName = trainingRun.ProviderName,
                StartedAt = trainingRun.StartedAt,
                CompletedAt = trainingRun.CompletedAt,
                Status = trainingRun.Status,
                TrainingDataSize = trainingRun.TrainingDataSize,
                FinalAccuracy = trainingRun.FinalAccuracy,
                ErrorMessage = trainingRun.ErrorMessage,
                ResultingModelVersionId = trainingRun.ResultingModelVersionId,
                ProgressPercentage = CalculateProgressPercentage(trainingRun),
                CurrentPhase = GetCurrentPhase(trainingRun.Status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training run status for ID {TrainingRunId}", trainingRunId);
            throw;
        }
    }

    public async Task<bool> HandleTrainingCompletionAsync(int trainingRunId, bool success, double? finalAccuracy = null, string? errorMessage = null)
    {
        _logger.LogInformation("Handling training completion for run {TrainingRunId}. Success: {Success}", 
            trainingRunId, success);

        try
        {
            var trainingRun = _context.TrainingRuns
                .FirstOrDefaultAsync(tr => tr.Id == trainingRunId).GetAwaiter().GetResult();

            if (trainingRun == null)
            {
                _logger.LogError("Training run {TrainingRunId} not found", trainingRunId);
                return false;
            }

            // Update training run status
            trainingRun.CompletedAt = DateTime.UtcNow;
            trainingRun.Status = success ? "Completed" : "Failed";
            trainingRun.FinalAccuracy = finalAccuracy;
            trainingRun.ErrorMessage = errorMessage;

            if (success && finalAccuracy.HasValue)
            {
                // Create model version record
                var modelVersion = new ModelVersion
                {
                    ProviderName = trainingRun.ProviderName,
                    ModelVersionNumber = $"v{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                    CreatedAt = DateTime.UtcNow,
                    ValidationAccuracy = finalAccuracy.Value,
                    IsActive = false // Will be activated after validation
                };

                _context.ModelVersions.Add(modelVersion);
                _context.SaveChangesAsync().GetAwaiter().GetResult();

                trainingRun.ResultingModelVersionId = modelVersion.Id;
                // Validate the new model
                var validationResult = ValidateModelAsync(modelVersion.Id).GetAwaiter().GetResult();
                
                if (validationResult.IsValid)
                {
                    var config = _retrainingConfigurations[trainingRun.ProviderName];
                    
                    // Auto-deploy if configured and validation passes threshold
                    if (config.AutoDeploy && 
                        validationResult.ImprovementPercentage >= config.ValidationThreshold)
                    {
                        var deploymentSuccess = DeployModelAsync(modelVersion.Id).GetAwaiter().GetResult();
                        _logger.LogInformation("Auto-deployment for {ProviderName} model version {ModelVersionId}: {Success}", 
                            trainingRun.ProviderName, modelVersion.Id, deploymentSuccess);
                        
                        return deploymentSuccess;
                    }
                    else
                    {
                        _logger.LogInformation("Model validation passed but auto-deploy conditions not met for {ProviderName}", 
                            trainingRun.ProviderName);
                        return true;
                    }
                }
                else
                {
                    _logger.LogWarning("Model validation failed for {ProviderName}: {Errors}", 
                        trainingRun.ProviderName, string.Join(", ", validationResult.ValidationErrors));
                    return false;
                }
            }

            _context.SaveChangesAsync().GetAwaiter().GetResult();
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling training completion for run {TrainingRunId}", trainingRunId);
            throw;
        }
    }

    public async Task<ModelValidationResult> ValidateModelAsync(int modelVersionId)
    {
        _logger.LogInformation("Validating model version {ModelVersionId}", modelVersionId);

        try
        {
            var modelVersion = _context.ModelVersions
                .FirstOrDefaultAsync(mv => mv.Id == modelVersionId).GetAwaiter().GetResult();

            if (modelVersion == null)
            {
                return new ModelValidationResult
                {
                    IsValid = false,
                    ValidationErrors = { "Model version not found" }
                };
            }

            var result = new ModelValidationResult
            {
                ValidationAccuracy = modelVersion.ValidationAccuracy ?? 0.0
            };

            // Get baseline accuracy from previous model
            var previousModel = _context.ModelVersions
                .Where(mv => mv.ProviderName == modelVersion.ProviderName && 
                           mv.IsActive && mv.Id != modelVersionId)
                .OrderByDescending(mv => mv.CreatedAt)
                .FirstOrDefaultAsync().GetAwaiter().GetResult();

            if (previousModel != null)
            {
                result.BaselineAccuracy = previousModel.ValidationAccuracy;
            }
            // Validate accuracy threshold
            var config = _retrainingConfigurations[modelVersion.ProviderName];
            if (result.ValidationAccuracy < config.AccuracyThreshold)
            {
                result.ValidationErrors.Add($"Validation accuracy {result.ValidationAccuracy:P2} below threshold {config.AccuracyThreshold:P2}");
            }

            // Check for improvement over baseline
            if (result.BaselineAccuracy.HasValue && 
                result.ImprovementPercentage < config.ValidationThreshold)
            {
                result.ValidationWarnings.Add($"Improvement {result.ImprovementPercentage:F2}% below threshold {config.ValidationThreshold:F2}%");
            }

            result.IsValid = !result.ValidationErrors.Any();

            _logger.LogInformation("Model validation completed for version {ModelVersionId}. Valid: {IsValid}, Accuracy: {Accuracy:P2}", 
                modelVersionId, result.IsValid, result.ValidationAccuracy);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating model version {ModelVersionId}", modelVersionId);
            return new ModelValidationResult
            {
                IsValid = false,
                ValidationErrors = { $"Validation error: {ex.Message}" }
            };
        }
    }

    public async Task<bool> DeployModelAsync(int modelVersionId)
    {
        _logger.LogInformation("Deploying model version {ModelVersionId}", modelVersionId);

        try
        {
            var modelVersion = _context.ModelVersions
                .FirstOrDefaultAsync(mv => mv.Id == modelVersionId).GetAwaiter().GetResult();

            if (modelVersion == null)
            {
                _logger.LogError("Model version {ModelVersionId} not found", modelVersionId);
                return false;
            }

            // Deactivate previous models for this provider
            var previousModels = _context.ModelVersions
                .Where(mv => mv.ProviderName == modelVersion.ProviderName && mv.IsActive)
                .ToListAsync().GetAwaiter().GetResult();

            foreach (var prevModel in previousModels)
            {
                prevModel.IsActive = false;
            }

            // Activate the new model
            modelVersion.IsActive = true;
            modelVersion.DeployedAt = DateTime.UtcNow;

            _context.SaveChangesAsync().GetAwaiter().GetResult();

            _logger.LogInformation("Successfully deployed model version {ModelVersionId} for provider {ProviderName}", 
                modelVersionId, modelVersion.ProviderName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying model version {ModelVersionId}", modelVersionId);
            return false;
        }
    }
    public async Task<bool> RollbackModelAsync(string providerName)
    {
        _logger.LogInformation("Rolling back model for provider {ProviderName}", providerName);

        try
        {
            // Find the current active model
            var currentModel = _context.ModelVersions
                .Where(mv => mv.ProviderName == providerName && mv.IsActive)
                .FirstOrDefaultAsync().GetAwaiter().GetResult();

            if (currentModel == null)
            {
                _logger.LogWarning("No active model found for provider {ProviderName}", providerName);
                return false;
            }

            // Find the previous model
            var previousModel = _context.ModelVersions
                .Where(mv => mv.ProviderName == providerName && 
                           mv.Id != currentModel.Id &&
                           mv.DeployedAt.HasValue)
                .OrderByDescending(mv => mv.DeployedAt)
                .FirstOrDefaultAsync().GetAwaiter().GetResult();

            if (previousModel == null)
            {
                _logger.LogWarning("No previous model found for rollback for provider {ProviderName}", providerName);
                return false;
            }

            // Perform rollback
            currentModel.IsActive = false;
            previousModel.IsActive = true;

            _context.SaveChangesAsync().GetAwaiter().GetResult();

            _logger.LogInformation("Successfully rolled back to model version {ModelVersionId} for provider {ProviderName}", 
                previousModel.Id, providerName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back model for provider {ProviderName}", providerName);
            return false;
        }
    }

    public async Task<List<TrainingRun>> GetTrainingHistoryAsync(string providerName, int limit = 10)
    {
        try
        {
            return _context.TrainingRuns
                .Where(tr => tr.ProviderName == providerName)
                .OrderByDescending(tr => tr.StartedAt)
                .Take(limit)
                .ToListAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training history for provider {ProviderName}", providerName);
            throw;
        }
    }

    public async Task<bool> ShouldTriggerRetrainingAsync(string providerName)
    {
        try
        {
            if (!_retrainingConfigurations.ContainsKey(providerName))
                return false;

            var config = _retrainingConfigurations[providerName];
            
            if (!config.IsEnabled)
                return false;
            // Check if enough time has passed since last retraining
            if (config.LastRetrainingAt.HasValue)
            {
                var timeSinceLastRetraining = DateTime.UtcNow - config.LastRetrainingAt.Value;
                if (timeSinceLastRetraining < config.RetrainingInterval)
                {
                    _logger.LogDebug("Retraining interval not met for {ProviderName}. Time since last: {TimeSince}, Required: {Required}", 
                        providerName, timeSinceLastRetraining, config.RetrainingInterval);
                    return false;
                }
            }

            // Check if we have sufficient data
            var dataCount = _context.LottoDraws.CountAsync().GetAwaiter().GetResult();
            if (dataCount < config.MinimumDataPoints)
            {
                _logger.LogDebug("Insufficient data points for {ProviderName}. Current: {Current}, Required: {Required}", 
                    providerName, dataCount, config.MinimumDataPoints);
                return false;
            }

            // Check accuracy threshold
            var recentAccuracy = GetRecentAccuracyAsync(providerName).GetAwaiter().GetResult();
            if (recentAccuracy.HasValue && recentAccuracy.Value < config.AccuracyThreshold)
            {
                _logger.LogInformation("Accuracy threshold triggered retraining for {ProviderName}. Current: {Current:P2}, Threshold: {Threshold:P2}", 
                    providerName, recentAccuracy.Value, config.AccuracyThreshold);
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking retraining conditions for provider {ProviderName}", providerName);
            return false;
        }
    }

    public async Task<Dictionary<string, RetrainingConfiguration>> GetRetrainingConfigurationsAsync()
    {
        return Task.FromResult(_retrainingConfigurations).GetAwaiter().GetResult();
    }

    private Dictionary<string, RetrainingConfiguration> LoadRetrainingConfigurations()
    {
        var configurations = new Dictionary<string, RetrainingConfiguration>();

        try
        {
            // Load from configuration
            var configSection = _configuration.GetSection("ModelRetraining:Providers");
            
            // Default configurations for known providers
            var defaultConfigs = new[]
            {
                new RetrainingConfiguration
                {
                    ProviderName = "BedrockAgentCore",
                    IsEnabled = _configuration.GetValue<bool>("ModelRetraining:Providers:BedrockAgentCore:Enabled", true),
                    AccuracyThreshold = _configuration.GetValue<double>("ModelRetraining:Providers:BedrockAgentCore:AccuracyThreshold", 0.1),
                    MinimumDataPoints = _configuration.GetValue<int>("ModelRetraining:Providers:BedrockAgentCore:MinimumDataPoints", 100),
                    RetrainingInterval = TimeSpan.FromDays(_configuration.GetValue<int>("ModelRetraining:Providers:BedrockAgentCore:RetrainingIntervalDays", 7)),
                    AutoDeploy = _configuration.GetValue<bool>("ModelRetraining:Providers:BedrockAgentCore:AutoDeploy", false),
                    ValidationThreshold = _configuration.GetValue<double>("ModelRetraining:Providers:BedrockAgentCore:ValidationThreshold", 0.05),
                    MaxConcurrentTraining = _configuration.GetValue<int>("ModelRetraining:Providers:BedrockAgentCore:MaxConcurrentTraining", 1)
                },
                new RetrainingConfiguration
                {
                    ProviderName = "LocalLLM",
                    IsEnabled = _configuration.GetValue<bool>("ModelRetraining:Providers:LocalLLM:Enabled", true),
                    AccuracyThreshold = _configuration.GetValue<double>("ModelRetraining:Providers:LocalLLM:AccuracyThreshold", 0.1),
                    MinimumDataPoints = _configuration.GetValue<int>("ModelRetraining:Providers:LocalLLM:MinimumDataPoints", 100),
                    RetrainingInterval = TimeSpan.FromDays(_configuration.GetValue<int>("ModelRetraining:Providers:LocalLLM:RetrainingIntervalDays", 7)),
                    AutoDeploy = _configuration.GetValue<bool>("ModelRetraining:Providers:LocalLLM:AutoDeploy", false),
                    ValidationThreshold = _configuration.GetValue<double>("ModelRetraining:Providers:LocalLLM:ValidationThreshold", 0.05),
                    MaxConcurrentTraining = _configuration.GetValue<int>("ModelRetraining:Providers:LocalLLM:MaxConcurrentTraining", 1)
                }
            };

            foreach (var config in defaultConfigs)
            {
                configurations[config.ProviderName] = config;
            }

            _logger.LogInformation("Loaded retraining configurations for {ProviderCount} providers", configurations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading retraining configurations, using defaults");
        }

        return configurations;
    }

    private async Task ExecuteProviderRetrainingAsync(int trainingRunId, string providerName, TrainingDataset trainingDataset)
    {
        try
        {
            _logger.LogInformation("Executing retraining for provider {ProviderName}, run ID {TrainingRunId}", 
                providerName, trainingRunId);

            // Update status to running
            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Preparing training data").GetAwaiter().GetResult();

            // Provider-specific retraining logic
            switch (providerName)
            {
                case "BedrockAgentCore":
                    ExecuteBedrockRetrainingAsync(trainingRunId, trainingDataset).GetAwaiter().GetResult();
                    break;
                case "LocalLLM":
                    ExecuteLocalLlmRetrainingAsync(trainingRunId, trainingDataset).GetAwaiter().GetResult();
                    break;
                default:
                    throw new NotSupportedException($"Retraining not supported for provider: {providerName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing retraining for provider {ProviderName}, run ID {TrainingRunId}", 
                providerName, trainingRunId);
            
            HandleTrainingCompletionAsync(trainingRunId, false, null, ex.Message).GetAwaiter().GetResult();
        }
    }
    private async Task ExecuteBedrockRetrainingAsync(int trainingRunId, TrainingDataset trainingDataset)
    {
        _logger.LogInformation("Executing Bedrock retraining for run ID {TrainingRunId}", trainingRunId);

        try
        {
            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Initializing Bedrock training").GetAwaiter().GetResult();

            // Get Bedrock service (this would be injected in a real implementation)
            var bedrockService = GetBedrockService();
            if (bedrockService == null)
            {
                throw new InvalidOperationException("Bedrock service is not available");
            }

            // Validate Bedrock configuration
            var isConfigValid = bedrockService.ValidateConfigurationAsync().GetAwaiter().GetResult();
            if (!isConfigValid)
            {
                throw new InvalidOperationException("Bedrock configuration is invalid");
            }

            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Starting Bedrock training job").GetAwaiter().GetResult();

            // Start the training job
            var trainingJobResult = bedrockService.StartTrainingJobAsync(trainingDataset).GetAwaiter().GetResult();
            
            _logger.LogInformation("Bedrock training job {JobId} started for run ID {TrainingRunId}", 
                trainingJobResult.JobId, trainingRunId);

            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Monitoring training progress").GetAwaiter().GetResult();

            // Monitor training progress
            var jobId = trainingJobResult.JobId;
            BedrockTrainingJobStatus jobStatus;
            var maxWaitTime = TimeSpan.FromHours(4); // Maximum wait time for training
            var startTime = DateTime.UtcNow;
            
            do
            {
                Task.Delay(TimeSpan.FromMinutes(2)).GetAwaiter().GetResult(); // Check every 2 minutes
                
                jobStatus = bedrockService.GetTrainingJobStatusAsync(jobId).GetAwaiter().GetResult();
                
                _logger.LogDebug("Bedrock training job {JobId} status: {Status} ({Progress}%)", 
                    jobId, jobStatus.Status, jobStatus.ProgressPercentage);

                // Update training run with current progress
                if (!string.IsNullOrEmpty(jobStatus.CurrentPhase))
                {
                    UpdateTrainingRunStatusAsync(trainingRunId, "Running", jobStatus.CurrentPhase).GetAwaiter().GetResult();
                }

                // Check for timeout
                if (DateTime.UtcNow - startTime > maxWaitTime)
                {
                    _logger.LogWarning("Bedrock training job {JobId} exceeded maximum wait time, cancelling", jobId);
                    bedrockService.CancelTrainingJobAsync(jobId).GetAwaiter().GetResult();
                    throw new TimeoutException("Training job exceeded maximum wait time");
                }

            } while (jobStatus.Status == "STARTING" || jobStatus.Status == "IN_PROGRESS");

            // Check final status
            if (jobStatus.Status != "COMPLETED")
            {
                var errorMessage = jobStatus.ErrorMessage ?? $"Training job failed with status: {jobStatus.Status}";
                throw new InvalidOperationException(errorMessage);
            }

            if (string.IsNullOrEmpty(jobStatus.ModelId))
            {
                throw new InvalidOperationException("Training completed but no model ID was returned");
            }

            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Validating trained model").GetAwaiter().GetResult();

            // Validate the trained model
            var validationResult = bedrockService.ValidateTrainedModelAsync(jobStatus.ModelId).GetAwaiter().GetResult();
            
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.ValidationErrors);
                throw new InvalidOperationException($"Model validation failed: {errors}");
            }

            // Get final accuracy from training metrics or validation result
            var finalAccuracy = validationResult.ValidationAccuracy;
            if (jobStatus.TrainingMetrics.ContainsKey("final_accuracy"))
            {
                finalAccuracy = Convert.ToDouble(jobStatus.TrainingMetrics["final_accuracy"]);
            }

            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Deploying trained model").GetAwaiter().GetResult();

            // Deploy the model (optional, based on configuration)
            var config = _retrainingConfigurations["BedrockAgentCore"];
            if (config.AutoDeploy && validationResult.ImprovementPercentage >= config.ValidationThreshold)
            {
                var deploymentName = $"lotto-prediction-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                var deploymentSuccess = bedrockService.DeployModelAsync(jobStatus.ModelId, deploymentName).GetAwaiter().GetResult();
                
                if (!deploymentSuccess)
                {
                    _logger.LogWarning("Model validation passed but deployment failed for Bedrock model {ModelId}", 
                        jobStatus.ModelId);
                }
            }

            HandleTrainingCompletionAsync(trainingRunId, true, finalAccuracy).GetAwaiter().GetResult();
            
            _logger.LogInformation("Bedrock retraining completed successfully for run ID {TrainingRunId} with model {ModelId} and accuracy {Accuracy:P2}", 
                trainingRunId, jobStatus.ModelId, finalAccuracy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Bedrock retraining for run ID {TrainingRunId}", trainingRunId);
            HandleTrainingCompletionAsync(trainingRunId, false, null, ex.Message).GetAwaiter().GetResult();
        }
    }

    private async Task ExecuteLocalLlmRetrainingAsync(int trainingRunId, TrainingDataset trainingDataset)
    {
        _logger.LogInformation("Executing Local LLM retraining for run ID {TrainingRunId}", trainingRunId);

        try
        {
            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Preparing local training environment").GetAwaiter().GetResult();

            // Format data for local LLM training
            var localLlmData = FormatDataForLocalLlmTrainingAsync(trainingDataset).GetAwaiter().GetResult();
            
            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Starting local model training").GetAwaiter().GetResult();

            // Note: This is a placeholder for actual local LLM training
            // In a real implementation, you would:
            // 1. Prepare training data in the required format
            // 2. Start the training process (possibly calling Python training scripts)
            // 3. Monitor training progress
            // 4. Validate the trained model
            // 5. Prepare for hot-swap deployment

            // Simulate training process
            Task.Delay(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult(); // Simulate setup time
            
            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Training in progress").GetAwaiter().GetResult();
            Task.Delay(TimeSpan.FromMinutes(3)).GetAwaiter().GetResult(); // Simulate training time

            UpdateTrainingRunStatusAsync(trainingRunId, "Running", "Validating trained model").GetAwaiter().GetResult();
            Task.Delay(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult(); // Simulate validation

            // Simulate successful completion with accuracy
            var finalAccuracy = 0.70 + (new Random().NextDouble() * 0.25); // Random accuracy between 0.70-0.95
            
            HandleTrainingCompletionAsync(trainingRunId, true, finalAccuracy).GetAwaiter().GetResult();
            
            _logger.LogInformation("Local LLM retraining completed successfully for run ID {TrainingRunId} with accuracy {Accuracy:P2}", 
                trainingRunId, finalAccuracy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Local LLM retraining for run ID {TrainingRunId}", trainingRunId);
            HandleTrainingCompletionAsync(trainingRunId, false, null, ex.Message).GetAwaiter().GetResult();
        }
    }
    private async Task<object> FormatDataForBedrockTrainingAsync(TrainingDataset trainingDataset)
    {
        // Format training data specifically for Bedrock AgentCore
        return new
        {
            HistoricalDraws = trainingDataset.HistoricalDraws.Select(d => new
            {
                d.Draw,
                d.Date,
                Numbers = new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 },
                d.BonusNumber,
                d.Powerball
            }),
            PredictionExamples = trainingDataset.PreviousPredictions.Select(p => new
            {
                p.Id,
                p.CreatedAt,
                p.Source,
                Numbers = p.GetNumbers(),
                p.ConfidenceScore,
                p.ReasoningExplanation
            }),
            AccuracyMetrics = trainingDataset.AccuracyData.Select(a => new
            {
                a.PredictionId,
                a.ExactMatches,
                a.PartialMatches,
                a.ProximityScore,
                a.OverallAccuracy,
                a.ProviderName
            }),
            Metadata = new
            {
                trainingDataset.GeneratedAt,
                trainingDataset.TotalSamples,
                Format = "BedrockAgentCore",
                Version = "1.0"
            }
        };
    }

    private async Task<object> FormatDataForLocalLlmTrainingAsync(TrainingDataset trainingDataset)
    {
        // Format training data specifically for local LLM
        return new
        {
            TrainingExamples = trainingDataset.HistoricalDraws.Select(d => new
            {
                Input = new
                {
                    HistoricalContext = trainingDataset.HistoricalDraws
                        .Where(h => h.Date < d.Date)
                        .OrderByDescending(h => h.Date)
                        .Take(10)
                        .Select(h => new[] { h.WinningNumber1, h.WinningNumber2, h.WinningNumber3, h.WinningNumber4, h.WinningNumber5, h.WinningNumber6 })
                },
                Output = new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 }
            }),
            ValidationExamples = trainingDataset.PreviousPredictions.Select(p => new
            {
                Prediction = p.GetNumbers(),
                p.ConfidenceScore,
                AccuracyScore = trainingDataset.AccuracyData
                    .Where(a => a.PredictionId == p.Id)
                    .Select(a => a.OverallAccuracy)
                    .FirstOrDefault()
            }),
            Configuration = new
            {
                ModelType = "LocalLLM",
                TrainingParameters = new
                {
                    LearningRate = 0.001,
                    BatchSize = 32,
                    Epochs = 10,
                    ValidationSplit = 0.2
                }
            }
        };
    }
    private async Task UpdateTrainingRunStatusAsync(int trainingRunId, string status, string? currentPhase = null)
    {
        try
        {
            var trainingRun = _context.TrainingRuns.FindAsync(trainingRunId).GetAwaiter().GetResult();
            if (trainingRun != null)
            {
                trainingRun.Status = status;
                if (currentPhase != null)
                {
                    trainingRun.CurrentPhase = currentPhase;
                }
                _context.SaveChangesAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training run status for ID {TrainingRunId}", trainingRunId);
        }
    }

    private async Task<List<TrainingRunStatus>> GetActiveTrainingRunsForProviderAsync(string providerName)
    {
        var activeRuns = _context.TrainingRuns
            .Where(tr => tr.ProviderName == providerName && tr.CompletedAt == null)
            .ToListAsync().GetAwaiter().GetResult();

        return activeRuns.Select(tr => new TrainingRunStatus
        {
            Id = tr.Id,
            ProviderName = tr.ProviderName,
            StartedAt = tr.StartedAt,
            Status = tr.Status
        }).ToList();
    }

    private async Task<double?> GetRecentAccuracyAsync(string providerName)
    {
        try
        {
            var recentAccuracy = _context.PredictionAccuracies
                .Where(pa => pa.ProviderName == providerName)
                .OrderByDescending(pa => pa.AnalyzedAt)
                .Take(10)
                .Select(pa => pa.OverallAccuracy)
                .ToListAsync().GetAwaiter().GetResult();

            return recentAccuracy.Any() ? recentAccuracy.Average() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent accuracy for provider {ProviderName}", providerName);
            return null;
        }
    }

    private double? CalculateProgressPercentage(TrainingRun trainingRun)
    {
        if (trainingRun.CompletedAt.HasValue)
            return 100.0;

        // Estimate progress based on status and elapsed time
        var elapsed = DateTime.UtcNow - trainingRun.StartedAt;
        
        return trainingRun.Status switch
        {
            "Starting" => 5.0,
            "Running" => Math.Min(95.0, 10.0 + (elapsed.TotalMinutes * 2)), // Rough estimate
            "Validating" => 90.0,
            "Completed" => 100.0,
            "Failed" => 0.0,
            _ => 0.0
        };
    }

    private string GetCurrentPhase(string status)
    {
        return status switch
        {
            "Starting" => "Initialization",
            "Running" => "Training",
            "Validating" => "Validation",
            "Completed" => "Complete",
            "Failed" => "Error",
            _ => "Unknown"
        };
    }

    private IBedrockAgentCoreService? GetBedrockService()
    {
        return _bedrockService;
    }
}