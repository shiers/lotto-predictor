using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public class AutoCompletionService : IAutoCompletionService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<AutoCompletionService> _logger;

    public AutoCompletionService(LottoDbContext context, ILogger<AutoCompletionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SearchSuggestion>> GetSearchSuggestionsAsync(string query, SearchType type, int maxSuggestions = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<SearchSuggestion>();

        return type switch
        {
            SearchType.Number => await GetNumberSuggestionsAsync(query, maxSuggestions),
            SearchType.Combination => await GetCombinationSuggestionsAsync(query, maxSuggestions),
            SearchType.Range => await GetRangeSuggestionsAsync(query, maxSuggestions),
            SearchType.DrawNumber => await GetDrawNumberSuggestionsAsync(query, maxSuggestions),
            SearchType.Date => await GetDateSuggestionsAsync(query, maxSuggestions),
            _ => Enumerable.Empty<SearchSuggestion>()
        };
    }

    public async Task<IEnumerable<SearchSuggestion>> GetNumberSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        var suggestions = new List<SearchSuggestion>();

        // Parse the query to see if it's a valid number or partial number
        if (int.TryParse(query, out int exactNumber) && exactNumber >= 1 && exactNumber <= 40)
        {
            // Get frequency data for the exact number
            var frequency = await GetNumberFrequencyAsync(exactNumber);
            suggestions.Add(new SearchSuggestion
            {
                Text = exactNumber.ToString(),
                Type = SearchType.Number,
                Relevance = 100,
                PreviewInfo = frequency > 0 ? $"Appeared {frequency} times" : "No occurrences found",
                Metadata = new { Number = exactNumber, Frequency = frequency }
            });
        }

        // Add similar numbers based on partial match
        if (query.Length <= 2 && query.All(char.IsDigit))
        {
            var matchingNumbers = Enumerable.Range(1, 40)
                .Where(n => n.ToString().StartsWith(query) && n != exactNumber)
                .Take(maxSuggestions - suggestions.Count);

            foreach (var number in matchingNumbers)
            {
                var frequency = await GetNumberFrequencyAsync(number);
                suggestions.Add(new SearchSuggestion
                {
                    Text = number.ToString(),
                    Type = SearchType.Number,
                    Relevance = 80 - Math.Abs(number - (exactNumber > 0 ? exactNumber : int.Parse(query + "0"))),
                    PreviewInfo = frequency > 0 ? $"Appeared {frequency} times" : "No occurrences found",
                    Metadata = new { Number = number, Frequency = frequency }
                });
            }
        }

        return suggestions.OrderByDescending(s => s.Relevance).Take(maxSuggestions);
    }

    public async Task<IEnumerable<SearchSuggestion>> GetCombinationSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        var suggestions = new List<SearchSuggestion>();

        // Parse numbers from the query
        var numbers = ParseNumbersFromQuery(query);
        
        if (numbers.Length > 0 && numbers.Length < 6)
        {
            // Find historical combinations that include some of these numbers
            var historicalCombinations = await GetHistoricalCombinationsContaining(numbers, maxSuggestions);
            
            foreach (var combo in historicalCombinations)
            {
                var matchCount = numbers.Intersect(combo.Numbers).Count();
                suggestions.Add(new SearchSuggestion
                {
                    Text = string.Join(", ", combo.Numbers.OrderBy(n => n)),
                    Type = SearchType.Combination,
                    Relevance = 90 - (6 - matchCount) * 10,
                    PreviewInfo = $"Won on {combo.Date:yyyy-MM-dd} (Draw {combo.DrawNumber})",
                    Metadata = new { DrawNumber = combo.DrawNumber, Date = combo.Date, MatchCount = matchCount }
                });
            }
        }

        return suggestions.OrderByDescending(s => s.Relevance).Take(maxSuggestions);
    }

    public async Task<IEnumerable<SearchSuggestion>> GetRangeSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        var suggestions = new List<SearchSuggestion>();

        // Get preset ranges first
        var presetRanges = GetPresetRanges();
        
        foreach (var range in presetRanges)
        {
            if (IsRangeRelevantToQuery(range, query))
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = range.Range,
                    Type = SearchType.Range,
                    Relevance = CalculateRangeRelevance(range, query),
                    PreviewInfo = range.Description,
                    Metadata = new { StartNumber = range.Start, EndNumber = range.End }
                });
            }
        }

        // Try to parse custom range from query
        if (TryParseCustomRange(query, out var customStart, out var customEnd))
        {
            if (customStart >= 1 && customEnd <= 40 && customStart <= customEnd)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = $"{customStart}-{customEnd}",
                    Type = SearchType.Range,
                    Relevance = 95,
                    PreviewInfo = $"Custom range: {customStart} to {customEnd}",
                    Metadata = new { StartNumber = customStart, EndNumber = customEnd }
                });
            }
        }

        return suggestions.OrderByDescending(s => s.Relevance).Take(maxSuggestions);
    }

    public async Task<IEnumerable<SearchSuggestion>> GetDrawNumberSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        var suggestions = new List<SearchSuggestion>();

        if (int.TryParse(query, out int drawNumber))
        {
            // Find draws that match or are close to the query
            var draws = await _context.LottoDraws
                .Where(d => d.Draw.ToString().StartsWith(query))
                .OrderBy(d => Math.Abs(d.Draw - drawNumber))
                .Take(maxSuggestions)
                .Select(d => new { d.Draw, d.Date })
                .ToListAsync();

            foreach (var draw in draws)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = draw.Draw.ToString(),
                    Type = SearchType.DrawNumber,
                    Relevance = 95 - Math.Abs(draw.Draw - drawNumber),
                    PreviewInfo = $"Draw {draw.Draw} on {draw.Date:yyyy-MM-dd}",
                    Metadata = new { DrawNumber = draw.Draw, Date = draw.Date }
                });
            }
        }

        return suggestions.OrderByDescending(s => s.Relevance);
    }

    public async Task<IEnumerable<SearchSuggestion>> GetPresetRangeOptionsAsync()
    {
        var presetRanges = GetPresetRanges();
        
        return presetRanges.Select(range => new SearchSuggestion
        {
            Text = range.Range,
            Type = SearchType.Range,
            Relevance = 85,
            PreviewInfo = range.Description,
            Metadata = new { StartNumber = range.Start, EndNumber = range.End }
        });
    }

    private async Task<IEnumerable<SearchSuggestion>> GetDateSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        var suggestions = new List<SearchSuggestion>();

        // Try to parse partial dates
        if (DateTime.TryParse(query, out var date))
        {
            var closestDraws = await _context.LottoDraws
                .OrderBy(d => Math.Abs((d.Date - date).TotalDays))
                .Take(maxSuggestions)
                .Select(d => new { d.Draw, d.Date })
                .ToListAsync();

            foreach (var draw in closestDraws)
            {
                suggestions.Add(new SearchSuggestion
                {
                    Text = draw.Date.ToString("yyyy-MM-dd"),
                    Type = SearchType.Date,
                    Relevance = 90,
                    PreviewInfo = $"Draw {draw.Draw}",
                    Metadata = new { DrawNumber = draw.Draw, Date = draw.Date }
                });
            }
        }

        return suggestions;
    }

    private async Task<int> GetNumberFrequencyAsync(int number)
    {
        try
        {
            var frequency = await _context.LottoDraws
                .CountAsync(d => d.WinningNumber1 == number || 
                               d.WinningNumber2 == number || 
                               d.WinningNumber3 == number || 
                               d.WinningNumber4 == number || 
                               d.WinningNumber5 == number || 
                               d.WinningNumber6 == number);
            return frequency;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting frequency for number {Number}", number);
            return 0;
        }
    }

    private int[] ParseNumbersFromQuery(string query)
    {
        return query.Split(',', ' ', '-', ';')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out int n) && n >= 1 && n <= 40)
            .Select(int.Parse)
            .Distinct()
            .ToArray();
    }

    private async Task<IEnumerable<(int DrawNumber, DateTime Date, int[] Numbers)>> GetHistoricalCombinationsContaining(int[] numbers, int maxResults)
    {
        try
        {
            var draws = await _context.LottoDraws
                .Where(d => numbers.Any(n => n == d.WinningNumber1 || n == d.WinningNumber2 || 
                                           n == d.WinningNumber3 || n == d.WinningNumber4 || 
                                           n == d.WinningNumber5 || n == d.WinningNumber6))
                .OrderByDescending(d => d.Date)
                .Take(maxResults)
                .Select(d => new { 
                    d.Draw, 
                    d.Date, 
                    Numbers = new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, 
                                    d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 }
                })
                .ToListAsync();

            return draws.Select(d => (d.Draw, d.Date, d.Numbers));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical combinations");
            return Enumerable.Empty<(int, DateTime, int[])>();
        }
    }

    private static IEnumerable<(string Range, string Description, int Start, int End)> GetPresetRanges()
    {
        return new[]
        {
            ("1-10", "Low numbers", 1, 10),
            ("11-20", "Mid-low numbers", 11, 20),
            ("21-30", "Mid-high numbers", 21, 30),
            ("31-40", "High numbers", 31, 40),
            ("1-20", "Lower half", 1, 20),
            ("21-40", "Upper half", 21, 40),
            ("1-15", "First quarter", 1, 15),
            ("16-30", "Middle half", 16, 30),
            ("31-40", "Last quarter", 31, 40)
        };
    }

    private static bool IsRangeRelevantToQuery(
        (string Range, string Description, int Start, int End) range, 
        string query)
    {
        var queryLower = query.ToLower();
        var descriptionLower = range.Description.ToLower();
        
        return descriptionLower.Contains(queryLower) || 
               range.Range.Contains(query) ||
               (queryLower.Contains("low") && descriptionLower.Contains("low")) ||
               (queryLower.Contains("high") && descriptionLower.Contains("high")) ||
               (queryLower.Contains("mid") && descriptionLower.Contains("mid")) ||
               (queryLower.Contains("half") && descriptionLower.Contains("half"));
    }

    private static int CalculateRangeRelevance(
        (string Range, string Description, int Start, int End) range, 
        string query)
    {
        var queryLower = query.ToLower();
        var descriptionLower = range.Description.ToLower();
        
        if (range.Range.Equals(query, StringComparison.OrdinalIgnoreCase))
            return 100;
        
        if (range.Range.Contains(query))
            return 95;
        
        if (descriptionLower.Contains(queryLower))
            return 90;
        
        return 85;
    }

    private static bool TryParseCustomRange(string query, out int start, out int end)
    {
        start = 0;
        end = 0;

        if (query.Contains('-'))
        {
            var parts = query.Split('-');
            if (parts.Length == 2 && 
                int.TryParse(parts[0].Trim(), out start) && 
                int.TryParse(parts[1].Trim(), out end))
            {
                return true;
            }
        }

        return false;
    }
}