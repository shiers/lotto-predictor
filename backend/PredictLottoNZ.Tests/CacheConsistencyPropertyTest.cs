using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Services;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace PredictLottoNZ.Tests;

/// <summary>
/// Property-based tests for cache consistency and reliability
/// **Feature: lottery-lookup-navigation, Property tests for cache consistency**
/// </summary>
public class CacheConsistencyPropertyTest : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly LottoDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDistributedCache> _mockDistributedCache;

    public CacheConsistencyPropertyTest()
    {
        var services = new ServiceCollection();
        
        // Setup in-memory database
        services.AddDbContext<LottoDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        // Setup memory cache
        services.AddMemoryCache();
        
        // Setup mock distributed cache
        _mockDistributedCache = new Mock<IDistributedCache>();
        services.AddSingleton(_mockDistributedCache.Object);
        
        // Setup mock Redis
        _mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDatabase = new Mock<IDatabase>();
        var mockServer = new Mock<IServer>();
        
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
               .Returns(mockDatabase.Object);
        _mockRedis.Setup(r => r.GetEndPoints(It.IsAny<bool>()))
               .Returns(new System.Net.EndPoint[] { new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6379) });
        _mockRedis.Setup(r => r.GetServer(It.IsAny<System.Net.EndPoint>(), It.IsAny<object>()))
               .Returns(mockServer.Object);
        
        services.AddSingleton(_mockRedis.Object);
        
        // Setup logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Register cache service
        services.AddScoped<ICacheService, CacheService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<LottoDbContext>();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var draws = new List<LottoDraw>();
        var frequencies = new List<NumberFrequency>();
        
        for (int i = 1; i <= 10; i++)
        {
            draws.Add(new LottoDraw
            {
                Draw = i,
                Date = DateTime.UtcNow.AddDays(-i),
                WinningNumber1 = 1 + (i % 6),
                WinningNumber2 = 7 + (i % 6),
                WinningNumber3 = 13 + (i % 6),
                WinningNumber4 = 19 + (i % 6),
                WinningNumber5 = 25 + (i % 6),
                WinningNumber6 = 31 + (i % 6),
                BonusNumber = 37 + (i % 4)
            });
        }
        
        for (int i = 1; i <= 40; i++)
        {
            frequencies.Add(new NumberFrequency
            {
                Number = i,
                TotalOccurrences = i % 10 + 1,
                LastAppearance = DateTime.UtcNow.AddDays(-(i % 30)),
                FirstAppearance = DateTime.UtcNow.AddDays(-365),
                LongestGap = i % 50 + 1,
                AverageFrequency = (i % 10 + 1) / 10.0
            });
        }
        
        _context.LottoDraws.AddRange(draws);
        _context.NumberFrequencies.AddRange(frequencies);
        _context.SaveChanges();
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 1: Cache get-set consistency**
    /// **Validates: Cache performance and reliability**
    /// Property: For any valid cache key and value, setting then getting should return the same value
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CacheGetSetConsistency()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Arb.From<int>(),
            (key, value) =>
            {
                // Arrange
                var cacheKey = $"test_{key}_{Guid.NewGuid()}";
                var expiration = TimeSpan.FromMinutes(5);
                
                // Act & Assert
                try
                {
                    var task = Task.Run(async () =>
                    {
                        _cacheService.SetAsync(cacheKey, value, expiration, CacheLevel.Memory).GetAwaiter().GetResult();
                        var retrievedValue = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                        return retrievedValue == value;
                    });
                    
                    return task.Result;
                }
                catch
                {
                    return false;
                }
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 2: Cache expiration behavior**
    /// **Validates: Cache performance and reliability**
    /// Property: For any cache entry with expiration, the entry should not be available after expiration
    /// </summary>
    [Property(MaxTest = 50)]
    public Property CacheExpirationBehavior()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Arb.From<int>(),
            (key, value) =>
            {
                // Arrange
                var cacheKey = $"expire_test_{key}_{Guid.NewGuid()}";
                var shortExpiration = TimeSpan.FromMilliseconds(100);
                
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    _cacheService.SetAsync(cacheKey, value, shortExpiration, CacheLevel.Memory).GetAwaiter().GetResult();
                    
                    // Wait for expiration
                    Task.Delay(200).GetAwaiter().GetResult();
                    
                    var retrievedValue = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                    
                    // Assert - value should be null/default after expiration
                    return retrievedValue == default(int);
                });
                
                return task.Result;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 3: Cache invalidation consistency**
    /// **Validates: Cache performance and reliability**
    /// Property: For any cached value, removing it should make it unavailable
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CacheInvalidationConsistency()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Arb.From<int>(),
            (key, value) =>
            {
                // Arrange
                var cacheKey = $"invalidate_test_{key}_{Guid.NewGuid()}";
                
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    _cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(5), CacheLevel.Memory).GetAwaiter().GetResult();
                    var beforeRemoval = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                    
                    _cacheService.RemoveAsync(cacheKey, CacheLevel.Memory).GetAwaiter().GetResult();
                    var afterRemoval = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                    
                    return beforeRemoval == value && afterRemoval == default(int);
                });
                
                return task.Result;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 4: Cache level isolation**
    /// **Validates: Cache performance and reliability**
    /// Property: For any value cached at memory level only, it should not be available at distributed level
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CacheLevelIsolation()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Arb.From<int>(),
            (key, value) =>
            {
                // Arrange
                var cacheKey = $"isolation_test_{key}_{Guid.NewGuid()}";
                
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    // Set only in memory cache
                    _cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(5), CacheLevel.Memory).GetAwaiter().GetResult();
                    
                    var memoryValue = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                    var distributedValue = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Distributed).Result;
                    
                    return memoryValue == value && distributedValue == default(int);
                });
                
                return task.Result;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 5: GetOrSet factory execution**
    /// **Validates: Cache performance and reliability**
    /// Property: For any cache miss, the factory function should be executed exactly once
    /// </summary>
    [Property(MaxTest = 50)]
    public Property GetOrSetFactoryExecution()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Arb.From<int>(),
            (key, value) =>
            {
                // Arrange
                var cacheKey = $"factory_test_{key}_{Guid.NewGuid()}";
                var factoryCallCount = 0;
                
                Func<Task<int>> factory = () =>
                {
                    factoryCallCount++;
                    return Task.FromResult(value);
                };
                
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    // First call should execute factory
                    var firstResult = _cacheService.GetOrSetAsync(cacheKey, factory, TimeSpan.FromMinutes(5), CacheLevel.Memory).Result;
                    
                    // Second call should use cache, not execute factory
                    var secondResult = _cacheService.GetOrSetAsync(cacheKey, factory, TimeSpan.FromMinutes(5), CacheLevel.Memory).Result;
                    
                    return factoryCallCount == 1 && firstResult == value && secondResult == value;
                });
                
                return task.Result;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 6: Cache key generation consistency**
    /// **Validates: Cache performance and reliability**
    /// Property: For any identical input parameters, cache key generation should be deterministic
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CacheKeyGenerationConsistency()
    {
        return Prop.ForAll(
            Arb.From<int>().Filter(n => n >= 1 && n <= 40),
            (number) =>
            {
                // Act
                var key1 = CacheService.GetNumberLookupKey(number);
                var key2 = CacheService.GetNumberLookupKey(number);
                
                // Assert
                return key1 == key2 && !string.IsNullOrEmpty(key1);
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 7: Cache key uniqueness for different inputs**
    /// **Validates: Cache performance and reliability**
    /// Property: For any different input parameters, cache keys should be unique
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CacheKeyUniqueness()
    {
        return Prop.ForAll(
            Arb.From<int>().Filter(n => n >= 1 && n <= 40),
            Arb.From<int>().Filter(n => n >= 1 && n <= 40),
            (number1, number2) =>
            {
                // Skip if numbers are the same
                if (number1 == number2) return true;
                
                // Act
                var key1 = CacheService.GetNumberLookupKey(number1);
                var key2 = CacheService.GetNumberLookupKey(number2);
                
                // Assert
                return key1 != key2;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 8: Cache statistics accuracy**
    /// **Validates: Cache performance and reliability**
    /// Property: Cache statistics should accurately reflect cache operations
    /// </summary>
    [Property(MaxTest = 50)]
    public Property CacheStatisticsAccuracy()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Arb.From<int>(),
            (key, value) =>
            {
                // Arrange
                var cacheKey = $"stats_test_{key}_{Guid.NewGuid()}";
                
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    // Get initial statistics
                    var initialStats = _cacheService.GetStatisticsAsync().Result;
                    
                    // Perform cache operations
                    _cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(5), CacheLevel.Memory).GetAwaiter().GetResult();
                    var retrievedValue = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                    
                    // Get final statistics
                    var finalStats = _cacheService.GetStatisticsAsync().Result;
                    
                    // Statistics should show the operations
                    return finalStats.MemoryCacheHits >= initialStats.MemoryCacheHits &&
                           retrievedValue == value;
                });
                
                return task.Result;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 9: Concurrent cache access safety**
    /// **Validates: Cache performance and reliability**
    /// Property: Concurrent cache operations should not cause data corruption
    /// </summary>
    [Property(MaxTest = 30)]
    public Property ConcurrentCacheAccessSafety()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            Gen.ListOf(Arb.Generate<int>()).Where(list => list != null && list.Count() > 0 && list.Count() <= 10).ToArbitrary(),
            (keyPrefix, values) =>
            {
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    // Arrange
                    var valuesList = values.ToList();
                    var tasks = new List<Task>();
                    var results = new List<int>();
                    var lockObject = new object();
                    
                    // Perform concurrent cache operations
                    for (int i = 0; i < valuesList.Count; i++)
                    {
                        var index = i;
                        var value = valuesList[index];
                        var cacheKey = $"concurrent_test_{keyPrefix}_{index}_{Guid.NewGuid()}";
                        
                        tasks.Add(Task.Run(async () =>
                        {
                            _cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(5), CacheLevel.Memory).GetAwaiter().GetResult();
                            var retrieved = _cacheService.GetAsync<int>(cacheKey, CacheLevel.Memory).Result;
                            
                            lock (lockObject)
                            {
                                results.Add(retrieved);
                            }
                        }));
                    }
                    
                    Task.WhenAll(tasks).GetAwaiter().GetResult();
                    
                    // All values should be retrieved correctly
                    return results.Count == valuesList.Count && 
                           results.All(r => valuesList.Contains(r));
                });
                
                return task.Result;
            });
    }

    /// <summary>
    /// **Feature: lottery-lookup-navigation, Property 10: Cache error handling resilience**
    /// **Validates: Cache performance and reliability**
    /// Property: Cache operations should handle errors gracefully without throwing exceptions
    /// </summary>
    [Property(MaxTest = 50)]
    public Property CacheErrorHandlingResilience()
    {
        return Prop.ForAll(
            Arb.From<string>(),
            (key) =>
            {
                // Act & Assert
                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Try operations with potentially problematic keys
                        _cacheService.SetAsync(key, "test_value", TimeSpan.FromMinutes(5), CacheLevel.Memory).GetAwaiter().GetResult();
                        var result = _cacheService.GetAsync<string>(key, CacheLevel.Memory).Result;
                        _cacheService.RemoveAsync(key, CacheLevel.Memory).GetAwaiter().GetResult();
                        
                        // Operations should complete without throwing
                        return true;
                    }
                    catch
                    {
                        // If any exception is thrown, the property fails
                        return false;
                    }
                });
                
                return task.Result;
            });
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}


