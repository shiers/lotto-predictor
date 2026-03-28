using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("NumberFrequencies")]
public class NumberFrequency
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number { get; set; }
    
    [Required]
    public int TotalOccurrences { get; set; }
    
    [Required]
    public DateTime LastAppearance { get; set; }
    
    [Required]
    public DateTime FirstAppearance { get; set; }
    
    public int LongestGap { get; set; }
    
    public double AverageFrequency { get; set; }
    
    // Calculated properties for visualization
    public double Percentage { get; set; }
    public int CurrentGap { get; set; }
    public bool IsHot { get; set; }
    public bool IsCold { get; set; }
    
    [Required]
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
}