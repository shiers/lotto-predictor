using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IAutoCompletionService
{
    Task<IEnumerable<SearchSuggestion>> GetSearchSuggestionsAsync(string query, SearchType type, int maxSuggestions = 10);
    Task<IEnumerable<SearchSuggestion>> GetNumberSuggestionsAsync(string query, int maxSuggestions = 10);
    Task<IEnumerable<SearchSuggestion>> GetCombinationSuggestionsAsync(string query, int maxSuggestions = 10);
    Task<IEnumerable<SearchSuggestion>> GetRangeSuggestionsAsync(string query, int maxSuggestions = 10);
    Task<IEnumerable<SearchSuggestion>> GetDrawNumberSuggestionsAsync(string query, int maxSuggestions = 10);
    Task<IEnumerable<SearchSuggestion>> GetPresetRangeOptionsAsync();
}