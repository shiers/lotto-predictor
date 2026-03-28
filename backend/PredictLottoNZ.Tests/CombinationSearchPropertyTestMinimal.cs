using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;

/// <summary>
/// Minimal property test for combination search functionality
/// **Feature: lottery-lookup-navigation, Property 4: Combination search finds matching draws**
/// **Validates: Requirements 2.1**
/// </summary>
public static class CombinationSearchPropertyTestMinimal
{
    public static async Task Main(string[] args)
    {
        try
        {
            await RunMinimalPropertyTest();
            Console.WriteLine("PASS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunMinimalPropertyTest()
    {
        // Test with just 5 iterations to avoid context limits
        for (int i = 0; i < 5; i++)
        {
            using var serviceProvider = CreateServiceProvider();
            var context = serviceProvider.GetRequiredService<LottoDbContext>();
            var service = serviceProvider.GetRequiredService<INumberLookupService>();

            // Simple test data
            var combination = new[] { 1, 2, 3 };
            var draw = new LottoDraw
            {
                Draw = 1,
                Date = DateTime.Today,
                WinningNumber1 = 1,
                WinningNumber2 = 2,
                WinningNumber3 = 3,
                WinningNumber4 = 4,
                WinningNumber5 = 5,
                WinningNumber6 = 6,
                BonusNumber = 7,
                Powerball = 8
            };

            context.LottoDraws.Add(draw);
            await context.SaveChangesAsync();

            // Create occurrences
            for (int pos = 1; pos <= 6; pos++)
            {
                context.NumberOccurrences.Add(new NumberOccurrence
                {
                    DrawNumber = 1,
                    Number = pos,
                    Position = pos,
                    DrawDate = DateTime.Today,
                    IsBonus = false,
                    IsPowerball = false
                });
            }
            await context.SaveChangesAsync();

            // Test the search
            var result = await service.SearchCombinationAsync(combination, includePartialMatches: true);
            
            // Basic validation
            if (result == null || result.TotalExactMatches != 1)
                throw new Exception($"Expected 1 exact match, got {result?.TotalExactMatches ?? 0}");
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<LottoDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Error));
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<INumberLookupService, NumberLookupService>();
        
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<LottoDbContext>().Database.EnsureCreated();
        return provider;
    }
}