namespace PredictLottoNZ.Models.DTOs;

/// <summary>
/// Structured data formatted specifically for LLM consumption and training
/// </summary>
public class LlmTrainingData
{
    public List<LlmHistoricalPattern> HistoricalPatterns { get; set; } = new();
    public List<LlmPredictionExample> PredictionExamples { get; set; } = new();
    public Dictionary<string, LlmProviderMetrics> ProviderPerformance { get; set; } = new();
    public LlmMetaInformation MetaInformation { get; set; } = new();
}

/// <summary>
/// Historical lottery draw data with contextual information for LLM analysis
/// </summary>
public class LlmHistoricalPattern
{
    public int DrawNumber { get; set; }
    public DateTime Date { get; set; }
    public int[] WinningNumbers { get; set; } = Array.Empty<int>();
    public int BonusNumber { get; set; }
    public int Powerball { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public int MonthOfYear { get; set; }
    public Dictionary<int, double> NumberFrequencies { get; set; } = new();
    public TemporalContext TemporalContext { get; set; } = new();
}

/// <summary>
/// Temporal context information for understanding draw patterns
/// </summary>
public class TemporalContext
{
    public int DaysSinceLastDraw { get; set; }
    public int DrawsInLastMonth { get; set; }
    public int DrawsInLastYear { get; set; }
}

/// <summary>
/// Prediction examples with accuracy feedback for LLM learning
/// </summary>
public class LlmPredictionExample
{
    public int PredictionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public int[] Numbers { get; set; } = Array.Empty<int>();
    public double? ConfidenceScore { get; set; }
    public string? ReasoningExplanation { get; set; }
    public DateTime? TargetDrawDate { get; set; }
    public List<LlmAccuracyMetric> AccuracyMetrics { get; set; } = new();
}

/// <summary>
/// Accuracy metrics for individual predictions
/// </summary>
public class LlmAccuracyMetric
{
    public int ExactMatches { get; set; }
    public int PartialMatches { get; set; }
    public double ProximityScore { get; set; }
    public double OverallAccuracy { get; set; }
    public DateTime? ActualDrawDate { get; set; }
    public int[]? ActualNumbers { get; set; }
}

/// <summary>
/// Provider performance metrics for LLM context
/// </summary>
public class LlmProviderMetrics
{
    public string ProviderName { get; set; } = string.Empty;
    public int TotalPredictions { get; set; }
    public double AverageAccuracy { get; set; }
    public int BestPerformance { get; set; }
    public double ConsistencyScore { get; set; }
    public double RecentTrend { get; set; }
}

/// <summary>
/// Meta information about the training dataset
/// </summary>
public class LlmMetaInformation
{
    public int TotalDraws { get; set; }
    public int TotalPredictions { get; set; }
    public DateRange DateRange { get; set; } = new();
    public NumberRange NumberRange { get; set; } = new();
    public int CombinationSize { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string SchemaVersion { get; set; } = string.Empty;
}

/// <summary>
/// Date range information
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

// NumberRange is now defined in LookupModels.cs to avoid conflicts

/// <summary>
/// Enhanced training dataset with comprehensive data for LLM training
/// </summary>
public class TrainingDataset
{
    public List<LottoDraw> HistoricalDraws { get; set; } = new();
    public List<Prediction> PreviousPredictions { get; set; } = new();
    public List<PredictionAccuracy> AccuracyData { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public int TotalSamples { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsIncremental { get; set; }
    public Dictionary<string, ProviderPerformanceMetrics> ProviderPerformanceMetrics { get; set; } = new();
    public LlmTrainingData LlmFormattedData { get; set; } = new();
    public TrainingDatasetMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata about the training dataset generation process
/// </summary>
public class TrainingDatasetMetadata
{
    public string Version { get; set; } = "1.0";
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = "AccuracyAnalysisService";
    public bool IsValidated { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}

/// <summary>
/// Schema validation result for training datasets
/// </summary>
public class TrainingDatasetValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, int> Statistics { get; set; } = new();
}

