using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NavigationController : ControllerBase
{
    private readonly IDrawNavigationService _navigationService;
    private readonly IBookmarkService _bookmarkService;
    private readonly ILogger<NavigationController> _logger;

    public NavigationController(
        IDrawNavigationService navigationService,
        IBookmarkService bookmarkService,
        ILogger<NavigationController> logger)
    {
        _navigationService = navigationService;
        _bookmarkService = bookmarkService;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific lottery draw by draw number
    /// </summary>
    /// <param name="drawNumber">Draw number to retrieve</param>
    /// <returns>Lottery draw details</returns>
    [HttpGet("draw/{drawNumber:int}")]
    public async Task<ActionResult<LottoDrawDto>> GetDrawByNumber(int drawNumber)
    {
        try
        {
            if (drawNumber <= 0)
            {
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            _logger.LogInformation("Retrieving draw {DrawNumber}", drawNumber);

            var draw = await _navigationService.GetDrawByNumberAsync(drawNumber);

            if (draw == null)
            {
                _logger.LogWarning("Draw {DrawNumber} not found", drawNumber);
                return NotFound(new { error = $"Draw {drawNumber} not found" });
            }

            _logger.LogInformation("Retrieved draw {DrawNumber} from {Date}", drawNumber, draw.Date);

            return Ok(draw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draw {DrawNumber}", drawNumber);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving draw" });
        }
    }

    /// <summary>
    /// Get a lottery draw by date
    /// </summary>
    /// <param name="date">Date to search for (YYYY-MM-DD format)</param>
    /// <returns>Lottery draw closest to the specified date</returns>
    [HttpGet("draw/by-date/{date:datetime}")]
    public async Task<ActionResult<LottoDrawDto>> GetDrawByDate(DateTime date)
    {
        try
        {
            _logger.LogInformation("Retrieving draw for date {Date}", date.ToString("yyyy-MM-dd"));

            var draw = await _navigationService.GetDrawByDateAsync(date);

            if (draw == null)
            {
                _logger.LogWarning("No draw found for date {Date}", date.ToString("yyyy-MM-dd"));
                return NotFound(new { error = $"No draw found for date {date:yyyy-MM-dd}" });
            }

            _logger.LogInformation("Retrieved draw {DrawNumber} for date {Date}", draw.Draw, date.ToString("yyyy-MM-dd"));

            return Ok(draw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draw for date {Date}", date.ToString("yyyy-MM-dd"));
            return StatusCode(500, new { error = "Internal server error occurred while retrieving draw by date" });
        }
    }

    /// <summary>
    /// Get the previous draw relative to the current draw number
    /// </summary>
    /// <param name="currentDrawNumber">Current draw number</param>
    /// <returns>Previous lottery draw</returns>
    [HttpGet("draw/{currentDrawNumber:int}/previous")]
    public async Task<ActionResult<LottoDrawDto>> GetPreviousDraw(int currentDrawNumber)
    {
        try
        {
            if (currentDrawNumber <= 0)
            {
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            _logger.LogInformation("Retrieving previous draw for current draw {DrawNumber}", currentDrawNumber);

            var previousDraw = await _navigationService.GetPreviousDrawAsync(currentDrawNumber);

            if (previousDraw == null)
            {
                _logger.LogWarning("No previous draw found for draw {DrawNumber}", currentDrawNumber);
                return NotFound(new { error = $"No previous draw found for draw {currentDrawNumber}" });
            }

            _logger.LogInformation("Retrieved previous draw {PreviousDrawNumber} for current draw {DrawNumber}", 
                previousDraw.Draw, currentDrawNumber);

            return Ok(previousDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving previous draw for draw {DrawNumber}", currentDrawNumber);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving previous draw" });
        }
    }

    /// <summary>
    /// Get the next draw relative to the current draw number
    /// </summary>
    /// <param name="currentDrawNumber">Current draw number</param>
    /// <returns>Next lottery draw</returns>
    [HttpGet("draw/{currentDrawNumber:int}/next")]
    public async Task<ActionResult<LottoDrawDto>> GetNextDraw(int currentDrawNumber)
    {
        try
        {
            if (currentDrawNumber <= 0)
            {
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            _logger.LogInformation("Retrieving next draw for current draw {DrawNumber}", currentDrawNumber);

            var nextDraw = await _navigationService.GetNextDrawAsync(currentDrawNumber);

            if (nextDraw == null)
            {
                _logger.LogWarning("No next draw found for draw {DrawNumber}", currentDrawNumber);
                return NotFound(new { error = $"No next draw found for draw {currentDrawNumber}" });
            }

            _logger.LogInformation("Retrieved next draw {NextDrawNumber} for current draw {DrawNumber}", 
                nextDraw.Draw, currentDrawNumber);

            return Ok(nextDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving next draw for draw {DrawNumber}", currentDrawNumber);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving next draw" });
        }
    }

    /// <summary>
    /// Get navigation context for a specific draw
    /// </summary>
    /// <param name="drawNumber">Draw number to get context for</param>
    /// <returns>Navigation context with position and boundary information</returns>
    [HttpGet("context/{drawNumber:int}")]
    public async Task<ActionResult<NavigationContext>> GetNavigationContext(int drawNumber)
    {
        try
        {
            if (drawNumber <= 0)
            {
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            _logger.LogInformation("Retrieving navigation context for draw {DrawNumber}", drawNumber);

            var context = await _navigationService.GetNavigationContextAsync(drawNumber);

            _logger.LogInformation("Retrieved navigation context for draw {DrawNumber}: position {Position} of {Total}", 
                drawNumber, context.CurrentPosition, context.TotalDraws);

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving navigation context for draw {DrawNumber}", drawNumber);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving navigation context" });
        }
    }

    /// <summary>
    /// Get draws within a date range
    /// </summary>
    /// <param name="startDate">Start date (YYYY-MM-DD format)</param>
    /// <param name="endDate">End date (YYYY-MM-DD format)</param>
    /// <returns>List of draws within the specified date range</returns>
    [HttpGet("draws/range")]
    public async Task<ActionResult<IEnumerable<LottoDrawDto>>> GetDrawsInRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new { error = "Start date must be before or equal to end date" });
            }

            var dateRange = endDate - startDate;
            if (dateRange.TotalDays > 365)
            {
                return BadRequest(new { error = "Date range cannot exceed 365 days" });
            }

            _logger.LogInformation("Retrieving draws in range {StartDate} to {EndDate}", 
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var draws = await _navigationService.GetDrawsInRangeAsync(startDate, endDate);

            _logger.LogInformation("Retrieved {Count} draws in range {StartDate} to {EndDate}", 
                draws.Count(), startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            return Ok(draws);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draws in range {StartDate} to {EndDate}", 
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            return StatusCode(500, new { error = "Internal server error occurred while retrieving draws in range" });
        }
    }

    /// <summary>
    /// Jump to a specific draw by number or date
    /// </summary>
    /// <param name="request">Jump-to request with draw number or date</param>
    /// <returns>Target draw or closest available draw</returns>
    [HttpPost("jump-to")]
    public async Task<ActionResult<LottoDrawDto>> JumpToDraw([FromBody] JumpToRequest request)
    {
        try
        {
            if (!request.DrawNumber.HasValue && !request.Date.HasValue)
            {
                return BadRequest(new { error = "Either draw number or date must be provided" });
            }

            if (request.DrawNumber.HasValue && request.DrawNumber <= 0)
            {
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            _logger.LogInformation("Jumping to draw: DrawNumber={DrawNumber}, Date={Date}, FindClosest={FindClosest}", 
                request.DrawNumber, request.Date?.ToString("yyyy-MM-dd"), request.FindClosest);

            var draw = await _navigationService.JumpToDrawAsync(request);

            if (draw == null)
            {
                var target = request.DrawNumber?.ToString() ?? request.Date?.ToString("yyyy-MM-dd");
                _logger.LogWarning("No draw found for jump-to target: {Target}", target);
                return NotFound(new { error = $"No draw found for the specified criteria" });
            }

            _logger.LogInformation("Jump-to successful: found draw {DrawNumber} from {Date}", 
                draw.Draw, draw.Date.ToString("yyyy-MM-dd"));

            return Ok(draw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during jump-to operation");
            return StatusCode(500, new { error = "Internal server error occurred during jump-to operation" });
        }
    }

    /// <summary>
    /// Navigate using a navigation request
    /// </summary>
    /// <param name="request">Navigation request with direction and optional parameters</param>
    /// <returns>Target draw based on navigation direction</returns>
    [HttpPost("navigate")]
    public async Task<ActionResult<LottoDrawDto>> Navigate([FromBody] NavigationRequest request)
    {
        try
        {
            LottoDrawDto? draw = null;

            switch (request.Direction)
            {
                case NavigationDirection.Previous:
                    if (!request.DrawNumber.HasValue)
                    {
                        return BadRequest(new { error = "Draw number is required for Previous navigation" });
                    }
                    draw = await _navigationService.GetPreviousDrawAsync(request.DrawNumber.Value);
                    break;

                case NavigationDirection.Next:
                    if (!request.DrawNumber.HasValue)
                    {
                        return BadRequest(new { error = "Draw number is required for Next navigation" });
                    }
                    draw = await _navigationService.GetNextDrawAsync(request.DrawNumber.Value);
                    break;

                case NavigationDirection.Specific:
                    if (request.DrawNumber.HasValue)
                    {
                        draw = await _navigationService.GetDrawByNumberAsync(request.DrawNumber.Value);
                    }
                    else if (request.Date.HasValue)
                    {
                        draw = await _navigationService.GetDrawByDateAsync(request.Date.Value);
                    }
                    else
                    {
                        return BadRequest(new { error = "Draw number or date is required for Specific navigation" });
                    }
                    break;

                case NavigationDirection.First:
                case NavigationDirection.Last:
                    // These would require additional service methods to get first/last draws
                    return BadRequest(new { error = $"Navigation direction {request.Direction} is not yet implemented" });

                default:
                    return BadRequest(new { error = "Invalid navigation direction" });
            }

            if (draw == null)
            {
                return NotFound(new { error = $"No draw found for {request.Direction} navigation" });
            }

            _logger.LogInformation("Navigation successful: {Direction} to draw {DrawNumber}", 
                request.Direction, draw.Draw);

            return Ok(draw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during navigation with direction {Direction}", request.Direction);
            return StatusCode(500, new { error = "Internal server error occurred during navigation" });
        }
    }

    /// <summary>
    /// Create a bookmark for a specific draw
    /// </summary>
    /// <param name="request">Bookmark creation request</param>
    /// <returns>Created bookmark</returns>
    [HttpPost("bookmarks")]
    public async Task<ActionResult<BookmarkDto>> CreateBookmark([FromBody] CreateBookmarkRequest request)
    {
        try
        {
            if (request.DrawNumber <= 0)
            {
                return BadRequest(new { error = "Draw number must be a positive integer" });
            }

            if (string.IsNullOrWhiteSpace(request.Label))
            {
                return BadRequest(new { error = "Bookmark label is required" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Creating bookmark for draw {DrawNumber} with label '{Label}' for user {UserId}", 
                request.DrawNumber, request.Label, userId);

            var bookmark = await _bookmarkService.CreateBookmarkAsync(
                request.DrawNumber, 
                request.Label, 
                userId);

            _logger.LogInformation("Bookmark created with ID {BookmarkId} for draw {DrawNumber}", 
                bookmark.Id, request.DrawNumber);

            return CreatedAtAction(
                nameof(GetBookmark), 
                new { bookmarkId = bookmark.Id }, 
                bookmark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bookmark for draw {DrawNumber}", request.DrawNumber);
            return StatusCode(500, new { error = "Internal server error occurred while creating bookmark" });
        }
    }

    /// <summary>
    /// Get user's bookmarks
    /// </summary>
    /// <returns>List of user's bookmarks</returns>
    [HttpGet("bookmarks")]
    public async Task<ActionResult<IEnumerable<BookmarkDto>>> GetBookmarks()
    {
        try
        {
            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Retrieving bookmarks for user {UserId}", userId);

            var bookmarks = await _bookmarkService.GetUserBookmarksAsync(userId);

            _logger.LogInformation("Retrieved {Count} bookmarks for user {UserId}", bookmarks.Count(), userId);

            return Ok(bookmarks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookmarks");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving bookmarks" });
        }
    }

    /// <summary>
    /// Get a specific bookmark
    /// </summary>
    /// <param name="bookmarkId">Bookmark ID</param>
    /// <returns>Bookmark details</returns>
    [HttpGet("bookmarks/{bookmarkId:int}")]
    public async Task<ActionResult<BookmarkDto>> GetBookmark(int bookmarkId)
    {
        try
        {
            if (bookmarkId <= 0)
            {
                return BadRequest(new { error = "Bookmark ID must be a positive integer" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Retrieving bookmark {BookmarkId} for user {UserId}", bookmarkId, userId);

            var bookmarks = await _bookmarkService.GetUserBookmarksAsync(userId);
            var bookmark = bookmarks.FirstOrDefault(b => b.Id == bookmarkId);

            if (bookmark == null)
            {
                _logger.LogWarning("Bookmark {BookmarkId} not found for user {UserId}", bookmarkId, userId);
                return NotFound(new { error = "Bookmark not found" });
            }

            return Ok(bookmark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookmark {BookmarkId}", bookmarkId);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving bookmark" });
        }
    }

    /// <summary>
    /// Update a bookmark
    /// </summary>
    /// <param name="bookmarkId">Bookmark ID to update</param>
    /// <param name="request">Updated bookmark data</param>
    /// <returns>Updated bookmark</returns>
    [HttpPut("bookmarks/{bookmarkId:int}")]
    public async Task<ActionResult<BookmarkDto>> UpdateBookmark(
        int bookmarkId, 
        [FromBody] UpdateBookmarkRequest request)
    {
        try
        {
            if (bookmarkId <= 0)
            {
                return BadRequest(new { error = "Bookmark ID must be a positive integer" });
            }

            if (string.IsNullOrWhiteSpace(request.Label))
            {
                return BadRequest(new { error = "Bookmark label is required" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Updating bookmark {BookmarkId} for user {UserId}", bookmarkId, userId);

            var bookmark = await _bookmarkService.UpdateBookmarkAsync(bookmarkId, request.Label, userId);

            _logger.LogInformation("Bookmark {BookmarkId} updated successfully", bookmarkId);

            return Ok(bookmark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bookmark {BookmarkId}", bookmarkId);
            return StatusCode(500, new { error = "Internal server error occurred while updating bookmark" });
        }
    }

    /// <summary>
    /// Delete a bookmark
    /// </summary>
    /// <param name="bookmarkId">Bookmark ID to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("bookmarks/{bookmarkId:int}")]
    public async Task<ActionResult> DeleteBookmark(int bookmarkId)
    {
        try
        {
            if (bookmarkId <= 0)
            {
                return BadRequest(new { error = "Bookmark ID must be a positive integer" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Deleting bookmark {BookmarkId} for user {UserId}", bookmarkId, userId);

            var deleted = await _bookmarkService.DeleteBookmarkAsync(bookmarkId, userId);

            if (!deleted)
            {
                _logger.LogWarning("Bookmark {BookmarkId} not found for deletion by user {UserId}", 
                    bookmarkId, userId);
                return NotFound(new { error = "Bookmark not found" });
            }

            _logger.LogInformation("Bookmark {BookmarkId} deleted successfully", bookmarkId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bookmark {BookmarkId}", bookmarkId);
            return StatusCode(500, new { error = "Internal server error occurred while deleting bookmark" });
        }
    }
}