using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("ExportJobs")]
public class ExportJob
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ExportId { get; set; } = null!;
    
    [Required]
    [StringLength(450)] // Standard user ID length
    public string UserId { get; set; } = null!;
    
    [Required]
    [StringLength(50)]
    public string ExportType { get; set; } = null!; // "Lookup", "Frequency", "Navigation"
    
    [Required]
    [StringLength(4000)]
    public string Parameters { get; set; } = null!; // JSON serialized export parameters
    
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = null!; // "Pending", "Processing", "Completed", "Failed"
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }
}