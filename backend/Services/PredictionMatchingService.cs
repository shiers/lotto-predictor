using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Services;

public class PredictionMatchingService : IPredictionMatchingService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<PredictionMatchingService> _logger;

    public PredictionMatchingService(LottoDbContext context, ILogger<PredictionMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PredictionMatchResult> CheckAndUpdateMatchesAsync(IEnumerable<LottoDraw> newDraws)
    {
        var result = new PredictionMatchResult();
        var drawsList = newDraws.ToList();

        if (!drawsList.Any())
        {
            _logger.LogInformation("No draws provided for prediction matching");
            return result;
        }

        _logger.LogInformation("Checking prediction matches for {Count} new draws", drawsList.Count);

        foreach (var draw in drawsList)
        {
            try
            {
                var drawResult = await CheckAndUpdateMatchesAsync(draw);
                result.TotalPredictionsChecked += drawResult.TotalPredictionsChecked;
                result.MatchesFound += drawResult.MatchesFound;
                result.Matches.AddRange(drawResult.Matches);
                result.Errors.AddRange(drawResult.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking matches for draw {DrawId}", draw.Draw);
                result.Errors.Add($"Error processing draw {draw.Draw}: {ex.Message}");
            }
        }

        _logger.LogInformation("Prediction matching completed. Checked: {Checked}, Matches: {Matches}", 
            result.TotalPredictionsChecked, result.MatchesFound);

        return result;
    }

    public async Task<PredictionMatchResult> CheckAndUpdateMatchesAsync(LottoDraw draw)
    {
        var result = new PredictionMatchResult();

        try
        {
            _logger.LogInformation("Checking prediction matches for draw {DrawId} dated {DrawDate}", 
                draw.Draw, draw.Date);

            // Get all unmatched predictions that were created before this draw
            var predictions = await _context.Predictions
                .Where(p => !p.IsMatched && p.CreatedAt <= draw.Date)
                .ToListAsync();

            result.TotalPredictionsChecked = predictions.Count;

            if (!predictions.Any())
            {
                _logger.LogInformation("No unmatched predictions found for draw {DrawId}", draw.Draw);
                return result;
            }

            _logger.LogDebug("Checking {Count} unmatched predictions for perfect matches (all 6 main numbers) against draw {DrawId}", 
                predictions.Count, draw.Draw);

            var drawNumbers = new[] 
            { 
                draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
                draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 
            }.OrderBy(n => n).ToArray();

            var matchedPredictions = new List<Prediction>();

            foreach (var prediction in predictions)
            {
                var predictionNumbers = prediction.GetNumbers().OrderBy(n => n).ToArray();
                var mainNumberMatches = CountMatches(predictionNumbers, drawNumbers);
                var powerballMatch = prediction.Powerball == draw.Powerball;
                var bonusNumberMatch = predictionNumbers.Contains(draw.BonusNumber);

                // Only mark as matched if all 6 main numbers match
                if (mainNumberMatches == 6)
                {
                    var matchType = DetermineMatchType(mainNumberMatches, powerballMatch, bonusNumberMatch);
                    // Mark prediction as matched
                    prediction.IsMatched = true;
                    prediction.MatchedDrawId = draw.Draw;
                    prediction.MatchedAt = DateTime.UtcNow;

                    matchedPredictions.Add(prediction);

                    var match = new PredictionMatch
                    {
                        PredictionId = prediction.Id,
                        DrawId = draw.Draw,
                        MainNumberMatches = mainNumberMatches,
                        PowerballMatch = powerballMatch,
                        BonusNumberMatch = bonusNumberMatch,
                        MatchedAt = prediction.MatchedAt.Value,
                        MatchType = matchType
                    };

                    result.Matches.Add(match);
                    result.MatchesFound++;

                    _logger.LogInformation("Perfect match found! Prediction {PredictionId} matches draw {DrawId}: All 6 main numbers match, Powerball: {PowerballMatch}, Type: {MatchType}",
                        prediction.Id, draw.Draw, powerballMatch, matchType);
                }
            }

            // Save changes if we found matches
            if (matchedPredictions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} predictions as matched for draw {DrawId}", 
                    matchedPredictions.Count, draw.Draw);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking prediction matches for draw {DrawId}", draw.Draw);
            result.Errors.Add($"Error processing draw {draw.Draw}: {ex.Message}");
        }

        return result;
    }

    private static int CountMatches(int[] predictionNumbers, int[] drawNumbers)
    {
        return predictionNumbers.Intersect(drawNumbers).Count();
    }

    private static string DetermineMatchType(int mainNumberMatches, bool powerballMatch, bool bonusNumberMatch)
    {
        // Since we only mark predictions as matched when all 6 main numbers match
        return powerballMatch switch
        {
            true => "Division 1 - Jackpot (6 + Powerball)",
            false => "Division 2 - Second Prize (6 numbers only)"
        };
    }
}