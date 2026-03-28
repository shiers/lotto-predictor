using PredictLottoNZ.Models;

namespace PredictLottoNZ.Services;

public interface IPredictionMatchingService
{
    Task<PredictionMatchResult> CheckAndUpdateMatchesAsync(IEnumerable<LottoDraw> newDraws);
    Task<PredictionMatchResult> CheckAndUpdateMatchesAsync(LottoDraw draw);
}

public class PredictionMatchResult
{
    public int TotalPredictionsChecked { get; set; }
    public int MatchesFound { get; set; }
    public List<PredictionMatch> Matches { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class PredictionMatch
{
    public int PredictionId { get; set; }
    public int DrawId { get; set; }
    public int MainNumberMatches { get; set; }
    public bool PowerballMatch { get; set; }
    public bool BonusNumberMatch { get; set; }
    public DateTime MatchedAt { get; set; }
    public string MatchType { get; set; } = string.Empty; // e.g., "Full Match", "Partial Match", etc.
}