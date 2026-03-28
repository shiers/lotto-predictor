using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Controllers;

/// <summary>
/// Controller for managing lottery draw data and historical information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LottoController : ControllerBase
{
    private readonly ILottoImportService _importService;
    private readonly ILottoQueryService _queryService;
    private readonly ILogger<LottoController> _logger;

    public LottoController(
        ILottoImportService importService,
        ILottoQueryService queryService,
        ILogger<LottoController> logger)
    {
        _importService = importService;
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a CSV file containing historical lottery draw data
    /// </summary>
    /// <param name="file">CSV file with lottery draw data</param>
    /// <returns>Import result with counts of added and skipped records</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<ActionResult<ImportResult>> UploadCsv(IFormFile file)
    {
        try
        {
            _logger.LogInformation("Received CSV upload request for file: {FileName}", file?.FileName);

            // Validate file presence
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided in upload request");
                return BadRequest(new { error = "No file provided" });
            }

            // Validate file type
            if (!IsValidCsvFile(file))
            {
                _logger.LogWarning("Invalid file type uploaded: {ContentType}, {FileName}", 
                    file.ContentType, file.FileName);
                return BadRequest(new { error = "Only CSV files are allowed" });
            }

            // Validate file size
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("File too large: {FileSize} bytes, max allowed: {MaxSize} bytes", 
                    file.Length, maxFileSize);
                return BadRequest(new { error = $"File size exceeds maximum limit of {maxFileSize / (1024 * 1024)}MB" });
            }

            _logger.LogInformation("Processing CSV file: {FileName}, Size: {FileSize} bytes", 
                file.FileName, file.Length);

            // Process the file
            using var stream = file.OpenReadStream();
            var result = await _importService.ImportAsync(stream);

            _logger.LogInformation("CSV import completed. Added: {Added}, Skipped: {Skipped}, Errors: {ErrorCount}", 
                result.RecordsAdded, result.RecordsSkipped, result.Errors.Count);

            // TODO: Add cache invalidation after fixing dependency injection issue
            // if (result.RecordsAdded > 0)
            // {
            //     _logger.LogInformation("Invalidating cache due to {RecordsAdded} new draws imported", result.RecordsAdded);
            //     await _cacheInvalidationService.InvalidateAllAsync();
            //     _logger.LogInformation("Cache invalidation completed");
            // }

            // Return appropriate status based on results
            if (result.HasErrors && result.RecordsAdded == 0)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV upload");
            return StatusCode(500, new { error = "Internal server error occurred during file processing" });
        }
    }

    /// <summary>
    /// Check if a specific lottery draw exists in the database
    /// </summary>
    /// <param name="draw">Draw number to check</param>
    /// <returns>Boolean indicating if the draw exists</returns>
    [HttpGet("exists/{draw:int}")]
    public async Task<ActionResult<bool>> DrawExists(int draw)
    {
        try
        {
            _logger.LogDebug("Checking existence of draw {DrawNumber}", draw);

            if (draw <= 0)
            {
                _logger.LogWarning("Invalid draw number provided: {DrawNumber}", draw);
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            var exists = await _queryService.DrawExistsAsync(draw);
            
            _logger.LogDebug("Draw {DrawNumber} exists: {Exists}", draw, exists);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if draw {DrawNumber} exists", draw);
            return StatusCode(500, new { error = "Internal server error occurred while checking draw existence" });
        }
    }

    /// <summary>
    /// Get the latest lottery draw information
    /// </summary>
    /// <returns>Latest lottery draw data or 404 if no draws exist</returns>
    [HttpGet("latest")]
    public async Task<ActionResult<LottoDrawDto>> GetLatestDraw()
    {
        try
        {
            _logger.LogDebug("Retrieving latest lottery draw");

            var latestDraw = await _queryService.GetLatestDrawAsync();

            if (latestDraw == null)
            {
                _logger.LogInformation("No lottery draws found in database");
                return NotFound(new { error = "No lottery draws found in the database" });
            }

            _logger.LogDebug("Latest draw retrieved: {DrawNumber} from {Date}", 
                latestDraw.Draw, latestDraw.Date);

            return Ok(latestDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest lottery draw");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving latest draw" });
        }
    }

    /// <summary>
    /// Get a specific lottery draw by draw number
    /// </summary>
    /// <param name="draw">Draw number to retrieve</param>
    /// <returns>Lottery draw data or 404 if not found</returns>
    [HttpGet("draw/{draw:int}")]
    public async Task<ActionResult<LottoDrawDto>> GetDraw(int draw)
    {
        try
        {
            _logger.LogDebug("Retrieving draw {DrawNumber}", draw);

            if (draw <= 0)
            {
                _logger.LogWarning("Invalid draw number provided: {DrawNumber}", draw);
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            var drawData = await _queryService.GetDrawAsync(draw);

            if (drawData == null)
            {
                _logger.LogInformation("Draw {DrawNumber} not found", draw);
                return NotFound(new { error = $"Draw {draw} not found in the database" });
            }

            _logger.LogDebug("Draw {DrawNumber} retrieved successfully", draw);
            return Ok(drawData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draw {DrawNumber}", draw);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving draw" });
        }
    }

    /// <summary>
    /// Get the previous lottery draw before the specified draw number
    /// </summary>
    /// <param name="draw">Current draw number</param>
    /// <returns>Previous lottery draw data or 404 if not found</returns>
    [HttpGet("draw/{draw:int}/previous")]
    public async Task<ActionResult<LottoDrawDto>> GetPreviousDraw(int draw)
    {
        try
        {
            _logger.LogDebug("Retrieving previous draw before {DrawNumber}", draw);

            if (draw <= 0)
            {
                _logger.LogWarning("Invalid draw number provided: {DrawNumber}", draw);
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            var previousDraw = await _queryService.GetPreviousDrawAsync(draw);

            if (previousDraw == null)
            {
                _logger.LogInformation("No previous draw found before {DrawNumber}", draw);
                return NotFound(new { error = $"No previous draw found before draw {draw}" });
            }

            _logger.LogDebug("Previous draw {PreviousDrawNumber} retrieved for draw {DrawNumber}", 
                previousDraw.Draw, draw);
            return Ok(previousDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving previous draw before {DrawNumber}", draw);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving previous draw" });
        }
    }

    /// <summary>
    /// Get the next lottery draw after the specified draw number
    /// </summary>
    /// <param name="draw">Current draw number</param>
    /// <returns>Next lottery draw data or 404 if not found</returns>
    [HttpGet("draw/{draw:int}/next")]
    public async Task<ActionResult<LottoDrawDto>> GetNextDraw(int draw)
    {
        try
        {
            _logger.LogDebug("Retrieving next draw after {DrawNumber}", draw);

            if (draw <= 0)
            {
                _logger.LogWarning("Invalid draw number provided: {DrawNumber}", draw);
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            var nextDraw = await _queryService.GetNextDrawAsync(draw);

            if (nextDraw == null)
            {
                _logger.LogInformation("No next draw found after {DrawNumber}", draw);
                return NotFound(new { error = $"No next draw found after draw {draw}" });
            }

            _logger.LogDebug("Next draw {NextDrawNumber} retrieved for draw {DrawNumber}", 
                nextDraw.Draw, draw);
            return Ok(nextDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving next draw after {DrawNumber}", draw);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving next draw" });
        }
    }

    /// <summary>
    /// Get paginated list of lottery draws
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 50, max: 100)</param>
    /// <param name="sortBy">Sort field: 'date' or 'draw' (default: 'date')</param>
    /// <param name="sortDirection">Sort direction: 'asc' or 'desc' (default: 'desc')</param>
    /// <returns>Paginated list of lottery draws</returns>
    [HttpGet("draws")]
    public async Task<ActionResult<PaginatedResponse<LottoDrawDto>>> GetDraws(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "date",
        [FromQuery] string sortDirection = "desc")
    {
        try
        {
            _logger.LogDebug("Retrieving draws page {Page}, size {PageSize}, sort by {SortBy} {SortDirection}", 
                page, pageSize, sortBy, sortDirection);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var sortDescending = sortDirection.ToLowerInvariant() == "desc";
            var result = await _queryService.GetDrawsAsync(page, pageSize, sortBy, sortDescending);

            _logger.LogDebug("Retrieved {ItemCount} draws out of {TotalItems} total", 
                result.ItemCount, result.TotalItems);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draws page {Page}", page);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving draws" });
        }
    }

    private static bool IsValidCsvFile(IFormFile file)
    {
        // Check file extension
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (extension != ".csv")
        {
            return false;
        }

        // Check content type (allow common CSV MIME types)
        var validContentTypes = new[]
        {
            "text/csv",
            "application/csv",
            "text/plain",
            "application/vnd.ms-excel"
        };

        return validContentTypes.Contains(file.ContentType?.ToLowerInvariant());
    }
}