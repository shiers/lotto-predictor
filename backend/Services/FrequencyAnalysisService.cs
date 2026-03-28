using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public class FrequencyAnalysisService : IFrequencyAnalysisService
{
    private readonly LottoDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<FrequencyAnalysisService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public FrequencyAnalysisService(
        LottoDbContext context,
        IDistributedCache cache,
        ILogger<FrequencyAnalysisService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<NumberFrequencyDto>> GetNumberFrequenciesAsync()
    {
        var cacheKey = "number_frequencies_all";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for number frequencies");
            return JsonSerializer.Deserialize<IEnumerable<NumberFrequencyDto>>(cachedResult) ?? Enumerable.Empty<NumberFrequencyDto>();
        }

        _logger.LogDebug("Cache miss for number frequencies");

        // Get all number frequencies from the database
        var frequencies = await _context.NumberFrequencies
            .OrderByDescending(nf => nf.TotalOccurrences)
            .Select(nf => new NumberFrequencyDto
            {
                Number = nf.Number,
                TotalOccurrences = nf.TotalOccurrences,
                LastAppearance = nf.LastAppearance,
                FirstAppearance = nf.FirstAppearance,
                LongestGap = nf.LongestGap,
                AverageFrequency = nf.AverageFrequency,
                Percentage = CalculatePercentage(nf.TotalOccurrences),
                IsHot = IsHotNumber(nf.LastAppearance, nf.TotalOccurrences),
                IsCold = IsColdNumber(nf.LastAppearance, nf.TotalOccurrences),
                CurrentGap = CalculateCurrentGap(nf.LastAppearance)
            })
            .ToListAsync();

        // Cache the result
        var serializedResult = JsonSerializer.Serialize(frequencies);
        await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        });

        _logger.LogInformation("Retrieved {Count} number frequencies", frequencies.Count);
        return frequencies;
    }

    public async Task<IEnumerable<RangeFrequency>> GetRangeFrequenciesAsync(IEnumerable<NumberRange> ranges)
    {
        if (ranges == null || !ranges.Any())
        {
            throw new ArgumentException("Ranges cannot be null or empty", nameof(ranges));
        }

        var cacheKey = $"range_frequencies_{string.Join("_", ranges.Select(r => $"{r.StartNumber}-{r.EndNumber}"))}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for range frequencies");
            return JsonSerializer.Deserialize<IEnumerable<RangeFrequency>>(cachedResult) ?? Enumerable.Empty<RangeFrequency>();
        }

        _logger.LogDebug("Cache miss for range frequencies");

        var result = new List<RangeFrequency>();
        var totalDraws = await GetTotalDrawsCount();

        foreach (var range in ranges)
        {
            ValidateRange(range);

            var numbersInRange = Enumerable.Range(range.StartNumber, range.EndNumber - range.StartNumber + 1).ToArray();
            
            var rangeOccurrences = await _context.NumberOccurrences
                .Where(no => numbersInRange.Contains(no.Number))
                .GroupBy(no => no.Number)
                .Select(g => new NumberFrequencyDto
                {
                    Number = g.Key,
                    TotalOccurrences = g.Count(),
                    LastAppearance = g.Max(no => no.DrawDate),
                    FirstAppearance = g.Min(no => no.DrawDate),
                    Percentage = (double)g.Count() / totalDraws * 100,
                    CurrentGap = CalculateCurrentGap(g.Max(no => no.DrawDate))
                })
                .ToListAsync();

            var totalRangeOccurrences = rangeOccurrences.Sum(ro => ro.TotalOccurrences);
            var averagePerDraw = totalDraws > 0 ? (double)totalRangeOccurrences / totalDraws : 0;

            result.Add(new RangeFrequency
            {
                Range = range,
                TotalOccurrences = totalRangeOccurrences,
                Percentage = totalDraws > 0 ? (double)totalRangeOccurrences / totalDraws * 100 : 0,
                AveragePerDraw = averagePerDraw,
                IndividualNumbers = rangeOccurrences
            });
        }

        // Cache the result
        var serializedResult = JsonSerializer.Serialize(result);
        await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        });

        _logger.LogInformation("Calculated frequencies for {Count} ranges", ranges.Count());
        return result;
    }

    public async Task<IEnumerable<NumberFrequencyDto>> CompareFrequenciesAsync(FrequencyAnalysisRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Ranges == null || !request.Ranges.Any())
        {
            throw new ArgumentException("Ranges cannot be null or empty", nameof(request));
        }

        var cacheKey = $"frequency_comparison_{GetComparisonCacheKey(request)}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for frequency comparison");
            var comparison = JsonSerializer.Deserialize<FrequencyComparisonDto>(cachedResult) ?? new FrequencyComparisonDto();
            return comparison.Numbers;
        }

        _logger.LogDebug("Cache miss for frequency comparison");

        // For now, return all number frequencies
        // In a full implementation, this would do proper comparison based on the request
        var frequencies = await GetNumberFrequenciesAsync();

        _logger.LogInformation("Completed frequency comparison for {Count} ranges", request.Ranges.Count());
        return frequencies;
    }

    public async Task<IEnumerable<HotColdNumber>> GetHotColdAnalysisAsync(int periodDays = 365)
    {
        if (periodDays <= 0)
        {
            throw new ArgumentException("Period days must be positive", nameof(periodDays));
        }

        var cacheKey = $"hot_cold_analysis_{periodDays}";
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResult != null)
        {
            _logger.LogDebug("Cache hit for hot/cold analysis");
            var hotColdDtos = JsonSerializer.Deserialize<IEnumerable<HotColdNumberDto>>(cachedResult) ?? Enumerable.Empty<HotColdNumberDto>();
            return hotColdDtos.Select(dto => new HotColdNumber
            {
                Number = dto.Number,
                RecentOccurrences = dto.RecentOccurrences,
                HistoricalAverage = dto.HistoricalAverage,
                HotColdScore = dto.HotColdScore,
                Classification = dto.Classification
            });
        }

        _logger.LogDebug("Cache miss for hot/cold analysis");

        var cutoffDate = DateTime.UtcNow.AddDays(-periodDays);
        
        var recentOccurrences = await _context.NumberOccurrences
            .Where(no => no.DrawDate >= cutoffDate)
            .GroupBy(no => no.Number)
            .Select(g => new
            {
                Number = g.Key,
                RecentCount = g.Count(),
                LastAppearance = g.Max(no => no.DrawDate)
            })
            .ToListAsync();

        var historicalAverages = await _context.NumberFrequencies
            .ToDictionaryAsync(nf => nf.Number, nf => nf.AverageFrequency);

        var totalRecentDraws = await _context.LottoDraws
            .Where(d => d.Date >= cutoffDate)
            .CountAsync();

        var hotColdNumbers = new List<HotColdNumber>();

        for (int number = 1; number <= 40; number++)
        {
            var recentOccurrence = recentOccurrences.FirstOrDefault(ro => ro.Number == number);
            var recentCount = recentOccurrence?.RecentCount ?? 0;
            var historicalAverage = historicalAverages.GetValueOrDefault(number, 0);
            
            var expectedRecent = totalRecentDraws > 0 ? historicalAverage * totalRecentDraws : 0;
            var hotColdScore = expectedRecent > 0 ? (recentCount - expectedRecent) / expectedRecent : 0;

            var classification = ClassifyHotCold(hotColdScore);

            hotColdNumbers.Add(new HotColdNumber
            {
                Number = number,
                RecentOccurrences = recentCount,
                HistoricalAverage = (int)historicalAverage,
                HotColdScore = hotColdScore,
                Classification = classification
            });
        }

        var result = hotColdNumbers.OrderByDescending(hcn => hcn.HotColdScore).AsEnumerable();

        // Cache the result
        var serializedResult = JsonSerializer.Serialize(result);
        await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        });

        _logger.LogInformation("Completed hot/cold analysis for {PeriodDays} days", periodDays);
        return result;
    }

    public async Task RefreshFrequencyDataAsync()
    {
        _logger.LogInformation("Starting frequency data refresh");
        
        // This would recalculate all frequency data
        // For now, just clear the cache to force recalculation
        await _cache.RemoveAsync("number_frequencies_all");
        
        _logger.LogInformation("Frequency data refresh completed");
    }

    private async Task<int> GetTotalDrawsCount()
    {
        return await _context.LottoDraws.CountAsync();
    }

    private async Task<DateTime> GetEarliestDrawDate()
    {
        return await _context.LottoDraws.MinAsync(d => d.Date);
    }

    private async Task<DateTime> GetLatestDrawDate()
    {
        return await _context.LottoDraws.MaxAsync(d => d.Date);
    }

    private double CalculatePercentage(int occurrences)
    {
        var totalDraws = _context.LottoDraws.Count();
        return totalDraws > 0 ? (double)occurrences / totalDraws * 100 : 0;
    }

    private bool IsHotNumber(DateTime lastAppearance, int totalOccurrences)
    {
        var daysSinceLastAppearance = (DateTime.UtcNow - lastAppearance).Days;
        return daysSinceLastAppearance <= 30 && totalOccurrences > GetAverageOccurrences();
    }

    private bool IsColdNumber(DateTime lastAppearance, int totalOccurrences)
    {
        var daysSinceLastAppearance = (DateTime.UtcNow - lastAppearance).Days;
        return daysSinceLastAppearance > 90 || totalOccurrences < GetAverageOccurrences() * 0.5;
    }

    private int CalculateCurrentGap(DateTime lastAppearance)
    {
        return (DateTime.UtcNow - lastAppearance).Days;
    }

    private double GetAverageOccurrences()
    {
        var totalOccurrences = _context.NumberFrequencies.Sum(nf => nf.TotalOccurrences);
        return totalOccurrences / 40.0; // 40 possible numbers
    }

    private void ValidateRange(NumberRange range)
    {
        if (range.StartNumber < 1 || range.StartNumber > 40)
        {
            throw new ArgumentException($"Start number must be between 1 and 40, got {range.StartNumber}");
        }

        if (range.EndNumber < 1 || range.EndNumber > 40)
        {
            throw new ArgumentException($"End number must be between 1 and 40, got {range.EndNumber}");
        }

        if (range.StartNumber > range.EndNumber)
        {
            throw new ArgumentException($"Start number ({range.StartNumber}) cannot be greater than end number ({range.EndNumber})");
        }
    }

    private string GetComparisonCacheKey(FrequencyAnalysisRequest request)
    {
        var rangeKey = string.Join("_", request.Ranges.Select(r => $"{r.StartNumber}-{r.EndNumber}"));
        var dateKey = $"{request.StartDate?.ToString("yyyyMMdd") ?? "null"}_{request.EndDate?.ToString("yyyyMMdd") ?? "null"}";
        var filterKey = $"{request.IncludeBonus}_{request.IncludePowerball}";
        return $"{rangeKey}_{dateKey}_{filterKey}";
    }

    private Task<IEnumerable<RangeFrequency>> ApplyDateFiltering(
        IEnumerable<RangeFrequency> frequencies, 
        DateTime? startDate, 
        DateTime? endDate)
    {
        // This would require recalculating frequencies with date filters
        // For now, return the original frequencies
        // In a full implementation, this would re-query the database with date constraints
        return Task.FromResult(frequencies);
    }

    private string ClassifyHotCold(double hotColdScore)
    {
        if (hotColdScore > 0.2)
            return "Hot";
        else if (hotColdScore < -0.2)
            return "Cold";
        else
            return "Normal";
    }
}