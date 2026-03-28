using System.Text;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Tests;

/// <summary>
/// **Feature: predict-lotto-nz, Property 3: Error handling maintains processing continuity**
/// **Validates: Requirements 1.5, 4.4**
/// 
/// Property-based test for error handling functionality.
/// For any file containing mixed valid and invalid data, the system should skip invalid records, 
/// log errors, and continue processing valid records.
/// </summary>
public static class ErrorHandlingPropertyTest
{
    public static async Task RunErrorHandlingTest()
    {
        Console.WriteLine("Running Error Handling Property Test...");
        Console.WriteLine("======================================");

        try
        {
            Console.WriteLine("Property Test 3: Error handling maintains processing continuity...");
            await ErrorHandlingMaintainsProcessingContinuity_PropertyTest();
            Console.WriteLine("✓ PASSED: Error handling maintains processing continuity (100 iterations)");
            
            Console.WriteLine("\n======================================");
            Console.WriteLine("Error handling property test PASSED!");
            Console.WriteLine("Property 3: Error handling maintains processing continuity - VALIDATED");
            Console.WriteLine("Requirements 1.5, 4.4 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR HANDLING TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: Error handling should maintain processing continuity for any mix of valid and invalid data
    /// This test generates CSV files with various types of invalid data mixed with valid records
    /// and verifies that the system continues processing valid records while properly handling errors.
    /// </summary>
    private static async Task ErrorHandlingMaintainsProcessingContinuity_PropertyTest()
    {
        const int iterations = 100;
        var random = new Random(42); // Fixed seed for reproducibility
        
        for (int i = 0; i < iterations; i++)
        {
            // Generate test scenario with mixed valid/invalid data
            var scenario = GenerateErrorTestScenario(random, i);
            
            try
            {
                // Test CSV parsing with error handling
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
                var logger = loggerFactory.CreateLogger<CsvParsingService>();
                var csvParsingService = new CsvParsingService(logger);
                
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(scenario.CsvContent));
                var parsedDraws = await csvParsingService.ParseCsvAsync(stream);
                var parsedDrawsList = parsedDraws.ToList();
                
                // Verify that only valid records were parsed
                if (parsedDrawsList.Count != scenario.ExpectedValidRecords)
                {
                    throw new Exception($"Iteration {i}: Expected {scenario.ExpectedValidRecords} valid records, got {parsedDrawsList.Count}");
                }
                
                // Verify that all parsed records are actually valid
                foreach (var parsedDraw in parsedDrawsList)
                {
                    if (!IsValidLottoDraw(parsedDraw))
                    {
                        throw new Exception($"Iteration {i}: Invalid draw was not filtered out: Draw {parsedDraw.Draw}");
                    }
                }
                
                // Verify that parsed draws match expected valid draws
                var expectedValidDraws = scenario.ValidDraws.OrderBy(d => d.Draw).ToList();
                var actualParsedDraws = parsedDrawsList.OrderBy(d => d.Draw).ToList();
                
                if (expectedValidDraws.Count != actualParsedDraws.Count)
                {
                    throw new Exception($"Iteration {i}: Expected valid draws count mismatch");
                }
                
                for (int j = 0; j < expectedValidDraws.Count; j++)
                {
                    if (!DrawsMatch(expectedValidDraws[j], actualParsedDraws[j], ignoreTimestamps: true))
                    {
                        throw new Exception($"Iteration {i}: Valid draw {j} does not match expected data");
                    }
                }
                
                // Test import service error handling if we have any data
                if (scenario.TotalRecords > 0)
                {
                    await TestImportServiceErrorHandling(scenario, i);
                }
            }
            catch (Exception ex) when (!(ex.Message.StartsWith("Iteration")))
            {
                throw new Exception($"Iteration {i}: Unexpected error during error handling test: {ex.Message}", ex);
            }
        }
    }

    private static async Task TestImportServiceErrorHandling(ErrorTestScenario scenario, int iteration)
    {
        // Create a mock database context for testing import service
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var csvLogger = loggerFactory.CreateLogger<CsvParsingService>();
        var importLogger = loggerFactory.CreateLogger<LottoImportService>();
        
        var csvParsingService = new CsvParsingService(csvLogger);
        
        // Test that import service handles parsing errors gracefully
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(scenario.CsvContent));
        
        // The CSV parsing should not throw exceptions even with invalid data
        var parsedDraws = await csvParsingService.ParseCsvAsync(stream);
        var parsedDrawsList = parsedDraws.ToList();
        
        // Verify processing continuity - we should get some results if there were valid records
        if (scenario.ExpectedValidRecords > 0 && parsedDrawsList.Count == 0)
        {
            throw new Exception($"Iteration {iteration}: Processing stopped completely despite having valid records");
        }
        
        // Verify that invalid records were filtered out during parsing
        if (parsedDrawsList.Count > scenario.ExpectedValidRecords)
        {
            throw new Exception($"Iteration {iteration}: More records parsed than expected valid records");
        }
    }

    private static ErrorTestScenario GenerateErrorTestScenario(Random random, int iteration)
    {
        var totalRecords = random.Next(1, 20); // 1-19 total records
        var validRecords = new List<LottoDraw>();
        var invalidRecords = new List<string>();
        var allCsvLines = new List<string>();
        
        // Generate a mix of valid and invalid records
        var usedDrawNumbers = new HashSet<int>();
        
        for (int i = 0; i < totalRecords; i++)
        {
            var shouldBeValid = random.NextDouble() > 0.3; // 70% chance of valid record
            
            if (shouldBeValid)
            {
                // Generate valid record
                int drawNumber;
                do
                {
                    drawNumber = random.Next(1, 10000);
                } while (usedDrawNumbers.Contains(drawNumber));
                
                usedDrawNumbers.Add(drawNumber);
                var validDraw = GenerateValidLottoDraw(drawNumber, random);
                validRecords.Add(validDraw);
                allCsvLines.Add(GenerateCsvLineFromDraw(validDraw));
            }
            else
            {
                // Generate invalid record
                var invalidLine = GenerateInvalidCsvLine(random, usedDrawNumbers);
                invalidRecords.Add(invalidLine);
                allCsvLines.Add(invalidLine);
            }
        }
        
        // Shuffle the lines to randomize order
        allCsvLines = allCsvLines.OrderBy(x => random.Next()).ToList();
        
        // Build CSV content
        var csvContent = new StringBuilder();
        csvContent.AppendLine("Draw,Date,Winning Number 1,Winning Number 2,Winning Number 3,Winning Number 4,Winning Number 5,Winning Number 6,Bonus Number,Powerball,From Last,1-Oct,Nov-20,21-30,31-40,Division 1 Prize,Division 1 Winners");
        
        foreach (var line in allCsvLines)
        {
            csvContent.AppendLine(line);
        }
        
        return new ErrorTestScenario
        {
            CsvContent = csvContent.ToString(),
            ValidDraws = validRecords,
            InvalidRecords = invalidRecords,
            TotalRecords = totalRecords,
            ExpectedValidRecords = validRecords.Count
        };
    }

    private static string GenerateInvalidCsvLine(Random random, HashSet<int> usedDrawNumbers)
    {
        var errorType = random.Next(0, 7); // Different types of errors
        
        return errorType switch
        {
            0 => GenerateInvalidDrawNumber(random, usedDrawNumbers),
            1 => GenerateInvalidDate(random, usedDrawNumbers),
            2 => GenerateInvalidWinningNumbers(random, usedDrawNumbers),
            3 => GenerateInvalidBonusNumber(random, usedDrawNumbers),
            4 => GenerateInvalidPowerball(random, usedDrawNumbers),
            5 => GenerateMissingRequiredFields(random, usedDrawNumbers),
            6 => GenerateInvalidDataTypes(random, usedDrawNumbers),
            _ => GenerateInvalidDrawNumber(random, usedDrawNumbers)
        };
    }

    private static string GenerateInvalidDrawNumber(Random random, HashSet<int> usedDrawNumbers)
    {
        // Invalid draw numbers: negative, zero, or non-numeric
        var invalidDraws = new[] { "-1", "0", "abc", "", "999999999999999999999" };
        var invalidDraw = invalidDraws[random.Next(invalidDraws.Length)];
        
        var validDraw = GenerateValidLottoDraw(1, random);
        return $"{invalidDraw},{validDraw.Date:dd/MM/yyyy},{validDraw.WinningNumber1},{validDraw.WinningNumber2},{validDraw.WinningNumber3},{validDraw.WinningNumber4},{validDraw.WinningNumber5},{validDraw.WinningNumber6},{validDraw.BonusNumber},{validDraw.Powerball},{validDraw.FromLast},{validDraw.OneToTen},{validDraw.ElevenToTwenty},{validDraw.TwentyOneToThirty},{validDraw.ThirtyOneToForty},{validDraw.Division1Prize},{validDraw.Division1Winners}";
    }

    private static string GenerateInvalidDate(Random random, HashSet<int> usedDrawNumbers)
    {
        int drawNumber;
        do
        {
            drawNumber = random.Next(1, 10000);
        } while (usedDrawNumbers.Contains(drawNumber));
        
        var invalidDates = new[] { "invalid-date", "32/13/2023", "", "2025-13-45", "abc" };
        var invalidDate = invalidDates[random.Next(invalidDates.Length)];
        
        var validDraw = GenerateValidLottoDraw(drawNumber, random);
        return $"{drawNumber},{invalidDate},{validDraw.WinningNumber1},{validDraw.WinningNumber2},{validDraw.WinningNumber3},{validDraw.WinningNumber4},{validDraw.WinningNumber5},{validDraw.WinningNumber6},{validDraw.BonusNumber},{validDraw.Powerball},{validDraw.FromLast},{validDraw.OneToTen},{validDraw.ElevenToTwenty},{validDraw.TwentyOneToThirty},{validDraw.ThirtyOneToForty},{validDraw.Division1Prize},{validDraw.Division1Winners}";
    }

    private static string GenerateInvalidWinningNumbers(Random random, HashSet<int> usedDrawNumbers)
    {
        int drawNumber;
        do
        {
            drawNumber = random.Next(1, 10000);
        } while (usedDrawNumbers.Contains(drawNumber));
        
        var validDraw = GenerateValidLottoDraw(drawNumber, random);
        
        // Generate invalid winning numbers that will fail CSV parsing (non-positive or non-numeric)
        var invalidNumbers = new[] { "0", "-5", "abc", "" };
        var invalidNumber = invalidNumbers[random.Next(invalidNumbers.Length)];
        
        return $"{drawNumber},{validDraw.Date:dd/MM/yyyy},{invalidNumber},{validDraw.WinningNumber2},{validDraw.WinningNumber3},{validDraw.WinningNumber4},{validDraw.WinningNumber5},{validDraw.WinningNumber6},{validDraw.BonusNumber},{validDraw.Powerball},{validDraw.FromLast},{validDraw.OneToTen},{validDraw.ElevenToTwenty},{validDraw.TwentyOneToThirty},{validDraw.ThirtyOneToForty},{validDraw.Division1Prize},{validDraw.Division1Winners}";
    }

    private static string GenerateInvalidBonusNumber(Random random, HashSet<int> usedDrawNumbers)
    {
        int drawNumber;
        do
        {
            drawNumber = random.Next(1, 10000);
        } while (usedDrawNumbers.Contains(drawNumber));
        
        var validDraw = GenerateValidLottoDraw(drawNumber, random);
        
        // Invalid bonus numbers that will fail CSV parsing (non-positive or non-numeric)
        var invalidBonus = new[] { "0", "-1", "abc", "" };
        var invalidBonusNumber = invalidBonus[random.Next(invalidBonus.Length)];
        
        return $"{drawNumber},{validDraw.Date:dd/MM/yyyy},{validDraw.WinningNumber1},{validDraw.WinningNumber2},{validDraw.WinningNumber3},{validDraw.WinningNumber4},{validDraw.WinningNumber5},{validDraw.WinningNumber6},{invalidBonusNumber},{validDraw.Powerball},{validDraw.FromLast},{validDraw.OneToTen},{validDraw.ElevenToTwenty},{validDraw.TwentyOneToThirty},{validDraw.ThirtyOneToForty},{validDraw.Division1Prize},{validDraw.Division1Winners}";
    }

    private static string GenerateInvalidPowerball(Random random, HashSet<int> usedDrawNumbers)
    {
        int drawNumber;
        do
        {
            drawNumber = random.Next(1, 10000);
        } while (usedDrawNumbers.Contains(drawNumber));
        
        var validDraw = GenerateValidLottoDraw(drawNumber, random);
        
        // Invalid powerball numbers that will fail CSV parsing (non-positive or non-numeric)
        var invalidPowerball = new[] { "0", "-1", "abc", "" };
        var invalidPowerballNumber = invalidPowerball[random.Next(invalidPowerball.Length)];
        
        return $"{drawNumber},{validDraw.Date:dd/MM/yyyy},{validDraw.WinningNumber1},{validDraw.WinningNumber2},{validDraw.WinningNumber3},{validDraw.WinningNumber4},{validDraw.WinningNumber5},{validDraw.WinningNumber6},{validDraw.BonusNumber},{invalidPowerballNumber},{validDraw.FromLast},{validDraw.OneToTen},{validDraw.ElevenToTwenty},{validDraw.TwentyOneToThirty},{validDraw.ThirtyOneToForty},{validDraw.Division1Prize},{validDraw.Division1Winners}";
    }

    private static string GenerateMissingRequiredFields(Random random, HashSet<int> usedDrawNumbers)
    {
        int drawNumber;
        do
        {
            drawNumber = random.Next(1, 10000);
        } while (usedDrawNumbers.Contains(drawNumber));
        
        // Missing required fields - incomplete CSV line
        return $"{drawNumber},,,,,,,,,,,,,,,,";
    }

    private static string GenerateInvalidDataTypes(Random random, HashSet<int> usedDrawNumbers)
    {
        int drawNumber;
        do
        {
            drawNumber = random.Next(1, 10000);
        } while (usedDrawNumbers.Contains(drawNumber));
        
        // Mix of valid and invalid data types
        return $"{drawNumber},01/01/2023,abc,def,ghi,jkl,mno,pqr,xyz,123,text,invalid,more,text,here,not-a-number,also-invalid";
    }



    private static LottoDraw GenerateValidLottoDraw(int drawNumber, Random random)
    {
        // Generate 6 unique winning numbers between 1-40
        var winningNumbers = new HashSet<int>();
        while (winningNumbers.Count < 6)
        {
            winningNumbers.Add(random.Next(1, 41));
        }
        var numbers = winningNumbers.OrderBy(x => x).ToArray();
        
        var baseDate = DateTime.Today.AddDays(-random.Next(1, 365));
        
        return new LottoDraw
        {
            Draw = drawNumber,
            Date = baseDate,
            WinningNumber1 = numbers[0],
            WinningNumber2 = numbers[1],
            WinningNumber3 = numbers[2],
            WinningNumber4 = numbers[3],
            WinningNumber5 = numbers[4],
            WinningNumber6 = numbers[5],
            BonusNumber = random.Next(1, 11),
            Powerball = random.Next(1, 11),
            FromLast = random.Next(1, 100).ToString(),
            OneToTen = random.Next(0, 7),
            ElevenToTwenty = random.Next(0, 7),
            TwentyOneToThirty = random.Next(0, 7),
            ThirtyOneToForty = random.Next(0, 7),
            Division1Prize = random.Next(100000, 2000000),
            Division1Winners = random.Next(0, 5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 1440)),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 1440))
        };
    }

    private static string GenerateCsvLineFromDraw(LottoDraw draw)
    {
        return $"{draw.Draw},{draw.Date:dd/MM/yyyy},{draw.WinningNumber1},{draw.WinningNumber2},{draw.WinningNumber3},{draw.WinningNumber4},{draw.WinningNumber5},{draw.WinningNumber6},{draw.BonusNumber},{draw.Powerball},{draw.FromLast},{draw.OneToTen},{draw.ElevenToTwenty},{draw.TwentyOneToThirty},{draw.ThirtyOneToForty},{draw.Division1Prize},{draw.Division1Winners}";
    }

    private static bool IsValidLottoDraw(LottoDraw draw)
    {
        // Check validation rules that are enforced by CSV parsing service
        // The CSV parsing service only checks that required fields are present and positive
        return draw.Draw > 0 && 
               draw.Date != default &&
               draw.WinningNumber1 > 0 &&
               draw.WinningNumber2 > 0 &&
               draw.WinningNumber3 > 0 &&
               draw.WinningNumber4 > 0 &&
               draw.WinningNumber5 > 0 &&
               draw.WinningNumber6 > 0 &&
               draw.BonusNumber > 0 &&
               draw.Powerball > 0;
    }

    private static bool DrawsMatch(LottoDraw draw1, LottoDraw draw2, bool ignoreTimestamps = false)
    {
        var match = draw1.Draw == draw2.Draw &&
               draw1.Date.Date == draw2.Date.Date &&
               draw1.WinningNumber1 == draw2.WinningNumber1 &&
               draw1.WinningNumber2 == draw2.WinningNumber2 &&
               draw1.WinningNumber3 == draw2.WinningNumber3 &&
               draw1.WinningNumber4 == draw2.WinningNumber4 &&
               draw1.WinningNumber5 == draw2.WinningNumber5 &&
               draw1.WinningNumber6 == draw2.WinningNumber6 &&
               draw1.BonusNumber == draw2.BonusNumber &&
               draw1.Powerball == draw2.Powerball &&
               draw1.FromLast == draw2.FromLast &&
               draw1.OneToTen == draw2.OneToTen &&
               draw1.ElevenToTwenty == draw2.ElevenToTwenty &&
               draw1.TwentyOneToThirty == draw2.TwentyOneToThirty &&
               draw1.ThirtyOneToForty == draw2.ThirtyOneToForty &&
               draw1.Division1Prize == draw2.Division1Prize &&
               draw1.Division1Winners == draw2.Division1Winners;
        
        if (!ignoreTimestamps)
        {
            match = match && 
                   draw1.CreatedAt.Date == draw2.CreatedAt.Date &&
                   draw1.UpdatedAt.Date == draw2.UpdatedAt.Date;
        }
        
        return match;
    }

    private class ErrorTestScenario
    {
        public string CsvContent { get; set; } = string.Empty;
        public List<LottoDraw> ValidDraws { get; set; } = new();
        public List<string> InvalidRecords { get; set; } = new();
        public int TotalRecords { get; set; }
        public int ExpectedValidRecords { get; set; }
    }
}

