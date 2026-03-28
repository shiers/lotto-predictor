using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;

namespace PredictLottoNZ.Controllers;

/// <summary>
/// Health check controller for monitoring system status and connectivity
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly LottoDbContext _context;
    private readonly ILogger<HealthController> _logger;
    
    public HealthController(LottoDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Performs a comprehensive health check of the system
    /// </summary>
    /// <returns>System health status including database connectivity and statistics</returns>
    /// <response code="200">System is healthy and operational</response>
    /// <response code="503">System is unhealthy or experiencing issues</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(503, new { status = "Unhealthy", message = "Cannot connect to database" });
            }
            
            // Get basic statistics
            var stats = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                database = new
                {
                    connected = true,
                    lottoDraws = await _context.LottoDraws.CountAsync(),
                    numberCombinations = await _context.NumberCombinations.CountAsync(),
                    predictions = await _context.Predictions.CountAsync()
                }
            };
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new { status = "Unhealthy", message = ex.Message });
        }
    }
}