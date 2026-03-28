using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("SearchHistory")]
public class SearchHistory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)] // Standard user ID length
    public string UserId { get; set; } = null!;
    
    [Required]
    [StringLength(50)]
    public string SearchType { get; set; } = null!; // "Number", "Combination", "Range", "Date"
    
    [Required]
    [StringLength(2000)]
    public string SearchCriteria { get; set; } = null!; // JSON serialized search parameters
    
    [Required]
    public int ResultCount { get; set; }
    
    [Required]
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public TimeSpan ExecutionTime { get; set; }
}