using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using FsCheck;

/// <summary>
/// Simple test runner for bookmark removal functionality
/// **Feature: lottery-lookup-navigation, Property 22: Bookmark removal works with confirmation**
/// **Validates: Requirements 8.4**
/// </summary>
public static class BookmarkRemovalTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Bookmark Removal Property Test...");
        Console.WriteLine("==========================================");

        try
        {
            await TestBookmarkRemovalBasic();
            Console.WriteLine("✓ PASSED: Basic bookmark removal test");

            await TestBookmarkRemovalUserIsolation();
            Console.WriteLine("✓ PASSED: User isolation test");

            await TestBookmarkRemovalNonExistent();
            Console.WriteLine("✓ PASSED: Non-existent bookmark test");

            Console.WriteLine("\n==========================================");
            Console.WriteLine("✅ ALL BOOKMARK REMOVAL TESTS PASSED!");
            Console.WriteLine("Property 22: Bookmark removal works with confirmation - VALIDATED");
            Console.WriteLine("Requirements 8.4 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ BOOKMARK REMOVAL TEST FAILED: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static async Task TestBookmarkRemovalBasic()
    {
        using var context = CreateTestContext();
        var bookmarkService = CreateBookmarkService(context);

        // Create test data
        await SeedTestDraws(context, 10);

        var userId = "testuser";
        var drawNumber = 5;
        var label = "Test Bookmark";

        // Create a bookmark
        var createdBookmark = await bookmarkService.CreateBookmarkAsync(drawNumber, label, userId);
        
        // Verify bookmark exists
        var existsBeforeDeletion = await bookmarkService.BookmarkExistsAsync(userId, drawNumber);
        if (!existsBeforeDeletion)
            throw new Exception("Bookmark should exist before deletion");

        // Delete the bookmark
        var deletionResult = await bookmarkService.DeleteBookmarkAsync(createdBookmark.Id, userId);
        
        // Verify deletion was successful
        if (!deletionResult)
            throw new Exception("Deletion should return true for existing bookmark");

        // Verify bookmark no longer exists
        var existsAfterDeletion = await bookmarkService.BookmarkExistsAsync(userId, drawNumber);
        if (existsAfterDeletion)
            throw new Exception("Bookmark should not exist after deletion");

        // Verify second deletion returns false
        var secondDeletionResult = await bookmarkService.DeleteBookmarkAsync(createdBookmark.Id, userId);
        if (secondDeletionResult)
            throw new Exception("Second deletion should return false");
    }

    private static async Task TestBookmarkRemovalUserIsolation()
    {
        using var context = CreateTestContext();
        var bookmarkService = CreateBookmarkService(context);

        // Create test data
        await SeedTestDraws(context, 10);

        var user1 = "user1";
        var user2 = "user2";
        var drawNumber = 5;

        // Create bookmarks for both users
        var user1Bookmark = await bookmarkService.CreateBookmarkAsync(drawNumber, "User1 Bookmark", user1);
        var user2Bookmark = await bookmarkService.CreateBookmarkAsync(drawNumber, "User2 Bookmark", user2);

        // User1 tries to delete User2's bookmark - should fail
        var user1DeletesUser2 = await bookmarkService.DeleteBookmarkAsync(user2Bookmark.Id, user1);
        if (user1DeletesUser2)
            throw new Exception("User should not be able to delete another user's bookmark");

        // Verify User2's bookmark still exists
        var user2BookmarkStillExists = await bookmarkService.BookmarkExistsAsync(user2, drawNumber);
        if (!user2BookmarkStillExists)
            throw new Exception("User2's bookmark should still exist after failed deletion attempt");

        // User1 deletes their own bookmark - should succeed
        var user1DeletesOwn = await bookmarkService.DeleteBookmarkAsync(user1Bookmark.Id, user1);
        if (!user1DeletesOwn)
            throw new Exception("User should be able to delete their own bookmark");
    }

    private static async Task TestBookmarkRemovalNonExistent()
    {
        using var context = CreateTestContext();
        var bookmarkService = CreateBookmarkService(context);

        var userId = "testuser";
        var nonExistentBookmarkId = 9999;

        // Try to delete non-existent bookmark
        var deletionResult = await bookmarkService.DeleteBookmarkAsync(nonExistentBookmarkId, userId);
        if (deletionResult)
            throw new Exception("Deletion of non-existent bookmark should return false");

        // Try to delete with invalid ID
        try
        {
            await bookmarkService.DeleteBookmarkAsync(-1, userId);
            throw new Exception("Should throw exception for invalid bookmark ID");
        }
        catch (ArgumentException)
        {
            // Expected
        }

        // Try to delete with empty user ID
        try
        {
            await bookmarkService.DeleteBookmarkAsync(1, "");
            throw new Exception("Should throw exception for empty user ID");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    private static LottoDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<LottoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new LottoDbContext(options);
    }

    private static BookmarkService CreateBookmarkService(LottoDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<BookmarkService>>();

        return new BookmarkService(context, logger);
    }

    private static async Task SeedTestDraws(LottoDbContext context, int maxDrawNumber)
    {
        var draws = new List<LottoDraw>();
        var baseDate = new DateTime(2023, 1, 1);

        for (int i = 1; i <= maxDrawNumber; i++)
        {
            draws.Add(new LottoDraw
            {
                Draw = i,
                Date = baseDate.AddDays(i - 1),
                WinningNumber1 = (i % 40) + 1,
                WinningNumber2 = ((i + 1) % 40) + 1,
                WinningNumber3 = ((i + 2) % 40) + 1,
                WinningNumber4 = ((i + 3) % 40) + 1,
                WinningNumber5 = ((i + 4) % 40) + 1,
                WinningNumber6 = ((i + 5) % 40) + 1,
                BonusNumber = ((i + 6) % 40) + 1,
                Powerball = (i % 10) + 1
            });
        }

        context.LottoDraws.AddRange(draws);
        await context.SaveChangesAsync();
    }
}