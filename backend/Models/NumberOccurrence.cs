using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("NumberOccurrences")]
public class NumberOccurrence
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int DrawNumber { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number { get; set; }
    
    [Required]
    [Range(1, 8)]
    public int Position { get; set; } // 1-6 for main numbers, 7 for bonus, 8 for powerball
    
    [Required]
    public DateTime DrawDate { get; set; }
    
    public bool IsBonus { get; set; }
    
    public bool IsPowerball { get; set; }
    
    // Navigation property
    public virtual LottoDraw Draw { get; set; } = null!;
}