using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class BookmarkDto
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    public int DrawNumber { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Label { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    // Additional properties for service compatibility
    public DateTime DrawDate { get; set; }
    
    public int[] WinningNumbers { get; set; } = Array.Empty<int>();
    
    public LottoDrawDto Draw { get; set; } = new LottoDrawDto();
}

public class CreateBookmarkRequest
{
    [Required]
    public int DrawNumber { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Label { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateBookmarkRequest
{
    [Required]
    [StringLength(100)]
    public string Label { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}