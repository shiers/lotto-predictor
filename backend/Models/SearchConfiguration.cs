using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Models;

[Table("SearchConfigurations")]
public class SearchConfiguration
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "jsonb")]
    public string SearchRequestJson { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastUsed { get; set; }
    
    public int UsageCount { get; set; }
    
    public bool IsPublic { get; set; }
    
    // Navigation properties
    [NotMapped]
    public AdvancedSearchRequest? SearchRequest { get; set; }
}