using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface ILottoImportService
{
    Task<ImportResult> ImportAsync(Stream csvStream);
}

public class LottoImportService : ILottoImportService
{
    private readonly LottoDbContext _context;
    private readonly ICsvParsingService _csvParsingService;
    private readonly IPredictionScoreUpdateService _scoreUpdateService;
    private readonly IPredictionMatchingService _predictionMatchingService;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<LottoImportService> _logger;

    public LottoImportService(
        LottoDbContext context,
        ICsvParsingService csvParsingService,
        IPredictionScoreUpdateService scoreUpdateService,
        IPredictionMatchingService predictionMatchingService,
        IPredictionService predictionService,
        ILogger<LottoImportService> logger)
    {
        _context = context;
        _csvParsingService = csvParsingService;
        _scoreUpdateService = scoreUpdateService;
        _predictionMatchingService = predictionMatchingService;
        _predictionService = predictionService;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(Stream csvStream)
    {
        var result = new ImportResult();
        var importedDraws = new List<LottoDraw>();
        
        try
        {
            _logger.LogInformation("Starting CSV import process");
            
            // Parse CSV data
            var parsedDraws = await _csvParsingService.ParseCsvAsync(csvStream);
            var drawsList = parsedDraws.ToList();
            
            if (!drawsList.Any())
            {
                _logger.LogWarning("No valid draws found in CSV file after parsing");
                result.Errors.Add("No valid lottery draws found in the CSV file. Please check the file format and ensure it contains the required columns: Draw, Date, WinningNumber1-6, BonusNumber, Powerball");
                return result;
            }

            _logger.LogInformation("Parsed {Count} draws from CSV", drawsList.Count);

            // Get existing draw numbers for duplicate detection
            var drawNumbers = drawsList.Select(d => d.Draw).ToHashSet();
            var existingDrawNumbers = (await _context.LottoDraws
                .Where(d => drawNumbers.Contains(d.Draw))
                .Select(d => d.Draw)
                .ToListAsync())
                .ToHashSet();

            _logger.LogInformation("Found {ExistingCount} existing draws out of {TotalCount}", 
                existingDrawNumbers.Count, drawNumbers.Count);

            // Process draws in batches
            const int batchSize = 100;
            var batches = drawsList
                .Select((draw, index) => new { draw, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.draw).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                var batchImportedDraws = await ProcessBatch(batch, existingDrawNumbers, result);
                importedDraws.AddRange(batchImportedDraws);
            }

            // Trigger prediction score updates and matching for newly imported draws
            if (importedDraws.Any())
            {
                _logger.LogInformation("Processing {Count} newly imported draws for prediction updates", 
                    importedDraws.Count);
                
                // First, check for prediction matches
                try
                {
                    var matchingResult = await _predictionMatchingService.CheckAndUpdateMatchesAsync(importedDraws);
                    
                    _logger.LogInformation("Prediction matching completed: {Checked} predictions checked, {Matches} matches found",
                        matchingResult.TotalPredictionsChecked, matchingResult.MatchesFound);
                    
                    // Add matching info to import result
                    if (matchingResult.Errors.Any())
                    {
                        result.Errors.AddRange(matchingResult.Errors.Select(e => $"Prediction Matching: {e}"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during prediction matching after import");
                    result.Errors.Add($"Import successful but prediction matching failed: {ex.Message}");
                }
                
                // Then, trigger prediction score updates
                try
                {
                    var scoreUpdateResult = await _scoreUpdateService.UpdateScoresAfterDrawImportAsync(importedDraws);
                    
                    _logger.LogInformation("Score update completed: {Processed} predictions processed, {Updated} updated",
                        scoreUpdateResult.TotalPredictionsProcessed, scoreUpdateResult.PredictionsUpdated);
                    
                    // Add score update info to import result
                    if (scoreUpdateResult.Errors.Any())
                    {
                        result.Errors.AddRange(scoreUpdateResult.Errors.Select(e => $"Score Update: {e}"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during prediction score updates after import");
                    result.Errors.Add($"Import successful but score updates failed: {ex.Message}");
                }
                
                // Generate new predictions for future draws after importing new data
                try
                {
                    _logger.LogInformation("Generating new predictions after importing {Count} new draws", importedDraws.Count);
                    
                    // Generate a reasonable number of predictions (5-10) for the next draws
                    const int predictionsToGenerate = 5;
                    var newPredictions = await _predictionService.GeneratePredictionsAsync(predictionsToGenerate);
                    var predictionsList = newPredictions.ToList();
                    
                    _logger.LogInformation("Successfully generated {Count} new predictions after data import", predictionsList.Count);
                    
                    // If we generated predictions, run score updates again to process them against the new draws
                    if (predictionsList.Any())
                    {
                        _logger.LogInformation("Running score updates for newly generated predictions");
                        var newScoreUpdateResult = await _scoreUpdateService.UpdateScoresAfterDrawImportAsync(importedDraws);
                        
                        _logger.LogInformation("New predictions score update completed: {Processed} predictions processed, {Updated} updated",
                            newScoreUpdateResult.TotalPredictionsProcessed, newScoreUpdateResult.PredictionsUpdated);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating predictions after import - this is not critical for data import");
                    result.Errors.Add($"Import successful but prediction generation failed: {ex.Message}");
                }
                
                // TODO: Add cache invalidation here after fixing dependency injection issue
                // try
                // {
                //     _logger.LogInformation("Invalidating cache due to {Count} new draws imported", importedDraws.Count);
                //     await _cacheInvalidationService.InvalidateAllAsync();
                //     _logger.LogInformation("Cache invalidation completed successfully");
                // }
                // catch (Exception ex)
                // {
                //     _logger.LogError(ex, "Error during cache invalidation after import");
                //     result.Errors.Add($"Import successful but cache invalidation failed: {ex.Message}");
                // }
            }

            _logger.LogInformation("Import completed. Added: {Added}, Skipped: {Skipped}, Errors: {Errors}", 
                result.RecordsAdded, result.RecordsSkipped, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during CSV import");
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    private async Task<List<LottoDraw>> ProcessBatch(
        List<LottoDraw> batch, 
        HashSet<int> existingDrawNumbers, 
        ImportResult result)
    {
        var drawsToAdd = new List<LottoDraw>();
        var importedDraws = new List<LottoDraw>();

        foreach (var draw in batch)
        {
            try
            {
                // Check for duplicates
                if (existingDrawNumbers.Contains(draw.Draw))
                {
                    result.RecordsSkipped++;
                    _logger.LogDebug("Skipping duplicate draw {DrawNumber}", draw.Draw);
                    continue;
                }

                // Validate the draw data
                if (!ValidateLottoDraw(draw, result))
                {
                    continue;
                }

                // Set timestamps (ensure UTC)
                draw.CreatedAt = DateTime.UtcNow;
                draw.UpdatedAt = DateTime.UtcNow;

                drawsToAdd.Add(draw);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing draw {DrawNumber}", draw.Draw);
                result.Errors.Add($"Error processing draw {draw.Draw}: {ex.Message}");
                result.RecordsSkipped++;
            }
        }

        // Batch insert new draws
        if (drawsToAdd.Any())
        {
            try
            {
                _context.LottoDraws.AddRange(drawsToAdd);
                await _context.SaveChangesAsync();
                
                result.RecordsAdded += drawsToAdd.Count;
                importedDraws.AddRange(drawsToAdd);
                
                // Update existing draw numbers set to prevent duplicates in subsequent batches
                foreach (var draw in drawsToAdd)
                {
                    existingDrawNumbers.Add(draw.Draw);
                }
                
                _logger.LogDebug("Successfully added batch of {Count} draws", drawsToAdd.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save batch of {Count} draws", drawsToAdd.Count);
                result.Errors.Add($"Database error: {ex.Message}");
                result.RecordsSkipped += drawsToAdd.Count;
            }
        }

        return importedDraws;
    }

    private bool ValidateLottoDraw(LottoDraw draw, ImportResult result)
    {
        var errors = new List<string>();

        // Validate draw number
        if (draw.Draw <= 0)
        {
            errors.Add("Draw number must be positive");
        }

        // Validate date
        if (draw.Date == default)
        {
            errors.Add("Date is required");
        }
        else if (draw.Date > DateTime.UtcNow.AddDays(1))
        {
            errors.Add("Date cannot be in the future");
        }

        // Validate winning numbers (1-40 range)
        var winningNumbers = new[] 
        { 
            draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3,
            draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 
        };

        for (int i = 0; i < winningNumbers.Length; i++)
        {
            if (winningNumbers[i] < 1 || winningNumbers[i] > 40)
            {
                errors.Add($"Winning number {i + 1} must be between 1 and 40");
            }
        }

        // Check for duplicate winning numbers
        if (winningNumbers.Distinct().Count() != winningNumbers.Length)
        {
            errors.Add("Winning numbers must be unique");
        }

        // Validate bonus number (1-40 range)
        if (draw.BonusNumber < 1 || draw.BonusNumber > 40)
        {
            errors.Add("Bonus number must be between 1 and 40");
        }

        // Validate powerball (1-10 range)
        if (draw.Powerball < 1 || draw.Powerball > 10)
        {
            errors.Add("Powerball must be between 1 and 10");
        }

        // Validate prize amounts (if present)
        var prizeFields = new[]
        {
            draw.Division1Prize, draw.Division2Prize, draw.Division3Prize,
            draw.Division4Prize, draw.Division5Prize, draw.Division6Prize, draw.Division7Prize
        };

        foreach (var prize in prizeFields.Where(p => p.HasValue))
        {
            if (prize < 0)
            {
                errors.Add("Prize amounts cannot be negative");
                break;
            }
        }

        // Validate winner counts (if present)
        var winnerFields = new[]
        {
            draw.Division1Winners, draw.Division2Winners, draw.Division3Winners,
            draw.Division4Winners, draw.Division5Winners, draw.Division6Winners, draw.Division7Winners
        };

        foreach (var winners in winnerFields.Where(w => w.HasValue))
        {
            if (winners < 0)
            {
                errors.Add("Winner counts cannot be negative");
                break;
            }
        }

        // Validate statistical fields (if present)
        var statisticalFields = new[]
        {
            draw.OneToTen, draw.ElevenToTwenty, 
            draw.TwentyOneToThirty, draw.ThirtyOneToForty
        };

        foreach (var stat in statisticalFields.Where(s => s.HasValue))
        {
            if (stat < 0 || stat > 6)
            {
                errors.Add("Statistical field values must be between 0 and 6");
                break;
            }
        }

        if (errors.Any())
        {
            var errorMessage = $"Draw {draw.Draw} validation failed: {string.Join(", ", errors)}";
            _logger.LogWarning(errorMessage);
            result.Errors.Add(errorMessage);
            result.RecordsSkipped++;
            return false;
        }

        return true;
    }
}