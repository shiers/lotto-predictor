using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictionService;
    private readonly LottoDbContext _context;
    private readonly ILogger<PredictionsController> _logger;
    
    public PredictionsController(
        IPredictionService predictionService,
        LottoDbContext context,
        ILogger<PredictionsController> logger)
    {
        _predictionService = predictionService;
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Manually update prediction scores against recent draws
    /// </summary>
    /// <param name="drawCount">Number of recent draws to process (default: 10)</param>
    /// <returns>Score update results</returns>
    [HttpPost("update-scores")]
    public async Task<ActionResult<object>> UpdatePredictionScores([FromQuery] int drawCount = 10)
    {
        try
        {
            if (drawCount <= 0 || drawCount > 50)
            {
                return BadRequest("Draw count must be between 1 and 50");
            }
            
            _logger.LogInformation("Manually updating prediction scores against {DrawCount} recent draws", drawCount);
            
            // Get recent draws
            var recentDraws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .Take(drawCount)
                .ToListAsync();
            
            if (!recentDraws.Any())
            {
                return BadRequest("No draws found in database");
            }
            
            // Get score update service and run updates
            var scoreUpdateService = HttpContext.RequestServices.GetRequiredService<IPredictionScoreUpdateService>();
            var result = await scoreUpdateService.UpdateScoresAfterDrawImportAsync(recentDraws);
            
            _logger.LogInformation("Score update completed: {Processed} predictions processed, {Updated} updated, {History} history records created",
                result.TotalPredictionsProcessed, result.PredictionsUpdated, result.ScoreHistoryRecordsCreated);
            
            return Ok(new
            {
                DrawsProcessed = recentDraws.Count,
                TotalPredictionsProcessed = result.TotalPredictionsProcessed,
                PredictionsUpdated = result.PredictionsUpdated,
                ScoreHistoryRecordsCreated = result.ScoreHistoryRecordsCreated,
                ProcessedDrawIds = result.ProcessedDrawIds,
                Errors = result.Errors,
                ProcessedAt = result.ProcessedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update prediction scores");
            return StatusCode(500, "Failed to update prediction scores");
        }
    }

    /// <summary>
    /// Generate predictions and update scores for recent draws
    /// </summary>
    /// <param name="count">Number of predictions to generate</param>
    /// <param name="updateExistingScores">Whether to update scores for existing predictions</param>
    /// <returns>Generation and update results</returns>
    [HttpPost("generate-and-update")]
    public async Task<ActionResult<object>> GenerateAndUpdatePredictions([FromQuery] int count = 5, [FromQuery] bool updateExistingScores = true)
    {
        try
        {
            if (count <= 0 || count > 20)
            {
                return BadRequest("Count must be between 1 and 20");
            }
            
            _logger.LogInformation("Generating {Count} predictions and updating scores", count);
            
            // Generate new predictions
            var predictions = await _predictionService.GeneratePredictionsAsync(count);
            var predictionsList = predictions.ToList();
            
            var result = new
            {
                PredictionsGenerated = predictionsList.Count,
                ScoreUpdateResults = new List<object>()
            };
            
            if (updateExistingScores && predictionsList.Any())
            {
                // Get recent draws to update scores against
                var recentDraws = await _context.LottoDraws
                    .OrderByDescending(d => d.Date)
                    .Take(10) // Update against last 10 draws
                    .ToListAsync();
                
                if (recentDraws.Any())
                {
                    _logger.LogInformation("Updating scores against {DrawCount} recent draws", recentDraws.Count);
                    
                    var scoreUpdateService = HttpContext.RequestServices.GetRequiredService<IPredictionScoreUpdateService>();
                    var scoreUpdateResult = await scoreUpdateService.UpdateScoresAfterDrawImportAsync(recentDraws);
                    
                    result.ScoreUpdateResults.Add(new
                    {
                        TotalPredictionsProcessed = scoreUpdateResult.TotalPredictionsProcessed,
                        PredictionsUpdated = scoreUpdateResult.PredictionsUpdated,
                        ScoreHistoryRecordsCreated = scoreUpdateResult.ScoreHistoryRecordsCreated,
                        Errors = scoreUpdateResult.Errors
                    });
                }
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate predictions and update scores");
            return StatusCode(500, "Failed to generate predictions and update scores");
        }
    }

    /// <summary>
    /// Generate new predictions using the prediction provider chain
    /// </summary>
    /// <param name="count">Number of predictions to generate (1-10)</param>
    /// <returns>Generated predictions</returns>
    [HttpGet("generate")]
    public async Task<ActionResult<IEnumerable<PredictionResult>>> GeneratePredictions([FromQuery] int count = 5)
    {
        try
        {
            if (count <= 0 || count > 10)
            {
                return BadRequest("Count must be between 1 and 10");
            }
            
            _logger.LogInformation("Generating {Count} predictions", count);
            
            var predictions = await _predictionService.GeneratePredictionsAsync(count);
            
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate predictions");
            return StatusCode(500, "Failed to generate predictions");
        }
    }
    
    /// <summary>
    /// Get stored predictions from the database
    /// </summary>
    /// <param name="count">Number of predictions to retrieve (1-50)</param>
    /// <returns>Stored predictions</returns>
    [HttpGet("stored")]
    public async Task<ActionResult<IEnumerable<PredictionResult>>> GetStoredPredictions([FromQuery] int count = 10)
    {
        try
        {
            if (count <= 0 || count > 50)
            {
                return BadRequest("Count must be between 1 and 50");
            }
            
            _logger.LogInformation("Retrieving {Count} stored predictions", count);
            
            var predictions = await _predictionService.GetStoredPredictionsAsync(count);
            
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve stored predictions");
            return StatusCode(500, "Failed to retrieve stored predictions");
        }
    }
    
    /// <summary>
    /// Get paginated stored predictions from the database
    /// </summary>
    /// <param name="request">Pagination parameters</param>
    /// <returns>Paginated stored predictions</returns>
    [HttpGet("stored/paginated")]
    public async Task<ActionResult<PaginatedResponse<PredictionResult>>> GetStoredPredictionsPaginated([FromQuery] PredictionPaginationRequest request)
    {
        try
        {
            _logger.LogInformation("Retrieving paginated stored predictions - Page: {Page}, PageSize: {PageSize}", 
                request.Page, request.PageSize);
            
            var paginatedPredictions = await _predictionService.GetStoredPredictionsPaginatedAsync(request);
            
            return Ok(paginatedPredictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated stored predictions");
            return StatusCode(500, "Failed to retrieve paginated stored predictions");
        }
    }
}