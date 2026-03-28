using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public class DrawNavigationService : IDrawNavigationService
{
    private readonly LottoDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DrawNavigationService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public DrawNavigationService(
        LottoDbContext context,
        IDistributedCache cache,
        ILogger<DrawNavigationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LottoDrawDto?> GetDrawByNumberAsync(int drawNumber)
    {
        if (drawNumber <= 0)
        {
            throw new ArgumentException("Draw number must be positive", nameof(drawNumber));
        }

        var cacheKey = $"draw_by_number_{drawNumber}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for draw {DrawNumber}", drawNumber);
            return JsonSerializer.Deserialize<LottoDrawDto>(cachedResult);
        }

        _logger.LogDebug("Cache miss for draw {DrawNumber}", drawNumber);

        var draw = await _context.LottoDraws
            .FirstOrDefaultAsync(d => d.Draw == drawNumber);

        if (draw != null)
        {
            var drawDto = MapToDto(draw);
            
            // Cache the result
            var serializedResult = JsonSerializer.Serialize(drawDto);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });

            _logger.LogInformation("Found draw {DrawNumber} dated {Date}", drawNumber, draw.Date);
            return drawDto;
        }
        else
        {
            _logger.LogWarning("Draw {DrawNumber} not found", drawNumber);
            return null;
        }
    }

    public async Task<LottoDrawDto?> GetDrawByDateAsync(DateTime date)
    {
        var cacheKey = $"draw_by_date_{date:yyyyMMdd}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for draw on date {Date}", date);
            return JsonSerializer.Deserialize<LottoDrawDto>(cachedResult);
        }

        _logger.LogDebug("Cache miss for draw on date {Date}", date);

        // Find the draw closest to the specified date
        var draw = await _context.LottoDraws
            .OrderBy(d => Math.Abs((d.Date - date).Ticks))
            .FirstOrDefaultAsync();

        if (draw != null)
        {
            var drawDto = MapToDto(draw);
            
            // Cache the result
            var serializedResult = JsonSerializer.Serialize(drawDto);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });

            _logger.LogInformation("Found closest draw {DrawNumber} on {DrawDate} for requested date {RequestedDate}", 
                draw.Draw, draw.Date, date);
            return drawDto;
        }
        else
        {
            _logger.LogWarning("No draws found for date {Date}", date);
            return null;
        }
    }

    public async Task<LottoDrawDto?> GetPreviousDrawAsync(int currentDrawNumber)
    {
        if (currentDrawNumber <= 0)
        {
            throw new ArgumentException("Current draw number must be positive", nameof(currentDrawNumber));
        }

        var cacheKey = $"previous_draw_{currentDrawNumber}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for previous draw of {CurrentDraw}", currentDrawNumber);
            return JsonSerializer.Deserialize<LottoDrawDto>(cachedResult);
        }

        _logger.LogDebug("Cache miss for previous draw of {CurrentDraw}", currentDrawNumber);

        var previousDraw = await _context.LottoDraws
            .Where(d => d.Draw < currentDrawNumber)
            .OrderByDescending(d => d.Draw)
            .FirstOrDefaultAsync();

        if (previousDraw != null)
        {
            // Cache the result
            var serializedResult = JsonSerializer.Serialize(previousDraw);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });

            _logger.LogInformation("Found previous draw {PreviousDraw} for current draw {CurrentDraw}", 
                previousDraw.Draw, currentDrawNumber);
        }
        else
        {
            _logger.LogInformation("No previous draw found for draw {CurrentDraw}", currentDrawNumber);
        }

        return previousDraw != null ? MapToDto(previousDraw) : null;
    }

    public async Task<LottoDrawDto?> GetNextDrawAsync(int currentDrawNumber)
    {
        if (currentDrawNumber <= 0)
        {
            throw new ArgumentException("Current draw number must be positive", nameof(currentDrawNumber));
        }

        var cacheKey = $"next_draw_{currentDrawNumber}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for next draw of {CurrentDraw}", currentDrawNumber);
            return JsonSerializer.Deserialize<LottoDrawDto>(cachedResult);
        }

        _logger.LogDebug("Cache miss for next draw of {CurrentDraw}", currentDrawNumber);

        var nextDraw = await _context.LottoDraws
            .Where(d => d.Draw > currentDrawNumber)
            .OrderBy(d => d.Draw)
            .FirstOrDefaultAsync();

        if (nextDraw != null)
        {
            // Cache the result
            var serializedResult = JsonSerializer.Serialize(nextDraw);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });

            _logger.LogInformation("Found next draw {NextDraw} for current draw {CurrentDraw}", 
                nextDraw.Draw, currentDrawNumber);
        }
        else
        {
            _logger.LogInformation("No next draw found for draw {CurrentDraw}", currentDrawNumber);
        }

        return nextDraw != null ? MapToDto(nextDraw) : null;
    }

    public async Task<NavigationContext> GetNavigationContextAsync(int drawNumber)
    {
        if (drawNumber <= 0)
        {
            throw new ArgumentException("Draw number must be positive", nameof(drawNumber));
        }

        var cacheKey = $"navigation_context_{drawNumber}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for navigation context of draw {DrawNumber}", drawNumber);
            return JsonSerializer.Deserialize<NavigationContext>(cachedResult) ?? new NavigationContext();
        }

        _logger.LogDebug("Cache miss for navigation context of draw {DrawNumber}", drawNumber);

        var currentDraw = await GetDrawByNumberAsync(drawNumber);
        if (currentDraw == null)
        {
            throw new ArgumentException($"Draw {drawNumber} not found", nameof(drawNumber));
        }

        var totalDraws = await _context.LottoDraws.CountAsync();
        var position = await _context.LottoDraws
            .Where(d => d.Draw <= drawNumber)
            .CountAsync();

        var hasPrevious = await _context.LottoDraws
            .AnyAsync(d => d.Draw < drawNumber);

        var hasNext = await _context.LottoDraws
            .AnyAsync(d => d.Draw > drawNumber);

        var earliestDate = await _context.LottoDraws.MinAsync(d => d.Date);
        var latestDate = await _context.LottoDraws.MaxAsync(d => d.Date);

        var missingDrawNumbers = await FindMissingDrawNumbers(drawNumber);

        var context = new NavigationContext
        {
            CurrentDraw = currentDraw,
            CurrentPosition = position,
            TotalDraws = totalDraws,
            HasPrevious = hasPrevious,
            HasNext = hasNext,
            EarliestDate = earliestDate,
            LatestDate = latestDate,
            MissingDrawNumbers = missingDrawNumbers
        };

        // Cache the result
        var serializedResult = JsonSerializer.Serialize(context);
        await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        });

        _logger.LogInformation("Generated navigation context for draw {DrawNumber}: position {Position} of {Total}", 
            drawNumber, position, totalDraws);

        return context;
    }

    public async Task<IEnumerable<LottoDrawDto>> GetDrawsInRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date cannot be after end date", nameof(startDate));
        }

        var cacheKey = $"draws_range_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for draws in range {StartDate} to {EndDate}", startDate, endDate);
            return JsonSerializer.Deserialize<IEnumerable<LottoDrawDto>>(cachedResult) ?? Enumerable.Empty<LottoDrawDto>();
        }

        _logger.LogDebug("Cache miss for draws in range {StartDate} to {EndDate}", startDate, endDate);

        var draws = await _context.LottoDraws
            .Where(d => d.Date >= startDate && d.Date <= endDate)
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Cache the result
        var serializedResult = JsonSerializer.Serialize(draws);
        await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        });

        _logger.LogInformation("Found {Count} draws in range {StartDate} to {EndDate}", 
            draws.Count, startDate, endDate);

        return draws.Select(MapToDto);
    }

    public async Task<LottoDrawDto?> JumpToDrawAsync(JumpToRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.DrawNumber.HasValue)
        {
            return await GetDrawByNumberAsync(request.DrawNumber.Value);
        }

        if (request.Date.HasValue)
        {
            return await GetDrawByDateAsync(request.Date.Value);
        }

        throw new ArgumentException("Either DrawNumber or Date must be specified", nameof(request));
    }

    private async Task<int[]> FindMissingDrawNumbers(int currentDrawNumber)
    {
        // Find gaps in the sequence around the current draw
        var minDraw = Math.Max(1, currentDrawNumber - 50);
        var maxDraw = currentDrawNumber + 50;

        var existingDraws = await _context.LottoDraws
            .Where(d => d.Draw >= minDraw && d.Draw <= maxDraw)
            .Select(d => d.Draw)
            .ToListAsync();

        var expectedDraws = Enumerable.Range(minDraw, maxDraw - minDraw + 1);
        var missingDraws = expectedDraws.Except(existingDraws).ToArray();

        return missingDraws;
    }

    private LottoDrawDto MapToDto(LottoDraw draw)
    {
        return new LottoDrawDto
        {
            Draw = draw.Draw,
            Date = draw.Date,
            WinningNumbers = new[] 
            { 
                draw.WinningNumber1, 
                draw.WinningNumber2, 
                draw.WinningNumber3, 
                draw.WinningNumber4, 
                draw.WinningNumber5, 
                draw.WinningNumber6 
            },
            BonusNumber = draw.BonusNumber,
            Powerball = draw.Powerball,
            Division1Prize = draw.Division1Prize,
            Division2Prize = draw.Division2Prize,
            Division3Prize = draw.Division3Prize,
            Division4Prize = draw.Division4Prize,
            Division5Prize = draw.Division5Prize,
            Division6Prize = draw.Division6Prize,
            Division7Prize = draw.Division7Prize
        };
    }
}