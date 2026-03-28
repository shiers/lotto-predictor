using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class SearchSuggestion
{
    [Required]
    public string Text { get; set; } = string.Empty;
    
    public SearchType Type { get; set; }
    
    public int Relevance { get; set; }
    
    public string PreviewInfo { get; set; } = string.Empty;
    
    public object? Metadata { get; set; }
}

public enum SearchType
{
    Number,
    Combination,
    Range,
    Date,
    DrawNumber
}

public class AutoCompleteRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;
    
    public SearchType Type { get; set; }
    
    [Range(1, 50)]
    public int MaxSuggestions { get; set; } = 10;
    
    public bool IncludePreview { get; set; } = true;
}

