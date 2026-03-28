using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class NavigationRequest
{
    public int? DrawNumber { get; set; }
    
    public DateTime? Date { get; set; }
    
    public NavigationDirection Direction { get; set; }
}

public enum NavigationDirection
{
    Previous,
    Next,
    First,
    Last,
    Specific
}

public class NavigationContext
{
    public LottoDrawDto CurrentDraw { get; set; } = new LottoDrawDto();
    
    public int CurrentPosition { get; set; }
    
    public int TotalDraws { get; set; }
    
    public bool HasPrevious { get; set; }
    
    public bool HasNext { get; set; }
    
    public DateTime EarliestDate { get; set; }
    
    public DateTime LatestDate { get; set; }
    
    public int[] MissingDrawNumbers { get; set; } = Array.Empty<int>();
}

public class JumpToRequest
{
    public int? DrawNumber { get; set; }
    
    public DateTime? Date { get; set; }
    
    public bool FindClosest { get; set; } = true;
}