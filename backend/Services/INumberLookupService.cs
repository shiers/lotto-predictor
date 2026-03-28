using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Services;

public interface INumberLookupService
{
    // Original methods
    Task<IEnumerable<NumberOccurrenceDto>> LookupNumberAsync(int number);
    
    Task<IEnumerable<NumberOccurrenceDto>> LookupNumbersAsync(int[] numbers);
    
    Task<CombinationSearchResult> SearchCombinationAsync(int[] combination, bool includePartialMatches = true);
    
    Task<IEnumerable<SearchSuggestion>> GetSearchSuggestionsAsync(string query, SearchType type);
    
    // Advanced search functionality
    Task<AdvancedSearchResult> AdvancedSearchAsync(AdvancedSearchRequest request);
    
    // Paginated methods for performance optimization
    Task<PaginatedResponse<NumberOccurrenceDto>> LookupNumberPaginatedAsync(int number, PaginationRequest pagination);
    
    Task<PaginatedResponse<NumberOccurrenceDto>> LookupNumbersPaginatedAsync(NumberLookupPaginationRequest request);
    
    Task<PaginatedResponse<CombinationMatch>> SearchCombinationPaginatedAsync(CombinationSearchPaginationRequest request);
    
    // Search configuration management
    Task<SearchConfiguration> SaveSearchConfigurationAsync(SaveSearchConfigurationRequest request, string userId);
    
    Task<IEnumerable<SearchConfiguration>> GetUserSearchConfigurationsAsync(string userId);
    
    Task<IEnumerable<SearchConfiguration>> GetPublicSearchConfigurationsAsync();
    
    Task<SearchConfiguration?> GetSearchConfigurationAsync(int configurationId, string userId);
    
    Task<bool> DeleteSearchConfigurationAsync(int configurationId, string userId);
    
    Task<SearchConfiguration> UpdateSearchConfigurationAsync(int configurationId, SaveSearchConfigurationRequest request, string userId);
}