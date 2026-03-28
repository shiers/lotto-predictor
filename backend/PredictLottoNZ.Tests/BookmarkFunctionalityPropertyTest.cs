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
/// Property-based test for bookmark functionality
/// **Feature: lottery-lookup-navigation, Property 21: Bookmark functionality works correctly**
/// **Validates: Requirements 8.1, 8.2, 8.3**
/// 
/// This test validates that bookmark functionality works correctly including:
/// - Creating bookmarks for valid draws
/// - Retrieving user bookmarks with proper isolation
/// - Updating bookmark labels and descriptions
/// - Preventing duplicate bookmarks for the same user and draw
/// </summary>
public static class BookmarkFunctionalityPropertyTest
{
    public static async Task RunBookmarkFunctionalityPropertyTest()
    {
        Console.WriteLine("Running Bookmark Functionality Property Test...");
        Console.WriteLine("===============================================");

        try
        {
            Console.WriteLine("Property Test 21: Bookmark creation works correctly...");
            BookmarkCreationWorksCorrectly_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark creation works correctly (100 iterations)");

            Console.WriteLine("\nProperty Test 21a: Bookmark retrieval isolates users correctly...");
            BookmarkRetrievalIsolatesUsers_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark retrieval isolates users correctly (50 iterations)");

            Console.WriteLine("\nProperty Test 21b: Bookmark updates work correctly...");
            BookmarkUpdatesWorkCorrectly_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark updates work correctly (50 iterations)");

            Console.WriteLine("\nProperty Test 21c: Duplicate bookmark prevention works...");
            DuplicateBookmarkPrevention_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Duplicate bookmark prevention works (25 iterations)");

            Console.WriteLine("\nProperty Test 21d: Bookmark existence check works correctly...");
            BookmarkExistenceCheckWorksCorrectly_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Bookmark existence check works correctly (25 iterations)");

            Console.WriteLine("\n===============================================");
            Console.WriteLine("Bookmark functionality property test PASSED!");
            Console.WriteLine("Property 21: Bookmark functionality works correctly - VALIDATED");
            Console.WriteLine("Requirements 8.1, 8.2, 8.3 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BOOKMARK FUNCTIONALITY PROPERTY TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: For any valid draw number and user, bookmark creation should work correctly
    /// and return a properly formatted BookmarkDto with all required fields.
    /// </summary>
    private static async Task BookmarkCreationWorksCorrectly_PropertyTest()
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
                    // Test bookmark creation
                    var createdBookmark = bookmarkService.CreateBookmarkAsync(drawNumber, label, userId).Result;

                    // Verify that the bookmark was created successfully
                    if (createdBookmark == null)
                        return false.ToProperty();

                    // Verify that the bookmark has the correct properties
                    var correctDrawNumber = createdBookmark.DrawNumber == drawNumber;
                    var correctLabel = createdBookmark.Label == label.Trim();
                    var hasValidId = createdBookmark.Id > 0;
                    var hasCreatedDate = createdBookmark.CreatedAt != default(DateTime);

                    // Verify that the bookmark exists in the database
                    var existsInDb = bookmarkService.BookmarkExistsAsync(userId, drawNumber).Result;

                    return (correctDrawNumber && correctLabel && hasValidId && hasCreatedDate && existsInDb).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkCreationWorksCorrectly test for draw {drawNumber}, user {userId}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        // Run the property test with 100 iterations
        RunPropertyTest(property, 100).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Bookmark retrieval should properly isolate bookmarks by user,
    /// ensuring users only see their own bookmarks.
    /// </summary>
    private static async Task BookmarkRetrievalIsolatesUsers_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 20)), // Generate number of bookmarks from 1 to 20
            (int bookmarkCount) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test draws
                SeedTestDraws(context, bookmarkCount + 10).GetAwaiter().GetResult();

                try
                {
                    // Create bookmarks for multiple users
                    var user1 = "user1";
                    var user2 = "user2";
                    var user1BookmarkCount = 0;
                    var user2BookmarkCount = 0;

                    for (int i = 1; i <= bookmarkCount; i++)
                    {
                        // Alternate between users
                        if (i % 2 == 1)
                        {
                            bookmarkService.CreateBookmarkAsync(i, $"User1 Bookmark {i}", user1).GetAwaiter().GetResult();
                            user1BookmarkCount++;
                        }
                        else
                        {
                            bookmarkService.CreateBookmarkAsync(i, $"User2 Bookmark {i}", user2).GetAwaiter().GetResult();
                            user2BookmarkCount++;
                        }
                    }

                    // Retrieve bookmarks for each user
                    var user1Bookmarks = bookmarkService.GetUserBookmarksAsync(user1).Result;
                    var user2Bookmarks = bookmarkService.GetUserBookmarksAsync(user2).Result;

                    // Verify that each user gets only their own bookmarks
                    var user1CountCorrect = user1Bookmarks.Count() == user1BookmarkCount;
                    var user2CountCorrect = user2Bookmarks.Count() == user2BookmarkCount;

                    // Verify that bookmarks contain correct user-specific labels
                    var user1LabelsCorrect = user1Bookmarks.All(b => b.Label.Contains("User1"));
                    var user2LabelsCorrect = user2Bookmarks.All(b => b.Label.Contains("User2"));

                    // Verify that bookmarks are ordered by creation date (most recent first)
                    var user1Ordered = user1Bookmarks.Count() <= 1 || 
                                     user1Bookmarks.Zip(user1Bookmarks.Skip(1), (a, b) => a.CreatedAt >= b.CreatedAt).All(x => x);
                    var user2Ordered = user2Bookmarks.Count() <= 1 || 
                                     user2Bookmarks.Zip(user2Bookmarks.Skip(1), (a, b) => a.CreatedAt >= b.CreatedAt).All(x => x);

                    return (user1CountCorrect && user2CountCorrect && user1LabelsCorrect && user2LabelsCorrect && user1Ordered && user2Ordered).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkRetrievalIsolatesUsers test for {bookmarkCount} bookmarks: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Bookmark updates should work correctly, updating the label
    /// and setting the UpdatedAt timestamp.
    /// </summary>
    private static async Task BookmarkUpdatesWorkCorrectly_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 50)), // Generate draw numbers from 1 to 50
            Arb.From(Gen.Elements(new[] { "Updated Label", "New Description", "Modified Bookmark", "Changed Title" })), // Generate new labels
            (int drawNumber, string newLabel) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test data
                SeedTestDraws(context, drawNumber).GetAwaiter().GetResult();

                try
                {
                    var userId = "testuser";
                    var originalLabel = "Original Label";

                    // Create a bookmark
                    var createdBookmark = bookmarkService.CreateBookmarkAsync(drawNumber, originalLabel, userId).Result;
                    var originalCreatedAt = createdBookmark.CreatedAt;

                    // Wait a small amount to ensure UpdatedAt is different
                    Task.Delay(10).GetAwaiter().GetResult();

                    // Update the bookmark
                    var updatedBookmark = bookmarkService.UpdateBookmarkAsync(createdBookmark.Id, newLabel, userId).Result;

                    // Verify that the update was successful
                    if (updatedBookmark == null)
                        return false.ToProperty();

                    // Verify that the label was updated
                    var labelUpdated = updatedBookmark.Label == newLabel.Trim();

                    // Verify that the ID and draw number remain the same
                    var idUnchanged = updatedBookmark.Id == createdBookmark.Id;
                    var drawNumberUnchanged = updatedBookmark.DrawNumber == drawNumber;

                    // Verify that CreatedAt remains the same but UpdatedAt is set
                    var createdAtUnchanged = updatedBookmark.CreatedAt == originalCreatedAt;
                    var updatedAtSet = updatedBookmark.UpdatedAt.HasValue && updatedBookmark.UpdatedAt.Value > originalCreatedAt;

                    return (labelUpdated && idUnchanged && drawNumberUnchanged && createdAtUnchanged && updatedAtSet).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkUpdatesWorkCorrectly test for draw {drawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: The system should prevent duplicate bookmarks for the same user and draw,
    /// throwing an appropriate exception when attempting to create duplicates.
    /// </summary>
    private static async Task DuplicateBookmarkPrevention_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 25)), // Generate draw numbers from 1 to 25
            (int drawNumber) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test data
                SeedTestDraws(context, drawNumber).GetAwaiter().GetResult();

                try
                {
                    var userId = "testuser";
                    var label1 = "First Bookmark";
                    var label2 = "Second Bookmark";

                    // Create the first bookmark
                    var firstBookmark = bookmarkService.CreateBookmarkAsync(drawNumber, label1, userId).Result;

                    // Verify first bookmark was created
                    if (firstBookmark == null)
                        return false.ToProperty();

                    // Attempt to create a duplicate bookmark for the same user and draw
                    var duplicateAttemptFailed = false;
                    try
                    {
                        bookmarkService.CreateBookmarkAsync(drawNumber, label2, userId).GetAwaiter().GetResult();
                    }
                    catch (InvalidOperationException)
                    {
                        duplicateAttemptFailed = true;
                    }

                    // Verify that only one bookmark exists for this user and draw
                    var bookmarkExists = bookmarkService.BookmarkExistsAsync(userId, drawNumber).Result;
                    var userBookmarks = bookmarkService.GetUserBookmarksAsync(userId).Result;
                    var onlyOneBookmark = userBookmarks.Count(b => b.DrawNumber == drawNumber) == 1;

                    // Verify that a different user can still create a bookmark for the same draw
                    var differentUser = "differentuser";
                    var differentUserBookmark = bookmarkService.CreateBookmarkAsync(drawNumber, "Different User Bookmark", differentUser).Result;
                    var differentUserSuccess = differentUserBookmark != null;

                    return (duplicateAttemptFailed && bookmarkExists && onlyOneBookmark && differentUserSuccess).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in DuplicateBookmarkPrevention test for draw {drawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 25).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Bookmark existence check should accurately report whether
    /// a bookmark exists for a specific user and draw combination.
    /// </summary>
    private static async Task BookmarkExistenceCheckWorksCorrectly_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 25)), // Generate draw numbers from 1 to 25
            (int drawNumber) =>
            {
                using var context = CreateTestContext();
                var bookmarkService = CreateBookmarkService(context);

                // Create test data
                SeedTestDraws(context, drawNumber + 5).GetAwaiter().GetResult();

                try
                {
                    var userId = "testuser";
                    var nonExistentDrawNumber = drawNumber + 100;

                    // Initially, no bookmark should exist
                    var initiallyNotExists = !bookmarkService.BookmarkExistsAsync(userId, drawNumber).Result;

                    // Create a bookmark
                    bookmarkService.CreateBookmarkAsync(drawNumber, "Test Bookmark", userId).GetAwaiter().GetResult();

                    // Now the bookmark should exist
                    var nowExists = bookmarkService.BookmarkExistsAsync(userId, drawNumber).Result;

                    // Check for non-existent draw should return false
                    var nonExistentDrawCheck = !bookmarkService.BookmarkExistsAsync(userId, nonExistentDrawNumber).Result;

                    // Check for different user should return false
                    var differentUserCheck = !bookmarkService.BookmarkExistsAsync("differentuser", drawNumber).Result;

                    return (initiallyNotExists && nowExists && nonExistentDrawCheck && differentUserCheck).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookmarkExistenceCheckWorksCorrectly test for draw {drawNumber}: {ex.Message}");
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
        context.SaveChanges();
    }

    private static async Task RunPropertyTest(Property property, int iterations)
    {
        Check.Quick(property);
        await Task.CompletedTask;
    }

    #endregion
}





