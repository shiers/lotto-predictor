using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
    private readonly LottoDbContext _context;
    private readonly ILogger<TicketController> _logger;

    public TicketController(LottoDbContext context, ILogger<TicketController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Add a purchased ticket with its lines.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PurchasedTicket>> AddTicket([FromBody] AddTicketRequest request)
    {
        if (request.Lines == null || !request.Lines.Any())
            return BadRequest("At least one line is required");

        if (request.Lines.Count > 10)
            return BadRequest("Maximum 10 lines per ticket");

        var ticket = new PurchasedTicket
        {
            DrawNumber = request.DrawNumber,
            DrawDate = request.DrawDate.HasValue ? DateTime.SpecifyKind(request.DrawDate.Value, DateTimeKind.Utc) : null,
            TicketNumber = request.TicketNumber,
            Cost = request.Cost ?? 1.50m * request.Lines.Count,
            Source = request.Source ?? "Manual"
        };

        var labels = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
        for (int i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            var numbers = line.Numbers.OrderBy(n => n).ToArray();

            if (numbers.Length != 6 || numbers.Any(n => n < 1 || n > 40) || numbers.Distinct().Count() != 6)
                return BadRequest($"Line {i + 1}: must have exactly 6 unique numbers between 1-40");

            if (line.Powerball < 0 || line.Powerball > 10)
                return BadRequest($"Line {i + 1}: Powerball must be between 0-10 (0 = Lotto only)");

            ticket.Lines.Add(new PurchasedTicketLine
            {
                LineLabel = labels[i],
                Number1 = numbers[0],
                Number2 = numbers[1],
                Number3 = numbers[2],
                Number4 = numbers[3],
                Number5 = numbers[4],
                Number6 = numbers[5],
                Powerball = line.Powerball
            });
        }

        _context.PurchasedTickets.Add(ticket);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added ticket for draw {Draw} with {Lines} lines", request.DrawNumber, request.Lines.Count);

        // Auto-check if draw results are available
        await CheckTicketAgainstDraw(ticket);

        return Ok(ticket);
    }

    /// <summary>
    /// Get all tickets, optionally filtered by draw number.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTickets([FromQuery] int? drawNumber = null)
    {
        var query = _context.PurchasedTickets
            .Include(t => t.Lines)
            .OrderByDescending(t => t.DrawNumber)
            .AsQueryable();

        if (drawNumber.HasValue)
            query = query.Where(t => t.DrawNumber == drawNumber.Value);

        var tickets = await query.Take(50).ToListAsync();

        // Enrich with draw data for number highlighting
        var drawNumbers = tickets.Select(t => t.DrawNumber).Distinct().ToList();
        var draws = await _context.LottoDraws
            .Where(d => drawNumbers.Contains(d.Draw))
            .ToDictionaryAsync(d => d.Draw);

        var enrichedTickets = tickets.Select(t =>
        {
            var draw = draws.GetValueOrDefault(t.DrawNumber);
            return new
            {
                t.Id, t.DrawNumber, t.DrawDate, t.TicketNumber, t.Cost, t.Winnings, t.IsChecked, t.Source, t.CreatedAt,
                WinningNumbers = draw != null ? new[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 } : null,
                BonusNumber = draw?.BonusNumber,
                DrawPowerball = draw?.Powerball,
                Lines = t.Lines.Select(l => new
                {
                    l.Id, l.TicketId, l.LineLabel, l.Number1, l.Number2, l.Number3, l.Number4, l.Number5, l.Number6,
                    l.Powerball, l.MainMatches, l.BonusMatched, l.PowerballMatched, l.Division, l.Prize
                })
            };
        });

        return Ok(enrichedTickets);
    }

    /// <summary>
    /// Update the actual winnings for a ticket (override calculated amount).
    /// </summary>
    [HttpPatch("{id}/winnings")]
    public async Task<ActionResult> UpdateWinnings(int id, [FromBody] decimal actualWinnings)
    {
        var ticket = await _context.PurchasedTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();

        ticket.Winnings = actualWinnings;
        await _context.SaveChangesAsync();
        return Ok(new { message = $"Winnings updated to ${actualWinnings}" });
    }

    /// <summary>
    /// Check a ticket against draw results and calculate winnings.
    /// </summary>
    [HttpPost("{id}/check")]
    public async Task<ActionResult<PurchasedTicket>> CheckTicket(int id)
    {
        var ticket = await _context.PurchasedTickets
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound("Ticket not found");

        await CheckTicketAgainstDraw(ticket);
        return Ok(ticket);
    }

    /// <summary>
    /// Get a summary of all ticket performance.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetTicketSummary()
    {
        var tickets = await _context.PurchasedTickets
            .Include(t => t.Lines)
            .Where(t => t.IsChecked)
            .ToListAsync();

        if (!tickets.Any())
            return Ok(new { message = "No checked tickets found" });

        var totalCost = tickets.Sum(t => t.Cost);
        var totalWinnings = tickets.Sum(t => t.Winnings);
        var allLines = tickets.SelectMany(t => t.Lines).Where(l => l.MainMatches.HasValue).ToList();

        return Ok(new
        {
            TotalTickets = tickets.Count,
            TotalLines = allLines.Count,
            TotalCost = totalCost,
            TotalWinnings = totalWinnings,
            NetReturn = totalWinnings - totalCost,
            ROI = totalCost > 0 ? (double)((totalWinnings - totalCost) / totalCost * 100) : 0,
            WinningLines = allLines.Count(l => l.Prize > 0),
            MatchDistribution = new
            {
                Match0 = allLines.Count(l => l.MainMatches == 0),
                Match1 = allLines.Count(l => l.MainMatches == 1),
                Match2 = allLines.Count(l => l.MainMatches == 2),
                Match3 = allLines.Count(l => l.MainMatches == 3 && l.BonusMatched != true),
                Match3PlusBonus = allLines.Count(l => l.MainMatches == 3 && l.BonusMatched == true),
                Match4 = allLines.Count(l => l.MainMatches == 4 && l.BonusMatched != true),
                Match4PlusBonus = allLines.Count(l => l.MainMatches == 4 && l.BonusMatched == true),
                Match5 = allLines.Count(l => l.MainMatches == 5 && l.BonusMatched != true),
                Match5PlusBonus = allLines.Count(l => l.MainMatches == 5 && l.BonusMatched == true),
                Match6 = allLines.Count(l => l.MainMatches == 6)
            },
            AverageMatchesPerLine = allLines.Average(l => l.MainMatches ?? 0),
            BestResult = allLines.OrderByDescending(l => l.MainMatches).ThenByDescending(l => l.BonusMatched).FirstOrDefault()
        });
    }

    private async Task CheckTicketAgainstDraw(PurchasedTicket ticket)
    {
        var draw = await _context.LottoDraws.FirstOrDefaultAsync(d => d.Draw == ticket.DrawNumber);
        if (draw == null)
        {
            _logger.LogInformation("Draw {Draw} not yet available for checking", ticket.DrawNumber);
            return;
        }

        var winningNumbers = new[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
                                     draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 };

        decimal totalWinnings = 0;

        foreach (var line in ticket.Lines)
        {
            var lineNumbers = new[] { line.Number1, line.Number2, line.Number3, line.Number4, line.Number5, line.Number6 };
            var mainMatches = lineNumbers.Intersect(winningNumbers).Count();
            var bonusMatched = lineNumbers.Contains(draw.BonusNumber);
            var pbMatched = line.Powerball > 0 && line.Powerball == draw.Powerball; // 0 = not playing Powerball

            line.MainMatches = mainMatches;
            line.BonusMatched = bonusMatched;
            line.PowerballMatched = pbMatched;

            var (division, prize) = DeterminePrize(mainMatches, bonusMatched, draw);
            line.Division = division;
            line.Prize = prize;
            totalWinnings += prize;
        }

        ticket.Winnings = totalWinnings;
        ticket.IsChecked = true;
        ticket.DrawDate = draw.Date;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Checked ticket {Id} for draw {Draw}: won ${Winnings}", ticket.Id, ticket.DrawNumber, totalWinnings);
    }

    private (string division, decimal prize) DeterminePrize(int mainMatches, bool bonusMatched, Models.LottoDraw draw)
    {
        // Use actual prize data from the draw record when available
        return mainMatches switch
        {
            6 => ("Div1", draw.Division1Prize ?? 1_000_000m),
            5 when bonusMatched => ("Div2", draw.Division2Prize ?? 25_000m),
            5 => ("Div3", draw.Division3Prize ?? 1_000m),
            4 when bonusMatched => ("Div4", draw.Division4Prize ?? 100m),
            4 => ("Div5", draw.Division5Prize ?? 50m),
            3 when bonusMatched => ("Div6", draw.Division6Prize ?? 40m),
            3 => ("Div7", draw.Division7Prize ?? 16.50m),
            _ => ("None", 0m)
        };
    }
}

public class AddTicketRequest
{
    public int DrawNumber { get; set; }
    public DateTime? DrawDate { get; set; }
    public string? TicketNumber { get; set; }
    public decimal? Cost { get; set; }
    public string? Source { get; set; }
    public List<AddTicketLineRequest> Lines { get; set; } = new();
}

public class AddTicketLineRequest
{
    public int[] Numbers { get; set; } = Array.Empty<int>();
    public int Powerball { get; set; }
}
