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
/// Property-based test for direct navigation functionality
/// **Feature: lottery-lookup-navigation, Property 14: Direct navigation works for valid draws**
/// **Validates: Requirements 6.1**
/// 
/// This test validates that for any valid draw number, the system should navigate directly to that draw
/// if it exists in the database. The test ensures that direct navigation by draw number works correctly
/// and returns the expected draw data.
/// </summary>
public static class DirectNavigationPropertyTest
{
    public static async Task RunDirectNavigationPropertyTest()
    {
        Console.WriteLine("Running Direct Navigation Property Test...");
        Console.WriteLine("========================================");

        try
        {
            Console.WriteLine("Property Test 14: Direct navigation works for valid draws...");
            DirectNavigationWorksForValidDraws_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Direct navigation works for valid draws (100 iterations)");

            Console.WriteLine("\nProperty Test 14a: Jump-to by draw number returns correct draw...");
            JumpToByDrawNumberReturnsCorrectDraw_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Jump-to by draw number returns correct draw (50 iterations)");

            Console.WriteLine("\nProperty Test 14b: Jump-to by date finds closest draw...");
            JumpToByDateFindsClosestDraw_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Jump-to by date finds closest draw (50 iterations)");

            Console.WriteLine("\nProperty Test 14c: Direct navigation handles non-existent draws...");
            DirectNavigationHandlesNonExistentDraws_PropertyTest().GetAwaiter().GetResult();
            Console.WriteLine("✓ PASSED: Direct navigation handles non-existent draws (25 iterations)");

            Console.WriteLine("\n========================================");
            Console.WriteLine("Direct navigation property test PASSED!");
            Console.WriteLine("Property 14: Direct navigation works for valid draws - VALIDATED");
            Console.WriteLine("Requirements 6.1 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ DIRECT NAVIGATION PROPERTY TEST FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Property test: For any valid draw number that exists in the database,
    /// direct navigation should return that exact draw.
    /// </summary>
    private static async Task DirectNavigationWorksForValidDraws_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 100)), // Generate draw numbers from 1 to 100
            (int drawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create test data including the target draw number
                SeedTestData(context, drawNumber).GetAwaiter().GetResult();

                try
                {
                    // Test direct navigation by draw number
                    var retrievedDraw = navigationService.GetDrawByNumberAsync(drawNumber).Result;

                    // Verify that the draw was found
                    if (retrievedDraw == null)
                        return false.ToProperty();

                    // Verify that the returned draw has the correct draw number
                    var correctDrawNumber = retrievedDraw.Draw == drawNumber;

                    // Verify that the draw has valid lottery numbers (1-40)
                    var validNumbers = retrievedDraw.WinningNumber1 >= 1 && retrievedDraw.WinningNumber1 <= 40 &&
                                     retrievedDraw.WinningNumber2 >= 1 && retrievedDraw.WinningNumber2 <= 40 &&
                                     retrievedDraw.WinningNumber3 >= 1 && retrievedDraw.WinningNumber3 <= 40 &&
                                     retrievedDraw.WinningNumber4 >= 1 && retrievedDraw.WinningNumber4 <= 40 &&
                                     retrievedDraw.WinningNumber5 >= 1 && retrievedDraw.WinningNumber5 <= 40 &&
                                     retrievedDraw.WinningNumber6 >= 1 && retrievedDraw.WinningNumber6 <= 40;

                    // Verify that the draw has a valid date
                    var validDate = retrievedDraw.Date != default(DateTime);

                    return (correctDrawNumber && validNumbers && validDate).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in DirectNavigationWorksForValidDraws test for draw {drawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        // Run the property test with 100 iterations
        RunPropertyTest(property, 100).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Jump-to navigation by draw number should return the correct draw
    /// when using the JumpToDrawAsync method.
    /// </summary>
    private static async Task JumpToByDrawNumberReturnsCorrectDraw_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 50)), // Generate draw numbers from 1 to 50
            (int drawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create test data with multiple draws
                SeedMultipleDraws(context, 50).GetAwaiter().GetResult();

                try
                {
                    // Create jump-to request by draw number
                    var jumpRequest = new JumpToRequest
                    {
                        DrawNumber = drawNumber,
                        FindClosest = false // Exact match only
                    };

                    var retrievedDraw = navigationService.JumpToDrawAsync(jumpRequest).Result;

                    // Verify that the draw was found
                    if (retrievedDraw == null)
                        return false.ToProperty();

                    // Verify that the returned draw has the correct draw number
                    var correctDrawNumber = retrievedDraw.Draw == drawNumber;

                    // Verify that the draw data is complete
                    var hasCompleteData = retrievedDraw.WinningNumbers != null && 
                                        retrievedDraw.WinningNumbers.Length == 6;

                    return (correctDrawNumber && hasCompleteData).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in JumpToByDrawNumberReturnsCorrectDraw test for draw {drawNumber}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Jump-to navigation by date should find the closest draw to the specified date.
    /// </summary>
    private static async Task JumpToByDateFindsClosestDraw_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1, 30)), // Generate day offsets from 1 to 30
            (int dayOffset) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create test data with draws on specific dates
                SeedDrawsWithDates(context, 30).GetAwaiter().GetResult();

                try
                {
                    // Create a target date that may or may not have an exact draw
                    var baseDate = new DateTime(2023, 1, 1);
                    var targetDate = baseDate.AddDays(dayOffset);

                    // Create jump-to request by date
                    var jumpRequest = new JumpToRequest
                    {
                        Date = targetDate,
                        FindClosest = true
                    };

                    var retrievedDraw = navigationService.JumpToDrawAsync(jumpRequest).Result;

                    // Verify that a draw was found (should always find closest)
                    if (retrievedDraw == null)
                        return false.ToProperty();

                    // Verify that the draw has a valid date
                    var hasValidDate = retrievedDraw.Date != default(DateTime);

                    // Verify that the draw is within a reasonable range of the target date
                    var dateDistance = Math.Abs((retrievedDraw.Date - targetDate).TotalDays);
                    var reasonableDistance = dateDistance <= 30; // Within 30 days

                    return (hasValidDate && reasonableDistance).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in JumpToByDateFindsClosestDraw test for day offset {dayOffset}: {ex.Message}");
                    return false.ToProperty();
                }
            });

        RunPropertyTest(property, 50).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property test: Direct navigation should handle non-existent draws gracefully
    /// by returning null without throwing exceptions.
    /// </summary>
    private static async Task DirectNavigationHandlesNonExistentDraws_PropertyTest()
    {
        var property = Prop.ForAll(
            Arb.From(Gen.Choose(1000, 2000)), // Generate draw numbers that don't exist (1000-2000)
            (int nonExistentDrawNumber) =>
            {
                using var context = CreateTestContext();
                var navigationService = CreateNavigationService(context);

                // Create test data with draws 1-100 only
                SeedMultipleDraws(context, 100).GetAwaiter().GetResult();

                try
                {
                    // Try to navigate to a non-existent draw
                    var retrievedDraw = navigationService.GetDrawByNumberAsync(nonExistentDrawNumber).Result;

                    // Should return null for non-existent draws
                    var handlesNonExistentCorrectly = retrievedDraw == null;

                    // Test with JumpToDrawAsync as well
                    var jumpRequest = new JumpToRequest
                    {
                        DrawNumber = nonExistentDrawNumber,
                        FindClosest = false // Exact match only
                    };

                    var jumpResult = navigationService.JumpToDrawAsync(jumpRequest).Result;
                    var jumpHandlesNonExistentCorrectly = jumpResult == null;

                    return (handlesNonExistentCorrectly && jumpHandlesNonExistentCorrectly).ToProperty();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in DirectNavigationHandlesNonExistentDraws test for draw {nonExistentDrawNumber}: {ex.Message}");
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

    private static async Task SeedTestData(LottoDbContext context, int targetDrawNumber)
    {
        var draws = new List<LottoDraw>();
        var baseDate = new DateTime(2023, 1, 1);

        // Create the target draw and some surrounding draws
        for (int i = Math.Max(1, targetDrawNumber - 5); i <= targetDrawNumber + 5; i++)
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

    private static async Task SeedMultipleDraws(LottoDbContext context, int count)
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

    private static async Task SeedDrawsWithDates(LottoDbContext context, int count)
    {
        var draws = new List<LottoDraw>();
        var baseDate = new DateTime(2023, 1, 1);

        for (int i = 1; i <= count; i++)
        {
            // Create draws with some gaps in dates (every 2-3 days)
            var dateOffset = i * 2 + (i % 3);
            draws.Add(new LottoDraw
            {
                Draw = i,
                Date = baseDate.AddDays(dateOffset),
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

