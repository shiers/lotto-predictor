using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("LottoDraws")]
public class LottoDraw
{
    [Key]
    public int Draw { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int WinningNumber1 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int WinningNumber2 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int WinningNumber3 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int WinningNumber4 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int WinningNumber5 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int WinningNumber6 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int BonusNumber { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Powerball { get; set; }
    
    public string? FromLast { get; set; }
    
    // Statistical fields based on CSV structure
    public int? OneToTen { get; set; }
    public int? ElevenToTwenty { get; set; }
    public int? TwentyOneToThirty { get; set; }
    public int? ThirtyOneToForty { get; set; }
    public int? Low { get; set; }
    public int? High { get; set; }
    public int? Odd { get; set; }
    public int? Even { get; set; }
    
    // Prize division fields
    public decimal? Division1Prize { get; set; }
    public int? Division1Winners { get; set; }
    public decimal? Division2Prize { get; set; }
    public int? Division2Winners { get; set; }
    public decimal? Division3Prize { get; set; }
    public int? Division3Winners { get; set; }
    public decimal? Division4Prize { get; set; }
    public int? Division4Winners { get; set; }
    public decimal? Division5Prize { get; set; }
    public int? Division5Winners { get; set; }
    public decimal? Division6Prize { get; set; }
    public int? Division6Winners { get; set; }
    public decimal? Division7Prize { get; set; }
    public int? Division7Winners { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}