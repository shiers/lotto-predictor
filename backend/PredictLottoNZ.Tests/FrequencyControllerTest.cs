using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PredictLottoNZ.Controllers;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Unit tests for FrequencyController API endpoints.
/// Tests validation, error handling, and proper HTTP status codes.
/// </summary>
public static class FrequencyControllerTest
{
    public static async Task RunFrequencyControllerTests()
    {
        Console.WriteLine("Running Frequency Controller Tests...");
        Console.WriteLine("====================================");

        try
        {
            Console.WriteLine("Test 1: Number frequency retrieval...");
            await TestNumberFrequencyRetrieval();
            Console.WriteLine("✓ PASSED: Number frequency retrieval");

            Console.WriteLine("\nTest 2: Range frequency analysis validation...");
            await TestRangeFrequencyAnalysisValidation();
            Console.WriteLine("✓ PASSED: Range frequency analysis validation");

            Console.WriteLine("\nTest 3: Hot/cold analysis validation...");
            await TestHotColdAnalysisValidation();
            Console.WriteLine("✓ PASSED: Hot/cold analysis validation");

            Console.WriteLine("\nTest 4: Frequency comparison validation...");
            await TestFrequencyComparisonValidation();
            Console.WriteLine("✓ PASSED: Frequency comparison validation");

            Console.WriteLine("\nTest 5: Export functionality validation...");
            await TestExportFunctionalityValidation();
            Console.WriteLine("✓ PASSED: Export functionality validation");

            Console.WriteLine("\n====================================");
            Console.WriteLine("Frequency Controller tests PASSED!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FREQUENCY CONTROLLER TEST FAILED: {ex.Message}");
            throw;
        }
    }

    private static async Task TestNumberFrequencyRetrieval()
    {
        // Arrange
        var mockFrequencyService = new Mock<IFrequencyAnalysisService>();
        var mockExportService = new Mock<IExportService>();
        var mockLogger = new Mock<ILogger<FrequencyController>>();
        var controller = new FrequencyController(mockFrequencyService.Object, mockExportService.Object, mockLogger.Object);

        // Test get all number frequencies
        var sampleFrequencies = new List<NumberFrequencyDto>
        {
            new NumberFrequencyDto { Number = 1, TotalOccurrences = 10, Percentage = 5.0 },
            new NumberFrequencyDto { Number = 2, TotalOccurrences = 15, Percentage = 7.5 }
        };
        mockFrequencyService.Setup(s => s.GetNumberFrequenciesAsync())
                          .ReturnsAsync(sampleFrequencies);

        var result = await controller.GetNumberFrequencies();
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for number frequencies request");

        var returnedFrequencies = okResult.Value as IEnumerable<NumberFrequencyDto>;
        if (returnedFrequencies?.Count() != 2)
            throw new Exception("Should return the correct number of frequencies");

        // Test get specific number frequency - invalid number
        var singleResult = await controller.GetNumberFrequency(0);
        var badRequestResult = singleResult.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for number 0");

        singleResult = await controller.GetNumberFrequency(41);
        badRequestResult = singleResult.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for number 41");

        // Test get specific number frequency - not found
        singleResult = await controller.GetNumberFrequency(20);
        var notFoundResult = singleResult.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for number not in frequency data");

        // Test get specific number frequency - valid
        mockFrequencyService.Setup(s => s.GetNumberFrequenciesAsync())
                          .ReturnsAsync(new List<NumberFrequencyDto>
                          {
                              new NumberFrequencyDto { Number = 20, TotalOccurrences = 25, Percentage = 12.5 }
                          });

        singleResult = await controller.GetNumberFrequency(20);
        okResult = singleResult.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid number frequency request");

        mockFrequencyService.Verify(s => s.GetNumberFrequenciesAsync(), Times.AtLeast(2));
    }

    private static async Task TestRangeFrequencyAnalysisValidation()
    {
        // Arrange
        var mockFrequencyService = new Mock<IFrequencyAnalysisService>();
        var mockExportService = new Mock<IExportService>();
        var mockLogger = new Mock<ILogger<FrequencyController>>();
        var controller = new FrequencyController(mockFrequencyService.Object, mockExportService.Object, mockLogger.Object);

        // Test empty ranges
        var request = new FrequencyAnalysisRequest { Ranges = new List<NumberRange>() };
        var result = await controller.GetRangeFrequencies(request);
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for empty ranges");

        // Test invalid range numbers
        request = new FrequencyAnalysisRequest 
        { 
            Ranges = new List<NumberRange> 
            { 
                new NumberRange { StartNumber = 0, EndNumber = 10 }
            }
        };
        result = await controller.GetRangeFrequencies(request);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for range with StartNumber 0");

        request = new FrequencyAnalysisRequest 
        { 
            Ranges = new List<NumberRange> 
            { 
                new NumberRange { StartNumber = 1, EndNumber = 41 }
            }
        };
        result = await controller.GetRangeFrequencies(request);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for range with EndNumber 41");

        // Test invalid range order
        request = new FrequencyAnalysisRequest 
        { 
            Ranges = new List<NumberRange> 
            { 
                new NumberRange { StartNumber = 10, EndNumber = 5 }
            }
        };
        result = await controller.GetRangeFrequencies(request);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for invalid range order");

        // Test invalid date range
        request = new FrequencyAnalysisRequest 
        { 
            Ranges = new List<NumberRange> 
            { 
                new NumberRange { StartNumber = 1, EndNumber = 10 }
            },
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(-1)
        };
        result = await controller.GetRangeFrequencies(request);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for invalid date range");

        // Test valid range frequency request
        mockFrequencyService.Setup(s => s.GetRangeFrequenciesAsync(It.IsAny<IEnumerable<NumberRange>>()))
                          .ReturnsAsync(new List<RangeFrequency>());

        request = new FrequencyAnalysisRequest 
        { 
            Ranges = new List<NumberRange> 
            { 
                new NumberRange { StartNumber = 1, EndNumber = 10 }
            }
        };
        result = await controller.GetRangeFrequencies(request);
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid range frequency request");

        mockFrequencyService.Verify(s => s.GetRangeFrequenciesAsync(It.IsAny<IEnumerable<NumberRange>>()), Times.Once);
    }

    private static async Task TestHotColdAnalysisValidation()
    {
        // Arrange
        var mockFrequencyService = new Mock<IFrequencyAnalysisService>();
        var mockExportService = new Mock<IExportService>();
        var mockLogger = new Mock<ILogger<FrequencyController>>();
        var controller = new FrequencyController(mockFrequencyService.Object, mockExportService.Object, mockLogger.Object);

        // Test invalid period days (too small)
        var result = await controller.GetHotColdAnalysis(0);
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for period days 0");

        // Test invalid period days (too large)
        result = await controller.GetHotColdAnalysis(3651);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for period days > 3650");

        // Test valid hot/cold analysis
        var sampleHotColdNumbers = new List<HotColdNumber>
        {
            new HotColdNumber { Number = 1, Classification = "Hot", HotColdScore = 1.5 },
            new HotColdNumber { Number = 2, Classification = "Cold", HotColdScore = -1.2 }
        };
        mockFrequencyService.Setup(s => s.GetHotColdAnalysisAsync(It.IsAny<int>()))
                          .ReturnsAsync(sampleHotColdNumbers);

        result = await controller.GetHotColdAnalysis(365);
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid hot/cold analysis request");

        var returnedNumbers = okResult.Value as IEnumerable<HotColdNumber>;
        if (returnedNumbers?.Count() != 2)
            throw new Exception("Should return the correct number of hot/cold numbers");

        mockFrequencyService.Verify(s => s.GetHotColdAnalysisAsync(365), Times.Once);
    }

    private static async Task TestFrequencyComparisonValidation()
    {
        // Arrange
        var mockFrequencyService = new Mock<IFrequencyAnalysisService>();
        var mockExportService = new Mock<IExportService>();
        var mockLogger = new Mock<ILogger<FrequencyController>>();
        var controller = new FrequencyController(mockFrequencyService.Object, mockExportService.Object, mockLogger.Object);

        // Test invalid date range for comparison
        var request = new FrequencyAnalysisRequest 
        { 
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(-1)
        };
        var result = await controller.CompareFrequencies(request);
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for invalid date range in comparison");

        // Test valid frequency comparison
        var sampleComparison = new List<NumberFrequencyDto>
        {
            new NumberFrequencyDto { Number = 1, TotalOccurrences = 10, Percentage = 5.0 },
            new NumberFrequencyDto { Number = 2, TotalOccurrences = 15, Percentage = 7.5 }
        };
        mockFrequencyService.Setup(s => s.CompareFrequenciesAsync(It.IsAny<FrequencyAnalysisRequest>()))
                          .ReturnsAsync(sampleComparison);

        request = new FrequencyAnalysisRequest();
        result = await controller.CompareFrequencies(request);
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid frequency comparison request");

        var returnedComparison = okResult.Value as IEnumerable<NumberFrequencyDto>;
        if (returnedComparison?.Count() != 2)
            throw new Exception("Should return the correct comparison data");

        mockFrequencyService.Verify(s => s.CompareFrequenciesAsync(It.IsAny<FrequencyAnalysisRequest>()), Times.Once);
    }

    private static async Task TestExportFunctionalityValidation()
    {
        // Arrange
        var mockFrequencyService = new Mock<IFrequencyAnalysisService>();
        var mockExportService = new Mock<IExportService>();
        var mockLogger = new Mock<ILogger<FrequencyController>>();
        var controller = new FrequencyController(mockFrequencyService.Object, mockExportService.Object, mockLogger.Object);

        // Test export with invalid range numbers
        var request = new FrequencyExportRequest 
        { 
            SearchCriteria = new FrequencyAnalysisRequest
            {
                Ranges = new List<NumberRange> 
                { 
                    new NumberRange { StartNumber = 0, EndNumber = 10 }
                }
            }
        };
        var result = await controller.ExportFrequencyData(request);
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for export with invalid range numbers");

        // Test export with invalid range order
        request = new FrequencyExportRequest 
        { 
            SearchCriteria = new FrequencyAnalysisRequest
            {
                Ranges = new List<NumberRange> 
                { 
                    new NumberRange { StartNumber = 10, EndNumber = 5 }
                }
            }
        };
        result = await controller.ExportFrequencyData(request);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for export with invalid range order");

        // Test export with invalid date range
        request = new FrequencyExportRequest 
        { 
            SearchCriteria = new FrequencyAnalysisRequest
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(-1)
            }
        };
        result = await controller.ExportFrequencyData(request);
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for export with invalid date range");

        // Test valid export request
        var sampleExportResult = new ExportResult 
        { 
            ExportId = "test-export-123",
            DownloadUrl = "https://example.com/download/test-export-123",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        mockExportService.Setup(s => s.ExportFrequencyDataAsync(It.IsAny<FrequencyExportRequest>()))
                        .ReturnsAsync(sampleExportResult);

        request = new FrequencyExportRequest 
        { 
            SearchCriteria = new FrequencyAnalysisRequest(),
            Format = ExportFormat.CSV
        };
        result = await controller.ExportFrequencyData(request);
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid export request");

        var returnedResult = okResult.Value as ExportResult;
        if (returnedResult?.ExportId != "test-export-123")
            throw new Exception("Should return the correct export result");

        // Test get export status with empty ID
        var statusResult = await controller.GetExportStatus("");
        badRequestResult = statusResult.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for empty export ID");

        // Test valid export status request
        var sampleStatus = new ExportStatus 
        { 
            ExportId = "test-export-123",
            Status = "Completed",
            Progress = 100
        };
        mockExportService.Setup(s => s.GetExportStatusAsync("test-export-123"))
                        .ReturnsAsync(sampleStatus);

        statusResult = await controller.GetExportStatus("test-export-123");
        okResult = statusResult.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid export status request");

        var returnedStatus = okResult.Value as ExportStatus;
        if (returnedStatus?.Status != "Completed")
            throw new Exception("Should return the correct export status");

        mockExportService.Verify(s => s.ExportFrequencyDataAsync(It.IsAny<FrequencyExportRequest>()), Times.Once);
        mockExportService.Verify(s => s.GetExportStatusAsync("test-export-123"), Times.Once);
    }
}