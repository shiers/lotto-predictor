using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PredictLottoNZ.Controllers;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Unit tests for NavigationController API endpoints.
/// Tests validation, error handling, and proper HTTP status codes.
/// </summary>
public static class NavigationControllerTest
{
    public static async Task RunNavigationControllerTests()
    {
        Console.WriteLine("Running Navigation Controller Tests...");
        Console.WriteLine("=====================================");

        try
        {
            Console.WriteLine("Test 1: Draw retrieval by number validation...");
            TestDrawRetrievalByNumberValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Draw retrieval by number validation");

            Console.WriteLine("\nTest 2: Draw retrieval by date validation...");
            TestDrawRetrievalByDateValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Draw retrieval by date validation");

            Console.WriteLine("\nTest 3: Previous/Next navigation validation...");
            TestPreviousNextNavigationValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Previous/Next navigation validation");

            Console.WriteLine("\nTest 4: Navigation context validation...");
            TestNavigationContextValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Navigation context validation");

            Console.WriteLine("\nTest 5: Date range retrieval validation...");
            TestDateRangeRetrievalValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Date range retrieval validation");

            Console.WriteLine("\nTest 6: Jump-to navigation validation...");
            TestJumpToNavigationValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Jump-to navigation validation");

            Console.WriteLine("\nTest 7: Navigation request validation...");
            TestNavigationRequestValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Navigation request validation");

            Console.WriteLine("\nTest 8: Bookmark management validation...");
            TestBookmarkManagementValidation().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark management validation");

            Console.WriteLine("\n=====================================");
            Console.WriteLine("Navigation Controller tests PASSED!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ NAVIGATION CONTROLLER TEST FAILED: {ex.Message}");
            throw;
        }
    }

    private static async Task TestDrawRetrievalByNumberValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        // Test invalid draw number (zero)
        var result = controller.GetDrawByNumber(0).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for draw number 0");

        // Test invalid draw number (negative)
        result = controller.GetDrawByNumber(-1).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for negative draw number");

        // Test draw not found
        mockNavigationService.Setup(s => s.GetDrawByNumberAsync(999))
                           .ReturnsAsync((LottoDrawDto?)null);

        result = controller.GetDrawByNumber(999).Result;
        var notFoundResult = result.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for non-existent draw");

        // Test valid draw retrieval
        var sampleDraw = new LottoDrawDto 
        { 
            Draw = 100, 
            Date = DateTime.Today,
            WinningNumbers = new[] { 1, 2, 3, 4, 5, 6 }
        };
        mockNavigationService.Setup(s => s.GetDrawByNumberAsync(100))
                           .ReturnsAsync(sampleDraw);

        result = controller.GetDrawByNumber(100).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid draw number");

        var returnedDraw = okResult.Value as LottoDrawDto;
        if (returnedDraw?.Draw != 100)
            throw new Exception("Should return the correct draw");

        mockNavigationService.Verify(s => s.GetDrawByNumberAsync(100), Times.Once);
        mockNavigationService.Verify(s => s.GetDrawByNumberAsync(999), Times.Once);
    }

    private static async Task TestDrawRetrievalByDateValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        var testDate = new DateTime(2023, 1, 1);

        // Test draw not found for date
        mockNavigationService.Setup(s => s.GetDrawByDateAsync(testDate))
                           .ReturnsAsync((LottoDrawDto?)null);

        var result = controller.GetDrawByDate(testDate).Result;
        var notFoundResult = result.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for date with no draw");

        // Test valid draw retrieval by date
        var sampleDraw = new LottoDrawDto 
        { 
            Draw = 200, 
            Date = testDate,
            WinningNumbers = new[] { 7, 8, 9, 10, 11, 12 }
        };
        mockNavigationService.Setup(s => s.GetDrawByDateAsync(testDate))
                           .ReturnsAsync(sampleDraw);

        result = controller.GetDrawByDate(testDate).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid date");

        var returnedDraw = okResult.Value as LottoDrawDto;
        if (returnedDraw?.Draw != 200)
            throw new Exception("Should return the correct draw for date");

        mockNavigationService.Verify(s => s.GetDrawByDateAsync(testDate), Times.Exactly(2));
    }

    private static async Task TestPreviousNextNavigationValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        // Test previous draw with invalid draw number
        var result = controller.GetPreviousDraw(0).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for previous draw with number 0");

        // Test next draw with invalid draw number
        result = controller.GetNextDraw(-1).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for next draw with negative number");

        // Test previous draw not found (first draw)
        mockNavigationService.Setup(s => s.GetPreviousDrawAsync(1))
                           .ReturnsAsync((LottoDrawDto?)null);

        result = controller.GetPreviousDraw(1).Result;
        var notFoundResult = result.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for previous draw of first draw");

        // Test next draw not found (last draw)
        mockNavigationService.Setup(s => s.GetNextDrawAsync(1000))
                           .ReturnsAsync((LottoDrawDto?)null);

        result = controller.GetNextDraw(1000).Result;
        notFoundResult = result.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for next draw of last draw");

        // Test valid previous draw
        var previousDraw = new LottoDrawDto { Draw = 99, Date = DateTime.Today.AddDays(-1) };
        mockNavigationService.Setup(s => s.GetPreviousDrawAsync(100))
                           .ReturnsAsync(previousDraw);

        result = controller.GetPreviousDraw(100).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid previous draw request");

        var returnedDraw = okResult.Value as LottoDrawDto;
        if (returnedDraw?.Draw != 99)
            throw new Exception("Should return the correct previous draw");

        // Test valid next draw
        var nextDraw = new LottoDrawDto { Draw = 101, Date = DateTime.Today.AddDays(1) };
        mockNavigationService.Setup(s => s.GetNextDrawAsync(100))
                           .ReturnsAsync(nextDraw);

        result = controller.GetNextDraw(100).Result;
        okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid next draw request");

        returnedDraw = okResult.Value as LottoDrawDto;
        if (returnedDraw?.Draw != 101)
            throw new Exception("Should return the correct next draw");

        mockNavigationService.Verify(s => s.GetPreviousDrawAsync(100), Times.Once);
        mockNavigationService.Verify(s => s.GetNextDrawAsync(100), Times.Once);
    }

    private static async Task TestNavigationContextValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        // Test navigation context with invalid draw number
        var result = controller.GetNavigationContext(0).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for navigation context with draw number 0");

        // Test valid navigation context
        var sampleContext = new NavigationContext
        {
            CurrentPosition = 50,
            TotalDraws = 100,
            HasPrevious = true,
            HasNext = true,
            EarliestDate = DateTime.Today.AddYears(-1),
            LatestDate = DateTime.Today,
            MissingDrawNumbers = new[] { 25, 75 }
        };
        mockNavigationService.Setup(s => s.GetNavigationContextAsync(100))
                           .ReturnsAsync(sampleContext);

        result = controller.GetNavigationContext(100).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid navigation context request");

        var returnedContext = okResult.Value as NavigationContext;
        if (returnedContext?.CurrentPosition != 50 || returnedContext.TotalDraws != 100)
            throw new Exception("Should return the correct navigation context");

        mockNavigationService.Verify(s => s.GetNavigationContextAsync(100), Times.Once);
    }

    private static async Task TestDateRangeRetrievalValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        var startDate = DateTime.Today.AddDays(-10);
        var endDate = DateTime.Today;

        // Test invalid date range (start after end)
        var result = controller.GetDrawsInRange(endDate, startDate).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for invalid date range");

        // Test date range too large (> 365 days)
        var tooEarlyDate = DateTime.Today.AddDays(-400);
        result = controller.GetDrawsInRange(tooEarlyDate, endDate).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for date range > 365 days");

        // Test valid date range
        var sampleDraws = new List<LottoDrawDto>
        {
            new LottoDrawDto { Draw = 100, Date = startDate.AddDays(1) },
            new LottoDrawDto { Draw = 101, Date = startDate.AddDays(5) },
            new LottoDrawDto { Draw = 102, Date = endDate }
        };
        mockNavigationService.Setup(s => s.GetDrawsInRangeAsync(startDate, endDate))
                           .ReturnsAsync(sampleDraws);

        result = controller.GetDrawsInRange(startDate, endDate).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid date range");

        var returnedDraws = okResult.Value as IEnumerable<LottoDrawDto>;
        if (returnedDraws?.Count() != 3)
            throw new Exception("Should return the correct number of draws in range");

        mockNavigationService.Verify(s => s.GetDrawsInRangeAsync(startDate, endDate), Times.Once);
    }

    private static async Task TestJumpToNavigationValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        // Test jump-to with no criteria
        var request = new JumpToRequest();
        var result = controller.JumpToDraw(request).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for jump-to with no criteria");

        // Test jump-to with invalid draw number
        request = new JumpToRequest { DrawNumber = 0 };
        result = controller.JumpToDraw(request).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for jump-to with draw number 0");

        // Test jump-to not found
        mockNavigationService.Setup(s => s.JumpToDrawAsync(It.IsAny<JumpToRequest>()))
                           .ReturnsAsync((LottoDrawDto?)null);

        request = new JumpToRequest { DrawNumber = 999 };
        result = controller.JumpToDraw(request).Result;
        var notFoundResult = result.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for jump-to with non-existent criteria");

        // Test valid jump-to by draw number
        var targetDraw = new LottoDrawDto { Draw = 150, Date = DateTime.Today };
        mockNavigationService.Setup(s => s.JumpToDrawAsync(It.Is<JumpToRequest>(r => r.DrawNumber == 150)))
                           .ReturnsAsync(targetDraw);

        request = new JumpToRequest { DrawNumber = 150 };
        result = controller.JumpToDraw(request).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid jump-to by draw number");

        var returnedDraw = okResult.Value as LottoDrawDto;
        if (returnedDraw?.Draw != 150)
            throw new Exception("Should return the correct target draw");

        // Test valid jump-to by date
        var testDate = new DateTime(2023, 6, 15);
        mockNavigationService.Setup(s => s.JumpToDrawAsync(It.Is<JumpToRequest>(r => r.Date == testDate)))
                           .ReturnsAsync(targetDraw);

        request = new JumpToRequest { Date = testDate };
        result = controller.JumpToDraw(request).Result;
        okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid jump-to by date");

        mockNavigationService.Verify(s => s.JumpToDrawAsync(It.IsAny<JumpToRequest>()), Times.AtLeast(2));
    }

    private static async Task TestNavigationRequestValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        // Test Previous navigation without draw number
        var request = new NavigationRequest { Direction = NavigationDirection.Previous };
        var result = controller.Navigate(request).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for Previous navigation without draw number");

        // Test Next navigation without draw number
        request = new NavigationRequest { Direction = NavigationDirection.Next };
        result = controller.Navigate(request).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for Next navigation without draw number");

        // Test Specific navigation without criteria
        request = new NavigationRequest { Direction = NavigationDirection.Specific };
        result = controller.Navigate(request).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for Specific navigation without criteria");

        // Test First/Last navigation (not implemented)
        request = new NavigationRequest { Direction = NavigationDirection.First };
        result = controller.Navigate(request).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for First navigation (not implemented)");

        // Test valid Previous navigation
        var previousDraw = new LottoDrawDto { Draw = 99, Date = DateTime.Today };
        mockNavigationService.Setup(s => s.GetPreviousDrawAsync(100))
                           .ReturnsAsync(previousDraw);

        request = new NavigationRequest { Direction = NavigationDirection.Previous, DrawNumber = 100 };
        result = controller.Navigate(request).Result;
        var okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid Previous navigation");

        // Test valid Specific navigation by draw number
        var specificDraw = new LottoDrawDto { Draw = 200, Date = DateTime.Today };
        mockNavigationService.Setup(s => s.GetDrawByNumberAsync(200))
                           .ReturnsAsync(specificDraw);

        request = new NavigationRequest { Direction = NavigationDirection.Specific, DrawNumber = 200 };
        result = controller.Navigate(request).Result;
        okResult = result.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid Specific navigation by draw number");

        // Test navigation not found
        mockNavigationService.Setup(s => s.GetNextDrawAsync(1000))
                           .ReturnsAsync((LottoDrawDto?)null);

        request = new NavigationRequest { Direction = NavigationDirection.Next, DrawNumber = 1000 };
        result = controller.Navigate(request).Result;
        var notFoundResult = result.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for navigation with no result");

        mockNavigationService.Verify(s => s.GetPreviousDrawAsync(100), Times.Once);
        mockNavigationService.Verify(s => s.GetDrawByNumberAsync(200), Times.Once);
        mockNavigationService.Verify(s => s.GetNextDrawAsync(1000), Times.Once);
    }

    private static async Task TestBookmarkManagementValidation()
    {
        // Arrange
        var mockNavigationService = new Mock<IDrawNavigationService>();
        var mockBookmarkService = new Mock<IBookmarkService>();
        var mockLogger = new Mock<ILogger<NavigationController>>();
        var controller = new NavigationController(mockNavigationService.Object, mockBookmarkService.Object, mockLogger.Object);

        // Test create bookmark with invalid draw number
        var createRequest = new CreateBookmarkRequest { DrawNumber = 0, Label = "Test" };
        var result = controller.CreateBookmark(createRequest).Result;
        var badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for create bookmark with draw number 0");

        // Test create bookmark with empty label
        createRequest = new CreateBookmarkRequest { DrawNumber = 100, Label = "" };
        result = controller.CreateBookmark(createRequest).Result;
        badRequestResult = result.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for create bookmark with empty label");

        // Test valid bookmark creation
        var sampleBookmark = new BookmarkDto 
        { 
            Id = 1, 
            DrawNumber = 100, 
            Label = "Test Bookmark", 
            UserId = "anonymous",
            CreatedAt = DateTime.UtcNow
        };
        mockBookmarkService.Setup(s => s.CreateBookmarkAsync(100, "Test Bookmark", "anonymous"))
                         .ReturnsAsync(sampleBookmark);

        createRequest = new CreateBookmarkRequest { DrawNumber = 100, Label = "Test Bookmark" };
        result = controller.CreateBookmark(createRequest).Result;
        var createdResult = result.Result as CreatedAtActionResult;
        if (createdResult == null || createdResult.StatusCode != 201)
            throw new Exception("Should return Created for valid bookmark creation");

        var createdBookmark = createdResult.Value as BookmarkDto;
        if (createdBookmark?.Id != 1)
            throw new Exception("Should return the created bookmark");

        // Test get bookmarks
        var sampleBookmarks = new List<BookmarkDto> { sampleBookmark };
        mockBookmarkService.Setup(s => s.GetUserBookmarksAsync("anonymous"))
                         .ReturnsAsync(sampleBookmarks);

        var getResult = controller.GetBookmarks().Result;
        var okResult = getResult.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for get bookmarks");

        var returnedBookmarks = okResult.Value as IEnumerable<BookmarkDto>;
        if (returnedBookmarks?.Count() != 1)
            throw new Exception("Should return the correct number of bookmarks");

        // Test get specific bookmark with invalid ID
        var getBookmarkResult = controller.GetBookmark(0).Result;
        badRequestResult = getBookmarkResult.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for get bookmark with ID 0");

        // Test get specific bookmark not found
        mockBookmarkService.Setup(s => s.GetUserBookmarksAsync("anonymous"))
                         .ReturnsAsync(new List<BookmarkDto>());

        getBookmarkResult = controller.GetBookmark(999).Result;
        var notFoundResult = getBookmarkResult.Result as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for non-existent bookmark");

        // Test update bookmark with invalid ID
        var updateRequest = new UpdateBookmarkRequest { Label = "Updated Label" };
        var updateResult = controller.UpdateBookmark(0, updateRequest).Result;
        badRequestResult = updateResult.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for update bookmark with ID 0");

        // Test update bookmark with empty label
        updateRequest = new UpdateBookmarkRequest { Label = "" };
        updateResult = controller.UpdateBookmark(1, updateRequest).Result;
        badRequestResult = updateResult.Result as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for update bookmark with empty label");

        // Test valid bookmark update
        var updatedBookmark = new BookmarkDto 
        { 
            Id = 1, 
            DrawNumber = 100, 
            Label = "Updated Label", 
            UserId = "anonymous",
            UpdatedAt = DateTime.UtcNow
        };
        mockBookmarkService.Setup(s => s.UpdateBookmarkAsync(1, "Updated Label", "anonymous"))
                         .ReturnsAsync(updatedBookmark);

        updateRequest = new UpdateBookmarkRequest { Label = "Updated Label" };
        updateResult = controller.UpdateBookmark(1, updateRequest).Result;
        okResult = updateResult.Result as OkObjectResult;
        if (okResult == null || okResult.StatusCode != 200)
            throw new Exception("Should return Ok for valid bookmark update");

        // Test delete bookmark with invalid ID
        var deleteResult = controller.DeleteBookmark(0).Result;
        badRequestResult = deleteResult as BadRequestObjectResult;
        if (badRequestResult == null || badRequestResult.StatusCode != 400)
            throw new Exception("Should return BadRequest for delete bookmark with ID 0");

        // Test delete bookmark not found
        mockBookmarkService.Setup(s => s.DeleteBookmarkAsync(999, "anonymous"))
                         .ReturnsAsync(false);

        deleteResult = controller.DeleteBookmark(999).Result;
        notFoundResult = deleteResult as NotFoundObjectResult;
        if (notFoundResult == null || notFoundResult.StatusCode != 404)
            throw new Exception("Should return NotFound for delete non-existent bookmark");

        // Test valid bookmark deletion
        mockBookmarkService.Setup(s => s.DeleteBookmarkAsync(1, "anonymous"))
                         .ReturnsAsync(true);

        deleteResult = controller.DeleteBookmark(1).Result;
        var noContentResult = deleteResult as NoContentResult;
        if (noContentResult == null || noContentResult.StatusCode != 204)
            throw new Exception("Should return NoContent for valid bookmark deletion");

        mockBookmarkService.Verify(s => s.CreateBookmarkAsync(100, "Test Bookmark", "anonymous"), Times.Once);
        mockBookmarkService.Verify(s => s.GetUserBookmarksAsync("anonymous"), Times.AtLeast(2));
        mockBookmarkService.Verify(s => s.UpdateBookmarkAsync(1, "Updated Label", "anonymous"), Times.Once);
        mockBookmarkService.Verify(s => s.DeleteBookmarkAsync(1, "anonymous"), Times.Once);
    }
}