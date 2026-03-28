using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class FrequencyAnalysisRequest
{
    public IEnumerable<NumberRange> Ranges { get; set; } = new List<NumberRange>();
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public bool IncludeBonus { get; set; } = false;
    
    public bool IncludePowerball { get; set; } = false;
}

public class RangeFrequency
{
    public NumberRange Range { get; set; } = new NumberRange();
    
    public int TotalOccurrences { get; set; }
    
    public double Percentage { get; set; }
    
    public double AveragePerDraw { get; set; }
    
    public IEnumerable<NumberFrequencyDto> IndividualNumbers { get; set; } = new List<NumberFrequencyDto>();
}

public class NumberFrequencyDto
{
    [Range(1, 40)]
    public int Number { get; set; }
    
    public int TotalOccurrences { get; set; }
    
    public DateTime LastAppearance { get; set; }
    
    public DateTime FirstAppearance { get; set; }
    
    public int LongestGap { get; set; }
    
    public int CurrentGap { get; set; }
    
    public double Percentage { get; set; }
    
    public double AverageFrequency { get; set; }
    
    public bool IsHot { get; set; }
    
    public bool IsCold { get; set; }
}

public class HotColdNumber
{
    [Range(1, 40)]
    public int Number { get; set; }
    
    public int RecentOccurrences { get; set; }
    
    public int HistoricalAverage { get; set; }
    
    public double HotColdScore { get; set; }
    
    [StringLength(20)]
    public string Classification { get; set; } = string.Empty; // "Hot", "Cold", "Normal"
}

// Additional DTOs for service compatibility
public class HotColdNumberDto : HotColdNumber
{
    // Inherits all properties from HotColdNumber for compatibility
}

public class FrequencyComparisonDto
{
    public IEnumerable<NumberFrequencyDto> FirstSet { get; set; } = new List<NumberFrequencyDto>();
    
    public IEnumerable<NumberFrequencyDto> SecondSet { get; set; } = new List<NumberFrequencyDto>();
    
    // Alias for compatibility
    public IEnumerable<NumberFrequencyDto> Numbers 
    { 
        get => FirstSet; 
        set => FirstSet = value; 
    }
    
    public FrequencyAnalysisRequest ComparisonCriteria { get; set; } = new FrequencyAnalysisRequest();
    
    public DateTime GeneratedAt { get; set; }
}