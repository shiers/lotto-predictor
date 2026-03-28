using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IPredictionScoreUpdateService
{
    Task<PredictionScoreUpdateResult> UpdateScoresAfterDrawImportAsync(IEnumerable<LottoDraw> newDraws);
    Task<PredictionScoreUpdateResult> UpdateScoresForSingleDrawAsync(LottoDraw newDraw);
    Task<IEnumerable<PredictionScoreHistory>> GetScoreHistoryAsync(int predictionId);
    Task<bool> HasScoreHistoryAsync(int predictionId);
    Task<Dictionary<int, PredictionScoreInfo>> GetLatestScoresAsync(IEnumerable<int> predictionIds);
}

public class PredictionScoreUpdateResult
{
    public int TotalPredictionsProcessed { get; set; }
    public int PredictionsUpdated { get; set; }
    public int ScoreHistoryRecordsCreated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public List<int> ProcessedDrawIds { get; set; } = new();
}

public class PredictionScoreInfo
{
    public int PredictionId { get; set; }
    public double? OriginalScore { get; set; }
    public double? UpdatedScore { get; set; }
    public DateTime? LastScoreUpdate { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int TotalUpdates { get; set; }
    public double? LatestAccuracy { get; set; }
}