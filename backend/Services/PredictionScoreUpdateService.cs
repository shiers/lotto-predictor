using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class PredictionScoreUpdateService : IPredictionScoreUpdateService
{
    private readonly LottoDbContext _context;
    private readonly IAccuracyAnalysisService _accuracyAnalysisService;
    private readonly ILogger<PredictionScoreUpdateService> _logger;

    public PredictionScoreUpdateService(
        LottoDbContext context,
        IAccuracyAnalysisService accuracyAnalysisService,
        ILogger<PredictionScoreUpdateService> logger)
    {
        _context = context;
        _accuracyAnalysisService = accuracyAnalysisService;
        _logger = logger;
    }

    public async Task<PredictionScoreUpdateResult> UpdateScoresAfterDrawImportAsync(IEnumerable<LottoDraw> newDraws)
    {
        _logger.LogInformation("Starting prediction score updates for {DrawCount} new draws", newDraws.Count());
        
        var result = new PredictionScoreUpdateResult();
        var drawsList = newDraws.OrderBy(d => d.Date).ToList();

        foreach (var draw in drawsList)
        {
            try
            {
                var singleDrawResult = await UpdateScoresForSingleDrawAsync(draw);
                
                result.TotalPredictionsProcessed += singleDrawResult.TotalPredictionsProcessed;
                result.PredictionsUpdated += singleDrawResult.PredictionsUpdated;
                result.ScoreHistoryRecordsCreated += singleDrawResult.ScoreHistoryRecordsCreated;
                result.ProcessedDrawIds.AddRange(singleDrawResult.ProcessedDrawIds);
                result.Errors.AddRange(singleDrawResult.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scores for draw {DrawId}", draw.Draw);
                result.Errors.Add($"Failed to update scores for draw {draw.Draw}: {ex.Message}");
            }
        }

        _logger.LogInformation("Completed prediction score updates. Processed: {Processed}, Updated: {Updated}, History Records: {History}",
            result.TotalPredictionsProcessed, result.PredictionsUpdated, result.ScoreHistoryRecordsCreated);

        return result;
    }

    public async Task<PredictionScoreUpdateResult> UpdateScoresForSingleDrawAsync(LottoDraw newDraw)
    {
        _logger.LogInformation("Updating prediction scores for draw {DrawId} on {DrawDate}", 
            newDraw.Draw, newDraw.Date);

        var result = new PredictionScoreUpdateResult();
        result.ProcessedDrawIds.Add(newDraw.Draw);

        try
        {
            // First, run accuracy analysis for this draw
            var accuracyMetrics = await _accuracyAnalysisService.AnalyzePredictionAccuracyAsync(newDraw);
            
            if (!accuracyMetrics.AccuracyResults.Any())
            {
                _logger.LogInformation("No predictions found to update for draw {DrawId}", newDraw.Draw);
                return result;
            }

            // Get all predictions that were analyzed
            var predictionIds = accuracyMetrics.AccuracyResults.Select(ar => ar.PredictionId).ToList();
            var predictions = await _context.Predictions
                .Where(p => predictionIds.Contains(p.Id))
                .ToListAsync();

            result.TotalPredictionsProcessed = predictions.Count;

            // Update scores for each prediction
            foreach (var prediction in predictions)
            {
                var accuracyResult = accuracyMetrics.AccuracyResults
                    .First(ar => ar.PredictionId == prediction.Id);

                // Store original score if this is the first update
                var originalScore = prediction.Score ?? 0.0;
                
                // Calculate new updated score based on accuracy
                var newUpdatedScore = CalculateUpdatedScore(prediction, accuracyResult);

                // Create score history record
                var scoreHistory = new PredictionScoreHistory
                {
                    PredictionId = prediction.Id,
                    OriginalScore = originalScore,
                    UpdatedScore = newUpdatedScore,
                    UpdatedAt = DateTime.UtcNow,
                    UpdateReason = $"Updated after draw {newDraw.Draw} with {accuracyResult.ExactMatches} exact matches",
                    TriggeringDrawId = newDraw.Draw,
                    ExactMatches = accuracyResult.ExactMatches,
                    PartialMatches = accuracyResult.PartialMatches,
                    ProximityScore = accuracyResult.ProximityScore,
                    OverallAccuracy = accuracyResult.OverallAccuracy
                };

                _context.PredictionScoreHistories.Add(scoreHistory);

                // Update the prediction with new score
                prediction.UpdatedScore = newUpdatedScore;
                prediction.LastScoreUpdate = DateTime.UtcNow;

                result.PredictionsUpdated++;
                result.ScoreHistoryRecordsCreated++;

                _logger.LogDebug("Updated prediction {PredictionId}: Original={Original:F3}, Updated={Updated:F3}, Accuracy={Accuracy:F3}",
                    prediction.Id, originalScore, newUpdatedScore, accuracyResult.OverallAccuracy);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated {UpdatedCount} predictions for draw {DrawId}",
                result.PredictionsUpdated, newDraw.Draw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scores for draw {DrawId}", newDraw.Draw);
            result.Errors.Add($"Failed to update scores for draw {newDraw.Draw}: {ex.Message}");
        }

        return result;
    }

    public async Task<IEnumerable<PredictionScoreHistory>> GetScoreHistoryAsync(int predictionId)
    {
        _logger.LogDebug("Retrieving score history for prediction {PredictionId}", predictionId);

        return await _context.PredictionScoreHistories
            .Where(psh => psh.PredictionId == predictionId)
            .Include(psh => psh.TriggeringDraw)
            .OrderByDescending(psh => psh.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasScoreHistoryAsync(int predictionId)
    {
        _logger.LogDebug("Checking if prediction {PredictionId} has score history", predictionId);

        return await _context.PredictionScoreHistories
            .AnyAsync(psh => psh.PredictionId == predictionId);
    }

    public async Task<Dictionary<int, PredictionScoreInfo>> GetLatestScoresAsync(IEnumerable<int> predictionIds)
    {
        _logger.LogDebug("Retrieving latest scores for {Count} predictions", predictionIds.Count());

        var predictionIdsList = predictionIds.ToList();
        
        // Get predictions with their latest scores
        var predictions = await _context.Predictions
            .Where(p => predictionIdsList.Contains(p.Id))
            .ToListAsync();

        // Get score history counts
        var historyStats = await _context.PredictionScoreHistories
            .Where(psh => predictionIdsList.Contains(psh.PredictionId))
            .GroupBy(psh => psh.PredictionId)
            .Select(g => new
            {
                PredictionId = g.Key,
                UpdateCount = g.Count(),
                LatestAccuracy = g.OrderByDescending(psh => psh.UpdatedAt).First().OverallAccuracy
            })
            .ToListAsync();

        var result = new Dictionary<int, PredictionScoreInfo>();

        foreach (var prediction in predictions)
        {
            var historyInfo = historyStats.FirstOrDefault(hs => hs.PredictionId == prediction.Id);
            
            result[prediction.Id] = new PredictionScoreInfo
            {
                PredictionId = prediction.Id,
                OriginalScore = prediction.Score,
                UpdatedScore = prediction.UpdatedScore,
                LastScoreUpdate = prediction.LastScoreUpdate,
                ProviderName = prediction.Source,
                TotalUpdates = historyInfo?.UpdateCount ?? 0,
                LatestAccuracy = historyInfo?.LatestAccuracy
            };
        }

        return result;
    }

    private double CalculateUpdatedScore(Prediction prediction, PredictionAccuracyResult accuracyResult)
    {
        // Get the original score or use a default
        var originalScore = prediction.UpdatedScore ?? prediction.Score ?? 0.5;
        
        // Weight factors for score adjustment
        const double learningRate = 0.1; // How much to adjust based on new information
        const double accuracyWeight = 0.7; // Weight of overall accuracy
        const double exactMatchWeight = 0.3; // Weight of exact matches
        
        // Calculate accuracy-based adjustment
        var accuracyAdjustment = accuracyResult.OverallAccuracy * accuracyWeight;
        var exactMatchAdjustment = (accuracyResult.ExactMatches / 6.0) * exactMatchWeight;
        
        // Calculate target score based on current performance
        var targetScore = accuracyAdjustment + exactMatchAdjustment;
        
        // Use exponential moving average to update score
        // This prevents the consistent downward trend by properly weighting new vs old information
        var newScore = originalScore * (1 - learningRate) + targetScore * learningRate;
        
        // Ensure score stays within bounds [0, 1]
        return Math.Max(0.0, Math.Min(1.0, newScore));
    }
}