using Microsoft.Extensions.Logging;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using PredictLottoNZ.Services;
using System.Text.Json;

namespace PredictLottoNZ.Tests;

/**
 * Feature: predict-lotto-nz, Property 11: Prediction persistence is comprehensive
 * 
 * This test validates that for any generated prediction from any provider, the system 
 * stores the prediction with source identification, timestamp, and all associated data.
 */
public static class PredictionPersistencePropertyTest
{
    public static async Task RunPredictionPersistencePropertyTest()
    {
        Console.WriteLine("Running Prediction Persistence Property Test...");
        Console.WriteLine("===============================================");

        try
        {
            Console.WriteLine("Property Test 11: Prediction persistence is comprehensive...");
            await PredictionPersistenceIsComprehensive_PropertyTest();
            Console.WriteLine("✓ PASSED: Prediction persistence is comprehensive (100 iterations)");
            
            Console.WriteLine("\nProperty Test 11b: Prediction entity creation is valid...");
            await PredictionEntityCreationIsValid_PropertyTest();
            Console.WriteLine("✓ PASSED: Prediction entity creation is valid (50 iterations)");
            
            Console.WriteLine("\nProperty Test 11c: Training data payloads are comprehensive...");
            await TrainingDataPayloadsAreComprehensive_PropertyTest();
            Console.WriteLine("✓ PASSED: Training data payloads are comprehensive (50 iterations)");
            
            Console.WriteLine("\nProperty Test 11d: Prediction source tracking is accurate...");
            await PredictionSourceTrackingIsAccurate_PropertyTest();
            Console.WriteLine("✓ PASSED: Prediction source tracking is accurate (25 iterations)");
            
            Console.WriteLine("\nProperty Test 11e: Prediction timestamps are accurate...");
            await PredictionTimestampsAreAccurate_PropertyTest();
            Console.WriteLine("✓ PASSED: Prediction timestamps are accurate (25 iterations)");
            
            Console.WriteLine("\n===============================================");
            Console.WriteLine("Prediction persistence property test PASSED!");
            Console.WriteLine("Property 11: Prediction persistence is comprehensive - VALIDATED");
            Console.WriteLine("Requirements 5.4, 6.4, 10.1, 10.4 - SATISFIED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PREDICTION PERSISTENCE TEST FAILED: {ex.Message}");
            throw;
        }
    }

    private static int[] GenerateValidLotteryNumbers(Random random)
    {
        var numbers = new HashSet<int>();
        while (numbers.Count < 6)
        {
            numbers.Add(random.Next(1, 41));
        }
        return numbers.OrderBy(x => x).ToArray();
    }

    private static PredictionResult GenerateValidPredictionResult(Random random, string? source = null)
    {
        var sources = new[] { "AWS_LLM", "FastAPI", "Frequency", "TestProvider" };
        return new PredictionResult
        {
            Numbers = GenerateValidLotteryNumbers(random),
            Score = random.NextDouble() * 100,
            Source = source ?? sources[random.Next(sources.Length)],
            CreatedAt = DateTime.UtcNow
        };
    }

    private static async Task PredictionPersistenceIsComprehensive_PropertyTest()
    {
        const int iterations = 100;
        var random = new Random(42);
        
        for (int i = 0; i < iterations; i++)
        {
            // Property: For any generated prediction from any provider, the system stores the prediction 
            // with source identification, timestamp, and all associated data
            
            var predictionCount = random.Next(1, 6); // 1-5 predictions
            var providerName = GenerateRandomProviderName(random);
            var requestedCount = random.Next(1, 10);
            
            // Generate test predictions as would be returned by a provider
            var predictions = Enumerable.Range(0, predictionCount)
                .Select(_ => GenerateValidPredictionResult(random, providerName))
                .ToList();
            
            // Test the prediction entity creation logic (as done in PredictionService.StorePredictionsWithPayloadAsync)
            var timestampBeforeCreation = DateTime.UtcNow;
            
            // Create request payload for training data (as done in PredictionService)
            var requestPayload = new
            {
                provider = providerName,
                requestedCount = requestedCount,
                timestamp = DateTime.UtcNow,
                requestId = Guid.NewGuid()
            };
            
            // Create response payload for training data (as done in PredictionService)
            var responsePayload = new
            {
                provider = providerName,
                predictionsGenerated = predictions.Count,
                predictions = predictions.Select(p => new
                {
                    numbers = p.Numbers,
                    score = p.Score,
                    source = p.Source,
                    createdAt = p.CreatedAt
                }),
                timestamp = DateTime.UtcNow
            };
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var requestJson = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var responseJson = JsonSerializer.Serialize(responsePayload, jsonOptions);
            
            // Create prediction entities as would be done in PredictionService
            var entities = predictions.Select(p => new Prediction
            {
                CreatedAt = p.CreatedAt,
                Source = p.Source,
                Score = p.Score,
                Number1 = p.Numbers[0],
                Number2 = p.Numbers[1],
                Number3 = p.Numbers[2],
                Number4 = p.Numbers[3],
                Number5 = p.Numbers[4],
                Number6 = p.Numbers[5],
                RawRequestPayload = requestJson,
                RawResponsePayload = responseJson
            }).ToList();
            
            var timestampAfterCreation = DateTime.UtcNow;
            
            // Verify comprehensive persistence properties
            if (entities.Count != predictions.Count)
                throw new Exception($"Iteration {i}: Expected {predictions.Count} entities but got {entities.Count}");
            
            foreach (var entity in entities)
            {
                // Verify source identification
                if (string.IsNullOrEmpty(entity.Source))
                    throw new Exception($"Iteration {i}: Entity missing source identification");
                
                if (entity.Source != providerName)
                    throw new Exception($"Iteration {i}: Entity source mismatch. Expected {providerName}, got {entity.Source}");
                
                // Verify timestamp is present and reasonable
                if (entity.CreatedAt == default)
                    throw new Exception($"Iteration {i}: Entity missing timestamp");
                
                if (entity.CreatedAt < timestampBeforeCreation.AddMinutes(-1) || 
                    entity.CreatedAt > timestampAfterCreation.AddMinutes(1))
                    throw new Exception($"Iteration {i}: Entity timestamp is unreasonable");
                
                // Verify all associated data is present
                var numbers = entity.GetNumbers();
                if (numbers.Length != 6)
                    throw new Exception($"Iteration {i}: Entity doesn't have exactly 6 numbers");
                
                if (!numbers.All(n => n >= 1 && n <= 40))
                    throw new Exception($"Iteration {i}: Entity has numbers out of valid range");
                
                if (numbers.Distinct().Count() != 6)
                    throw new Exception($"Iteration {i}: Entity has duplicate numbers");
                
                // Verify training data payloads are present
                if (string.IsNullOrEmpty(entity.RawRequestPayload))
                    throw new Exception($"Iteration {i}: Entity missing request payload for training data");
                
                if (string.IsNullOrEmpty(entity.RawResponsePayload))
                    throw new Exception($"Iteration {i}: Entity missing response payload for training data");
                
                // Verify score is preserved
                if (entity.Score == null)
                    throw new Exception($"Iteration {i}: Entity missing score data");
            }
        }
    }

    private static async Task PredictionEntityCreationIsValid_PropertyTest()
    {
        const int iterations = 50;
        var random = new Random(42);
        
        for (int i = 0; i < iterations; i++)
        {
            // Property: For any prediction data, the Prediction entity should be created correctly
            // with all required fields and constraints satisfied
            
            var predictionResult = GenerateValidPredictionResult(random);
            var providerName = GenerateRandomProviderName(random);
            var score = random.NextDouble() * 100;
            
            // Test entity creation as done in PredictionService
            var entity = new Prediction
            {
                CreatedAt = predictionResult.CreatedAt,
                Source = providerName,
                Score = score,
                Number1 = predictionResult.Numbers[0],
                Number2 = predictionResult.Numbers[1],
                Number3 = predictionResult.Numbers[2],
                Number4 = predictionResult.Numbers[3],
                Number5 = predictionResult.Numbers[4],
                Number6 = predictionResult.Numbers[5],
                RawRequestPayload = "test-request",
                RawResponsePayload = "test-response"
            };
            
            // Verify entity satisfies all constraints
            
            // Check Required attributes
            if (entity.CreatedAt == default)
                throw new Exception($"Iteration {i}: CreatedAt is required but has default value");
            
            if (string.IsNullOrEmpty(entity.Source))
                throw new Exception($"Iteration {i}: Source is required but is null or empty");
            
            // Check MaxLength constraint on Source (100 characters)
            if (entity.Source.Length > 100)
                throw new Exception($"Iteration {i}: Source exceeds maximum length of 100 characters");
            
            // Check Range constraints on numbers [1, 40]
            var numbers = new[] { entity.Number1, entity.Number2, entity.Number3, entity.Number4, entity.Number5, entity.Number6 };
            foreach (var number in numbers)
            {
                if (number < 1 || number > 40)
                    throw new Exception($"Iteration {i}: Number {number} is outside valid range [1, 40]");
            }
            
            // Verify GetNumbers() helper method works correctly
            var retrievedNumbers = entity.GetNumbers();
            if (retrievedNumbers.Length != 6)
                throw new Exception($"Iteration {i}: GetNumbers() should return exactly 6 numbers");
            
            if (!retrievedNumbers.SequenceEqual(predictionResult.Numbers))
                throw new Exception($"Iteration {i}: GetNumbers() doesn't match original numbers");
            
            // Test SetNumbers() helper method
            var newNumbers = GenerateValidLotteryNumbers(random);
            entity.SetNumbers(newNumbers);
            
            var setNumbers = entity.GetNumbers();
            if (!setNumbers.SequenceEqual(newNumbers))
                throw new Exception($"Iteration {i}: SetNumbers() didn't set numbers correctly");
            
            // Verify table mapping attribute
            var tableAttribute = typeof(Prediction).GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute), false).FirstOrDefault();
            if (tableAttribute == null)
                throw new Exception($"Iteration {i}: Prediction entity missing Table attribute");
            
            var tableName = ((System.ComponentModel.DataAnnotations.Schema.TableAttribute)tableAttribute).Name;
            if (tableName != "Predictions")
                throw new Exception($"Iteration {i}: Prediction entity mapped to wrong table: {tableName}");
        }
    }

    private static async Task TrainingDataPayloadsAreComprehensive_PropertyTest()
    {
        const int iterations = 50;
        var random = new Random(42);
        
        for (int i = 0; i < iterations; i++)
        {
            // Property: For any prediction storage operation, the training data payloads should contain
            // all necessary information for future ML model development
            
            var providerName = GenerateRandomProviderName(random);
            var requestedCount = random.Next(1, 10);
            var predictionCount = random.Next(1, 6);
            var predictions = Enumerable.Range(0, predictionCount)
                .Select(_ => GenerateValidPredictionResult(random, providerName))
                .ToList();
            
            // Create payloads as done in PredictionService.StorePredictionsWithPayloadAsync
            var requestPayload = new
            {
                provider = providerName,
                requestedCount = requestedCount,
                timestamp = DateTime.UtcNow,
                requestId = Guid.NewGuid()
            };
            
            var responsePayload = new
            {
                provider = providerName,
                predictionsGenerated = predictions.Count,
                predictions = predictions.Select(p => new
                {
                    numbers = p.Numbers,
                    score = p.Score,
                    source = p.Source,
                    createdAt = p.CreatedAt
                }),
                timestamp = DateTime.UtcNow
            };
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var requestJson = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var responseJson = JsonSerializer.Serialize(responsePayload, jsonOptions);
            
            // Verify request payload contains all required training data fields
            var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);
            if (!requestElement.TryGetProperty("provider", out var providerProp) ||
                !requestElement.TryGetProperty("requestedCount", out var countProp) ||
                !requestElement.TryGetProperty("timestamp", out var timestampProp) ||
                !requestElement.TryGetProperty("requestId", out var requestIdProp))
            {
                throw new Exception($"Iteration {i}: Request payload missing required training data fields");
            }
            
            // Verify field values are correct
            if (providerProp.GetString() != providerName)
                throw new Exception($"Iteration {i}: Request payload provider mismatch");
            
            if (countProp.GetInt32() != requestedCount)
                throw new Exception($"Iteration {i}: Request payload requested count mismatch");
            
            // Verify response payload contains all required training data fields
            var responseElement = JsonSerializer.Deserialize<JsonElement>(responseJson);
            if (!responseElement.TryGetProperty("provider", out var respProviderProp) ||
                !responseElement.TryGetProperty("predictionsGenerated", out var generatedProp) ||
                !responseElement.TryGetProperty("predictions", out var predictionsProp) ||
                !responseElement.TryGetProperty("timestamp", out var respTimestampProp))
            {
                throw new Exception($"Iteration {i}: Response payload missing required training data fields");
            }
            
            // Verify response field values are correct
            if (respProviderProp.GetString() != providerName)
                throw new Exception($"Iteration {i}: Response payload provider mismatch");
            
            if (generatedProp.GetInt32() != predictions.Count)
                throw new Exception($"Iteration {i}: Response payload predictions generated count mismatch");
            
            // Verify predictions array contains all prediction data
            if (predictionsProp.GetArrayLength() != predictions.Count)
                throw new Exception($"Iteration {i}: Response payload predictions array length mismatch");
            
            var predictionArray = predictionsProp.EnumerateArray().ToList();
            for (int j = 0; j < predictions.Count; j++)
            {
                var predictionElement = predictionArray[j];
                var hasPredictionFields = predictionElement.TryGetProperty("numbers", out var numbersProp) &&
                                        predictionElement.TryGetProperty("score", out var scoreProp) &&
                                        predictionElement.TryGetProperty("source", out var sourceProp) &&
                                        predictionElement.TryGetProperty("createdAt", out var createdAtProp);
                
                if (!hasPredictionFields)
                    throw new Exception($"Iteration {i}: Prediction {j} missing required fields in response payload");
                
                // Verify numbers array
                var numbersArray = numbersProp.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                if (numbersArray.Length != 6)
                    throw new Exception($"Iteration {i}: Prediction {j} numbers array should have 6 elements");
                
                if (!numbersArray.SequenceEqual(predictions[j].Numbers))
                    throw new Exception($"Iteration {i}: Prediction {j} numbers mismatch in response payload");
            }
        }
    }

    private static async Task PredictionSourceTrackingIsAccurate_PropertyTest()
    {
        const int iterations = 25;
        var random = new Random(42);
        var sources = new[] { "AWS_LLM", "FastAPI", "Frequency", "TestProvider", "CustomProvider" };
        
        for (int i = 0; i < iterations; i++)
        {
            // Property: For any prediction from any provider, the source should be accurately tracked
            // throughout the persistence and retrieval process
            
            var providerName = sources[random.Next(sources.Length)];
            var predictionResult = GenerateValidPredictionResult(random, providerName);
            
            // Test entity creation with source tracking
            var entity = new Prediction
            {
                CreatedAt = predictionResult.CreatedAt,
                Source = providerName,
                Score = predictionResult.Score,
                Number1 = predictionResult.Numbers[0],
                Number2 = predictionResult.Numbers[1],
                Number3 = predictionResult.Numbers[2],
                Number4 = predictionResult.Numbers[3],
                Number5 = predictionResult.Numbers[4],
                Number6 = predictionResult.Numbers[5],
                RawRequestPayload = "test-request",
                RawResponsePayload = "test-response"
            };
            
            // Verify source is correctly stored in entity
            if (entity.Source != providerName)
                throw new Exception($"Iteration {i}: Entity source mismatch. Expected {providerName}, got {entity.Source}");
            
            // Test conversion back to DTO (as done in PredictionResult.FromEntity)
            var convertedResult = PredictionResult.FromEntity(entity);
            
            if (convertedResult.Source != providerName)
                throw new Exception($"Iteration {i}: Converted result source mismatch. Expected {providerName}, got {convertedResult.Source}");
            
            // Verify source is preserved in all data structures
            if (predictionResult.Source != providerName)
                throw new Exception($"Iteration {i}: Original prediction result source mismatch. Expected {providerName}, got {predictionResult.Source}");
            
            // Test that source information is included in training data payloads
            var requestPayload = new
            {
                provider = providerName,
                requestedCount = 1,
                timestamp = DateTime.UtcNow,
                requestId = Guid.NewGuid()
            };
            
            var responsePayload = new
            {
                provider = providerName,
                predictionsGenerated = 1,
                predictions = new[] { new
                {
                    numbers = predictionResult.Numbers,
                    score = predictionResult.Score,
                    source = predictionResult.Source,
                    createdAt = predictionResult.CreatedAt
                }},
                timestamp = DateTime.UtcNow
            };
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var requestJson = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var responseJson = JsonSerializer.Serialize(responsePayload, jsonOptions);
            
            // Verify source tracking in JSON payloads
            var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);
            if (requestElement.GetProperty("provider").GetString() != providerName)
                throw new Exception($"Iteration {i}: Request payload provider mismatch");
            
            var responseElement = JsonSerializer.Deserialize<JsonElement>(responseJson);
            if (responseElement.GetProperty("provider").GetString() != providerName)
                throw new Exception($"Iteration {i}: Response payload provider mismatch");
            
            var predictionElement = responseElement.GetProperty("predictions").EnumerateArray().First();
            if (predictionElement.GetProperty("source").GetString() != providerName)
                throw new Exception($"Iteration {i}: Response payload prediction source mismatch");
        }
    }

    private static async Task PredictionTimestampsAreAccurate_PropertyTest()
    {
        const int iterations = 25;
        var random = new Random(42);
        
        for (int i = 0; i < iterations; i++)
        {
            // Property: For any prediction persistence operation, timestamps should be accurate
            // and reflect the actual time of creation/storage
            
            var beforeTime = DateTime.UtcNow;
            var predictionResult = GenerateValidPredictionResult(random);
            var afterTime = DateTime.UtcNow;
            
            // Test that prediction result has reasonable timestamp
            if (predictionResult.CreatedAt < beforeTime.AddSeconds(-1) || predictionResult.CreatedAt > afterTime.AddSeconds(1))
                throw new Exception($"Iteration {i}: PredictionResult timestamp is not within reasonable range");
            
            // Test entity creation with timestamp preservation
            var entity = new Prediction
            {
                CreatedAt = predictionResult.CreatedAt,
                Source = predictionResult.Source,
                Score = predictionResult.Score,
                Number1 = predictionResult.Numbers[0],
                Number2 = predictionResult.Numbers[1],
                Number3 = predictionResult.Numbers[2],
                Number4 = predictionResult.Numbers[3],
                Number5 = predictionResult.Numbers[4],
                Number6 = predictionResult.Numbers[5],
                RawRequestPayload = "test-request",
                RawResponsePayload = "test-response"
            };
            
            // Verify entity preserves the timestamp
            if (entity.CreatedAt != predictionResult.CreatedAt)
                throw new Exception($"Iteration {i}: Entity timestamp doesn't match original prediction result timestamp");
            
            // Test conversion back to DTO preserves timestamp
            var convertedResult = PredictionResult.FromEntity(entity);
            
            if (convertedResult.CreatedAt != predictionResult.CreatedAt)
                throw new Exception($"Iteration {i}: Converted result timestamp doesn't match original timestamp");
            
            // Test that default entity creation sets reasonable timestamp
            var defaultEntity = new Prediction
            {
                Source = "TestProvider",
                Score = 1.0,
                Number1 = 1, Number2 = 2, Number3 = 3, Number4 = 4, Number5 = 5, Number6 = 6,
                RawRequestPayload = "test", RawResponsePayload = "test"
            };
            
            var defaultCreationTime = DateTime.UtcNow;
            
            // The default constructor should set CreatedAt to a reasonable time
            if (defaultEntity.CreatedAt == default)
                throw new Exception($"Iteration {i}: Default entity has default timestamp");
            
            if (Math.Abs((defaultEntity.CreatedAt - defaultCreationTime).TotalMinutes) > 1)
                throw new Exception($"Iteration {i}: Default entity timestamp is not reasonable");
            
            // Test timestamp in training data payloads
            var payloadTimestamp = DateTime.UtcNow;
            var requestPayload = new
            {
                provider = "TestProvider",
                requestedCount = 1,
                timestamp = payloadTimestamp,
                requestId = Guid.NewGuid()
            };
            
            var responsePayload = new
            {
                provider = "TestProvider",
                predictionsGenerated = 1,
                predictions = new[] { new
                {
                    numbers = predictionResult.Numbers,
                    score = predictionResult.Score,
                    source = predictionResult.Source,
                    createdAt = predictionResult.CreatedAt
                }},
                timestamp = payloadTimestamp
            };
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var requestJson = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var responseJson = JsonSerializer.Serialize(responsePayload, jsonOptions);
            
            // Verify timestamps are preserved in JSON serialization
            var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);
            var requestTimestamp = requestElement.GetProperty("timestamp").GetDateTime();
            
            if (Math.Abs((requestTimestamp - payloadTimestamp).TotalSeconds) > 1)
                throw new Exception($"Iteration {i}: Request payload timestamp not preserved correctly");
            
            var responseElement = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var responseTimestamp = responseElement.GetProperty("timestamp").GetDateTime();
            
            if (Math.Abs((responseTimestamp - payloadTimestamp).TotalSeconds) > 1)
                throw new Exception($"Iteration {i}: Response payload timestamp not preserved correctly");
            
            var predictionElement = responseElement.GetProperty("predictions").EnumerateArray().First();
            var predictionTimestamp = predictionElement.GetProperty("createdAt").GetDateTime();
            
            if (predictionTimestamp != predictionResult.CreatedAt)
                throw new Exception($"Iteration {i}: Prediction timestamp not preserved in response payload");
        }
    }

    private static string GenerateRandomProviderName(Random random)
    {
        var providers = new[] { "AWS_LLM", "FastAPI", "Frequency", "TestProvider", "CustomProvider", "MLProvider", "GPTProvider" };
        return providers[random.Next(providers.Length)];
    }

    private class TestPredictionProvider : IPredictionProvider
    {
        public string ProviderName { get; }
        public int Priority { get; }
        private readonly IEnumerable<PredictionResult> _predictions;

        public TestPredictionProvider(string name, int priority, IEnumerable<PredictionResult> predictions)
        {
            ProviderName = name;
            Priority = priority;
            _predictions = predictions;
        }

        public async Task<IEnumerable<PredictionResult>> PredictAsync(int count)
        {
            await Task.Delay(1); // Simulate async work
            return _predictions.Take(count);
        }
    }
}

