using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Services;

public interface IDatabaseSeedingService
{
    Task SeedAsync();
}

public class DatabaseSeedingService : IDatabaseSeedingService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<DatabaseSeedingService> _logger;
    
    public DatabaseSeedingService(
        LottoDbContext context, 
        ILogger<DatabaseSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task SeedAsync()
    {
        try
        {
            // Check if NumberOccurrences need to be populated
            var hasDraws = await _context.LottoDraws.AnyAsync();
            var hasOccurrences = await _context.NumberOccurrences.AnyAsync();
            
            if (hasDraws && !hasOccurrences)
            {
                _logger.LogInformation("Populating NumberOccurrences from existing LottoDraws");
                await PopulateNumberOccurrencesAsync();
                return;
            }
            
            // Only seed if database is empty
            if (hasDraws)
            {
                _logger.LogInformation("Database already contains data, skipping seeding");
                return;
            }
            
            _logger.LogInformation("Seeding database with sample data");
            
            // Add some sample lottery draws for development
            var sampleDraws = new[]
            {
                new LottoDraw
                {
                    Draw = 1,
                    Date = DateTime.UtcNow.AddDays(-30),
                    WinningNumber1 = 5,
                    WinningNumber2 = 12,
                    WinningNumber3 = 18,
                    WinningNumber4 = 25,
                    WinningNumber5 = 33,
                    WinningNumber6 = 40,
                    BonusNumber = 7,
                    Powerball = 3,
                    OneToTen = 2,
                    ElevenToTwenty = 2,
                    TwentyOneToThirty = 1,
                    ThirtyOneToForty = 1
                },
                new LottoDraw
                {
                    Draw = 2,
                    Date = DateTime.UtcNow.AddDays(-23),
                    WinningNumber1 = 3,
                    WinningNumber2 = 15,
                    WinningNumber3 = 22,
                    WinningNumber4 = 28,
                    WinningNumber5 = 35,
                    WinningNumber6 = 39,
                    BonusNumber = 9,
                    Powerball = 5,
                    OneToTen = 1,
                    ElevenToTwenty = 1,
                    TwentyOneToThirty = 2,
                    ThirtyOneToForty = 2
                },
                new LottoDraw
                {
                    Draw = 3,
                    Date = DateTime.UtcNow.AddDays(-16),
                    WinningNumber1 = 8,
                    WinningNumber2 = 14,
                    WinningNumber3 = 21,
                    WinningNumber4 = 27,
                    WinningNumber5 = 31,
                    WinningNumber6 = 38,
                    BonusNumber = 4,
                    Powerball = 8,
                    OneToTen = 1,
                    ElevenToTwenty = 2,
                    TwentyOneToThirty = 2,
                    ThirtyOneToForty = 1
                }
            };
            
            await _context.LottoDraws.AddRangeAsync(sampleDraws);
            await _context.SaveChangesAsync(); // Save draws first to get IDs
            
            // Create NumberOccurrence records for each draw
            var numberOccurrences = new List<NumberOccurrence>();
            
            foreach (var draw in sampleDraws)
            {
                var numbers = new[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                                    draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 };
                
                // Add main numbers
                for (int i = 0; i < numbers.Length; i++)
                {
                    numberOccurrences.Add(new NumberOccurrence
                    {
                        DrawNumber = draw.Draw,
                        DrawDate = draw.Date,
                        Number = numbers[i],
                        Position = i + 1,
                        IsBonus = false,
                        IsPowerball = false
                    });
                }
                
                // Add bonus number
                numberOccurrences.Add(new NumberOccurrence
                {
                    DrawNumber = draw.Draw,
                    DrawDate = draw.Date,
                    Number = draw.BonusNumber,
                    Position = 7,
                    IsBonus = true,
                    IsPowerball = false
                });
                
                // Add powerball
                numberOccurrences.Add(new NumberOccurrence
                {
                    DrawNumber = draw.Draw,
                    DrawDate = draw.Date,
                    Number = draw.Powerball,
                    Position = 8,
                    IsBonus = false,
                    IsPowerball = true
                });
            }
            
            await _context.NumberOccurrences.AddRangeAsync(numberOccurrences);
            
            // Add some sample number combinations
            var sampleCombinations = new[]
            {
                new NumberCombination { Number1 = 1, Number2 = 7, Number3 = 14, Number4 = 21, Number5 = 28, Number6 = 35 },
                new NumberCombination { Number1 = 2, Number2 = 9, Number3 = 16, Number4 = 23, Number5 = 30, Number6 = 37 },
                new NumberCombination { Number1 = 6, Number2 = 13, Number3 = 20, Number4 = 27, Number5 = 34, Number6 = 40 }
            };
            
            await _context.NumberCombinations.AddRangeAsync(sampleCombinations);
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Database seeding completed successfully. Added {DrawCount} draws, {OccurrenceCount} number occurrences, and {CombinationCount} combinations",
                sampleDraws.Length, numberOccurrences.Count, sampleCombinations.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed");
            throw;
        }
    }
    
    private async Task PopulateNumberOccurrencesAsync()
    {
        try
        {
            _logger.LogInformation("Starting to populate NumberOccurrences from existing draws");
            
            var draws = await _context.LottoDraws.OrderBy(d => d.Draw).ToListAsync();
            var numberOccurrences = new List<NumberOccurrence>();
            
            foreach (var draw in draws)
            {
                var numbers = new[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                                    draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 };
                
                // Add main numbers
                for (int i = 0; i < numbers.Length; i++)
                {
                    numberOccurrences.Add(new NumberOccurrence
                    {
                        DrawNumber = draw.Draw,
                        DrawDate = draw.Date,
                        Number = numbers[i],
                        Position = i + 1,
                        IsBonus = false,
                        IsPowerball = false
                    });
                }
                
                // Add bonus number
                numberOccurrences.Add(new NumberOccurrence
                {
                    DrawNumber = draw.Draw,
                    DrawDate = draw.Date,
                    Number = draw.BonusNumber,
                    Position = 7,
                    IsBonus = true,
                    IsPowerball = false
                });
                
                // Add powerball
                numberOccurrences.Add(new NumberOccurrence
                {
                    DrawNumber = draw.Draw,
                    DrawDate = draw.Date,
                    Number = draw.Powerball,
                    Position = 8,
                    IsBonus = false,
                    IsPowerball = true
                });
            }
            
            await _context.NumberOccurrences.AddRangeAsync(numberOccurrences);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully populated {Count} NumberOccurrence records from {DrawCount} draws", 
                numberOccurrences.Count, draws.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating NumberOccurrences");
            throw;
        }
    }
}