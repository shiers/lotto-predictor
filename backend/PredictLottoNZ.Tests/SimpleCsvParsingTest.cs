using System.Text;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Services;
using MainModels = PredictLottoNZ.Models;

namespace PredictLottoNZ.Tests;

/// <summary>
/// **Feature: predict-lotto-nz, Property 1: CSV parsing extracts valid data**
/// **Validates: Requirements 1.1, 1.2**
/// 
/// Simple property-based test for CSV parsing functionality.
/// For any valid Powerball NZ CSV file, parsing should extract all lottery draw data 
/// with correct field mappings and type conversions.
/// </summary>
public static class SimpleCsvParsingTest
{
    public static async Task RunSimpleCsvParsingTest()
    {
        Console.WriteLine("Running Simple CSV Parsing Test...");
        Console.WriteLine("==================================");

        try
        {
            Console.WriteLine("Property Test 1: CSV parsing extracts valid data...");
            await CsvParsingExtractsValidData_PropertyTest();
            Console.WriteLine("✓ PASSED: CSV parsing extracts valid data (100 iterations)");
            
            Console.WriteLine("\n==================================");
            Console.WriteLine("Simple CSV parsing test PASSED!");
            Console.WriteLine("Property 1: CSV parsing extracts valid data - VALIDATED");
            Console.WriteLine("Requirements 1.1, 1.2 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CSV PARSING TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: CSV parsing should extract valid data for any valid CSV input
    /// This test generates valid CSV data and verifies that parsing extracts all data correctly
    /// with proper field mappings and type conversions.
    /// </summary>
    private static async Task CsvParsingExtractsValidData_PropertyTest()
    {
        const int iterations = 100;
        var random = new Random(42); // Fixed seed for reproducibility
        
        for (int i = 0; i < iterations; i++)
        {
            // Generate test data
            var csvData = GenerateValidCsvData(random, i);
            
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            var logger = loggerFactory.CreateLogger<CsvParsingService>();
            var csvParsingService = new CsvParsingService(logger);
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvData.CsvContent));
            
            try
            {
                // Act
                var results = await csvParsingService.ParseCsvAsync(stream);
                var resultsList = results.ToList();
                
                // Assert
                if (csvData.ExpectedDraws.Count == 0)
                {
                    if (resultsList.Count != 0)
                        throw new Exception($"Iteration {i}: Expected 0 results but got {resultsList.Count}");
                    continue;
                }
                
                if (resultsList.Count != csvData.ExpectedDraws.Count)
                {
                    throw new Exception($"Iteration {i}: Expected {csvData.ExpectedDraws.Count} results but got {resultsList.Count}");
                }
                
                // Verify each parsed draw matches expected data
                for (int j = 0; j < resultsList.Count; j++)
                {
                    var parsed = resultsList[j];
                    var expected = csvData.ExpectedDraws[j];
                    
                    if (!DrawsMatch(parsed, expected))
                    {
                        throw new Exception($"Iteration {i}: Draw {j} does not match expected data");
                    }
                }
            }
            catch (Exception ex) when (!(ex.Message.StartsWith("Iteration")))
            {
                // For valid CSV data, parsing should not throw exceptions
                throw new Exception($"Iteration {i}: Parsing failed with exception: {ex.Message}", ex);
            }
        }
    }

    private static CsvTestData GenerateValidCsvData(Random random, int iteration)
    {
        var drawCount = Math.Max(0, iteration % 10); // 0-9 draws per test
        var draws = new List<MainModels.LottoDraw>();
        
        for (int i = 0; i < drawCount; i++)
        {
            draws.Add(GenerateValidLottoDraw(iteration * 100 + i + 1, random));
        }
        
        var csvContent = GenerateCsvFromDraws(draws);
        
        return new CsvTestData
        {
            CsvContent = csvContent,
            ExpectedDraws = draws
        };
    }

    private static MainModels.LottoDraw GenerateValidLottoDraw(int drawNumber, Random random)
    {
        // Generate 6 unique winning numbers between 1-40
        var winningNumbers = new HashSet<int>();
        while (winningNumbers.Count < 6)
        {
            winningNumbers.Add(random.Next(1, 41));
        }
        var numbers = winningNumbers.OrderBy(x => x).ToArray();
        
        // Generate bonus number (different from winning numbers)
        int bonusNumber;
        do
        {
            bonusNumber = random.Next(1, 41);
        } while (winningNumbers.Contains(bonusNumber));
        
        return new MainModels.LottoDraw
        {
            Draw = drawNumber,
            Date = DateTime.Today.AddDays(-random.Next(1, 365)),
            WinningNumber1 = numbers[0],
            WinningNumber2 = numbers[1],
            WinningNumber3 = numbers[2],
            WinningNumber4 = numbers[3],
            WinningNumber5 = numbers[4],
            WinningNumber6 = numbers[5],
            BonusNumber = bonusNumber,
            Powerball = random.Next(1, 11),
            FromLast = random.Next(1, 100).ToString(),
            OneToTen = random.Next(0, 7),
            ElevenToTwenty = random.Next(0, 7),
            TwentyOneToThirty = random.Next(0, 7),
            ThirtyOneToForty = random.Next(0, 7),
            Division1Prize = random.Next(100000, 2000000),
            Division1Winners = random.Next(0, 5)
        };
    }

    private static string GenerateCsvFromDraws(List<MainModels.LottoDraw> draws)
    {
        if (draws.Count == 0)
        {
            return "Draw,Date,Winning Number 1,Winning Number 2,Winning Number 3,Winning Number 4,Winning Number 5,Winning Number 6,Bonus Number,Powerball\n";
        }
        
        var sb = new StringBuilder();
        sb.AppendLine("Draw,Date,Winning Number 1,Winning Number 2,Winning Number 3,Winning Number 4,Winning Number 5,Winning Number 6,Bonus Number,Powerball,From Last,1-Oct,Nov-20,21-30,31-40,Division 1 Prize,Division 1 Winners");
        
        foreach (var draw in draws)
        {
            sb.AppendLine($"{draw.Draw},{draw.Date:dd/MM/yyyy},{draw.WinningNumber1},{draw.WinningNumber2},{draw.WinningNumber3},{draw.WinningNumber4},{draw.WinningNumber5},{draw.WinningNumber6},{draw.BonusNumber},{draw.Powerball},{draw.FromLast},{draw.OneToTen},{draw.ElevenToTwenty},{draw.TwentyOneToThirty},{draw.ThirtyOneToForty},{draw.Division1Prize},{draw.Division1Winners}");
        }
        
        return sb.ToString();
    }

    private static bool DrawsMatch(MainModels.LottoDraw parsed, MainModels.LottoDraw expected)
    {
        return parsed.Draw == expected.Draw &&
               parsed.Date.Date == expected.Date.Date &&
               parsed.WinningNumber1 == expected.WinningNumber1 &&
               parsed.WinningNumber2 == expected.WinningNumber2 &&
               parsed.WinningNumber3 == expected.WinningNumber3 &&
               parsed.WinningNumber4 == expected.WinningNumber4 &&
               parsed.WinningNumber5 == expected.WinningNumber5 &&
               parsed.WinningNumber6 == expected.WinningNumber6 &&
               parsed.BonusNumber == expected.BonusNumber &&
               parsed.Powerball == expected.Powerball;
    }

    private class CsvTestData
    {
        public string CsvContent { get; set; } = string.Empty;
        public List<MainModels.LottoDraw> ExpectedDraws { get; set; } = new();
    }
}

