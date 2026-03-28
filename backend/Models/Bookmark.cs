using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("Bookmarks")]
public class Bookmark
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)] // Standard user ID length
    public string UserId { get; set; } = null!;
    
    [Required]
    public int DrawNumber { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Label { get; set; } = null!;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public virtual LottoDraw Draw { get; set; } = null!;
}