using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("PredictionScoreHistory")]
public class PredictionScoreHistory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PredictionId { get; set; }
    
    [Required]
    public double OriginalScore { get; set; }
    
    [Required]
    public double UpdatedScore { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(200)]
    public string UpdateReason { get; set; } = string.Empty;
    
    [Required]
    public int TriggeringDrawId { get; set; }
    
    public int ExactMatches { get; set; }
    
    public int PartialMatches { get; set; }
    
    public double ProximityScore { get; set; }
    
    public double OverallAccuracy { get; set; }
    
    // Navigation properties
    public virtual Prediction Prediction { get; set; } = null!;
    public virtual LottoDraw TriggeringDraw { get; set; } = null!;
}