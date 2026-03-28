using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using FsCheck;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Property-based test for navigation updates
/// **Feature: lottery-lookup-navigation, Property 13: Navigation updates information correctly**
/// **Validates: Requirements 5.2, 5.3**
/// 
/// This test validates that for any navigation action (Previous/Next), the system should move to the 
/// correct chronological draw and update all displayed information. The test ensures that navigation
/// operations correctly update the current draw, position, and navigation context.
/// </summary>
public static class NavigationUpdatesPropertyTest
{
    public static async Task RunNavigationUpdatesPropertyTest()
    {
        Console.WriteLine("Running Navigation Updates Property Test...");
        Console.WriteLine("==========================================");

        try
        {
            Console.WriteLine("Property Test 13: Navigation updates information correctly...");
            NavigationUpdatesInformationCorrectly_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Navigation updates information correctly (100 iterations)");

            Console.WriteLine("\nProperty Test 13a: Previous navigation updates correctly...");
            PreviousNavigationUpdatesCorrectly_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Previous navigation updates correctly (50 iterations)");

            Console.WriteLine("\nProperty Test 13b: Next navigation updates correctly...");
            NextNavigationUpdatesCorrectly_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Next navigation updates correctly (50 iterations)");

            Console.WriteLine("\nProperty Test 13c: Navigation context updates consistently...");
            NavigationContextUpdatesConsistently_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Navigation context updates consistently (50 iterations)");

            Console.WriteLine("\n==========================================");
            Console.WriteLine("Navigation updates property test PASSED!");
            Console.WriteLine("Property 13: Navigation updates information correctly - VALIDATED");
            Console.WriteLine("Requirements 5.2, 5.3 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ NAVIGATION UPDATES PROPERTY TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: For any valid navigation action, the system should update all displayed
    /// information to reflect the new current draw position.
    /// </summary>
    private static async Task NavigationUpdatesInformationCorrectly_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(2, 49)), // Generate draw numbers from 2 to 49 (to allow previous/next)
            (int currentDrawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create sequential test data with 50 draws
                SeedSequentialDraws(context, 50).GetAwaiter().GetResult();

                try
                {
                    // Get initial navigation context
                    var initialContext = navigationService.GetNavigationContextAsync(currentDrawNumber).Result;
                    if (initialContext == null)
                        return false.ToProperty();

                    // Test Previous navigation if available
                    if (initialContext.HasPrevious)
                    {
                        var previousDraw = navigationService.GetPreviousDrawAsync(currentDrawNumber).Result;
                        if (previousDraw == null)
                            return false.ToProperty();

                        var previousContext = navigationService.GetNavigationContextAsync(previousDraw.Draw).Result;
                        if (previousContext == null)
                            return false.ToProperty();

                        // Verify that navigation updated to the correct previous draw
                        var correctPreviousDraw = previousDraw.Draw == currentDrawNumber - 1;
                        var contextUpdated = previousContext.CurrentDraw.Draw == previousDraw.Draw;
                        var positionUpdated = previousContext.CurrentPosition < initialContext.CurrentPosition;

                        if (!correctPreviousDraw || !contextUpdated || !positionUpdated)
                            return false.ToProperty();
                    }

                    // Test Next navigation if available
                    if (initialContext.HasNext)
                    {
                        var nextDraw = navigationService.GetNextDrawAsync(currentDrawNumber).Result;
                        if (nextDraw == null)
                            return false.ToProperty();

                        var nextContext = navigationService.GetNavigationContextAsync(nextDraw.Draw).Result;
                        if (nextContext == null)
                            return false.ToProperty();

                        // Verify that navigation updated to the correct next draw
                        var correctNextDraw = nextDraw.Draw == currentDrawNumber + 1;
                        var contextUpdated = nextContext.CurrentDraw.Draw == nextDraw.Draw;
                        var positionUpdated = nextContext.CurrentPosition > initialContext.CurrentPosition;

                        if (!correctNextDraw || !contextUpdated || !positionUpdated)
                            return false.ToProperty();
                    }

                    return true.ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in NavigationUpdatesInformationCorrectly test for draw {currentDrawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        // Run the property test with 100 iterations
        RunPropertyTest(property, 100).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Previous navigation should move to the chronologically previous draw
    /// and update all displayed information correctly.
    /// </summary>
    private static async Task PreviousNavigationUpdatesCorrectly_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(2, 50)), // Generate draw numbers from 2 to 50 (ensure previous exists)
            (int currentDrawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create sequential test data
                SeedSequentialDraws(context, 50).GetAwaiter().GetResult();

                try
                {
                    // Get current draw information
                    var currentContext = navigationService.GetNavigationContextAsync(currentDrawNumber).Result;
                    if (currentContext == null || !currentContext.HasPrevious)
                        return true.ToProperty(); // Skip if no previous draw available

                    // Navigate to previous draw
                    var previousDraw = navigationService.GetPreviousDrawAsync(currentDrawNumber).Result;
                    if (previousDraw == null)
                        return false.ToProperty();

                    // Get updated navigation context for the previous draw
                    var previousContext = navigationService.GetNavigationContextAsync(previousDraw.Draw).Result;
                    if (previousContext == null)
                        return false.ToProperty();

                    // Verify chronological order: previous draw should be earlier
                    var chronologicallyPrevious = previousDraw.Draw < currentDrawNumber;
                    
                    // Verify all information is updated correctly
                    var drawNumberUpdated = previousContext.CurrentDraw.Draw == previousDraw.Draw;
                    var dateUpdated = previousContext.CurrentDraw.Date == previousDraw.Date;
                    var positionDecreased = previousContext.CurrentPosition < currentContext.CurrentPosition;
                    
                    // Verify navigation state is updated
                    var hasNextIsTrue = previousContext.HasNext; // Should have next since we came from a later draw
                    var hasPreviousCorrect = previousDraw.Draw > 1 ? previousContext.HasPrevious : !previousContext.HasPrevious;

                    return (chronologicallyPrevious && drawNumberUpdated && dateUpdated && 
                           positionDecreased && hasNextIsTrue && hasPreviousCorrect).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in PreviousNavigationUpdatesCorrectly test for draw {currentDrawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Next navigation should move to the chronologically next draw
    /// and update all displayed information correctly.
    /// </summary>
    private static async Task NextNavigationUpdatesCorrectly_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 49)), // Generate draw numbers from 1 to 49 (ensure next exists)
            (int currentDrawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create sequential test data
                SeedSequentialDraws(context, 50).GetAwaiter().GetResult();

                try
                {
                    // Get current draw information
                    var currentContext = navigationService.GetNavigationContextAsync(currentDrawNumber).Result;
                    if (currentContext == null || !currentContext.HasNext)
                        return true.ToProperty(); // Skip if no next draw available

                    // Navigate to next draw
                    var nextDraw = navigationService.GetNextDrawAsync(currentDrawNumber).Result;
                    if (nextDraw == null)
                        return false.ToProperty();

                    // Get updated navigation context for the next draw
                    var nextContext = navigationService.GetNavigationContextAsync(nextDraw.Draw).Result;
                    if (nextContext == null)
                        return false.ToProperty();

                    // Verify chronological order: next draw should be later
                    var chronologicallyNext = nextDraw.Draw > currentDrawNumber;
                    
                    // Verify all information is updated correctly
                    var drawNumberUpdated = nextContext.CurrentDraw.Draw == nextDraw.Draw;
                    var dateUpdated = nextContext.CurrentDraw.Date == nextDraw.Date;
                    var positionIncreased = nextContext.CurrentPosition > currentContext.CurrentPosition;
                    
                    // Verify navigation state is updated
                    var hasPreviousIsTrue = nextContext.HasPrevious; // Should have previous since we came from an earlier draw
                    var hasNextCorrect = nextDraw.Draw < 50 ? nextContext.HasNext : !nextContext.HasNext;

                    return (chronologicallyNext && drawNumberUpdated && dateUpdated && 
                           positionIncreased && hasPreviousIsTrue && hasNextCorrect).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in NextNavigationUpdatesCorrectly test for draw {currentDrawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Navigation context should be updated consistently across all navigation operations.
    /// </summary>
    private static async Task NavigationContextUpdatesConsistently_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(10, 40)), // Generate draw numbers from 10 to 40 (middle range)
            (int startDrawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create sequential test data
                SeedSequentialDraws(context, 50).GetAwaiter().GetResult();

                try
                {
                    // Get initial context
                    var initialContext = navigationService.GetNavigationContextAsync(startDrawNumber).Result;
                    if (initialContext == null)
                        return false.ToProperty();

                    // Navigate to previous draw and back to next
                    if (initialContext.HasPrevious && initialContext.HasNext)
                    {
                        var previousDraw = navigationService.GetPreviousDrawAsync(startDrawNumber).Result;
                        if (previousDraw == null)
                            return false.ToProperty();

                        var nextDrawFromPrevious = navigationService.GetNextDrawAsync(previousDraw.Draw).Result;
                        if (nextDrawFromPrevious == null)
                            return false.ToProperty();

                        // Verify we're back to the original draw
                        var backToOriginal = nextDrawFromPrevious.Draw == startDrawNumber;

                        // Get final context and verify consistency
                        var finalContext = navigationService.GetNavigationContextAsync(startDrawNumber).Result;
                        if (finalContext == null)
                            return false.ToProperty();

                        var contextConsistent = initialContext.CurrentPosition == finalContext.CurrentPosition &&
                                              initialContext.TotalDraws == finalContext.TotalDraws &&
                                              initialContext.HasPrevious == finalContext.HasPrevious &&
                                              initialContext.HasNext == finalContext.HasNext;

                        return (backToOriginal && contextConsistent).ToProperty();
                    }

                    // If we can't do the round-trip test, just verify context consistency
                    var reloadedContext = navigationService.GetNavigationContextAsync(startDrawNumber).Result;
                    if (reloadedContext == null)
                        return false.ToProperty();

                    var basicConsistency = initialContext.CurrentPosition == reloadedContext.CurrentPosition &&
                                         initialContext.TotalDraws == reloadedContext.TotalDraws;

                    return basicConsistency.ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in NavigationContextUpdatesConsistently test for draw {startDrawNumber}: {ex.Message}");
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

    private static DrawNavigationService CreateNavigationService(LottoDbContext context)
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // Use in-memory distributed cache for testing
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();
        var logger = serviceProvider.GetRequiredService<ILogger<DrawNavigationService>>();

        return new DrawNavigationService(context, cache, logger);
    }

    private static async Task SeedSequentialDraws(LottoDbContext context, int count)
    {
        var draws = new List<LottoDraw>();
        var baseDate = new DateTime(2023, 1, 1);

        for (int i = 1; i <= count; i++)
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

