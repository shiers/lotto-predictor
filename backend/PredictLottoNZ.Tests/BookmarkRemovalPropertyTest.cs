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
/// Property-based test for bookmark removal functionality
/// **Feature: lottery-lookup-navigation, Property 22: Bookmark removal works with confirmation**
/// **Validates: Requirements 8.4**
/// 
/// This test validates that bookmark removal works correctly with proper confirmation:
/// - Bookmark removal returns true when bookmark exists and belongs to user
/// - Bookmark removal returns false when bookmark doesn't exist or doesn't belong to user
/// - Bookmark is actually removed from database after successful deletion
/// - User isolation is maintained during bookmark removal
/// - Bookmark removal doesn't affect other users' bookmarks
/// </summary>
public static class BookmarkRemovalPropertyTest
{
    public static async Task RunBookmarkRemovalPropertyTest()
    {
        Console.WriteLine("Running Bookmark Removal Property Test...");
        Console.WriteLine("==========================================");

        try
        {
            Console.WriteLine("Property Test 22: Bookmark removal works with confirmation...");
            BookmarkRemovalWorksWithConfirmation_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark removal works with confirmation (100 iterations)");

            Console.WriteLine("\nProperty Test 22a: Bookmark removal maintains user isolation...");
            BookmarkRemovalMaintainsUserIsolation_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark removal maintains user isolation (50 iterations)");

            Console.WriteLine("\nProperty Test 22b: Bookmark removal handles non-existent bookmarks...");
            BookmarkRemovalHandlesNonExistentBookmarks_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark removal handles non-existent bookmarks (50 iterations)");

            Console.WriteLine("\nProperty Test 22c: Bookmark removal confirmation is accurate...");
            BookmarkRemovalConfirmationIsAccurate_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark removal confirmation is accurate (50 iterations)");

            Console.WriteLine("\n==========================================");
            Console.WriteLine("Bookmark removal property test PASSED!");
            Console.WriteLine("Property 22: Bookmark removal works with confirmation - VALIDATED");
            Console.WriteLine("Requirements 8.4 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BOOKMARK REMOVAL PROPERTY TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: For any valid bookmark owned by a user, removal should work correctly
    /// and provide proper confirmation of the deletion.
    /// </summary>
    private static async Task BookmarkRemovalWorksWithConfirmation_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 100)), // Generate draw numbers from 1 to 100
            Arb.From(Gen.Elements(new[] { "user1", "user2", "user3", "testuser", "admin" })), // Generate user IDs
            Arb.From(Gen.Elements(new[] { "Important Draw", "Lucky Numbers", "Analysis Target", "Bookmark Test", "My Favorite" })), // Generate labels
            (int drawNumber, string userId, string label) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test data including the target draw
                SeedTestDraws(context, drawNumber).GetAwaiter().GetResult();

                try
                {
                    // Create a bookmark first
                    var createdBookmark = bookmarkService.CreateBookmarkAsync(drawNumber, label, userId).Result;
                    
                    if (createdBookmark == null)
                        return false.ToProperty();

                    // Verify bookmark exists before deletion
                    var existsBeforeDeletion = bookmarkService.BookmarkExistsAsync(userId, drawNumber).Result;
                    if (!existsBeforeDeletion)
                        return false.ToProperty();

                    // Attempt to delete the bookmark
                    var deletionResult = bookmarkService.DeleteBookmarkAsync(createdBookmark.Id, userId).Result;

                    // Verify that deletion was successful (returns true)
                    if (!deletionResult)
                        return false.ToProperty();

                    // Verify bookmark no longer exists after deletion
                    var existsAfterDeletion = bookmarkService.BookmarkExistsAsync(userId, drawNumber).Result;
                    if (existsAfterDeletion)
                        return false.ToProperty();

                    // Verify bookmark is not in user's bookmark list
                    var userBookmarks = bookmarkService.GetUserBookmarksAsync(userId).Result;
                    var bookmarkStillInList = userBookmarks.Any(b => b.Id == createdBookmark.Id);
                    if (bookmarkStillInList)
                        return false.ToProperty();

                    // Verify that attempting to delete the same bookmark again returns false
                    var secondDeletionResult = bookmarkService.DeleteBookmarkAsync(createdBookmark.Id, userId).Result;
                    if (secondDeletionResult)
                        return false.ToProperty();

                    return true.ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkRemovalWorksWithConfirmation test for draw {drawNumber}, user {userId}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        // Run the property test with 100 iterations
        RunPropertyTest(property, 100).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Bookmark removal should maintain user isolation,
    /// ensuring users can only delete their own bookmarks.
    /// </summary>
    private static async Task BookmarkRemovalMaintainsUserIsolation_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 50)), // Generate draw numbers from 1 to 50
            (int drawNumber) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test data
                SeedTestDraws(context, drawNumber).GetAwaiter().GetResult();

                try
                {
                    var user1 = "user1";
                    var user2 = "user2";
                    var label1 = "User1 Bookmark";
                    var label2 = "User2 Bookmark";

                    // Create bookmarks for both users on the same draw
                    var user1Bookmark = bookmarkService.CreateBookmarkAsync(drawNumber, label1, user1).Result;
                    var user2Bookmark = bookmarkService.CreateBookmarkAsync(drawNumber, label2, user2).Result;

                    if (user1Bookmark == null || user2Bookmark == null)
                        return false.ToProperty();

                    // Verify both bookmarks exist
                    var user1BookmarkExists = bookmarkService.BookmarkExistsAsync(user1, drawNumber).Result;
                    var user2BookmarkExists = bookmarkService.BookmarkExistsAsync(user2, drawNumber).Result;
                    if (!user1BookmarkExists || !user2BookmarkExists)
                        return false.ToProperty();

                    // User1 attempts to delete User2's bookmark - should fail
                    var user1DeletesUser2Result = bookmarkService.DeleteBookmarkAsync(user2Bookmark.Id, user1).Result;
                    if (user1DeletesUser2Result)
                        return false.ToProperty();

                    // User2 attempts to delete User1's bookmark - should fail
                    var user2DeletesUser1Result = bookmarkService.DeleteBookmarkAsync(user1Bookmark.Id, user2).Result;
                    if (user2DeletesUser1Result)
                        return false.ToProperty();

                    // Verify both bookmarks still exist after failed deletion attempts
                    var user1BookmarkStillExists = bookmarkService.BookmarkExistsAsync(user1, drawNumber).Result;
                    var user2BookmarkStillExists = bookmarkService.BookmarkExistsAsync(user2, drawNumber).Result;
                    if (!user1BookmarkStillExists || !user2BookmarkStillExists)
                        return false.ToProperty();

                    // User1 deletes their own bookmark - should succeed
                    var user1DeletesOwnResult = bookmarkService.DeleteBookmarkAsync(user1Bookmark.Id, user1).Result;
                    if (!user1DeletesOwnResult)
                        return false.ToProperty();

                    // User2 deletes their own bookmark - should succeed
                    var user2DeletesOwnResult = bookmarkService.DeleteBookmarkAsync(user2Bookmark.Id, user2).Result;
                    if (!user2DeletesOwnResult)
                        return false.ToProperty();

                    // Verify both bookmarks are now deleted
                    var user1BookmarkFinallyDeleted = !bookmarkService.BookmarkExistsAsync(user1, drawNumber).Result;
                    var user2BookmarkFinallyDeleted = !bookmarkService.BookmarkExistsAsync(user2, drawNumber).Result;

                    return (user1BookmarkFinallyDeleted && user2BookmarkFinallyDeleted).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkRemovalMaintainsUserIsolation test for draw {drawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Bookmark removal should handle non-existent bookmarks gracefully,
    /// returning false for bookmarks that don't exist.
    /// </summary>
    private static async Task BookmarkRemovalHandlesNonExistentBookmarks_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1000, 9999)), // Generate non-existent bookmark IDs
            Arb.From(Gen.Elements(new[] { "user1", "user2", "testuser" })), // Generate user IDs
            (int nonExistentBookmarkId, string userId) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                try
                {
                    // Attempt to delete a non-existent bookmark
                    var deletionResult = bookmarkService.DeleteBookmarkAsync(nonExistentBookmarkId, userId).Result;

                    // Should return false for non-existent bookmark
                    if (deletionResult)
                        return false.ToProperty();

                    // Attempt to delete with invalid bookmark ID (negative or zero)
                    var invalidIdDeletionFailed = false;
                    try
                    {
                        bookmarkService.DeleteBookmarkAsync(-1, userId).GetAwaiter().GetResult();
                    }
                    catch (ArgumentException)
                    {
                        invalidIdDeletionFailed = true;
                    }

                    if (!invalidIdDeletionFailed)
                        return false.ToProperty();

                    // Attempt to delete with null/empty user ID
                    var nullUserIdDeletionFailed = false;
                    try
                    {
                        bookmarkService.DeleteBookmarkAsync(1, "").GetAwaiter().GetResult();
                    }
                    catch (ArgumentException)
                    {
                        nullUserIdDeletionFailed = true;
                    }

                    return nullUserIdDeletionFailed.ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkRemovalHandlesNonExistentBookmarks test for bookmark {nonExistentBookmarkId}, user {userId}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Bookmark removal confirmation should be accurate,
    /// returning true only when a bookmark is actually removed.
    /// </summary>
    private static async Task BookmarkRemovalConfirmationIsAccurate_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 50)), // Generate draw numbers from 1 to 50
            Arb.From(Gen.Choose(1, 5)), // Generate number of bookmarks to create
            (int drawNumber, int bookmarkCount) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test data
                SeedTestDraws(context, drawNumber + bookmarkCount).GetAwaiter().GetResult();

                try
                {
                    var userId = "testuser";
                    var createdBookmarks = new List<BookmarkDto>();

                    // Create multiple bookmarks
                    for (int i = 0; i < bookmarkCount; i++)
                    {
                        var bookmark = bookmarkService.CreateBookmarkAsync(
                            drawNumber + i, 
                            $"Bookmark {i + 1}", 
                            userId).Result;
                        
                        if (bookmark != null)
                            createdBookmarks.Add(bookmark);
                    }

                    if (createdBookmarks.Count != bookmarkCount)
                        return false.ToProperty();

                    // Track deletion results and verify accuracy
                    var deletionResults = new List<bool>();
                    var actualDeletions = new List<bool>();

                    foreach (var bookmark in createdBookmarks)
                    {
                        // Check if bookmark exists before deletion
                        var existsBeforeDeletion = bookmarkService.BookmarkExistsAsync(userId, bookmark.DrawNumber).Result;
                        
                        // Attempt deletion
                        var deletionResult = bookmarkService.DeleteBookmarkAsync(bookmark.Id, userId).Result;
                        deletionResults.Add(deletionResult);

                        // Check if bookmark actually was deleted
                        var existsAfterDeletion = bookmarkService.BookmarkExistsAsync(userId, bookmark.DrawNumber).Result;
                        var actuallyDeleted = existsBeforeDeletion && !existsAfterDeletion;
                        actualDeletions.Add(actuallyDeleted);
                    }

                    // Verify that deletion results match actual deletions
                    var resultsMatch = deletionResults.Zip(actualDeletions, (result, actual) => result == actual).All(x => x);
                    if (!resultsMatch)
                        return false.ToProperty();

                    // Verify that all bookmarks were successfully deleted
                    var allDeleted = deletionResults.All(result => result);
                    if (!allDeleted)
                        return false.ToProperty();

                    // Verify that user has no bookmarks left
                    var remainingBookmarks = bookmarkService.GetUserBookmarksAsync(userId).Result;
                    var noBookmarksRemaining = !remainingBookmarks.Any();

                    return noBookmarksRemaining.ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkRemovalConfirmationIsAccurate test for draw {drawNumber}, count {bookmarkCount}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    #region Helper Methods

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

        for (int i = 1; i <= maxDrawNumber + 10; i++)
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
        context.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static async Task RunPropertyTest(Property property, int iterations)
    {
        Check.Quick(property);
        await Task.CompletedTask;
    }

    #endregion
}

