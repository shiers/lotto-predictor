using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PredictLottoNZ.Models;

[Table("PurchasedTickets")]
public class PurchasedTicket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>The draw number this ticket is for</summary>
    [Required]
    public int DrawNumber { get; set; }

    /// <summary>Draw date</summary>
    public DateTime? DrawDate { get; set; }

    /// <summary>Ticket number from the physical ticket</summary>
    public string? TicketNumber { get; set; }

    /// <summary>Cost of the ticket</summary>
    public decimal Cost { get; set; } = 6.00m; // Default NZ Lotto 4-line ticket

    /// <summary>Total winnings from this ticket</summary>
    public decimal Winnings { get; set; }

    /// <summary>Whether results have been checked against the draw</summary>
    public bool IsChecked { get; set; }

    /// <summary>Source of the ticket (manual pick, system generated, AI predicted)</summary>
    public string Source { get; set; } = "Manual";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PurchasedTicketLine> Lines { get; set; } = new List<PurchasedTicketLine>();
}

[Table("PurchasedTicketLines")]
public class PurchasedTicketLine
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int TicketId { get; set; }

    /// <summary>Line label (A, B, C, D)</summary>
    public string LineLabel { get; set; } = "";

    [Required] [Range(1, 40)] public int Number1 { get; set; }
    [Required] [Range(1, 40)] public int Number2 { get; set; }
    [Required] [Range(1, 40)] public int Number3 { get; set; }
    [Required] [Range(1, 40)] public int Number4 { get; set; }
    [Required] [Range(1, 40)] public int Number5 { get; set; }
    [Required] [Range(1, 40)] public int Number6 { get; set; }
    [Range(0, 10)] public int Powerball { get; set; } // 0 = Lotto only (no Powerball)

    // Results (populated after checking)
    public int? MainMatches { get; set; }
    public bool? BonusMatched { get; set; }
    public bool? PowerballMatched { get; set; }
    public string? Division { get; set; }
    public decimal? Prize { get; set; }

    // Navigation
    [ForeignKey("TicketId")]
    [JsonIgnore]
    public PurchasedTicket? Ticket { get; set; }
}
