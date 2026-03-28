using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class NumberLookupRequest
{
    [Required]
    public int[] Numbers { get; set; } = Array.Empty<int>();
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public bool IncludeBonus { get; set; } = true;
    
    public bool IncludePowerball { get; set; } = true;
}

public class NumberOccurrenceDto
{
    public int DrawNumber { get; set; }
    
    public DateTime DrawDate { get; set; }
    
    public int Number { get; set; }
    
    public int Position { get; set; }
    
    public bool IsBonus { get; set; }
    
    public bool IsPowerball { get; set; }
    
    public int[] FullCombination { get; set; } = Array.Empty<int>();
}

public class CombinationSearchRequest
{
    [Required]
    public int[] Combination { get; set; } = Array.Empty<int>();
    
    public bool IncludePartialMatches { get; set; } = true;
    
    [Range(1, 6)]
    public int MinimumMatches { get; set; } = 2;
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
}

public class CombinationSearchResult
{
    public int[] SearchedCombination { get; set; } = Array.Empty<int>();
    
    public IEnumerable<CombinationMatch> ExactMatches { get; set; } = new List<CombinationMatch>();
    
    public IEnumerable<CombinationMatch> PartialMatches { get; set; } = new List<CombinationMatch>();
    
    public int TotalExactMatches { get; set; }
    
    public int TotalPartialMatches { get; set; }
}

public class CombinationMatch
{
    public int DrawNumber { get; set; }
    
    public DateTime DrawDate { get; set; }
    
    public int[] WinningCombination { get; set; } = Array.Empty<int>();
    
    public int[] MatchedNumbers { get; set; } = Array.Empty<int>();
    
    public int MatchCount { get; set; }
    
    public bool IsExactMatch { get; set; }
}

// Advanced Search Models
// Number range for lookup operations - standardized definition
public class NumberRange
{
    [Range(1, 40)]
    public int StartNumber { get; set; }
    
    [Range(1, 40)]
    public int EndNumber { get; set; }
    
    [StringLength(50)]
    public string Label { get; set; } = string.Empty;
    
    // Compatibility properties for LLM training models
    public int Min 
    { 
        get => StartNumber; 
        set => StartNumber = value; 
    }
    
    public int Max 
    { 
        get => EndNumber; 
        set => EndNumber = value; 
    }
}

public class AdvancedSearchRequest
{
    public IEnumerable<SearchCriteria> Criteria { get; set; } = new List<SearchCriteria>();
    
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public FrequencyFilter? FrequencyFilter { get; set; }
    
    public bool IncludeBonus { get; set; } = true;
    
    public bool IncludePowerball { get; set; } = true;
    
    [Range(1, 1000)]
    public int MaxResults { get; set; } = 100;
}

public class SearchCriteria
{
    public SearchCriteriaType Type { get; set; }
    
    public object Value { get; set; } = new object();
    
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equals;
}

public enum SearchCriteriaType
{
    Number,
    NumberRange,
    Combination,
    DrawNumber,
    Date,
    Frequency,
    Position
}

public enum LogicalOperator
{
    And,
    Or
}

public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    NotContains,
    Between
}

public class FrequencyFilter
{
    public int? MinOccurrences { get; set; }
    
    public int? MaxOccurrences { get; set; }
    
    public DateTime? SinceDate { get; set; }
    
    public DateTime? UntilDate { get; set; }
}

public class AdvancedSearchResult
{
    public IEnumerable<NumberOccurrenceDto> Occurrences { get; set; } = new List<NumberOccurrenceDto>();
    
    public IEnumerable<CombinationMatch> CombinationMatches { get; set; } = new List<CombinationMatch>();
    
    public int TotalResults { get; set; }
    
    public AdvancedSearchRequest SearchCriteria { get; set; } = new AdvancedSearchRequest();
    
    public TimeSpan ExecutionTime { get; set; }
    
    public SearchStatistics Statistics { get; set; } = new SearchStatistics();
}

public class SearchStatistics
{
    public int TotalDrawsSearched { get; set; }
    
    public int UniqueNumbersFound { get; set; }
    
    public DateTime? EarliestMatch { get; set; }
    
    public DateTime? LatestMatch { get; set; }
    
    public double AverageMatchesPerDraw { get; set; }
}

// Search Configuration DTO - maps from entity model

public class SaveSearchConfigurationRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public AdvancedSearchRequest SearchRequest { get; set; } = new AdvancedSearchRequest();
    
    public bool IsPublic { get; set; } = false;
}

// SearchConfiguration is defined in Models/SearchConfiguration.cs to avoid conflicts

