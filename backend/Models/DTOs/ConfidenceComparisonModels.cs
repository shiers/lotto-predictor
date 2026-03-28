namespace PredictLottoNZ.Models.DTOs;

public class ConfidenceRankedPrediction
{
    public PredictionResult Prediction { get; set; } = new();
    public double ConfidenceRank { get; set; }
    public double ProviderWeight { get; set; }
    public double WeightedConfidence { get; set; }
    public bool IsHighestConfidence { get; set; }
    public string ConfidenceCategory { get; set; } = string.Empty; // "High", "Medium", "Low"
}

public class WeightedPredictionResult
{
    public PredictionResult Prediction { get; set; } = new();
    public double ProviderWeight { get; set; }
    public double WeightedScore { get; set; }
    public double WeightedConfidence { get; set; }
    public double OverallRating { get; set; }
}

public class PredictionRecommendations
{
    public PredictionResult? TopRecommendation { get; set; }
    public IEnumerable<PredictionResult> HighConfidencePredictions { get; set; } = new List<PredictionResult>();
    public IEnumerable<PredictionResult> BestPerformingProviderPredictions { get; set; } = new List<PredictionResult>();
    public Dictionary<string, double> ProviderConfidenceAverages { get; set; } = new();
    public Dictionary<string, double> ProviderPerformanceWeights { get; set; } = new();
    public double RecommendedConfidenceThreshold { get; set; }
    public string RecommendationReasoning { get; set; } = string.Empty;
}

public class ProviderPerformanceMetrics
{
    public string ProviderName { get; set; } = string.Empty;
    public double AverageAccuracy { get; set; }
    public double AverageConfidence { get; set; }
    public int TotalPredictions { get; set; }
    public int SuccessfulPredictions { get; set; }
    public double SuccessRate { get; set; }
    public double PerformanceWeight { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Additional properties for compatibility with existing code
    public double AverageExactMatches { get; set; }
    public double AveragePartialMatches { get; set; }
    public double AverageProximityScore { get; set; }
    public double AverageOverallAccuracy { get; set; }
    public int BestExactMatches { get; set; }
    public int WorstExactMatches { get; set; }
    public double StandardDeviationAccuracy { get; set; }
    public DateTime LastAnalyzedAt { get; set; }
}

public class ConfidenceFilterRequest
{
    public double MinConfidence { get; set; } = 0.0;
    public double MaxConfidence { get; set; } = 1.0;
    public string? PreferredProvider { get; set; }
    public bool IncludeWeighting { get; set; } = true;
    public int MaxResults { get; set; } = 10;
}