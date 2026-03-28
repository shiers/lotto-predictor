using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("ModelVersions")]
public class ModelVersion
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ModelVersionNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DeployedAt { get; set; }
    
    [Range(0.0, 1.0)]
    public double? ValidationAccuracy { get; set; }
    
    [MaxLength(500)]
    public string? ModelPath { get; set; }
    
    [Required]
    public bool IsActive { get; set; } = false;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<TrainingRun> TrainingRuns { get; set; } = new List<TrainingRun>();
}