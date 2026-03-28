using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class BookmarkService : IBookmarkService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<BookmarkService> _logger;

    public BookmarkService(LottoDbContext context, ILogger<BookmarkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BookmarkDto> CreateBookmarkAsync(int drawNumber, string label, string userId)
    {
        if (drawNumber <= 0)
        {
            throw new ArgumentException("Draw number must be positive", nameof(drawNumber));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Label cannot be null or empty", nameof(label));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        // Check if draw exists
        var drawExists = await _context.LottoDraws.AnyAsync(d => d.Draw == drawNumber);
        if (!drawExists)
        {
            throw new ArgumentException($"Draw {drawNumber} does not exist", nameof(drawNumber));
        }

        // Check if bookmark already exists for this user and draw
        var existingBookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.DrawNumber == drawNumber);

        if (existingBookmark != null)
        {
            throw new InvalidOperationException($"Bookmark already exists for draw {drawNumber} and user {userId}");
        }

        var bookmark = new Bookmark
        {
            UserId = userId,
            DrawNumber = drawNumber,
            Label = label.Trim(),
            Description = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created bookmark {BookmarkId} for draw {DrawNumber} by user {UserId}", 
            bookmark.Id, drawNumber, userId);

        return new BookmarkDto
        {
            Id = bookmark.Id,
            DrawNumber = bookmark.DrawNumber,
            Label = bookmark.Label,
            Description = bookmark.Description,
            CreatedAt = bookmark.CreatedAt,
            UpdatedAt = bookmark.UpdatedAt
        };
    }

    public async Task<IEnumerable<BookmarkDto>> GetUserBookmarksAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var bookmarks = await _context.Bookmarks
            .Where(b => b.UserId == userId)
            .Include(b => b.Draw)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookmarkDto
            {
                Id = b.Id,
                DrawNumber = b.DrawNumber,
                Label = b.Label,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                DrawDate = b.Draw != null ? b.Draw.Date : DateTime.MinValue,
                WinningNumbers = b.Draw != null ? new int[] 
                { 
                    b.Draw.WinningNumber1, 
                    b.Draw.WinningNumber2, 
                    b.Draw.WinningNumber3, 
                    b.Draw.WinningNumber4, 
                    b.Draw.WinningNumber5, 
                    b.Draw.WinningNumber6 
                } : Array.Empty<int>()
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} bookmarks for user {UserId}", bookmarks.Count, userId);
        return bookmarks;
    }

    public async Task<bool> DeleteBookmarkAsync(int bookmarkId, string userId)
    {
        if (bookmarkId <= 0)
        {
            throw new ArgumentException("Bookmark ID must be positive", nameof(bookmarkId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var bookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId);

        if (bookmark == null)
        {
            _logger.LogWarning("Bookmark {BookmarkId} not found for user {UserId}", bookmarkId, userId);
            return false;
        }

        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted bookmark {BookmarkId} for draw {DrawNumber} by user {UserId}", 
            bookmarkId, bookmark.DrawNumber, userId);

        return true;
    }

    public async Task<bool> BookmarkExistsAsync(string userId, int drawNumber)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        if (drawNumber <= 0)
        {
            throw new ArgumentException("Draw number must be positive", nameof(drawNumber));
        }

        return await _context.Bookmarks
            .AnyAsync(b => b.UserId == userId && b.DrawNumber == drawNumber);
    }

    public async Task<BookmarkDto?> UpdateBookmarkAsync(int bookmarkId, string newLabel, string userId)
    {
        if (bookmarkId <= 0)
        {
            throw new ArgumentException("Bookmark ID must be positive", nameof(bookmarkId));
        }

        if (string.IsNullOrWhiteSpace(newLabel))
        {
            throw new ArgumentException("New label cannot be null or empty", nameof(newLabel));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var bookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId);

        if (bookmark == null)
        {
            _logger.LogWarning("Bookmark {BookmarkId} not found for user {UserId}", bookmarkId, userId);
            return null;
        }

        bookmark.Label = newLabel.Trim();
        bookmark.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated bookmark {BookmarkId} label to '{NewLabel}' for user {UserId}", 
            bookmarkId, newLabel, userId);

        return new BookmarkDto
        {
            Id = bookmark.Id,
            DrawNumber = bookmark.DrawNumber,
            Label = bookmark.Label,
            Description = bookmark.Description,
            CreatedAt = bookmark.CreatedAt,
            UpdatedAt = bookmark.UpdatedAt
        };
    }
}
