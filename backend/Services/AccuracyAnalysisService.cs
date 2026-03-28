using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IAccuracyAnalysisService
{
    Task<AccuracyMetrics> AnalyzePredictionAccuracyAsync(LottoDraw actualDraw);
    Task<AccuracyMetrics> AnalyzeBatchPredictionAccuracyAsync(IEnumerable<LottoDraw> actualDraws);
    Task<TrainingDataset> GenerateTrainingDatasetAsync();
    Task<Dictionary<string, ProviderPerformanceMetrics>> GetProviderPerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null);
}

public class AccuracyAnalysisService : IAccuracyAnalysisService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<AccuracyAnalysisService> _logger;

    public AccuracyAnalysisService(LottoDbContext context, ILogger<AccuracyAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccuracyMetrics> AnalyzePredictionAccuracyAsync(LottoDraw actualDraw)
    {
        _logger.LogInformation("Analyzing prediction accuracy for draw {DrawNumber} on {DrawDate}", 
            actualDraw.Draw, actualDraw.Date);

        // Find all predictions that could have been for this draw
        // Look for predictions made before this draw date
        var predictions = await _context.Predictions
            .Where(p => p.CreatedAt <= actualDraw.Date && 
                       (p.TargetDrawDate == null || p.TargetDrawDate <= actualDraw.Date))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var accuracyResults = new List<PredictionAccuracyResult>();
        var actualNumbers = new[] { 
            actualDraw.WinningNumber1, actualDraw.WinningNumber2, actualDraw.WinningNumber3,
            actualDraw.WinningNumber4, actualDraw.WinningNumber5, actualDraw.WinningNumber6 
        }.OrderBy(n => n).ToArray();

        foreach (var prediction in predictions)
        {
            var predictionNumbers = prediction.GetNumbers().OrderBy(n => n).ToArray();
            
            // Calculate exact matches (numbers that appear in both arrays)
            var exactMatches = predictionNumbers.Intersect(actualNumbers).Count();
            
            // Calculate partial matches (numbers within proximity range)
            var partialMatches = CalculatePartialMatches(predictionNumbers, actualNumbers);
            
            // Calculate proximity score (how close the numbers are on average)
            var proximityScore = CalculateProximityScore(predictionNumbers, actualNumbers);
            
            // Calculate overall accuracy score
            var overallAccuracy = CalculateOverallAccuracy(exactMatches, partialMatches, proximityScore);

            var accuracyResult = new PredictionAccuracyResult
            {
                PredictionId = prediction.Id,
                ProviderName = prediction.Source,
                ExactMatches = exactMatches,
                PartialMatches = partialMatches,
                ProximityScore = proximityScore,
                OverallAccuracy = overallAccuracy,
                PredictionNumbers = predictionNumbers,
                ActualNumbers = actualNumbers,
                DaysBeforeDraw = (int)(actualDraw.Date - prediction.CreatedAt).TotalDays
            };

            accuracyResults.Add(accuracyResult);

            // Store the accuracy data in the database
            var accuracyRecord = new PredictionAccuracy
            {
                PredictionId = prediction.Id,
                ActualDrawId = actualDraw.Draw,
                ExactMatches = exactMatches,
                PartialMatches = partialMatches,
                ProximityScore = proximityScore,
                OverallAccuracy = overallAccuracy,
                ProviderName = prediction.Source,
                AnalyzedAt = DateTime.UtcNow
            };

            _context.PredictionAccuracies.Add(accuracyRecord);
        }

        await _context.SaveChangesAsync();

        // Calculate aggregate metrics
        var metrics = new AccuracyMetrics
        {
            TotalPredictions = predictions.Count,
            ExactMatches = accuracyResults.Sum(r => r.ExactMatches),
            PartialMatches = accuracyResults.Sum(r => r.PartialMatches),
            AverageProximityScore = accuracyResults.Any() ? accuracyResults.Average(r => r.ProximityScore) : 0.0,
            ProviderAccuracy = accuracyResults
                .GroupBy(r => r.ProviderName)
                .ToDictionary(g => g.Key, g => g.Average(r => r.OverallAccuracy)),
            AccuracyResults = accuracyResults,
            AnalyzedAt = DateTime.UtcNow,
            DrawNumber = actualDraw.Draw,
            DrawDate = actualDraw.Date
        };

        _logger.LogInformation("Completed accuracy analysis for draw {DrawNumber}: {TotalPredictions} predictions analyzed, " +
                              "average accuracy: {AverageAccuracy:F3}", 
            actualDraw.Draw, predictions.Count, 
            accuracyResults.Any() ? accuracyResults.Average(r => r.OverallAccuracy) : 0.0);

        return metrics;
    }

    public async Task<AccuracyMetrics> AnalyzeBatchPredictionAccuracyAsync(IEnumerable<LottoDraw> actualDraws)
    {
        _logger.LogInformation("Starting batch prediction accuracy analysis for {DrawCount} draws", 
            actualDraws.Count());

        var allAccuracyResults = new List<PredictionAccuracyResult>();
        var drawList = actualDraws.OrderBy(d => d.Date).ToList();

        foreach (var draw in drawList)
        {
            var drawMetrics = await AnalyzePredictionAccuracyAsync(draw);
            allAccuracyResults.AddRange(drawMetrics.AccuracyResults);
        }

        // Calculate aggregate metrics across all draws
        var aggregateMetrics = new AccuracyMetrics
        {
            TotalPredictions = allAccuracyResults.Count,
            ExactMatches = allAccuracyResults.Sum(r => r.ExactMatches),
            PartialMatches = allAccuracyResults.Sum(r => r.PartialMatches),
            AverageProximityScore = allAccuracyResults.Any() ? allAccuracyResults.Average(r => r.ProximityScore) : 0.0,
            ProviderAccuracy = allAccuracyResults
                .GroupBy(r => r.ProviderName)
                .ToDictionary(g => g.Key, g => g.Average(r => r.OverallAccuracy)),
            AccuracyResults = allAccuracyResults,
            AnalyzedAt = DateTime.UtcNow,
            DrawNumber = drawList.LastOrDefault()?.Draw ?? 0,
            DrawDate = drawList.LastOrDefault()?.Date ?? DateTime.MinValue
        };

        _logger.LogInformation("Completed batch accuracy analysis: {TotalPredictions} predictions across {DrawCount} draws, " +
                              "overall average accuracy: {AverageAccuracy:F3}", 
            allAccuracyResults.Count, drawList.Count,
            allAccuracyResults.Any() ? allAccuracyResults.Average(r => r.OverallAccuracy) : 0.0);

        return aggregateMetrics;
    }

    public async Task<TrainingDataset> GenerateTrainingDatasetAsync()
    {
        // Delegate to the dedicated TrainingDataService
        // This method is kept for backward compatibility
        _logger.LogInformation("Delegating training dataset generation to TrainingDataService");
        
        // Note: This would require injecting ITrainingDataService, but to avoid circular dependency,
        // we'll keep a simplified version here and recommend using TrainingDataService directly
        var drawsQuery = _context.LottoDraws.AsQueryable();
        var predictionsQuery = _context.Predictions.AsQueryable();
        var accuracyQuery = _context.PredictionAccuracies
            .Include(pa => pa.Prediction)
            .Include(pa => pa.ActualDraw)
            .AsQueryable();

        var historicalDraws = await drawsQuery.OrderBy(d => d.Date).ToListAsync();
        var previousPredictions = await predictionsQuery.OrderBy(p => p.CreatedAt).ToListAsync();
        var accuracyData = await accuracyQuery.OrderBy(pa => pa.AnalyzedAt).ToListAsync();

        var dataset = new TrainingDataset
        {
            HistoricalDraws = historicalDraws,
            PreviousPredictions = previousPredictions,
            AccuracyData = accuracyData,
            GeneratedAt = DateTime.UtcNow,
            TotalSamples = historicalDraws.Count + previousPredictions.Count + accuracyData.Count
        };

        return dataset;
    }





    public async Task<Dictionary<string, ProviderPerformanceMetrics>> GetProviderPerformanceAsync(
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Calculating provider performance metrics from {FromDate} to {ToDate}", 
            fromDate, toDate);

        var query = _context.PredictionAccuracies
            .Include(pa => pa.Prediction)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(pa => pa.AnalyzedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pa => pa.AnalyzedAt <= toDate.Value);

        var accuracyRecords = await query.ToListAsync();

        var providerMetrics = accuracyRecords
            .GroupBy(pa => pa.ProviderName)
            .ToDictionary(g => g.Key, g => new ProviderPerformanceMetrics
            {
                ProviderName = g.Key,
                TotalPredictions = g.Count(),
                AverageExactMatches = g.Average(pa => pa.ExactMatches),
                AveragePartialMatches = g.Average(pa => pa.PartialMatches),
                AverageProximityScore = g.Average(pa => pa.ProximityScore),
                AverageOverallAccuracy = g.Average(pa => pa.OverallAccuracy),
                BestExactMatches = g.Max(pa => pa.ExactMatches),
                WorstExactMatches = g.Min(pa => pa.ExactMatches),
                StandardDeviationAccuracy = CalculateStandardDeviation(g.Select(pa => pa.OverallAccuracy)),
                LastAnalyzedAt = g.Max(pa => pa.AnalyzedAt)
            });

        _logger.LogInformation("Calculated performance metrics for {ProviderCount} providers", 
            providerMetrics.Count);

        return providerMetrics;
    }

    private int CalculatePartialMatches(int[] predictionNumbers, int[] actualNumbers)
    {
        // Count numbers that are within +/- 2 of any actual number but not exact matches
        var exactMatches = predictionNumbers.Intersect(actualNumbers).ToHashSet();
        var partialMatches = 0;

        foreach (var predNum in predictionNumbers.Where(n => !exactMatches.Contains(n)))
        {
            if (actualNumbers.Any(actualNum => Math.Abs(predNum - actualNum) <= 2))
            {
                partialMatches++;
            }
        }

        return partialMatches;
    }

    private double CalculateProximityScore(int[] predictionNumbers, int[] actualNumbers)
    {
        // Calculate average minimum distance between predicted and actual numbers
        var totalDistance = 0.0;

        foreach (var predNum in predictionNumbers)
        {
            var minDistance = actualNumbers.Min(actualNum => Math.Abs(predNum - actualNum));
            totalDistance += minDistance;
        }

        // Normalize to 0-1 scale (lower distance = higher score)
        var averageDistance = totalDistance / predictionNumbers.Length;
        var maxPossibleDistance = 39.0; // Maximum distance in range [1,40]
        
        return Math.Max(0.0, 1.0 - (averageDistance / maxPossibleDistance));
    }

    private double CalculateOverallAccuracy(int exactMatches, int partialMatches, double proximityScore)
    {
        // Weighted combination of different accuracy measures
        var exactMatchWeight = 0.6;
        var partialMatchWeight = 0.2;
        var proximityWeight = 0.2;

        var exactMatchScore = exactMatches / 6.0; // Normalize to 0-1
        var partialMatchScore = partialMatches / 6.0; // Normalize to 0-1

        return (exactMatchScore * exactMatchWeight) + 
               (partialMatchScore * partialMatchWeight) + 
               (proximityScore * proximityWeight);
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count <= 1) return 0.0;

        var average = valueList.Average();
        var sumOfSquaresOfDifferences = valueList.Sum(val => (val - average) * (val - average));
        
        return Math.Sqrt(sumOfSquaresOfDifferences / valueList.Count);
    }
}

// Additional DTOs for accuracy analysis
public class AccuracyMetrics
{
    public int TotalPredictions { get; set; }
    public int ExactMatches { get; set; }
    public int PartialMatches { get; set; }
    public double AverageProximityScore { get; set; }
    public Dictionary<string, double> ProviderAccuracy { get; set; } = new();
    public List<PredictionAccuracyResult> AccuracyResults { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
    public int DrawNumber { get; set; }
    public DateTime DrawDate { get; set; }
}

public class PredictionAccuracyResult
{
    public int PredictionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int ExactMatches { get; set; }
    public int PartialMatches { get; set; }
    public double ProximityScore { get; set; }
    public double OverallAccuracy { get; set; }
    public int[] PredictionNumbers { get; set; } = Array.Empty<int>();
    public int[] ActualNumbers { get; set; } = Array.Empty<int>();
    public int DaysBeforeDraw { get; set; }
}



