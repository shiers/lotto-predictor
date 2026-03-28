using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IFrequencyCalculationService
{
    Task<Dictionary<int, int>> CalculateNumberFrequenciesAsync();
    Task<Dictionary<int, int>> CalculatePowerballFrequenciesAsync();
    Task<IEnumerable<PredictionResult>> GenerateTopPredictionsAsync(int count);
    Task<double> CalculateCombinationScoreAsync(int[] numbers);
}

public class FrequencyCalculationService : IFrequencyCalculationService
{
    private readonly LottoDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FrequencyCalculationService> _logger;
    
    private const string FREQUENCY_CACHE_KEY = "number_frequencies";
    private const string POWERBALL_FREQUENCY_CACHE_KEY = "powerball_frequencies";
    private const int CACHE_EXPIRY_MINUTES = 30;
    
    public FrequencyCalculationService(
        LottoDbContext context, 
        IMemoryCache cache,
        ILogger<FrequencyCalculationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<Dictionary<int, int>> CalculateNumberFrequenciesAsync()
    {
        // Check cache first
        if (_cache.TryGetValue(FREQUENCY_CACHE_KEY, out Dictionary<int, int>? cachedFrequencies))
        {
            _logger.LogDebug("Retrieved number frequencies from cache");
            return cachedFrequencies!;
        }
        
        _logger.LogInformation("Calculating number frequencies from database");
        
        var frequencies = new Dictionary<int, int>();
        
        // Initialize all numbers 1-40 with zero frequency
        for (int i = 1; i <= 40; i++)
        {
            frequencies[i] = 0;
        }
        
        // Get all historical combinations from LottoDraws
        var lottoDraws = await _context.LottoDraws
            .Select(d => new { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, 
                              d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 })
            .ToListAsync();
            
        // Count frequencies from lotto draws
        foreach (var draw in lottoDraws)
        {
            frequencies[draw.WinningNumber1]++;
            frequencies[draw.WinningNumber2]++;
            frequencies[draw.WinningNumber3]++;
            frequencies[draw.WinningNumber4]++;
            frequencies[draw.WinningNumber5]++;
            frequencies[draw.WinningNumber6]++;
        }
        
        // Get all historical combinations from NumberCombinations
        var numberCombinations = await _context.NumberCombinations
            .Select(c => new { c.Number1, c.Number2, c.Number3, c.Number4, c.Number5, c.Number6 })
            .ToListAsync();
            
        // Count frequencies from number combinations
        foreach (var combination in numberCombinations)
        {
            frequencies[combination.Number1]++;
            frequencies[combination.Number2]++;
            frequencies[combination.Number3]++;
            frequencies[combination.Number4]++;
            frequencies[combination.Number5]++;
            frequencies[combination.Number6]++;
        }
        
        // Cache the results
        _cache.Set(FREQUENCY_CACHE_KEY, frequencies, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
        
        _logger.LogInformation("Calculated frequencies for {TotalDraws} lotto draws and {TotalCombinations} number combinations", 
            lottoDraws.Count, numberCombinations.Count);
            
        return frequencies;
    }
    
    public async Task<Dictionary<int, int>> CalculatePowerballFrequenciesAsync()
    {
        // Check cache first
        if (_cache.TryGetValue(POWERBALL_FREQUENCY_CACHE_KEY, out Dictionary<int, int>? cachedFrequencies))
        {
            _logger.LogDebug("Retrieved Powerball frequencies from cache");
            return cachedFrequencies!;
        }
        
        _logger.LogInformation("Calculating Powerball frequencies from database");
        
        var frequencies = new Dictionary<int, int>();
        
        // Initialize all Powerball numbers 1-10 with zero frequency
        for (int i = 1; i <= 10; i++)
        {
            frequencies[i] = 0;
        }
        
        // Get all historical Powerball numbers from LottoDraws
        var powerballNumbers = await _context.LottoDraws
            .Select(d => d.Powerball)
            .ToListAsync();
            
        // Count frequencies
        foreach (var powerball in powerballNumbers)
        {
            if (powerball >= 1 && powerball <= 10)
            {
                frequencies[powerball]++;
            }
        }
        
        // Cache the results
        _cache.Set(POWERBALL_FREQUENCY_CACHE_KEY, frequencies, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
        
        _logger.LogInformation("Calculated Powerball frequencies for {TotalDraws} draws", powerballNumbers.Count);
            
        return frequencies;
    }
    
    public async Task<double> CalculateCombinationScoreAsync(int[] numbers)
    {
        if (numbers?.Length != 6)
            throw new ArgumentException("Must provide exactly 6 numbers", nameof(numbers));
            
        if (numbers.Any(n => n < 1 || n > 40))
            throw new ArgumentException("All numbers must be between 1 and 40", nameof(numbers));
            
        if (numbers.Distinct().Count() != 6)
            throw new ArgumentException("All numbers must be unique", nameof(numbers));
        
        var frequencies = await CalculateNumberFrequenciesAsync();
        
        // Score is the sum of individual number frequencies
        double score = numbers.Sum(n => frequencies[n]);
        
        _logger.LogDebug("Calculated score {Score} for combination [{Numbers}]", 
            score, string.Join(", ", numbers));
            
        return score;
    }
    
    public async Task<IEnumerable<PredictionResult>> GenerateTopPredictionsAsync(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));
            
        _logger.LogInformation("Generating top {Count} frequency-based predictions", count);
        
        var frequencies = await CalculateNumberFrequenciesAsync();
        var powerballFrequencies = await CalculatePowerballFrequenciesAsync();
        
        // Generate all possible combinations and score them
        var predictions = new List<(int[] numbers, int powerball, double score)>();
        
        // Get the most frequent numbers to focus on
        var topNumbers = frequencies
            .OrderByDescending(kvp => kvp.Value)
            .Take(20) // Focus on top 20 most frequent numbers
            .Select(kvp => kvp.Key)
            .ToArray();
        
        // Get the most frequent Powerball numbers
        var topPowerballs = powerballFrequencies
            .OrderByDescending(kvp => kvp.Value)
            .Take(5) // Focus on top 5 most frequent Powerball numbers
            .Select(kvp => kvp.Key)
            .ToArray();
        
        // Generate combinations from top numbers
        var combinations = GenerateCombinations(topNumbers, 6);
        
        foreach (var combination in combinations.Take(2000)) // Limit to prevent excessive computation
        {
            var mainScore = await CalculateCombinationScoreAsync(combination);
            
            // Generate predictions with different Powerball numbers
            foreach (var powerball in topPowerballs)
            {
                var powerballScore = powerballFrequencies[powerball];
                var totalScore = mainScore + (powerballScore * 0.1); // Weight Powerball less than main numbers
                
                predictions.Add((combination, powerball, totalScore));
            }
        }
        
        // Sort by score descending and take top N
        var topPredictions = predictions
            .OrderByDescending(p => p.score)
            .Take(count)
            .Select(p => new PredictionResult
            {
                Numbers = p.numbers,
                Powerball = p.powerball,
                Score = p.score,
                Source = "Frequency",
                CreatedAt = DateTime.UtcNow
            })
            .ToList();
            
        _logger.LogInformation("Generated {Count} predictions with scores ranging from {MaxScore} to {MinScore}",
            topPredictions.Count, 
            topPredictions.FirstOrDefault()?.Score ?? 0,
            topPredictions.LastOrDefault()?.Score ?? 0);
            
        return topPredictions;
    }
    
    private static IEnumerable<int[]> GenerateCombinations(int[] numbers, int length)
    {
        if (length == 0)
        {
            yield return Array.Empty<int>();
            yield break;
        }
        
        for (int i = 0; i <= numbers.Length - length; i++)
        {
            var first = numbers[i];
            var remaining = numbers.Skip(i + 1).ToArray();
            
            foreach (var combination in GenerateCombinations(remaining, length - 1))
            {
                yield return new[] { first }.Concat(combination).OrderBy(x => x).ToArray();
            }
        }
    }
}