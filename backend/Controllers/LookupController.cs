using Microsoft.AspNetCore.Mvc;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly INumberLookupService _lookupService;
    private readonly ILogger<LookupController> _logger;

    public LookupController(
        INumberLookupService lookupService,
        ILogger<LookupController> logger)
    {
        _lookupService = lookupService;
        _logger = logger;
    }

    /// <summary>
    /// Look up occurrences of a single lottery number
    /// </summary>
    /// <param name="number">Lottery number to look up (1-40)</param>
    /// <returns>All occurrences of the specified number</returns>
    [HttpGet("number/{number:int}")]
    public async Task<ActionResult<IEnumerable<NumberOccurrenceDto>>> LookupNumber(
        [Range(1, 40)] int number)
    {
        try
        {
            if (number < 1 || number > 40)
            {
                _logger.LogWarning("Invalid number provided: {Number}", number);
                return BadRequest(new { error = "Number must be between 1 and 40" });
            }

            _logger.LogInformation("Looking up occurrences for number {Number}", number);

            var occurrences = await _lookupService.LookupNumberAsync(number);

            _logger.LogInformation("Found {Count} occurrences for number {Number}", 
                occurrences.Count(), number);

            return Ok(occurrences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up number {Number}", number);
            return StatusCode(500, new { error = "Internal server error occurred during number lookup" });
        }
    }

    /// <summary>
    /// Look up occurrences of multiple lottery numbers
    /// </summary>
    /// <param name="request">Number lookup request with multiple numbers</param>
    /// <returns>Combined occurrences of all specified numbers</returns>
    [HttpPost("numbers")]
    public async Task<ActionResult<IEnumerable<NumberOccurrenceDto>>> LookupNumbers(
        [FromBody] NumberLookupRequest request)
    {
        try
        {
            if (request.Numbers == null || !request.Numbers.Any())
            {
                return BadRequest(new { error = "At least one number must be provided" });
            }

            if (request.Numbers.Length > 10)
            {
                return BadRequest(new { error = "Maximum 10 numbers allowed per request" });
            }

            if (request.Numbers.Any(n => n < 1 || n > 40))
            {
                return BadRequest(new { error = "All numbers must be between 1 and 40" });
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Looking up occurrences for {Count} numbers: [{Numbers}]", 
                request.Numbers.Length, string.Join(", ", request.Numbers));

            var occurrences = await _lookupService.LookupNumbersAsync(request.Numbers);

            _logger.LogInformation("Found {Count} total occurrences for numbers [{Numbers}]", 
                occurrences.Count(), string.Join(", ", request.Numbers));

            return Ok(occurrences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up numbers [{Numbers}]", 
                string.Join(", ", request.Numbers ?? Array.Empty<int>()));
            return StatusCode(500, new { error = "Internal server error occurred during number lookup" });
        }
    }

    /// <summary>
    /// Search for number combinations in historical draws
    /// </summary>
    /// <param name="request">Combination search request</param>
    /// <returns>Exact and partial matches for the combination</returns>
    [HttpPost("combination")]
    public async Task<ActionResult<CombinationSearchResult>> SearchCombination(
        [FromBody] CombinationSearchRequest request)
    {
        try
        {
            if (request.Combination == null || !request.Combination.Any())
            {
                return BadRequest(new { error = "At least one number must be provided in combination" });
            }

            if (request.Combination.Length > 6)
            {
                return BadRequest(new { error = "Maximum 6 numbers allowed in combination" });
            }

            if (request.Combination.Any(n => n < 1 || n > 40))
            {
                return BadRequest(new { error = "All numbers must be between 1 and 40" });
            }

            if (request.Combination.Distinct().Count() != request.Combination.Length)
            {
                return BadRequest(new { error = "Duplicate numbers are not allowed in combination" });
            }

            if (request.MinimumMatches < 1 || request.MinimumMatches > request.Combination.Length)
            {
                return BadRequest(new { error = "Minimum matches must be between 1 and the combination length" });
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Searching for combination [{Combination}] with minimum {MinMatches} matches", 
                string.Join(", ", request.Combination), request.MinimumMatches);

            var result = await _lookupService.SearchCombinationAsync(
                request.Combination, 
                request.IncludePartialMatches);

            _logger.LogInformation("Found {ExactMatches} exact matches and {PartialMatches} partial matches for combination [{Combination}]", 
                result.TotalExactMatches, result.TotalPartialMatches, string.Join(", ", request.Combination));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for combination [{Combination}]", 
                string.Join(", ", request.Combination ?? Array.Empty<int>()));
            return StatusCode(500, new { error = "Internal server error occurred during combination search" });
        }
    }

    /// <summary>
    /// Get search suggestions for auto-completion
    /// </summary>
    /// <param name="request">Auto-complete request</param>
    /// <returns>Search suggestions based on the query</returns>
    [HttpPost("suggestions")]
    public async Task<ActionResult<IEnumerable<SearchSuggestion>>> GetSearchSuggestions(
        [FromBody] AutoCompleteRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            if (request.MaxSuggestions < 1 || request.MaxSuggestions > 50)
            {
                return BadRequest(new { error = "MaxSuggestions must be between 1 and 50" });
            }

            _logger.LogInformation("Getting search suggestions for query '{Query}' of type {Type}", 
                request.Query, request.Type);

            var suggestions = await _lookupService.GetSearchSuggestionsAsync(request.Query, request.Type);

            var limitedSuggestions = suggestions.Take(request.MaxSuggestions);

            _logger.LogInformation("Returning {Count} suggestions for query '{Query}'", 
                limitedSuggestions.Count(), request.Query);

            return Ok(limitedSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query '{Query}'", request.Query);
            return StatusCode(500, new { error = "Internal server error occurred while getting suggestions" });
        }
    }

    /// <summary>
    /// Perform advanced search with complex criteria
    /// </summary>
    /// <param name="request">Advanced search request with multiple criteria</param>
    /// <returns>Advanced search results</returns>
    [HttpPost("advanced-search")]
    public async Task<ActionResult<AdvancedSearchResult>> AdvancedSearch(
        [FromBody] AdvancedSearchRequest request)
    {
        try
        {
            if (request.Criteria == null || !request.Criteria.Any())
            {
                return BadRequest(new { error = "At least one search criteria must be provided" });
            }

            if (request.MaxResults < 1 || request.MaxResults > 1000)
            {
                return BadRequest(new { error = "MaxResults must be between 1 and 1000" });
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Performing advanced search with {CriteriaCount} criteria and {LogicalOperator} logic", 
                request.Criteria.Count(), request.LogicalOperator);

            var result = await _lookupService.AdvancedSearchAsync(request);

            _logger.LogInformation("Advanced search completed with {ResultCount} results in {ExecutionTime}ms", 
                result.TotalResults, result.ExecutionTime.TotalMilliseconds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing advanced search");
            return StatusCode(500, new { error = "Internal server error occurred during advanced search" });
        }
    }

    /// <summary>
    /// Save a search configuration for reuse
    /// </summary>
    /// <param name="request">Search configuration to save</param>
    /// <returns>Saved search configuration</returns>
    [HttpPost("search-configurations")]
    public async Task<ActionResult<SearchConfiguration>> SaveSearchConfiguration(
        [FromBody] SaveSearchConfigurationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            if (request.SearchRequest.Criteria == null || !request.SearchRequest.Criteria.Any())
            {
                return BadRequest(new { error = "Search criteria are required" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Saving search configuration '{Name}' for user {UserId}", 
                request.Name, userId);

            var configuration = await _lookupService.SaveSearchConfigurationAsync(request, userId);

            _logger.LogInformation("Search configuration '{Name}' saved with ID {ConfigId}", 
                request.Name, configuration.Id);

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving search configuration '{Name}'", request.Name);
            return StatusCode(500, new { error = "Internal server error occurred while saving configuration" });
        }
    }

    /// <summary>
    /// Get user's saved search configurations
    /// </summary>
    /// <returns>List of user's search configurations</returns>
    [HttpGet("search-configurations")]
    public async Task<ActionResult<IEnumerable<SearchConfiguration>>> GetSearchConfigurations()
    {
        try
        {
            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Retrieving search configurations for user {UserId}", userId);

            var configurations = await _lookupService.GetUserSearchConfigurationsAsync(userId);

            _logger.LogInformation("Retrieved {Count} search configurations for user {UserId}", 
                configurations.Count(), userId);

            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving search configurations");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving configurations" });
        }
    }

    /// <summary>
    /// Get public search configurations
    /// </summary>
    /// <returns>List of public search configurations</returns>
    [HttpGet("search-configurations/public")]
    public async Task<ActionResult<IEnumerable<SearchConfiguration>>> GetPublicSearchConfigurations()
    {
        try
        {
            _logger.LogInformation("Retrieving public search configurations");

            var configurations = await _lookupService.GetPublicSearchConfigurationsAsync();

            _logger.LogInformation("Retrieved {Count} public search configurations", configurations.Count());

            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public search configurations");
            return StatusCode(500, new { error = "Internal server error occurred while retrieving public configurations" });
        }
    }

    /// <summary>
    /// Get a specific search configuration
    /// </summary>
    /// <param name="configurationId">ID of the configuration to retrieve</param>
    /// <returns>Search configuration details</returns>
    [HttpGet("search-configurations/{configurationId:int}")]
    public async Task<ActionResult<SearchConfiguration>> GetSearchConfiguration(int configurationId)
    {
        try
        {
            if (configurationId <= 0)
            {
                return BadRequest(new { error = "Configuration ID must be a positive integer" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Retrieving search configuration {ConfigId} for user {UserId}", 
                configurationId, userId);

            var configuration = await _lookupService.GetSearchConfigurationAsync(configurationId, userId);

            if (configuration == null)
            {
                _logger.LogWarning("Search configuration {ConfigId} not found for user {UserId}", 
                    configurationId, userId);
                return NotFound(new { error = "Search configuration not found" });
            }

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving search configuration {ConfigId}", configurationId);
            return StatusCode(500, new { error = "Internal server error occurred while retrieving configuration" });
        }
    }

    /// <summary>
    /// Update a search configuration
    /// </summary>
    /// <param name="configurationId">ID of the configuration to update</param>
    /// <param name="request">Updated configuration data</param>
    /// <returns>Updated search configuration</returns>
    [HttpPut("search-configurations/{configurationId:int}")]
    public async Task<ActionResult<SearchConfiguration>> UpdateSearchConfiguration(
        int configurationId, 
        [FromBody] SaveSearchConfigurationRequest request)
    {
        try
        {
            if (configurationId <= 0)
            {
                return BadRequest(new { error = "Configuration ID must be a positive integer" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Updating search configuration {ConfigId} for user {UserId}", 
                configurationId, userId);

            var configuration = await _lookupService.UpdateSearchConfigurationAsync(
                configurationId, request, userId);

            _logger.LogInformation("Search configuration {ConfigId} updated successfully", configurationId);

            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating search configuration {ConfigId}", configurationId);
            return StatusCode(500, new { error = "Internal server error occurred while updating configuration" });
        }
    }

    /// <summary>
    /// Delete a search configuration
    /// </summary>
    /// <param name="configurationId">ID of the configuration to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("search-configurations/{configurationId:int}")]
    public async Task<ActionResult> DeleteSearchConfiguration(int configurationId)
    {
        try
        {
            if (configurationId <= 0)
            {
                return BadRequest(new { error = "Configuration ID must be a positive integer" });
            }

            // For now, use a placeholder user ID - in a real app this would come from authentication
            var userId = "anonymous";

            _logger.LogInformation("Deleting search configuration {ConfigId} for user {UserId}", 
                configurationId, userId);

            var deleted = await _lookupService.DeleteSearchConfigurationAsync(configurationId, userId);

            if (!deleted)
            {
                _logger.LogWarning("Search configuration {ConfigId} not found for deletion by user {UserId}", 
                    configurationId, userId);
                return NotFound(new { error = "Search configuration not found" });
            }

            _logger.LogInformation("Search configuration {ConfigId} deleted successfully", configurationId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting search configuration {ConfigId}", configurationId);
            return StatusCode(500, new { error = "Internal server error occurred while deleting configuration" });
        }
    }

    // Paginated endpoints for performance optimization

    /// <summary>
    /// Look up occurrences of a single lottery number with pagination
    /// </summary>
    /// <param name="number">Lottery number to look up (1-40)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (1-1000)</param>
    /// <returns>Paginated occurrences of the specified number</returns>
    [HttpGet("number/{number:int}/paginated")]
    public async Task<ActionResult<PaginatedResponse<NumberOccurrenceDto>>> LookupNumberPaginated(
        [Range(1, 40)] int number,
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, 1000)] int pageSize = 50)
    {
        try
        {
            if (number < 1 || number > 40)
            {
                _logger.LogWarning("Invalid number provided: {Number}", number);
                return BadRequest(new { error = "Number must be between 1 and 40" });
            }

            _logger.LogInformation("Looking up paginated occurrences for number {Number} (page {Page}, size {PageSize})", 
                number, page, pageSize);

            var pagination = new PaginationRequest { Page = page, PageSize = pageSize };
            var result = await _lookupService.LookupNumberPaginatedAsync(number, pagination);

            _logger.LogInformation("Found {Count} occurrences on page {Page} for number {Number}", 
                result.ItemCount, page, number);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up paginated results for number {Number}", number);
            return StatusCode(500, new { error = "Internal server error occurred during paginated number lookup" });
        }
    }

    /// <summary>
    /// Look up occurrences of multiple lottery numbers with pagination and filtering
    /// </summary>
    /// <param name="request">Paginated number lookup request with filters</param>
    /// <returns>Paginated occurrences of the specified numbers</returns>
    [HttpPost("numbers/paginated")]
    public async Task<ActionResult<PaginatedResponse<NumberOccurrenceDto>>> LookupNumbersPaginated(
        [FromBody] NumberLookupPaginationRequest request)
    {
        try
        {
            if (request.Numbers == null || !request.Numbers.Any())
            {
                return BadRequest(new { error = "At least one number must be provided" });
            }

            if (request.Numbers.Length > 10)
            {
                return BadRequest(new { error = "Maximum 10 numbers allowed per request" });
            }

            if (request.Numbers.Any(n => n < 1 || n > 40))
            {
                return BadRequest(new { error = "All numbers must be between 1 and 40" });
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Looking up paginated occurrences for {Count} numbers: [{Numbers}] (page {Page}, size {PageSize})", 
                request.Numbers.Length, string.Join(", ", request.Numbers), request.Page, request.PageSize);

            var result = await _lookupService.LookupNumbersPaginatedAsync(request);

            _logger.LogInformation("Found {Count} occurrences on page {Page} for numbers [{Numbers}]", 
                result.ItemCount, request.Page, string.Join(", ", request.Numbers));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up paginated results for numbers [{Numbers}]", 
                string.Join(", ", request.Numbers ?? Array.Empty<int>()));
            return StatusCode(500, new { error = "Internal server error occurred during paginated numbers lookup" });
        }
    }

    /// <summary>
    /// Search for number combinations with pagination
    /// </summary>
    /// <param name="request">Paginated combination search request</param>
    /// <returns>Paginated combination matches</returns>
    [HttpPost("combination/paginated")]
    public async Task<ActionResult<PaginatedResponse<CombinationMatch>>> SearchCombinationPaginated(
        [FromBody] CombinationSearchPaginationRequest request)
    {
        try
        {
            if (request.Combination == null || !request.Combination.Any())
            {
                return BadRequest(new { error = "At least one number must be provided in combination" });
            }

            if (request.Combination.Length > 6)
            {
                return BadRequest(new { error = "Maximum 6 numbers allowed in combination" });
            }

            if (request.Combination.Any(n => n < 1 || n > 40))
            {
                return BadRequest(new { error = "All numbers must be between 1 and 40" });
            }

            if (request.Combination.Distinct().Count() != request.Combination.Length)
            {
                return BadRequest(new { error = "Duplicate numbers are not allowed in combination" });
            }

            if (request.MinimumMatches < 1 || request.MinimumMatches > request.Combination.Length)
            {
                return BadRequest(new { error = "Minimum matches must be between 1 and the combination length" });
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate > request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Searching paginated combination [{Combination}] with minimum {MinMatches} matches (page {Page}, size {PageSize})", 
                string.Join(", ", request.Combination), request.MinimumMatches, request.Page, request.PageSize);

            var result = await _lookupService.SearchCombinationPaginatedAsync(request);

            _logger.LogInformation("Found {Count} matches on page {Page} for combination [{Combination}]", 
                result.ItemCount, request.Page, string.Join(", ", request.Combination));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching paginated combination [{Combination}]", 
                string.Join(", ", request.Combination ?? Array.Empty<int>()));
            return StatusCode(500, new { error = "Internal server error occurred during paginated combination search" });
        }
    }
}