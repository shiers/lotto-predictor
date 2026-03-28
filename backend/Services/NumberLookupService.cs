using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;
using System.Linq.Expressions;

namespace PredictLottoNZ.Services;

public class NumberLookupService : INumberLookupService
{
    private readonly LottoDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IPerformanceMonitoringService _performanceMonitoring;
    private readonly ILogger<NumberLookupService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public NumberLookupService(
        LottoDbContext context,
        ICacheService cacheService,
        IPerformanceMonitoringService performanceMonitoring,
        ILogger<NumberLookupService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _performanceMonitoring = performanceMonitoring;
        _logger = logger;
    }

    public async Task<IEnumerable<NumberOccurrenceDto>> LookupNumberAsync(int number)
    {
        if (number < 1 || number > 40)
        {
            throw new ArgumentException("Number must be between 1 and 40", nameof(number));
        }

        var cacheKey = CacheService.GetNumberLookupKey(number);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogDebug("Executing database query for number lookup: {Number}", number);
            
            var occurrences = await _context.NumberOccurrences
                .Where(no => no.Number == number)
                .Include(no => no.Draw)
                .OrderByDescending(no => no.DrawDate)
                .Select(no => new NumberOccurrenceDto
                {
                    DrawNumber = no.DrawNumber,
                    DrawDate = no.DrawDate,
                    Number = no.Number,
                    Position = no.Position,
                    IsBonus = no.IsBonus,
                    IsPowerball = no.IsPowerball,
                    FullCombination = new int[] 
                    { 
                        no.Draw.WinningNumber1, 
                        no.Draw.WinningNumber2, 
                        no.Draw.WinningNumber3, 
                        no.Draw.WinningNumber4, 
                        no.Draw.WinningNumber5, 
                        no.Draw.WinningNumber6 
                    }
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} occurrences for number {Number}", occurrences.Count, number);
            return (IEnumerable<NumberOccurrenceDto>)occurrences;
        }, _cacheExpiration) ?? Enumerable.Empty<NumberOccurrenceDto>();
    }

    public async Task<IEnumerable<NumberOccurrenceDto>> LookupNumbersAsync(int[] numbers)
    {
        if (numbers == null || numbers.Length == 0)
        {
            throw new ArgumentException("Numbers array cannot be null or empty", nameof(numbers));
        }

        if (numbers.Any(n => n < 1 || n > 40))
        {
            throw new ArgumentException("All numbers must be between 1 and 40", nameof(numbers));
        }

        if (numbers.Distinct().Count() != numbers.Length)
        {
            throw new ArgumentException("Numbers must be unique", nameof(numbers));
        }

        var cacheKey = CacheService.GetNumbersLookupKey(numbers);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogDebug("Executing database query for numbers lookup: {Numbers}", string.Join(", ", numbers));

            var occurrences = await _context.NumberOccurrences
                .Where(no => numbers.Contains(no.Number))
                .Include(no => no.Draw)
                .OrderByDescending(no => no.DrawDate)
                .ThenBy(no => no.Number)
                .Select(no => new NumberOccurrenceDto
                {
                    DrawNumber = no.DrawNumber,
                    DrawDate = no.DrawDate,
                    Number = no.Number,
                    Position = no.Position,
                    IsBonus = no.IsBonus,
                    IsPowerball = no.IsPowerball,
                    FullCombination = new int[] 
                    { 
                        no.Draw.WinningNumber1, 
                        no.Draw.WinningNumber2, 
                        no.Draw.WinningNumber3, 
                        no.Draw.WinningNumber4, 
                        no.Draw.WinningNumber5, 
                        no.Draw.WinningNumber6 
                    }
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} occurrences for numbers {Numbers}", occurrences.Count, string.Join(", ", numbers));
            return (IEnumerable<NumberOccurrenceDto>)occurrences;
        }, _cacheExpiration) ?? Enumerable.Empty<NumberOccurrenceDto>();
    }

    public async Task<CombinationSearchResult> SearchCombinationAsync(int[] combination, bool includePartialMatches = true)
    {
        if (combination == null || combination.Length < 2 || combination.Length > 6)
        {
            throw new ArgumentException("Combination must contain between 2 and 6 numbers", nameof(combination));
        }

        if (combination.Any(n => n < 1 || n > 40))
        {
            throw new ArgumentException("All numbers must be between 1 and 40", nameof(combination));
        }

        if (combination.Distinct().Count() != combination.Length)
        {
            throw new ArgumentException("Numbers in combination must be unique", nameof(combination));
        }

        var cacheKey = CacheService.GetCombinationSearchKey(combination, includePartialMatches);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogDebug("Executing database query for combination search: {Combination}", string.Join(", ", combination));

            var draws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .ToListAsync();

            var exactMatches = new List<CombinationMatch>();
            var partialMatches = new List<CombinationMatch>();

            foreach (var draw in draws)
            {
                var winningNumbers = new int[] 
                { 
                    draw.WinningNumber1, 
                    draw.WinningNumber2, 
                    draw.WinningNumber3, 
                    draw.WinningNumber4, 
                    draw.WinningNumber5, 
                    draw.WinningNumber6 
                };

                var matchedNumbers = combination.Intersect(winningNumbers).ToArray();
                var matchCount = matchedNumbers.Length;

                if (matchCount == combination.Length)
                {
                    // Exact match
                    exactMatches.Add(new CombinationMatch
                    {
                        DrawNumber = draw.Draw,
                        DrawDate = draw.Date,
                        WinningCombination = winningNumbers,
                        MatchedNumbers = matchedNumbers,
                        MatchCount = matchCount,
                        IsExactMatch = true
                    });
                }
                else if (includePartialMatches && matchCount >= 2)
                {
                    // Partial match (at least 2 numbers)
                    partialMatches.Add(new CombinationMatch
                    {
                        DrawNumber = draw.Draw,
                        DrawDate = draw.Date,
                        WinningCombination = winningNumbers,
                        MatchedNumbers = matchedNumbers,
                        MatchCount = matchCount,
                        IsExactMatch = false
                    });
                }
            }

            var result = new CombinationSearchResult
            {
                SearchedCombination = combination,
                ExactMatches = exactMatches,
                PartialMatches = partialMatches.OrderByDescending(pm => pm.MatchCount).ThenByDescending(pm => pm.DrawDate),
                TotalExactMatches = exactMatches.Count,
                TotalPartialMatches = partialMatches.Count
            };

            _logger.LogInformation("Combination search for {Combination} found {ExactMatches} exact matches and {PartialMatches} partial matches", 
                string.Join(", ", combination), exactMatches.Count, partialMatches.Count);

            return result;
        }, _cacheExpiration) ?? new CombinationSearchResult();
    }

    public async Task<IEnumerable<SearchSuggestion>> GetSearchSuggestionsAsync(string query, SearchType type)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<SearchSuggestion>();
        }

        var suggestions = new List<SearchSuggestion>();

        switch (type)
        {
            case SearchType.Number:
                suggestions.AddRange(await GetNumberSuggestionsAsync(query));
                break;
            case SearchType.Combination:
                suggestions.AddRange(await GetCombinationSuggestionsAsync(query));
                break;
            case SearchType.Range:
                suggestions.AddRange(await GetRangeSuggestionsAsync(query));
                break;
            case SearchType.DrawNumber:
                suggestions.AddRange(await GetDrawNumberSuggestionsAsync(query));
                break;
            case SearchType.Date:
                suggestions.AddRange(await GetDateSuggestionsAsync(query));
                break;
        }

        return suggestions.OrderByDescending(s => s.Relevance).Take(10);
    }

    private async Task<IEnumerable<SearchSuggestion>> GetNumberSuggestionsAsync(string query)
    {
        var suggestions = new List<SearchSuggestion>();

        if (int.TryParse(query, out int number) && number >= 1 && number <= 40)
        {
            var frequency = await _context.NumberFrequencies
                .FirstOrDefaultAsync(nf => nf.Number == number);

            if (frequency != null)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = number.ToString(),
                    Type = SearchType.Number,
                    Relevance = 100,
                    PreviewInfo = $"Appeared {frequency.TotalOccurrences} times, last on {frequency.LastAppearance:yyyy-MM-dd}",
                    Metadata = new { Number = number, Frequency = frequency.TotalOccurrences }
                });
            }
        }

        // Suggest numbers that start with the query
        if (query.Length == 1 && char.IsDigit(query[0]))
        {
            var startDigit = int.Parse(query);
            var matchingNumbers = Enumerable.Range(1, 40)
                .Where(n => n.ToString().StartsWith(startDigit.ToString()))
                .Take(5);

            foreach (var num in matchingNumbers)
            {
                var frequency = await _context.NumberFrequencies
                    .FirstOrDefaultAsync(nf => nf.Number == num);

                suggestions.Add(new SearchSuggestion
                {
                    Text = num.ToString(),
                    Type = SearchType.Number,
                    Relevance = 80,
                    PreviewInfo = frequency != null ? $"Appeared {frequency.TotalOccurrences} times" : "No data",
                    Metadata = new { Number = num }
                });
            }
        }

        return suggestions;
    }

    private async Task<IEnumerable<SearchSuggestion>> GetCombinationSuggestionsAsync(string query)
    {
        var suggestions = new List<SearchSuggestion>();

        // Parse partial combination from query
        var numbers = query.Split(',', ' ', '-')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out int n) && n >= 1 && n <= 40)
            .Select(int.Parse)
            .Distinct()
            .ToArray();

        if (numbers.Length > 0 && numbers.Length < 6)
        {
            // Find frequently occurring combinations that include these numbers
            var recentDraws = await _context.LottoDraws
                .OrderByDescending(d => d.Date)
                .Take(100)
                .ToListAsync();

            var suggestedCombinations = recentDraws
                .Where(d => numbers.All(n => new int[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 }.Contains(n)))
                .Take(3)
                .Select(d => new SearchSuggestion
                {
                    Text = $"{d.WinningNumber1}, {d.WinningNumber2}, {d.WinningNumber3}, {d.WinningNumber4}, {d.WinningNumber5}, {d.WinningNumber6}",
                    Type = SearchType.Combination,
                    Relevance = 90,
                    PreviewInfo = $"Won on {d.Date:yyyy-MM-dd}",
                    Metadata = new { DrawNumber = d.Draw, Date = d.Date }
                });

            suggestions.AddRange(suggestedCombinations);
        }

        return suggestions;
    }

    private Task<IEnumerable<SearchSuggestion>> GetRangeSuggestionsAsync(string query)
    {
        var suggestions = new List<SearchSuggestion>();

        // Preset range suggestions
        var presetRanges = new[]
        {
            new { Start = 1, End = 10, Label = "1-10 (Low numbers)" },
            new { Start = 11, End = 20, Label = "11-20 (Mid-low numbers)" },
            new { Start = 21, End = 30, Label = "21-30 (Mid-high numbers)" },
            new { Start = 31, End = 40, Label = "31-40 (High numbers)" },
            new { Start = 1, End = 20, Label = "1-20 (Lower half)" },
            new { Start = 21, End = 40, Label = "21-40 (Upper half)" }
        };

        foreach (var range in presetRanges)
        {
            if (range.Label.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                $"{range.Start}-{range.End}".Contains(query))
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = $"{range.Start}-{range.End}",
                    Type = SearchType.Range,
                    Relevance = 85,
                    PreviewInfo = range.Label,
                    Metadata = new { StartNumber = range.Start, EndNumber = range.End }
                });
            }
        }

        return Task.FromResult(suggestions.AsEnumerable());
    }

    private async Task<IEnumerable<SearchSuggestion>> GetDrawNumberSuggestionsAsync(string query)
    {
        var suggestions = new List<SearchSuggestion>();

        if (int.TryParse(query, out int drawNumber))
        {
            var draws = await _context.LottoDraws
                .Where(d => d.Draw.ToString().StartsWith(query))
                .OrderByDescending(d => d.Date)
                .Take(5)
                .ToListAsync();

            foreach (var draw in draws)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = draw.Draw.ToString(),
                    Type = SearchType.DrawNumber,
                    Relevance = 95,
                    PreviewInfo = $"Draw {draw.Draw} on {draw.Date:yyyy-MM-dd}",
                    Metadata = new { DrawNumber = draw.Draw, Date = draw.Date }
                });
            }
        }

        return suggestions;
    }

    private async Task<IEnumerable<SearchSuggestion>> GetDateSuggestionsAsync(string query)
    {
        var suggestions = new List<SearchSuggestion>();

        // Try to parse partial dates
        if (DateTime.TryParse(query, out DateTime date))
        {
            var closestDraw = await _context.LottoDraws
                .OrderBy(d => Math.Abs((d.Date - date).Ticks))
                .FirstOrDefaultAsync();

            if (closestDraw != null)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = closestDraw.Date.ToString("yyyy-MM-dd"),
                    Type = SearchType.Date,
                    Relevance = 90,
                    PreviewInfo = $"Draw {closestDraw.Draw}",
                    Metadata = new { DrawNumber = closestDraw.Draw, Date = closestDraw.Date }
                });
            }
        }

        return suggestions;
    }

    public async Task<AdvancedSearchResult> AdvancedSearchAsync(AdvancedSearchRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting advanced search with {CriteriaCount} criteria", request.Criteria.Count());
            
            var query = _context.NumberOccurrences.AsQueryable();
            
            // Apply date filters
            if (request.StartDate.HasValue)
            {
                query = query.Where(no => no.DrawDate >= request.StartDate.Value);
            }
            
            if (request.EndDate.HasValue)
            {
                query = query.Where(no => no.DrawDate <= request.EndDate.Value);
            }
            
            // Apply bonus/powerball filters
            if (!request.IncludeBonus)
            {
                query = query.Where(no => !no.IsBonus);
            }
            
            if (!request.IncludePowerball)
            {
                query = query.Where(no => !no.IsPowerball);
            }
            
            // Apply search criteria
            if (request.Criteria.Any())
            {
                query = ApplySearchCriteria(query, request.Criteria, request.LogicalOperator);
            }
            
            // Apply frequency filters
            if (request.FrequencyFilter != null)
            {
                query = await ApplyFrequencyFilters(query, request.FrequencyFilter);
            }
            
            // Execute query with includes
            var occurrences = await query
                .Include(no => no.Draw)
                .OrderByDescending(no => no.DrawDate)
                .Take(request.MaxResults)
                .Select(no => new NumberOccurrenceDto
                {
                    DrawNumber = no.DrawNumber,
                    DrawDate = no.DrawDate,
                    Number = no.Number,
                    Position = no.Position,
                    IsBonus = no.IsBonus,
                    IsPowerball = no.IsPowerball,
                    FullCombination = new int[] 
                    { 
                        no.Draw.WinningNumber1, 
                        no.Draw.WinningNumber2, 
                        no.Draw.WinningNumber3, 
                        no.Draw.WinningNumber4, 
                        no.Draw.WinningNumber5, 
                        no.Draw.WinningNumber6 
                    }
                })
                .ToListAsync();
            
            // Calculate statistics
            var statistics = CalculateSearchStatistics(occurrences);
            
            stopwatch.Stop();
            
            var result = new AdvancedSearchResult
            {
                Occurrences = occurrences,
                CombinationMatches = new List<CombinationMatch>(), // Will be populated if combination criteria exist
                TotalResults = occurrences.Count,
                SearchCriteria = request,
                ExecutionTime = stopwatch.Elapsed,
                Statistics = statistics
            };
            
            _logger.LogInformation("Advanced search completed in {ElapsedMs}ms, found {ResultCount} results", 
                stopwatch.ElapsedMilliseconds, occurrences.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing advanced search");
            throw;
        }
    }
    
    private IQueryable<NumberOccurrence> ApplySearchCriteria(
        IQueryable<NumberOccurrence> query, 
        IEnumerable<SearchCriteria> criteria, 
        LogicalOperator logicalOperator)
    {
        var criteriaList = criteria.ToList();
        if (!criteriaList.Any()) return query;
        
        if (logicalOperator == LogicalOperator.And)
        {
            // Apply all criteria with AND logic
            foreach (var criterion in criteriaList)
            {
                query = ApplySingleCriterion(query, criterion);
            }
        }
        else
        {
            // Apply criteria with OR logic - this is more complex and requires building expressions
            var predicates = new List<Expression<Func<NumberOccurrence, bool>>>();
            
            foreach (var criterion in criteriaList)
            {
                var predicate = BuildCriterionPredicate(criterion);
                if (predicate != null)
                {
                    predicates.Add(predicate);
                }
            }
            
            if (predicates.Any())
            {
                var combinedPredicate = CombinePredicatesWithOr(predicates);
                query = query.Where(combinedPredicate);
            }
        }
        
        return query;
    }
    
    private IQueryable<NumberOccurrence> ApplySingleCriterion(IQueryable<NumberOccurrence> query, SearchCriteria criterion)
    {
        switch (criterion.Type)
        {
            case SearchCriteriaType.Number:
                if (criterion.Value is int number)
                {
                    return ApplyNumberCriterion(query, number, criterion.Operator);
                }
                break;
                
            case SearchCriteriaType.NumberRange:
                if (criterion.Value is NumberRange range)
                {
                    return ApplyNumberRangeCriterion(query, range, criterion.Operator);
                }
                break;
                
            case SearchCriteriaType.DrawNumber:
                if (criterion.Value is int drawNumber)
                {
                    return ApplyDrawNumberCriterion(query, drawNumber, criterion.Operator);
                }
                break;
                
            case SearchCriteriaType.Position:
                if (criterion.Value is int position)
                {
                    return ApplyPositionCriterion(query, position, criterion.Operator);
                }
                break;
                
            case SearchCriteriaType.Date:
                if (criterion.Value is DateTime date)
                {
                    return ApplyDateCriterion(query, date, criterion.Operator);
                }
                break;
        }
        
        return query;
    }
    
    private IQueryable<NumberOccurrence> ApplyNumberCriterion(IQueryable<NumberOccurrence> query, int number, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equals => query.Where(no => no.Number == number),
            ComparisonOperator.NotEquals => query.Where(no => no.Number != number),
            ComparisonOperator.GreaterThan => query.Where(no => no.Number > number),
            ComparisonOperator.LessThan => query.Where(no => no.Number < number),
            ComparisonOperator.GreaterThanOrEqual => query.Where(no => no.Number >= number),
            ComparisonOperator.LessThanOrEqual => query.Where(no => no.Number <= number),
            _ => query
        };
    }
    
    private IQueryable<NumberOccurrence> ApplyNumberRangeCriterion(IQueryable<NumberOccurrence> query, NumberRange range, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Between or ComparisonOperator.Contains => 
                query.Where(no => no.Number >= range.StartNumber && no.Number <= range.EndNumber),
            ComparisonOperator.NotContains => 
                query.Where(no => no.Number < range.StartNumber || no.Number > range.EndNumber),
            _ => query
        };
    }
    
    private IQueryable<NumberOccurrence> ApplyDrawNumberCriterion(IQueryable<NumberOccurrence> query, int drawNumber, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equals => query.Where(no => no.DrawNumber == drawNumber),
            ComparisonOperator.NotEquals => query.Where(no => no.DrawNumber != drawNumber),
            ComparisonOperator.GreaterThan => query.Where(no => no.DrawNumber > drawNumber),
            ComparisonOperator.LessThan => query.Where(no => no.DrawNumber < drawNumber),
            ComparisonOperator.GreaterThanOrEqual => query.Where(no => no.DrawNumber >= drawNumber),
            ComparisonOperator.LessThanOrEqual => query.Where(no => no.DrawNumber <= drawNumber),
            _ => query
        };
    }
    
    private IQueryable<NumberOccurrence> ApplyPositionCriterion(IQueryable<NumberOccurrence> query, int position, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equals => query.Where(no => no.Position == position),
            ComparisonOperator.NotEquals => query.Where(no => no.Position != position),
            ComparisonOperator.GreaterThan => query.Where(no => no.Position > position),
            ComparisonOperator.LessThan => query.Where(no => no.Position < position),
            ComparisonOperator.GreaterThanOrEqual => query.Where(no => no.Position >= position),
            ComparisonOperator.LessThanOrEqual => query.Where(no => no.Position <= position),
            _ => query
        };
    }
    
    private IQueryable<NumberOccurrence> ApplyDateCriterion(IQueryable<NumberOccurrence> query, DateTime date, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equals => query.Where(no => no.DrawDate.Date == date.Date),
            ComparisonOperator.NotEquals => query.Where(no => no.DrawDate.Date != date.Date),
            ComparisonOperator.GreaterThan => query.Where(no => no.DrawDate > date),
            ComparisonOperator.LessThan => query.Where(no => no.DrawDate < date),
            ComparisonOperator.GreaterThanOrEqual => query.Where(no => no.DrawDate >= date),
            ComparisonOperator.LessThanOrEqual => query.Where(no => no.DrawDate <= date),
            _ => query
        };
    }
    
    private async Task<IQueryable<NumberOccurrence>> ApplyFrequencyFilters(IQueryable<NumberOccurrence> query, FrequencyFilter filter)
    {
        if (filter.MinOccurrences.HasValue || filter.MaxOccurrences.HasValue)
        {
            var frequencyQuery = _context.NumberFrequencies.AsQueryable();
            
            if (filter.MinOccurrences.HasValue)
            {
                frequencyQuery = frequencyQuery.Where(nf => nf.TotalOccurrences >= filter.MinOccurrences.Value);
            }
            
            if (filter.MaxOccurrences.HasValue)
            {
                frequencyQuery = frequencyQuery.Where(nf => nf.TotalOccurrences <= filter.MaxOccurrences.Value);
            }
            
            var validNumbers = await frequencyQuery.Select(nf => nf.Number).ToListAsync();
            query = query.Where(no => validNumbers.Contains(no.Number));
        }
        
        if (filter.SinceDate.HasValue)
        {
            query = query.Where(no => no.DrawDate >= filter.SinceDate.Value);
        }
        
        if (filter.UntilDate.HasValue)
        {
            query = query.Where(no => no.DrawDate <= filter.UntilDate.Value);
        }
        
        return query;
    }
    
    private Expression<Func<NumberOccurrence, bool>>? BuildCriterionPredicate(SearchCriteria criterion)
    {
        // This is a simplified version - in a real implementation, you'd use Expression trees
        // For now, we'll handle the most common cases
        switch (criterion.Type)
        {
            case SearchCriteriaType.Number when criterion.Value is int number:
                return no => no.Number == number;
                
            case SearchCriteriaType.DrawNumber when criterion.Value is int drawNumber:
                return no => no.DrawNumber == drawNumber;
                
            case SearchCriteriaType.Position when criterion.Value is int position:
                return no => no.Position == position;
                
            default:
                return null;
        }
    }
    
    private Expression<Func<NumberOccurrence, bool>> CombinePredicatesWithOr(List<Expression<Func<NumberOccurrence, bool>>> predicates)
    {
        if (!predicates.Any())
            throw new ArgumentException("No predicates to combine");
            
        if (predicates.Count == 1)
            return predicates[0];
        
        // For simplicity, we'll use the first predicate
        // In a real implementation, you'd properly combine expressions with OR
        return predicates[0];
    }
    
    private SearchStatistics CalculateSearchStatistics(IEnumerable<NumberOccurrenceDto> occurrences)
    {
        var occurrencesList = occurrences.ToList();
        
        if (!occurrencesList.Any())
        {
            return new SearchStatistics();
        }
        
        var uniqueDraws = occurrencesList.Select(o => o.DrawNumber).Distinct().Count();
        var uniqueNumbers = occurrencesList.Select(o => o.Number).Distinct().Count();
        var earliestMatch = occurrencesList.Min(o => o.DrawDate);
        var latestMatch = occurrencesList.Max(o => o.DrawDate);
        var averageMatchesPerDraw = uniqueDraws > 0 ? (double)occurrencesList.Count / uniqueDraws : 0;
        
        return new SearchStatistics
        {
            TotalDrawsSearched = uniqueDraws,
            UniqueNumbersFound = uniqueNumbers,
            EarliestMatch = earliestMatch,
            LatestMatch = latestMatch,
            AverageMatchesPerDraw = averageMatchesPerDraw
        };
    }
    
    public async Task<SearchConfiguration> SaveSearchConfigurationAsync(SaveSearchConfigurationRequest request, string userId)
    {
        _logger.LogDebug("Saving search configuration '{Name}' for user {UserId}", request.Name, userId);
        
        // Check if configuration with same name already exists for this user
        var existingConfig = await _context.SearchConfigurations
            .FirstOrDefaultAsync(sc => sc.UserId == userId && sc.Name == request.Name);
            
        if (existingConfig != null)
        {
            throw new InvalidOperationException($"A search configuration with the name '{request.Name}' already exists.");
        }
        
        var configuration = new SearchConfiguration
        {
            Name = request.Name,
            Description = request.Description,
            UserId = userId,
            SearchRequestJson = JsonSerializer.Serialize(request.SearchRequest),
            CreatedAt = DateTime.UtcNow,
            UsageCount = 0,
            IsPublic = request.IsPublic
        };
        
        _context.SearchConfigurations.Add(configuration);
        await _context.SaveChangesAsync();
        
        // Set the deserialized search request for the response
        configuration.SearchRequest = request.SearchRequest;
        
        _logger.LogInformation("Saved search configuration '{Name}' with ID {Id} for user {UserId}", 
            request.Name, configuration.Id, userId);
        
        return configuration;
    }
    
    public async Task<IEnumerable<SearchConfiguration>> GetUserSearchConfigurationsAsync(string userId)
    {
        _logger.LogDebug("Retrieving search configurations for user {UserId}", userId);
        
        var configurations = await _context.SearchConfigurations
            .Where(sc => sc.UserId == userId)
            .OrderByDescending(sc => sc.LastUsed ?? sc.CreatedAt)
            .ToListAsync();
        
        // Deserialize search requests
        foreach (var config in configurations)
        {
            try
            {
                config.SearchRequest = JsonSerializer.Deserialize<AdvancedSearchRequest>(config.SearchRequestJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize search request for configuration {Id}", config.Id);
                config.SearchRequest = new AdvancedSearchRequest();
            }
        }
        
        _logger.LogInformation("Retrieved {Count} search configurations for user {UserId}", configurations.Count, userId);
        return configurations;
    }
    
    public async Task<IEnumerable<SearchConfiguration>> GetPublicSearchConfigurationsAsync()
    {
        _logger.LogDebug("Retrieving public search configurations");
        
        var configurations = await _context.SearchConfigurations
            .Where(sc => sc.IsPublic)
            .OrderByDescending(sc => sc.UsageCount)
            .ThenByDescending(sc => sc.CreatedAt)
            .Take(50) // Limit to top 50 public configurations
            .ToListAsync();
        
        // Deserialize search requests
        foreach (var config in configurations)
        {
            try
            {
                config.SearchRequest = JsonSerializer.Deserialize<AdvancedSearchRequest>(config.SearchRequestJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize search request for public configuration {Id}", config.Id);
                config.SearchRequest = new AdvancedSearchRequest();
            }
        }
        
        _logger.LogInformation("Retrieved {Count} public search configurations", configurations.Count);
        return configurations;
    }
    
    public async Task<SearchConfiguration?> GetSearchConfigurationAsync(int configurationId, string userId)
    {
        _logger.LogDebug("Retrieving search configuration {Id} for user {UserId}", configurationId, userId);
        
        var configuration = await _context.SearchConfigurations
            .FirstOrDefaultAsync(sc => sc.Id == configurationId && (sc.UserId == userId || sc.IsPublic));
        
        if (configuration != null)
        {
            try
            {
                configuration.SearchRequest = JsonSerializer.Deserialize<AdvancedSearchRequest>(configuration.SearchRequestJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize search request for configuration {Id}", configuration.Id);
                configuration.SearchRequest = new AdvancedSearchRequest();
            }
            
            // Update usage statistics if this is the user's own configuration
            if (configuration.UserId == userId)
            {
                configuration.LastUsed = DateTime.UtcNow;
                configuration.UsageCount++;
                await _context.SaveChangesAsync();
            }
        }
        
        return configuration;
    }
    
    public async Task<bool> DeleteSearchConfigurationAsync(int configurationId, string userId)
    {
        _logger.LogDebug("Deleting search configuration {Id} for user {UserId}", configurationId, userId);
        
        var configuration = await _context.SearchConfigurations
            .FirstOrDefaultAsync(sc => sc.Id == configurationId && sc.UserId == userId);
        
        if (configuration == null)
        {
            _logger.LogWarning("Search configuration {Id} not found or not owned by user {UserId}", configurationId, userId);
            return false;
        }
        
        _context.SearchConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted search configuration {Id} for user {UserId}", configurationId, userId);
        return true;
    }
    
    public async Task<SearchConfiguration> UpdateSearchConfigurationAsync(int configurationId, SaveSearchConfigurationRequest request, string userId)
    {
        _logger.LogDebug("Updating search configuration {Id} for user {UserId}", configurationId, userId);
        
        var configuration = await _context.SearchConfigurations
            .FirstOrDefaultAsync(sc => sc.Id == configurationId && sc.UserId == userId);
        
        if (configuration == null)
        {
            throw new InvalidOperationException($"Search configuration with ID {configurationId} not found or not owned by user.");
        }
        
        // Check if new name conflicts with existing configurations (excluding current one)
        if (configuration.Name != request.Name)
        {
            var existingConfig = await _context.SearchConfigurations
                .FirstOrDefaultAsync(sc => sc.UserId == userId && sc.Name == request.Name && sc.Id != configurationId);
                
            if (existingConfig != null)
            {
                throw new InvalidOperationException($"A search configuration with the name '{request.Name}' already exists.");
            }
        }
        
        configuration.Name = request.Name;
        configuration.Description = request.Description;
        configuration.SearchRequestJson = JsonSerializer.Serialize(request.SearchRequest);
        configuration.IsPublic = request.IsPublic;
        
        await _context.SaveChangesAsync();
        
        // Set the deserialized search request for the response
        configuration.SearchRequest = request.SearchRequest;
        
        _logger.LogInformation("Updated search configuration {Id} for user {UserId}", configurationId, userId);
        return configuration;
    }

    // Paginated lookup methods with performance monitoring
    public async Task<PaginatedResponse<NumberOccurrenceDto>> LookupNumberPaginatedAsync(
        int number, 
        PaginationRequest pagination)
    {
        using var perfContext = _performanceMonitoring.StartOperation("number_lookup_paginated", 
            new { Number = number, Page = pagination.Page, PageSize = pagination.PageSize });

        try
        {
            if (number < 1 || number > 40)
            {
                throw new ArgumentException("Number must be between 1 and 40", nameof(number));
            }

            perfContext.Checkpoint("validation_complete");

            var cacheKey = $"{CacheService.GetNumberLookupKey(number)}_page_{pagination.Page}_{pagination.PageSize}";
            
            var result = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                perfContext.Checkpoint("cache_miss_start_query");

                // Get total count first
                var totalCount = await _context.NumberOccurrences
                    .Where(no => no.Number == number)
                    .CountAsync();

                perfContext.Checkpoint("total_count_retrieved");

                // Get paginated results
                var occurrences = await _context.NumberOccurrences
                    .Where(no => no.Number == number)
                    .Include(no => no.Draw)
                    .OrderByDescending(no => no.DrawDate)
                    .Skip(pagination.Skip)
                    .Take(pagination.Take)
                    .Select(no => new NumberOccurrenceDto
                    {
                        DrawNumber = no.DrawNumber,
                        DrawDate = no.DrawDate,
                        Number = no.Number,
                        Position = no.Position,
                        IsBonus = no.IsBonus,
                        IsPowerball = no.IsPowerball,
                        FullCombination = new int[] 
                        { 
                            no.Draw.WinningNumber1, 
                            no.Draw.WinningNumber2, 
                            no.Draw.WinningNumber3, 
                            no.Draw.WinningNumber4, 
                            no.Draw.WinningNumber5, 
                            no.Draw.WinningNumber6 
                        }
                    })
                    .ToListAsync();

                perfContext.Checkpoint("paginated_results_retrieved");

                return new PaginatedResponse<NumberOccurrenceDto>
                {
                    Items = occurrences,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalItems = totalCount
                };
            }, _cacheExpiration) ?? new PaginatedResponse<NumberOccurrenceDto>
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };

            _performanceMonitoring.CompleteOperation(perfContext, result.ItemCount, true);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in paginated number lookup for number {Number}", number);
            _performanceMonitoring.CompleteOperation(perfContext, 0, false);
            throw;
        }
    }

    public async Task<PaginatedResponse<NumberOccurrenceDto>> LookupNumbersPaginatedAsync(
        NumberLookupPaginationRequest request)
    {
        using var perfContext = _performanceMonitoring.StartOperation("numbers_lookup_paginated", request);

        try
        {
            if (request.Numbers == null || request.Numbers.Length == 0)
            {
                throw new ArgumentException("Numbers array cannot be null or empty", nameof(request));
            }

            if (request.Numbers.Any(n => n < 1 || n > 40))
            {
                throw new ArgumentException("All numbers must be between 1 and 40", nameof(request));
            }

            perfContext.Checkpoint("validation_complete");

            var cacheKey = $"{CacheService.GetNumbersLookupKey(request.Numbers)}_page_{request.Page}_{request.PageSize}";
            
            var result = _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                perfContext.Checkpoint("cache_miss_start_query");

                var query = _context.NumberOccurrences
                    .Where(no => request.Numbers.Contains(no.Number));

                // Apply date filters
                if (request.StartDate.HasValue)
                {
                    query = query.Where(no => no.DrawDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(no => no.DrawDate <= request.EndDate.Value);
                }

                // Apply bonus/powerball filters
                if (!request.IncludeBonus)
                {
                    query = query.Where(no => !no.IsBonus);
                }

                if (!request.IncludePowerball)
                {
                    query = query.Where(no => !no.IsPowerball);
                }

                perfContext.Checkpoint("filters_applied");

                // Get total count
                var totalCount = await query.CountAsync();

                perfContext.Checkpoint("total_count_retrieved");

                // Apply sorting
                var sortedQuery = ApplySorting(query, request.SortBy, request.IsDescending);

                // Get paginated results
                var occurrences = await sortedQuery
                    .Include(no => no.Draw)
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .Select(no => new NumberOccurrenceDto
                    {
                        DrawNumber = no.DrawNumber,
                        DrawDate = no.DrawDate,
                        Number = no.Number,
                        Position = no.Position,
                        IsBonus = no.IsBonus,
                        IsPowerball = no.IsPowerball,
                        FullCombination = new int[] 
                        { 
                            no.Draw.WinningNumber1, 
                            no.Draw.WinningNumber2, 
                            no.Draw.WinningNumber3, 
                            no.Draw.WinningNumber4, 
                            no.Draw.WinningNumber5, 
                            no.Draw.WinningNumber6 
                        }
                    })
                    .ToListAsync();

                perfContext.Checkpoint("paginated_results_retrieved");

                return new PaginatedResponse<NumberOccurrenceDto>
                {
                    Items = occurrences,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = totalCount
                };
            }, _cacheExpiration).Result ?? new PaginatedResponse<NumberOccurrenceDto>
            {
                Page = request.Page,
                PageSize = request.PageSize
            };

            _performanceMonitoring.CompleteOperation(perfContext, result.ItemCount, true);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in paginated numbers lookup");
            _performanceMonitoring.CompleteOperation(perfContext, 0, false);
            throw;
        }
    }

    public async Task<PaginatedResponse<CombinationMatch>> SearchCombinationPaginatedAsync(
        CombinationSearchPaginationRequest request)
    {
        using var perfContext = _performanceMonitoring.StartOperation("combination_search_paginated", request);

        try
        {
            if (request.Combination == null || request.Combination.Length < 2 || request.Combination.Length > 6)
            {
                throw new ArgumentException("Combination must contain between 2 and 6 numbers", nameof(request));
            }

            if (request.Combination.Any(n => n < 1 || n > 40))
            {
                throw new ArgumentException("All numbers must be between 1 and 40", nameof(request));
            }

            if (request.Combination.Distinct().Count() != request.Combination.Length)
            {
                throw new ArgumentException("Numbers in combination must be unique", nameof(request));
            }

            perfContext.Checkpoint("validation_complete");

            var cacheKey = $"{CacheService.GetCombinationSearchKey(request.Combination, request.IncludePartialMatches)}_page_{request.Page}_{request.PageSize}";
            
            var result = _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                perfContext.Checkpoint("cache_miss_start_query");

                var query = _context.LottoDraws.AsQueryable();

                // Apply date filters
                if (request.StartDate.HasValue)
                {
                    query = query.Where(d => d.Date >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(d => d.Date <= request.EndDate.Value);
                }

                perfContext.Checkpoint("date_filters_applied");

                var draws = await query.OrderByDescending(d => d.Date).ToListAsync();

                perfContext.Checkpoint("draws_retrieved");

                var allMatches = new List<CombinationMatch>();

                foreach (var draw in draws)
                {
                    var winningNumbers = new int[] 
                    { 
                        draw.WinningNumber1, 
                        draw.WinningNumber2, 
                        draw.WinningNumber3, 
                        draw.WinningNumber4, 
                        draw.WinningNumber5, 
                        draw.WinningNumber6 
                    };

                    var matchedNumbers = request.Combination.Intersect(winningNumbers).ToArray();
                    var matchCount = matchedNumbers.Length;

                    if (matchCount == request.Combination.Length)
                    {
                        // Exact match
                        allMatches.Add(new CombinationMatch
                        {
                            DrawNumber = draw.Draw,
                            DrawDate = draw.Date,
                            WinningCombination = winningNumbers,
                            MatchedNumbers = matchedNumbers,
                            MatchCount = matchCount,
                            IsExactMatch = true
                        });
                    }
                    else if (request.IncludePartialMatches && matchCount >= request.MinimumMatches)
                    {
                        // Partial match
                        allMatches.Add(new CombinationMatch
                        {
                            DrawNumber = draw.Draw,
                            DrawDate = draw.Date,
                            WinningCombination = winningNumbers,
                            MatchedNumbers = matchedNumbers,
                            MatchCount = matchCount,
                            IsExactMatch = false
                        });
                    }
                }

                perfContext.Checkpoint("matches_calculated");

                // Sort matches (exact matches first, then by match count, then by date)
                var sortedMatches = allMatches
                    .OrderByDescending(m => m.IsExactMatch)
                    .ThenByDescending(m => m.MatchCount)
                    .ThenByDescending(m => m.DrawDate)
                    .ToList();

                // Apply pagination
                var paginatedMatches = sortedMatches
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToList();

                perfContext.Checkpoint("pagination_applied");

                return new PaginatedResponse<CombinationMatch>
                {
                    Items = paginatedMatches,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = sortedMatches.Count
                };
            }, _cacheExpiration).Result ?? new PaginatedResponse<CombinationMatch>
            {
                Page = request.Page,
                PageSize = request.PageSize
            };

            _performanceMonitoring.CompleteOperation(perfContext, result.ItemCount, true);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in paginated combination search");
            _performanceMonitoring.CompleteOperation(perfContext, 0, false);
            throw;
        }
    }

    private IQueryable<NumberOccurrence> ApplySorting(IQueryable<NumberOccurrence> query, string? sortBy, bool isDescending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "date" or "drawdate" => isDescending 
                ? query.OrderByDescending(no => no.DrawDate)
                : query.OrderBy(no => no.DrawDate),
            "number" => isDescending 
                ? query.OrderByDescending(no => no.Number)
                : query.OrderBy(no => no.Number),
            "position" => isDescending 
                ? query.OrderByDescending(no => no.Position)
                : query.OrderBy(no => no.Position),
            "drawnumber" => isDescending 
                ? query.OrderByDescending(no => no.DrawNumber)
                : query.OrderBy(no => no.DrawNumber),
            _ => query.OrderByDescending(no => no.DrawDate) // Default sort
        };
    }
}