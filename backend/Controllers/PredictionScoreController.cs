using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionScoreController : ControllerBase
{
    private readonly IPredictionScoreUpdateService _scoreUpdateService;
    private readonly ILogger<PredictionScoreController> _logger;

    public PredictionScoreController(
        IPredictionScoreUpdateService scoreUpdateService,
        ILogger<PredictionScoreController> logger)
    {
        _scoreUpdateService = scoreUpdateService;
        _logger = logger;
    }

    /// <summary>
    /// Check if a prediction has score history
    /// </summary>
    /// <param name="predictionId">Prediction ID</param>
    /// <returns>True if prediction has score history</returns>
    [HttpGet("{predictionId:int}/has-history")]
    public async Task<ActionResult<bool>> HasScoreHistory(int predictionId)
    {
        try
        {
            if (predictionId <= 0)
            {
                return BadRequest("Prediction ID must be a positive integer");
            }

            var hasHistory = await _scoreUpdateService.HasScoreHistoryAsync(predictionId);
            return Ok(hasHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking score history for prediction {PredictionId}", predictionId);
            return StatusCode(500, "Failed to check score history");
        }
    }

    /// <summary>
    /// Get score history for a specific prediction
    /// </summary>
    /// <param name="predictionId">Prediction ID</param>
    /// <returns>Score history records</returns>
    [HttpGet("{predictionId:int}/history")]
    public async Task<ActionResult<IEnumerable<PredictionScoreHistoryDto>>> GetScoreHistory(int predictionId)
    {
        try
        {
            _logger.LogInformation("Retrieving score history for prediction {PredictionId}", predictionId);

            if (predictionId <= 0)
            {
                return BadRequest("Prediction ID must be a positive integer");
            }

            var history = await _scoreUpdateService.GetScoreHistoryAsync(predictionId);
            
            var historyDtos = history.Select(h => new PredictionScoreHistoryDto
            {
                Id = h.Id,
                PredictionId = h.PredictionId,
                OriginalScore = h.OriginalScore,
                UpdatedScore = h.UpdatedScore,
                UpdatedAt = h.UpdatedAt,
                UpdateReason = h.UpdateReason,
                TriggeringDrawId = h.TriggeringDrawId,
                ExactMatches = h.ExactMatches,
                PartialMatches = h.PartialMatches,
                ProximityScore = h.ProximityScore,
                OverallAccuracy = h.OverallAccuracy,
                TriggeringDrawDate = h.TriggeringDraw?.Date
            });

            return Ok(historyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving score history for prediction {PredictionId}", predictionId);
            return StatusCode(500, "Failed to retrieve score history");
        }
    }

    /// <summary>
    /// Get latest scores for multiple predictions
    /// </summary>
    /// <param name="predictionIds">Comma-separated prediction IDs</param>
    /// <returns>Latest score information for each prediction</returns>
    [HttpGet("latest-scores")]
    public async Task<ActionResult<Dictionary<int, PredictionScoreInfo>>> GetLatestScores([FromQuery] string predictionIds)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(predictionIds))
            {
                return BadRequest("Prediction IDs are required");
            }

            var ids = predictionIds.Split(',')
                .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : 0)
                .Where(id => id > 0)
                .ToList();

            if (!ids.Any())
            {
                return BadRequest("No valid prediction IDs provided");
            }

            _logger.LogInformation("Retrieving latest scores for {Count} predictions", ids.Count);

            var scores = await _scoreUpdateService.GetLatestScoresAsync(ids);

            return Ok(scores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest scores");
            return StatusCode(500, "Failed to retrieve latest scores");
        }
    }

    /// <summary>
    /// Manually trigger score updates for a specific draw
    /// </summary>
    /// <param name="drawId">Draw ID to process</param>
    /// <returns>Score update result</returns>
    [HttpPost("update-for-draw/{drawId:int}")]
    public async Task<ActionResult<PredictionScoreUpdateResult>> UpdateScoresForDraw(int drawId)
    {
        try
        {
            _logger.LogInformation("Manually triggering score updates for draw {DrawId}", drawId);

            if (drawId <= 0)
            {
                return BadRequest("Draw ID must be a positive integer");
            }

            // This would require getting the draw from the database first
            // For now, return a method not implemented response
            return StatusCode(501, "Manual score updates not yet implemented. Scores are automatically updated during import.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scores for draw {DrawId}", drawId);
            return StatusCode(500, "Failed to update scores");
        }
    }
}

// DTOs for the API responses
public class PredictionScoreHistoryDto
{
    public int Id { get; set; }
    public int PredictionId { get; set; }
    public double OriginalScore { get; set; }
    public double UpdatedScore { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdateReason { get; set; } = string.Empty;
    public int TriggeringDrawId { get; set; }
    public int ExactMatches { get; set; }
    public int PartialMatches { get; set; }
    public double ProximityScore { get; set; }
    public double OverallAccuracy { get; set; }
    public DateTime? TriggeringDrawDate { get; set; }
}