using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class TrainingRunStatus
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TrainingDataSize { get; set; }
    public double? FinalAccuracy { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ResultingModelVersionId { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : DateTime.UtcNow - StartedAt;
    public double? ProgressPercentage { get; set; }
    public string? CurrentPhase { get; set; }
    public Dictionary<string, object>? TrainingMetrics { get; set; }
}

public class ModelValidationResult
{
    public bool IsValid { get; set; }
    public double ValidationAccuracy { get; set; }
    public double? BaselineAccuracy { get; set; }
    public double? ImprovementPercentage => BaselineAccuracy.HasValue && BaselineAccuracy.Value > 0 
        ? ((ValidationAccuracy - BaselineAccuracy.Value) / BaselineAccuracy.Value) * 100 
        : null;
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public Dictionary<string, double> DetailedMetrics { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    public string ValidatedBy { get; set; } = "ModelRetrainingService";
}

public class RetrainingConfiguration
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public double AccuracyThreshold { get; set; } = 0.1; // Trigger retraining if accuracy drops below this
    public int MinimumDataPoints { get; set; } = 100; // Minimum data points required for retraining
    public TimeSpan RetrainingInterval { get; set; } = TimeSpan.FromDays(7); // Maximum time between retraining
    public DateTime? LastRetrainingAt { get; set; }
    public bool AutoDeploy { get; set; } = false; // Whether to automatically deploy validated models
    public double ValidationThreshold { get; set; } = 0.05; // Minimum improvement required for deployment
    public int MaxConcurrentTraining { get; set; } = 1; // Maximum concurrent training runs for this provider
    public Dictionary<string, object> ProviderSpecificSettings { get; set; } = new();
}

public class RetrainingTriggerRequest
{
    [Required]
    public string ProviderName { get; set; } = string.Empty;
    
    [Required]
    public string TriggerReason { get; set; } = string.Empty;
    
    public bool ForceRetrain { get; set; } = false;
    public Dictionary<string, object>? CustomParameters { get; set; }
}

public class RetrainingPipelineResult
{
    public List<int> StartedTrainingRuns { get; set; } = new();
    public List<string> SkippedProviders { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public string TriggerReason { get; set; } = string.Empty;
    public bool Success => Errors.Count == 0;
}

public class ModelDeploymentResult
{
    public bool Success { get; set; }
    public int ModelVersionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; } = DateTime.UtcNow;
    public string? PreviousModelVersion { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? DeploymentMetrics { get; set; }
}

public class TrainingProgressUpdate
{
    public int TrainingRunId { get; set; }
    public double ProgressPercentage { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RetrainingHealthCheck
{
    public bool IsHealthy { get; set; }
    public int ActiveTrainingRuns { get; set; }
    public int QueuedTrainingRuns { get; set; }
    public List<string> UnhealthyProviders { get; set; } = new();
    public Dictionary<string, DateTime> LastSuccessfulRetraining { get; set; } = new();
    public List<string> RecentErrors { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}