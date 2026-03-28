using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PredictLottoNZ.Models;

[Table("Predictions")]
public class Prediction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 40)]
    public int Number1 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number2 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number3 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number4 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number5 { get; set; }
    
    [Required]
    [Range(1, 40)]
    public int Number6 { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Powerball { get; set; }
    
    public double? Score { get; set; }
    
    // Track if this prediction matched a draw
    public bool IsMatched { get; set; } = false;
    
    // Reference to the draw that matched this prediction
    public int? MatchedDrawId { get; set; }
    
    // When the match was detected
    public DateTime? MatchedAt { get; set; }
    
    public double? UpdatedScore { get; set; }
    
    public DateTime? LastScoreUpdate { get; set; }
    
    public double? ConfidenceScore { get; set; }
    
    [Column(TypeName = "text")]
    public string ReasoningExplanation { get; set; } = string.Empty;
    
    public DateTime? TargetDrawDate { get; set; }
    
    // Store raw request/response payloads for training data
    [Column(TypeName = "text")]
    public string? RawRequestPayload { get; set; }
    
    [Column(TypeName = "text")]
    public string? RawResponsePayload { get; set; }
    
    // Helper method to get numbers as array (excluding Powerball)
    public int[] GetNumbers() => new[] { Number1, Number2, Number3, Number4, Number5, Number6 };
    
    // Helper method to get all numbers including Powerball
    public int[] GetAllNumbers() => new[] { Number1, Number2, Number3, Number4, Number5, Number6, Powerball };
    
    // Helper method to set numbers from array (excluding Powerball)
    public void SetNumbers(int[] numbers)
    {
        if (numbers?.Length != 6)
            throw new ArgumentException("Must provide exactly 6 numbers", nameof(numbers));
            
        Number1 = numbers[0];
        Number2 = numbers[1];
        Number3 = numbers[2];
        Number4 = numbers[3];
        Number5 = numbers[4];
        Number6 = numbers[5];
    }
    
    // Helper method to set all numbers including Powerball
    public void SetAllNumbers(int[] numbers)
    {
        if (numbers?.Length != 7)
            throw new ArgumentException("Must provide exactly 7 numbers (6 main + 1 Powerball)", nameof(numbers));
            
        Number1 = numbers[0];
        Number2 = numbers[1];
        Number3 = numbers[2];
        Number4 = numbers[3];
        Number5 = numbers[4];
        Number6 = numbers[5];
        Powerball = numbers[6];
    }
}