using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("TrainingRuns")]
public class TrainingRun
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Started";
    
    [Required]
    [Range(0, int.MaxValue)]
    public int TrainingDataSize { get; set; }
    
    [Range(0.0, 1.0)]
    public double? FinalAccuracy { get; set; }
    
    [Column(TypeName = "text")]
    public string? ErrorMessage { get; set; }
    
    public int? ResultingModelVersionId { get; set; }
    
    [Column(TypeName = "text")]
    public string? TrainingParameters { get; set; }
    
    [Column(TypeName = "text")]
    public string? TrainingMetrics { get; set; }
    
    [MaxLength(200)]
    public string? TriggerReason { get; set; }
    
    [MaxLength(100)]
    public string? CurrentPhase { get; set; }
    
    // Navigation properties
    public virtual ModelVersion? ResultingModelVersion { get; set; }
}