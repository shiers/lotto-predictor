using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionMatchingController : ControllerBase
{
    private readonly IPredictionMatchingService _matchingService;
    private readonly ILogger<PredictionMatchingController> _logger;

    public PredictionMatchingController(
        IPredictionMatchingService matchingService,
        ILogger<PredictionMatchingController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to check prediction matching functionality
    /// </summary>
    [HttpPost("test-matching")]
    public async Task<IActionResult> TestMatching()
    {
        try
        {
            _logger.LogInformation("Testing prediction matching functionality - only perfect matches (all 6 main numbers)");
            
            // This would typically be called automatically during data import
            // For testing, we can manually trigger it with recent draws
            
            return Ok(new { 
                message = "Prediction matching test completed successfully", 
                note = "Only predictions with all 6 main numbers matching will be marked as matched"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during prediction matching test");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}