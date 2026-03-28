using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroqCloudTestController : ControllerBase
{
    private readonly ILogger<GroqCloudTestController> _logger;
    private readonly IEnumerable<IPredictionProvider> _providers;

    public GroqCloudTestController(
        ILogger<GroqCloudTestController> logger,
        IEnumerable<IPredictionProvider> providers)
    {
        _logger = logger;
        _providers = providers;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestGroqCloudProvider()
    {
        try
        {
            _logger.LogInformation("Testing GroqCloud provider...");

            // Find the GroqCloud provider
            var groqProvider = _providers.FirstOrDefault(p => p.ProviderName == "GroqCloud");
            
            if (groqProvider == null)
            {
                return BadRequest(new { error = "GroqCloud provider not found", providers = _providers.Select(p => p.ProviderName) });
            }

            _logger.LogInformation("Found GroqCloud provider, generating test prediction...");

            // Generate a single test prediction
            var predictions = await groqProvider.PredictAsync(1);
            var predictionList = predictions.ToList();

            _logger.LogInformation("Successfully generated {Count} predictions", predictionList.Count);

            return Ok(new 
            { 
                success = true,
                provider = groqProvider.ProviderName,
                priority = groqProvider.Priority,
                predictionsGenerated = predictionList.Count,
                predictions = predictionList.Select(p => new
                {
                    numbers = p.Numbers,
                    score = p.Score,
                    source = p.Source,
                    confidence = p.ConfidenceScore,
                    reasoning = p.ReasoningExplanation,
                    createdAt = p.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GroqCloud provider: {Message}", ex.Message);
            
            return BadRequest(new 
            { 
                error = ex.Message,
                type = ex.GetType().Name,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        try
        {
            var providerInfo = _providers.Select(p => new
            {
                name = p.ProviderName,
                priority = p.Priority,
                type = p.GetType().Name
            }).OrderBy(p => p.priority);

            return Ok(new
            {
                success = true,
                providers = providerInfo,
                count = _providers.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting providers: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        try
        {
            var groqProvider = _providers.FirstOrDefault(p => p.ProviderName == "GroqCloud") as GroqCloudPredictionProvider;
            
            return Ok(new
            {
                success = true,
                environment = new
                {
                    GROQCLOUD_API_KEY = Environment.GetEnvironmentVariable("GROQCLOUD_API_KEY")?.Substring(0, Math.Min(20, Environment.GetEnvironmentVariable("GROQCLOUD_API_KEY")?.Length ?? 0)) + "...",
                    GROQCLOUD_MODEL = Environment.GetEnvironmentVariable("GROQCLOUD_MODEL"),
                    GROQCLOUD_BASE_URL = Environment.GetEnvironmentVariable("GROQCLOUD_BASE_URL"),
                    GROQCLOUD_TEMPERATURE = Environment.GetEnvironmentVariable("GROQCLOUD_TEMPERATURE"),
                    GROQCLOUD_MAX_TOKENS = Environment.GetEnvironmentVariable("GROQCLOUD_MAX_TOKENS")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}