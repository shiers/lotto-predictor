using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("PredictionAccuracy")]
public class PredictionAccuracy
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PredictionId { get; set; }
    
    [Required]
    public int ActualDrawId { get; set; }
    
    [Required]
    [Range(0, 6)]
    public int ExactMatches { get; set; }
    
    [Required]
    [Range(0, 6)]
    public int PartialMatches { get; set; }
    
    [Required]
    [Range(0.0, 1.0)]
    public double ProximityScore { get; set; }
    
    [Required]
    [Range(0.0, 1.0)]
    public double OverallAccuracy { get; set; }
    
    [Required]
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Prediction Prediction { get; set; } = null!;
    public virtual LottoDraw ActualDraw { get; set; } = null!;
}