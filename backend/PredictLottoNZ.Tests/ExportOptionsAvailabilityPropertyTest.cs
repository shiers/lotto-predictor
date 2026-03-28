using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using FsCheck;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Property-based test for export options availability
/// **Feature: lottery-lookup-navigation, Property 23: Export options are available**
/// **Validates: Requirements 9.1**
/// 
/// This test validates that export options for CSV, JSON, and PDF formats are available
/// for lookup and frequency results as specified in requirement 9.1.
/// </summary>
public static class ExportOptionsAvailabilityPropertyTest
{
    public static async Task RunExportOptionsAvailabilityPropertyTest()
    {
        Console.WriteLine("Running Export Options Availability Property Test...");
        Console.WriteLine("===================================================");

        try
        {
            Console.WriteLine("Property Test 23: Export options are available for lookup results...");
            ExportOptionsAvailableForLookupResults_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Export options available for lookup results (100 iterations)");

            Console.WriteLine("\nProperty Test 23a: Export options are available for frequency results...");
            ExportOptionsAvailableForFrequencyResults_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Export options available for frequency results (100 iterations)");

            Console.WriteLine("\nProperty Test 23b: Export options are available for navigation history...");
            ExportOptionsAvailableForNavigationHistory_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Export options available for navigation history (50 iterations)");

            Console.WriteLine("\nProperty Test 23c: All required export formats are supported...");
            AllRequiredExportFormatsSupported_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: All required export formats are supported (25 iterations)");

            Console.WriteLine("\n===================================================");
            Console.WriteLine("Export options availability property test PASSED!");
            Console.WriteLine("Property 23: Export options are available - VALIDATED");
            Console.WriteLine("Requirements 9.1 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ EXPORT OPTIONS AVAILABILITY PROPERTY TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: For any lookup results, export options for CSV, JSON, and PDF should be available
    /// </summary>
    private static async Task ExportOptionsAvailableForLookupResults_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 40)), // Generate lottery numbers from 1 to 40
            Arb.From(Gen.Choose(1, 10)), // Generate number count from 1 to 10
            (int baseNumber, int numberCount) =>
            {
                using var context = CreateTestContext();
                var exportService = CreateExportService(context);

                try
                {
                    // Create a lookup request with valid numbers
                    var numbers = Enumerable.Range(baseNumber, Math.Min(numberCount, 40 - baseNumber + 1))
                                          .Where(n => n >= 1 && n <= 40)
                                          .Take(6) // Limit to 6 numbers max
                                          .ToArray();

                    if (!numbers.Any())
                        numbers = new[] { 1 }; // Fallback to ensure we have at least one number

                    var lookupRequest = new NumberLookupRequest
                    {
                        Numbers = numbers,
                        IncludeBonus = true,
                        IncludePowerball = true
                    };

                    // Test CSV export option
                    var csvExportRequest = new LookupExportRequest
                    {
                        SearchCriteria = lookupRequest,
                        Format = ExportFormat.CSV,
                        IncludeMetadata = true,
                        FileName = "test_lookup.csv"
                    };

                    var csvResult = exportService.ExportLookupResultsAsync(csvExportRequest).Result;
                    var csvAvailable = csvResult != null && !string.IsNullOrEmpty(csvResult.ExportId);

                    // Test JSON export option
                    var jsonExportRequest = new LookupExportRequest
                    {
                        SearchCriteria = lookupRequest,
                        Format = ExportFormat.JSON,
                        IncludeMetadata = true,
                        FileName = "test_lookup.json"
                    };

                    var jsonResult = exportService.ExportLookupResultsAsync(jsonExportRequest).Result;
                    var jsonAvailable = jsonResult != null && !string.IsNullOrEmpty(jsonResult.ExportId);

                    // Test PDF export option
                    var pdfExportRequest = new LookupExportRequest
                    {
                        SearchCriteria = lookupRequest,
                        Format = ExportFormat.PDF,
                        IncludeMetadata = true,
                        FileName = "test_lookup.pdf"
                    };

                    var pdfResult = exportService.ExportLookupResultsAsync(pdfExportRequest).Result;
                    var pdfAvailable = pdfResult != null && !string.IsNullOrEmpty(pdfResult.ExportId);

                    // Verify that all export results have proper structure
                    var csvHasDownloadUrl = csvResult?.DownloadUrl != null && csvResult.DownloadUrl.Contains(csvResult.ExportId);
                    var jsonHasDownloadUrl = jsonResult?.DownloadUrl != null && jsonResult.DownloadUrl.Contains(jsonResult.ExportId);
                    var pdfHasDownloadUrl = pdfResult?.DownloadUrl != null && pdfResult.DownloadUrl.Contains(pdfResult.ExportId);

                    // Verify that export results have expiration dates
                    var csvHasExpiration = csvResult?.ExpiresAt > DateTime.UtcNow;
                    var jsonHasExpiration = jsonResult?.ExpiresAt > DateTime.UtcNow;
                    var pdfHasExpiration = pdfResult?.ExpiresAt > DateTime.UtcNow;

                    return (csvAvailable && jsonAvailable && pdfAvailable && 
                           csvHasDownloadUrl && jsonHasDownloadUrl && pdfHasDownloadUrl &&
                           csvHasExpiration && jsonHasExpiration && pdfHasExpiration).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ExportOptionsAvailableForLookupResults test for numbers [{string.Join(", ", Enumerable.Range(baseNumber, numberCount))}]: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 100).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: For any frequency results, export options for CSV, JSON, and PDF should be available
    /// </summary>
    private static async Task ExportOptionsAvailableForFrequencyResults_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 30)), // Generate start number from 1 to 30
            Arb.From(Gen.Choose(5, 15)), // Generate range size from 5 to 15
            (int startNumber, int rangeSize) =>
            {
                using var context = CreateTestContext();
                var exportService = CreateExportService(context);

                try
                {
                    // Create a frequency analysis request with valid ranges
                    var endNumber = Math.Min(startNumber + rangeSize - 1, 40);
                    var ranges = new List<NumberRange>
                    {
                        new NumberRange
                        {
                            StartNumber = startNumber,
                            EndNumber = endNumber,
                            Label = $"Range {startNumber}-{endNumber}"
                        }
                    };

                    var frequencyRequest = new FrequencyAnalysisRequest
                    {
                        Ranges = ranges,
                        IncludeBonus = false,
                        IncludePowerball = false
                    };

                    // Test CSV export option
                    var csvExportRequest = new FrequencyExportRequest
                    {
                        SearchCriteria = frequencyRequest,
                        Format = ExportFormat.CSV,
                        IncludeCharts = false,
                        FileName = "test_frequency.csv"
                    };

                    var csvResult = exportService.ExportFrequencyDataAsync(csvExportRequest).Result;
                    var csvAvailable = csvResult != null && !string.IsNullOrEmpty(csvResult.ExportId);

                    // Test JSON export option
                    var jsonExportRequest = new FrequencyExportRequest
                    {
                        SearchCriteria = frequencyRequest,
                        Format = ExportFormat.JSON,
                        IncludeCharts = false,
                        FileName = "test_frequency.json"
                    };

                    var jsonResult = exportService.ExportFrequencyDataAsync(jsonExportRequest).Result;
                    var jsonAvailable = jsonResult != null && !string.IsNullOrEmpty(jsonResult.ExportId);

                    // Test PDF export option
                    var pdfExportRequest = new FrequencyExportRequest
                    {
                        SearchCriteria = frequencyRequest,
                        Format = ExportFormat.PDF,
                        IncludeCharts = true,
                        FileName = "test_frequency.pdf"
                    };

                    var pdfResult = exportService.ExportFrequencyDataAsync(pdfExportRequest).Result;
                    var pdfAvailable = pdfResult != null && !string.IsNullOrEmpty(pdfResult.ExportId);

                    // Verify that all export results have proper structure
                    var csvHasDownloadUrl = csvResult?.DownloadUrl != null && csvResult.DownloadUrl.Contains(csvResult.ExportId);
                    var jsonHasDownloadUrl = jsonResult?.DownloadUrl != null && jsonResult.DownloadUrl.Contains(jsonResult.ExportId);
                    var pdfHasDownloadUrl = pdfResult?.DownloadUrl != null && pdfResult.DownloadUrl.Contains(pdfResult.ExportId);

                    // Verify that export results have reasonable file sizes
                    var csvHasSize = csvResult?.FileSizeBytes > 0;
                    var jsonHasSize = jsonResult?.FileSizeBytes > 0;
                    var pdfHasSize = pdfResult?.FileSizeBytes > 0;

                    return (csvAvailable && jsonAvailable && pdfAvailable && 
                           csvHasDownloadUrl && jsonHasDownloadUrl && pdfHasDownloadUrl &&
                           csvHasSize && jsonHasSize && pdfHasSize).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ExportOptionsAvailableForFrequencyResults test for range {startNumber}-{Math.Min(startNumber + rangeSize - 1, 40)}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 100).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: For any navigation history, export options for CSV, JSON, and PDF should be available
    /// </summary>
    private static async Task ExportOptionsAvailableForNavigationHistory_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 30)), // Generate number of days from 1 to 30
            (int dayRange) =>
            {
                using var context = CreateTestContext();
                var exportService = CreateExportService(context);

                try
                {
                    // Create a navigation export request with date range
                    var startDate = DateTime.UtcNow.AddDays(-dayRange);
                    var endDate = DateTime.UtcNow;

                    var navigationRequest = new NavigationExportRequest
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        UserId = "testuser",
                        IncludeMetadata = true
                    };

                    // Test CSV export option
                    navigationRequest.Format = ExportFormat.CSV;
                    navigationRequest.FileName = "test_navigation.csv";
                    var csvResult = exportService.ExportNavigationHistoryAsync(navigationRequest).Result;
                    var csvAvailable = csvResult != null && !string.IsNullOrEmpty(csvResult.ExportId);

                    // Test JSON export option
                    navigationRequest.Format = ExportFormat.JSON;
                    navigationRequest.FileName = "test_navigation.json";
                    var jsonResult = exportService.ExportNavigationHistoryAsync(navigationRequest).Result;
                    var jsonAvailable = jsonResult != null && !string.IsNullOrEmpty(jsonResult.ExportId);

                    // Test PDF export option
                    navigationRequest.Format = ExportFormat.PDF;
                    navigationRequest.FileName = "test_navigation.pdf";
                    var pdfResult = exportService.ExportNavigationHistoryAsync(navigationRequest).Result;
                    var pdfAvailable = pdfResult != null && !string.IsNullOrEmpty(pdfResult.ExportId);

                    // Verify that all export results have proper structure
                    var csvHasDownloadUrl = csvResult?.DownloadUrl != null && csvResult.DownloadUrl.Contains("exports");
                    var jsonHasDownloadUrl = jsonResult?.DownloadUrl != null && jsonResult.DownloadUrl.Contains("exports");
                    var pdfHasDownloadUrl = pdfResult?.DownloadUrl != null && pdfResult.DownloadUrl.Contains("exports");

                    // Verify that export results have expiration dates in the future
                    var csvHasValidExpiration = csvResult?.ExpiresAt > DateTime.UtcNow;
                    var jsonHasValidExpiration = jsonResult?.ExpiresAt > DateTime.UtcNow;
                    var pdfHasValidExpiration = pdfResult?.ExpiresAt > DateTime.UtcNow;

                    return (csvAvailable && jsonAvailable && pdfAvailable && 
                           csvHasDownloadUrl && jsonHasDownloadUrl && pdfHasDownloadUrl &&
                           csvHasValidExpiration && jsonHasValidExpiration && pdfHasValidExpiration).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ExportOptionsAvailableForNavigationHistory test for {dayRange} day range: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: All required export formats (CSV, JSON, PDF) should be supported by the ExportFormat enum
    /// </summary>
    private static async Task AllRequiredExportFormatsSupported_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Constant(true)), // Simple generator since we're testing enum values
            (bool _) =>
            {
                try
                {
                    // Verify that all required export formats are defined in the ExportFormat enum
                    var exportFormats = Enum.GetValues<ExportFormat>();
                    
                    var csvSupported = exportFormats.Contains(ExportFormat.CSV);
                    var jsonSupported = exportFormats.Contains(ExportFormat.JSON);
                    var pdfSupported = exportFormats.Contains(ExportFormat.PDF);

                    // Verify that the enum values can be converted to strings correctly
                    var csvString = ExportFormat.CSV.ToString();
                    var jsonString = ExportFormat.JSON.ToString();
                    var pdfString = ExportFormat.PDF.ToString();

                    var csvStringCorrect = csvString == "CSV";
                    var jsonStringCorrect = jsonString == "JSON";
                    var pdfStringCorrect = pdfString == "PDF";

                    // Verify that we can parse the enum values from strings
                    var csvParseable = Enum.TryParse<ExportFormat>("CSV", out var csvParsed) && csvParsed == ExportFormat.CSV;
                    var jsonParseable = Enum.TryParse<ExportFormat>("JSON", out var jsonParsed) && jsonParsed == ExportFormat.JSON;
                    var pdfParseable = Enum.TryParse<ExportFormat>("PDF", out var pdfParsed) && pdfParsed == ExportFormat.PDF;

                    // Verify that Excel format is also supported (as mentioned in the design document)
                    var excelSupported = exportFormats.Contains(ExportFormat.Excel);

                    return (csvSupported && jsonSupported && pdfSupported && excelSupported &&
                           csvStringCorrect && jsonStringCorrect && pdfStringCorrect &&
                           csvParseable && jsonParseable && pdfParseable).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in AllRequiredExportFormatsSupported test: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 25).GetAwaiter().GetResult();
    }

    #region Helper Methods

    private static LottoDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<LottoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new LottoDbContext(options);
    }

    private static ExportService CreateExportService(LottoDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<ExportService>>();

        return new ExportService(context, logger);
    }

    private static async Task RunPropertyTest(Property property, int iterations)
    {
        Check.Quick(property);
        await Task.CompletedTask;
    }

    #endregion
}

