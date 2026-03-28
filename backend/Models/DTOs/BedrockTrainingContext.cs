using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class BedrockTrainingContext
{
    public List<LottoDraw> HistoricalDraws { get; set; } = new();
    public List<PredictionAccuracy> AccuracyMetrics { get; set; } = new();
    public Dictionary<string, double> ProviderPerformance { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalDraws { get; set; }
    public int TotalPredictions { get; set; }
    
    // Temporal patterns for enhanced context
    public Dictionary<string, object> TemporalPatterns { get; set; } = new();
    
    // Frequency analysis data
    public Dictionary<int, int> NumberFrequencies { get; set; } = new();
    public Dictionary<string, int> CombinationPatterns { get; set; } = new();
}

public class BedrockTrainingJobResult
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = "SUBMITTED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? TrainingDataS3Uri { get; set; }
    public string? OutputS3Uri { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> JobParameters { get; set; } = new();
}

public class BedrockTrainingJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = "UNKNOWN";
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? ProgressPercentage { get; set; }
    public string? CurrentPhase { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ModelId { get; set; }
    public Dictionary<string, object> TrainingMetrics { get; set; } = new();
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : null;
}

public class BedrockModelValidationResult
{
    public string ModelId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public double ValidationAccuracy { get; set; }
    public double? BaselineAccuracy { get; set; }
    public double? ImprovementPercentage => BaselineAccuracy.HasValue && BaselineAccuracy.Value > 0 
        ? ((ValidationAccuracy - BaselineAccuracy.Value) / BaselineAccuracy.Value) * 100 
        : null;
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    public string ValidatedBy { get; set; } = "BedrockAgentCoreService";
}

public class BedrockTrainingRequest
{
    [Required]
    public string JobName { get; set; } = string.Empty;
    
    [Required]
    public TrainingDataset TrainingDataset { get; set; } = new();
    
    public string? BaseModelId { get; set; }
    public Dictionary<string, object> HyperParameters { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public int? MaxTrainingTimeInSeconds { get; set; }
    public string? OutputS3Bucket { get; set; }
}

public class BedrockModelDeploymentRequest
{
    [Required]
    public string ModelId { get; set; } = string.Empty;
    
    [Required]
    public string DeploymentName { get; set; } = string.Empty;
    
    public string? InstanceType { get; set; }
    public int? InitialInstanceCount { get; set; } = 1;
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class BedrockModelDeploymentResult
{
    public string DeploymentId { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string Status { get; set; } = "CREATING";
    public string? EndpointUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}