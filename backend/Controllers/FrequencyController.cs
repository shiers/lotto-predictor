using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FrequencyController : ControllerBase
{
    private readonly IFrequencyAnalysisService _frequencyService;
    private readonly IExportService _exportService;
    private readonly ILogger<FrequencyController> _logger;

    public FrequencyController(
        IFrequencyAnalysisService frequencyService,
        IExportService exportService,
        ILogger<FrequencyController> logger)
    {
        _frequencyService = frequencyService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Get frequency analysis for all lottery numbers
    /// </summary>
    /// <returns>Frequency data for all numbers (1-40)</returns>
    [HttpGet("numbers")]
    public async Task<ActionResult<IEnumerable<NumberFrequencyDto>>> GetNumberFrequencies()
    {
        try
        {
            _logger.LogInformation("Retrieving frequency analysis for all numbers");

            var frequencies = await _frequencyService.GetNumberFrequenciesAsync();

            _logger.LogInformation("Retrieved frequency data for {Count} numbers", frequencies.Count());

            return Ok(frequencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving number frequencies");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving number frequencies" });
        }
    }

    /// <summary>
    /// Get frequency analysis for specific number ranges
    /// </summary>
    /// <param name="request">Frequency analysis request with ranges</param>
    /// <returns>Frequency data for the specified ranges</returns>
    [HttpPost("ranges")]
    public async Task<ActionResult<IEnumerable<RangeFrequency>>> GetRangeFrequencies(
        [FromBody] FrequencyAnalysisRequest request)
    {
        try
        {
            if (request.Ranges == null || !request.Ranges.Any())
            {
                return BadRequest(new { error = "At least one range must be provided" });
            }

            // Validate ranges
            foreach (var range in request.Ranges)
            {
                if (range.StartNumber < 1 || range.StartNumber > 40 ||
                    range.EndNumber < 1 || range.EndNumber > 40)
                {
                    return BadRequest(new { error = "All range numbers must be between 1 and 40" });
                }

                if (range.StartNumber > range.EndNumber)
                {
                    return BadRequest(new { error = "Range start number must be less than or equal to end number" });
                }
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Analyzing frequency for {Count} ranges", request.Ranges.Count());

            var rangeFrequencies = await _frequencyService.GetRangeFrequenciesAsync(request.Ranges);

            _logger.LogInformation("Completed frequency analysis for {Count} ranges", rangeFrequencies.Count());

            return Ok(rangeFrequencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing range frequencies");
            return StatusCode(500, new { error = "Internal server error occurred while analyzing range frequencies" });
        }
    }

    /// <summary>
    /// Compare frequencies across different criteria
    /// </summary>
    /// <param name="request">Frequency comparison request</param>
    /// <returns>Comparative frequency analysis</returns>
    [HttpPost("compare")]
    public async Task<ActionResult<IEnumerable<NumberFrequencyDto>>> CompareFrequencies(
        [FromBody] FrequencyAnalysisRequest request)
    {
        try
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Performing frequency comparison analysis");

            var comparison = await _frequencyService.CompareFrequenciesAsync(request);

            _logger.LogInformation("Completed frequency comparison for {Count} numbers", comparison.Count());

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing frequency comparison");
            return StatusCode(500, new { error = "Internal server error occurred during frequency comparison" });
        }
    }

    /// <summary>
    /// Get hot and cold number analysis
    /// </summary>
    /// <param name="periodDays">Analysis period in days (default: 365)</param>
    /// <returns>Hot and cold number classification</returns>
    [HttpGet("hot-cold")]
    public async Task<ActionResult<IEnumerable<HotColdNumber>>> GetHotColdAnalysis(
        [FromQuery] int periodDays = 365)
    {
        try
        {
            if (periodDays < 1 || periodDays > 3650) // Max 10 years
            {
                return BadRequest(new { error = "Period days must be between 1 and 3650" });
            }

            _logger.LogInformation("Performing hot/cold analysis for {PeriodDays} days", periodDays);

            var hotColdNumbers = await _frequencyService.GetHotColdAnalysisAsync(periodDays);

            _logger.LogInformation("Completed hot/cold analysis for {Count} numbers over {PeriodDays} days", 
                hotColdNumbers.Count(), periodDays);

            return Ok(hotColdNumbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hot/cold analysis");
            return StatusCode(500, new { error = "Internal server error occurred during hot/cold analysis" });
        }
    }

    /// <summary>
    /// Get frequency statistics for a specific number
    /// </summary>
    /// <param name="number">Number to analyze (1-40)</param>
    /// <returns>Detailed frequency statistics for the number</returns>
    [HttpGet("number/{number:int}")]
    public async Task<ActionResult<NumberFrequencyDto>> GetNumberFrequency(
        [Range(1, 40)] int number)
    {
        try
        {
            if (number < 1 || number > 40)
            {
                return BadRequest(new { error = "Number must be between 1 and 40" });
            }

            _logger.LogInformation("Retrieving frequency statistics for number {Number}", number);

            var frequencies = await _frequencyService.GetNumberFrequenciesAsync();
            var numberFrequency = frequencies.FirstOrDefault(f => f.Number == number);

            if (numberFrequency == null)
            {
                _logger.LogWarning("No frequency data found for number {Number}", number);
                return NotFound(new { error = $"No frequency data found for number {number}" });
            }

            _logger.LogInformation("Retrieved frequency statistics for number {Number}: {Occurrences} occurrences", 
                number, numberFrequency.TotalOccurrences);

            return Ok(numberFrequency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving frequency for number {Number}", number);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving number frequency" });
        }
    }

    /// <summary>
    /// Get frequency trends over time periods
    /// </summary>
    /// <param name="numbers">Numbers to analyze (optional, analyzes all if not provided)</param>
    /// <param name="startDate">Start date for trend analysis</param>
    /// <param name="endDate">End date for trend analysis</param>
    /// <param name="intervalDays">Interval in days for trend buckets (default: 30)</param>
    /// <returns>Frequency trends over time</returns>
    [HttpGet("trends")]
    public async Task<ActionResult<object>> GetFrequencyTrends(
        [FromQuery] int[]? numbers = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int intervalDays = 30)
    {
        try
        {
            if (numbers != null && numbers.Any(n => n < 1 || n > 40))
            {
                return BadRequest(new { error = "All numbers must be between 1 and 40" });
            }

            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            if (intervalDays < 1 || intervalDays > 365)
            {
                return BadRequest(new { error = "Interval days must be between 1 and 365" });
            }

            _logger.LogInformation("Analyzing frequency trends for {NumberCount} numbers with {IntervalDays} day intervals", 
                numbers?.Length ?? 40, intervalDays);

            // For now, return a placeholder response since trend analysis would require more complex implementation
            var trendData = new
            {
                message = "Frequency trend analysis is not yet implemented",
                parameters = new
                {
                    numbers = numbers ?? Array.Empty<int>(),
                    startDate,
                    endDate,
                    intervalDays
                }
            };

            return Ok(trendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing frequency trends");
            return StatusCode(500, new { error = "Internal server error occurred during trend analysis" });
        }
    }

    /// <summary>
    /// Refresh frequency data cache
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshFrequencyData()
    {
        try
        {
            _logger.LogInformation("Refreshing frequency data cache");

            await _frequencyService.RefreshFrequencyDataAsync();

            _logger.LogInformation("Frequency data cache refreshed successfully");

            return Ok(new { message = "Frequency data refreshed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing frequency data");
            return StatusCode(500, new { error = "Internal server error occurred while refreshing frequency data" });
        }
    }

    /// <summary>
    /// Export frequency analysis results
    /// </summary>
    /// <param name="request">Frequency export request</param>
    /// <returns>Export result with download information</returns>
    [HttpPost("export")]
    public async Task<ActionResult<ExportResult>> ExportFrequencyData(
        [FromBody] FrequencyExportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                request.FileName = $"frequency_analysis_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            }

            // Validate the search criteria if provided
            if (request.SearchCriteria.Ranges?.Any() == true)
            {
                foreach (var range in request.SearchCriteria.Ranges)
                {
                    if (range.StartNumber < 1 || range.StartNumber > 40 ||
                        range.EndNumber < 1 || range.EndNumber > 40)
                    {
                        return BadRequest(new { error = "All range numbers must be between 1 and 40" });
                    }

                    if (range.StartNumber > range.EndNumber)
                    {
                        return BadRequest(new { error = "Range start number must be less than or equal to end number" });
                    }
                }
            }

            if (request.SearchCriteria.StartDate.HasValue && request.SearchCriteria.EndDate.HasValue && 
                request.SearchCriteria.StartDate > request.SearchCriteria.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Exporting frequency data in {Format} format with filename '{FileName}'", 
                request.Format, request.FileName);

            var exportResult = await _exportService.ExportFrequencyDataAsync(request);

            _logger.LogInformation("Frequency data export completed: {ExportId}", exportResult.ExportId);

            return Ok(exportResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting frequency data");
            return StatusCode(500, new { error = "Internal server error occurred during frequency data export" });
        }
    }

    /// <summary>
    /// Get export status
    /// </summary>
    /// <param name="exportId">Export ID to check status for</param>
    /// <returns>Export status information</returns>
    [HttpGet("export/{exportId}/status")]
    public async Task<ActionResult<ExportStatus>> GetExportStatus(string exportId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(exportId))
            {
                return BadRequest(new { error = "Export ID is required" });
            }

            _logger.LogInformation("Checking export status for {ExportId}", exportId);

            var status = await _exportService.GetExportStatusAsync(exportId);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving export status for {ExportId}", exportId);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving export status" });
        }
    }

    /// <summary>
    /// Get frequency statistics summary
    /// </summary>
    /// <returns>Overall frequency statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetFrequencyStatistics()
    {
        try
        {
            _logger.LogInformation("Retrieving frequency statistics summary");

            var frequencies = await _frequencyService.GetNumberFrequenciesAsync();

            var statistics = new
            {
                totalNumbers = frequencies.Count(),
                mostFrequent = frequencies.OrderByDescending(f => f.TotalOccurrences).Take(5),
                leastFrequent = frequencies.OrderBy(f => f.TotalOccurrences).Take(5),
                averageOccurrences = frequencies.Average(f => f.TotalOccurrences),
                totalOccurrences = frequencies.Sum(f => f.TotalOccurrences),
                hotNumbers = frequencies.Where(f => f.IsHot).Count(),
                coldNumbers = frequencies.Where(f => f.IsCold).Count(),
                lastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Retrieved frequency statistics: {TotalNumbers} numbers analyzed", 
                statistics.totalNumbers);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving frequency statistics");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving frequency statistics" });
        }
    }

    /// <summary>
    /// Get frequency distribution by position (1-6 for main numbers)
    /// </summary>
    /// <returns>Frequency distribution by draw position</returns>
    [HttpGet("position-distribution")]
    public async Task<ActionResult<object>> GetPositionDistribution()
    {
        try
        {
            _logger.LogInformation("Retrieving frequency distribution by position");

            // This would require additional service implementation to analyze by position
            // For now, return a placeholder response
            var positionData = new
            {
                message = "Position distribution analysis is not yet implemented",
                positions = new[] { 1, 2, 3, 4, 5, 6 },
                note = "This would show frequency distribution across draw positions"
            };

            return Ok(positionData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving position distribution");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving position distribution" });
        }
    }
}