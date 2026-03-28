using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface ILottoQueryService
{
    Task<bool> DrawExistsAsync(int drawNumber);
    Task<LottoDrawDto?> GetLatestDrawAsync();
    Task<LottoDrawDto?> GetDrawAsync(int drawNumber);
    Task<LottoDrawDto?> GetPreviousDrawAsync(int currentDrawNumber);
    Task<LottoDrawDto?> GetNextDrawAsync(int currentDrawNumber);
    Task<PaginatedResponse<LottoDrawDto>> GetDrawsAsync(int page = 1, int pageSize = 50, string sortBy = "date", bool sortDescending = true);
}

public class LottoQueryService : ILottoQueryService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<LottoQueryService> _logger;

    public LottoQueryService(LottoDbContext context, ILogger<LottoQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> DrawExistsAsync(int drawNumber)
    {
        try
        {
            _logger.LogDebug("Checking if draw {DrawNumber} exists", drawNumber);
            
            if (drawNumber <= 0)
            {
                _logger.LogWarning("Invalid draw number {DrawNumber} provided", drawNumber);
                return false;
            }

            var exists = await _context.LottoDraws
                .AnyAsync(d => d.Draw == drawNumber);

            _logger.LogDebug("Draw {DrawNumber} exists: {Exists}", drawNumber, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if draw {DrawNumber} exists", drawNumber);
            throw;
        }
    }

    public async Task<LottoDrawDto?> GetLatestDrawAsync()
    {
        try
        {
            _logger.LogDebug("Retrieving latest lottery draw");

            var latestDraw = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .ThenByDescending(d => d.Draw)
                .FirstOrDefaultAsync();

            if (latestDraw == null)
            {
                _logger.LogInformation("No lottery draws found in database");
                return null;
            }

            _logger.LogDebug("Latest draw found: {DrawNumber} from {Date}", 
                latestDraw.Draw, latestDraw.Date);

            return LottoDrawDto.FromEntity(latestDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest lottery draw");
            throw;
        }
    }

    public async Task<LottoDrawDto?> GetDrawAsync(int drawNumber)
    {
        try
        {
            _logger.LogDebug("Retrieving draw {DrawNumber}", drawNumber);

            if (drawNumber <= 0)
            {
                _logger.LogWarning("Invalid draw number {DrawNumber} provided", drawNumber);
                return null;
            }

            var draw = await _context.LottoDraws
                .FirstOrDefaultAsync(d => d.Draw == drawNumber);

            if (draw == null)
            {
                _logger.LogInformation("Draw {DrawNumber} not found", drawNumber);
                return null;
            }

            return LottoDrawDto.FromEntity(draw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draw {DrawNumber}", drawNumber);
            throw;
        }
    }

    public async Task<LottoDrawDto?> GetPreviousDrawAsync(int currentDrawNumber)
    {
        try
        {
            _logger.LogDebug("Retrieving previous draw before {DrawNumber}", currentDrawNumber);

            var previousDraw = await _context.LottoDraws
                .Where(d => d.Draw < currentDrawNumber)
                .OrderByDescending(d => d.Draw)
                .FirstOrDefaultAsync();

            if (previousDraw == null)
            {
                _logger.LogDebug("No previous draw found before {DrawNumber}", currentDrawNumber);
                return null;
            }

            return LottoDrawDto.FromEntity(previousDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving previous draw before {DrawNumber}", currentDrawNumber);
            throw;
        }
    }

    public async Task<LottoDrawDto?> GetNextDrawAsync(int currentDrawNumber)
    {
        try
        {
            _logger.LogDebug("Retrieving next draw after {DrawNumber}", currentDrawNumber);

            var nextDraw = await _context.LottoDraws
                .Where(d => d.Draw > currentDrawNumber)
                .OrderBy(d => d.Draw)
                .FirstOrDefaultAsync();

            if (nextDraw == null)
            {
                _logger.LogDebug("No next draw found after {DrawNumber}", currentDrawNumber);
                return null;
            }

            return LottoDrawDto.FromEntity(nextDraw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving next draw after {DrawNumber}", currentDrawNumber);
            throw;
        }
    }

    public async Task<PaginatedResponse<LottoDrawDto>> GetDrawsAsync(int page = 1, int pageSize = 50, string sortBy = "date", bool sortDescending = true)
    {
        try
        {
            _logger.LogDebug("Retrieving draws page {Page}, size {PageSize}, sort by {SortBy} {SortDirection}", 
                page, pageSize, sortBy, sortDescending ? "DESC" : "ASC");

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var query = _context.LottoDraws.AsQueryable();

            // Apply sorting
            query = sortBy.ToLowerInvariant() switch
            {
                "draw" => sortDescending ? query.OrderByDescending(d => d.Draw) : query.OrderBy(d => d.Draw),
                "date" => sortDescending ? query.OrderByDescending(d => d.Date).ThenByDescending(d => d.Draw) : query.OrderBy(d => d.Date).ThenBy(d => d.Draw),
                _ => query.OrderByDescending(d => d.Date).ThenByDescending(d => d.Draw)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var draws = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var drawDtos = draws.Select(LottoDrawDto.FromEntity).ToList();

            return new PaginatedResponse<LottoDrawDto>
            {
                Items = drawDtos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draws page {Page}", page);
            throw;
        }
    }
}