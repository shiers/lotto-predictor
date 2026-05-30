using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BacktestController : ControllerBase
{
    private readonly IBacktestService _backtestService;
    private readonly ILogger<BacktestController> _logger;

    public BacktestController(IBacktestService backtestService, ILogger<BacktestController> logger)
    {
        _backtestService = backtestService;
        _logger = logger;
    }

    /// <summary>
    /// Run a backtest comparing the enhanced predictor against historical draws.
    /// </summary>
    /// <param name="drawCount">Number of recent draws to test against (default: 20, max: 100)</param>
    /// <param name="linesPerDraw">Lines per simulated ticket (default: 4)</param>
    /// <param name="enhanced">Use enhanced predictor (true) or random baseline (false)</param>
    [HttpGet("run")]
    public async Task<ActionResult<BacktestResult>> RunBacktest(
        [FromQuery] int drawCount = 20,
        [FromQuery] int linesPerDraw = 4,
        [FromQuery] bool enhanced = true)
    {
        try
        {
            if (drawCount < 1 || drawCount > 100)
                return BadRequest("drawCount must be between 1 and 100");

            if (linesPerDraw < 1 || linesPerDraw > 10)
                return BadRequest("linesPerDraw must be between 1 and 10");

            _logger.LogInformation("Running backtest: {DrawCount} draws, {Lines} lines, enhanced={Enhanced}",
                drawCount, linesPerDraw, enhanced);

            var request = new BacktestRequest
            {
                DrawCount = drawCount,
                LinesPerDraw = linesPerDraw,
                UseEnhancedPredictor = enhanced
            };

            var result = await _backtestService.RunBacktestAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest failed");
            return StatusCode(500, $"Backtest failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Run a comparison backtest: enhanced vs random baseline side by side.
    /// </summary>
    /// <param name="drawCount">Number of recent draws to test against</param>
    /// <param name="linesPerDraw">Lines per simulated ticket</param>
    [HttpGet("compare")]
    public async Task<ActionResult<object>> CompareStrategies(
        [FromQuery] int drawCount = 20,
        [FromQuery] int linesPerDraw = 4)
    {
        try
        {
            if (drawCount < 1 || drawCount > 100)
                return BadRequest("drawCount must be between 1 and 100");

            if (linesPerDraw < 1 || linesPerDraw > 10)
                return BadRequest("linesPerDraw must be between 1 and 10");

            _logger.LogInformation("Running comparison backtest: {DrawCount} draws, {Lines} lines", drawCount, linesPerDraw);

            var enhancedRequest = new BacktestRequest { DrawCount = drawCount, LinesPerDraw = linesPerDraw, UseEnhancedPredictor = true };
            var randomRequest = new BacktestRequest { DrawCount = drawCount, LinesPerDraw = linesPerDraw, UseEnhancedPredictor = false };

            var enhancedResult = await _backtestService.RunBacktestAsync(enhancedRequest);
            var randomResult = await _backtestService.RunBacktestAsync(randomRequest);

            return Ok(new
            {
                Enhanced = new
                {
                    enhancedResult.Strategy,
                    enhancedResult.MatchSummary,
                    enhancedResult.FinancialSummary,
                    enhancedResult.CoverageStats
                },
                Random = new
                {
                    randomResult.Strategy,
                    randomResult.MatchSummary,
                    randomResult.FinancialSummary,
                    randomResult.CoverageStats
                },
                Comparison = new
                {
                    AvgMatchImprovement = enhancedResult.MatchSummary.AverageMatchesPerLine - randomResult.MatchSummary.AverageMatchesPerLine,
                    ROIImprovement = enhancedResult.FinancialSummary.ReturnOnInvestment - randomResult.FinancialSummary.ReturnOnInvestment,
                    CoverageImprovement = enhancedResult.CoverageStats.AverageUniqueCoverage - randomResult.CoverageStats.AverageUniqueCoverage,
                    EnhancedWinningLines = enhancedResult.FinancialSummary.WinningLines,
                    RandomWinningLines = randomResult.FinancialSummary.WinningLines
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison backtest failed");
            return StatusCode(500, $"Comparison backtest failed: {ex.Message}");
        }
    }
}
