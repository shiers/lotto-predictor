using Microsoft.EntityFrameworkCore;
using PredictLottoNZ.Data;
using PredictLottoNZ.Models;
using PredictLottoNZ.Models.DTOs;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public interface ITrainingDataService
{
    Task LogExternalServiceCallAsync(string serviceName, string endpoint, string requestPayload, string responsePayload, bool success, TimeSpan duration, string? errorMessage = null);
    Task<TrainingDataExport> ExportTrainingDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<PredictionAccuracyReport> AnalyzePredictionAccuracyAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<ExternalServiceCallLog>> GetServiceCallLogsAsync(string? serviceName = null, DateTime? fromDate = null, DateTime? toDate = null);
}

public class TrainingDataService : ITrainingDataService
{
    private readonly LottoDbContext _context;
    private readonly ILogger<TrainingDataService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public TrainingDataService(LottoDbContext context, ILogger<TrainingDataService> logger)
    {
        _context = context;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    public async Task LogExternalServiceCallAsync(string serviceName, string endpoint, string requestPayload, 
        string responsePayload, bool success, TimeSpan duration, string? errorMessage = null)
    {
        try
        {
            var logEntry = new ExternalServiceCallLog
            {
                ServiceName = serviceName,
                Endpoint = endpoint,
                RequestPayload = requestPayload,
                ResponsePayload = responsePayload,
                Success = success,
                Duration = duration,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.ExternalServiceCallLogs.AddAsync(logEntry);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Logged external service call to {ServiceName} at {Endpoint} - Success: {Success}, Duration: {Duration}ms",
                serviceName, endpoint, success, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log external service call to {ServiceName}", serviceName);
            // Don't throw - logging failures shouldn't break the main flow
        }
    }
    
    public async Task<TrainingDataExport> ExportTrainingDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Exporting training data from {FromDate} to {ToDate}", fromDate, toDate);
        
        var query = _context.Predictions.AsQueryable();
        
        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        
        var predictions = await query
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
        
        var serviceCallsQuery = _context.ExternalServiceCallLogs.AsQueryable();
        
        if (fromDate.HasValue)
            serviceCallsQuery = serviceCallsQuery.Where(l => l.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            serviceCallsQuery = serviceCallsQuery.Where(l => l.CreatedAt <= toDate.Value);
        
        var serviceCalls = await serviceCallsQuery
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();
        
        var lottoDrawsQuery = _context.LottoDraws.AsQueryable();
        
        if (fromDate.HasValue)
            lottoDrawsQuery = lottoDrawsQuery.Where(d => d.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            lottoDrawsQuery = lottoDrawsQuery.Where(d => d.CreatedAt <= toDate.Value);
        
        var lottoDraws = await lottoDrawsQuery
            .OrderBy(d => d.Date)
            .ToListAsync();
        
        var combinationsQuery = _context.NumberCombinations.AsQueryable();
        
        if (fromDate.HasValue)
            combinationsQuery = combinationsQuery.Where(c => c.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            combinationsQuery = combinationsQuery.Where(c => c.CreatedAt <= toDate.Value);
        
        var combinations = await combinationsQuery
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        
        var export = new TrainingDataExport
        {
            ExportedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Predictions = predictions.Select(p => new TrainingDataPrediction
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                Source = p.Source,
                Numbers = p.GetNumbers(),
                Score = p.Score,
                RawRequestPayload = p.RawRequestPayload,
                RawResponsePayload = p.RawResponsePayload
            }).ToList(),
            ExternalServiceCalls = serviceCalls.Select(l => new TrainingDataServiceCall
            {
                Id = l.Id,
                CreatedAt = l.CreatedAt,
                ServiceName = l.ServiceName,
                Endpoint = l.Endpoint,
                Success = l.Success,
                Duration = l.Duration,
                RequestPayload = l.RequestPayload,
                ResponsePayload = l.ResponsePayload,
                ErrorMessage = l.ErrorMessage
            }).ToList(),
            LottoDraws = lottoDraws.Select(d => new TrainingDataLottoDraw
            {
                Draw = d.Draw,
                Date = d.Date,
                CreatedAt = d.CreatedAt,
                WinningNumbers = new[] { d.WinningNumber1, d.WinningNumber2, d.WinningNumber3, d.WinningNumber4, d.WinningNumber5, d.WinningNumber6 },
                BonusNumber = d.BonusNumber,
                Powerball = d.Powerball,
                PrizeDivisions = new Dictionary<int, decimal?>
                {
                    { 1, d.Division1Prize },
                    { 2, d.Division2Prize },
                    { 3, d.Division3Prize },
                    { 4, d.Division4Prize },
                    { 5, d.Division5Prize },
                    { 6, d.Division6Prize },
                    { 7, d.Division7Prize }
                }
            }).ToList(),
            NumberCombinations = combinations.Select(c => new TrainingDataCombination
            {
                Id = c.Id,
                CreatedAt = c.CreatedAt,
                Numbers = new[] { c.Number1, c.Number2, c.Number3, c.Number4, c.Number5, c.Number6 }
            }).ToList()
        };
        
        _logger.LogInformation("Exported training data: {PredictionCount} predictions, {ServiceCallCount} service calls, {DrawCount} draws, {CombinationCount} combinations",
            export.Predictions.Count, export.ExternalServiceCalls.Count, export.LottoDraws.Count, export.NumberCombinations.Count);
        
        return export;
    }
    
    public async Task<PredictionAccuracyReport> AnalyzePredictionAccuracyAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Analyzing prediction accuracy from {FromDate} to {ToDate}", fromDate, toDate);
        
        var predictionsQuery = _context.Predictions.AsQueryable();
        
        if (fromDate.HasValue)
            predictionsQuery = predictionsQuery.Where(p => p.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            predictionsQuery = predictionsQuery.Where(p => p.CreatedAt <= toDate.Value);
        
        var predictions = await predictionsQuery
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
        
        // Get all lotto draws for comparison
        var draws = await _context.LottoDraws
            .OrderBy(d => d.Date)
            .ToListAsync();
        
        var accuracyMetrics = new List<PredictionAccuracyMetric>();
        
        foreach (var prediction in predictions)
        {
            var predictionNumbers = prediction.GetNumbers().OrderBy(n => n).ToArray();
            
            // Find draws that occurred after this prediction
            var subsequentDraws = draws
                .Where(d => d.Date > prediction.CreatedAt)
                .Take(10) // Check accuracy against next 10 draws
                .ToList();
            
            foreach (var draw in subsequentDraws)
            {
                var drawNumbers = new[] { draw.WinningNumber1, draw.WinningNumber2, draw.WinningNumber3, 
                                        draw.WinningNumber4, draw.WinningNumber5, draw.WinningNumber6 }
                                        .OrderBy(n => n).ToArray();
                
                var matchingNumbers = predictionNumbers.Intersect(drawNumbers).Count();
                var bonusMatch = predictionNumbers.Contains(draw.BonusNumber);
                var powerballMatch = predictionNumbers.Contains(draw.Powerball);
                
                accuracyMetrics.Add(new PredictionAccuracyMetric
                {
                    PredictionId = prediction.Id,
                    PredictionSource = prediction.Source,
                    PredictionCreatedAt = prediction.CreatedAt,
                    DrawNumber = draw.Draw,
                    DrawDate = draw.Date,
                    MatchingNumbers = matchingNumbers,
                    BonusMatch = bonusMatch,
                    PowerballMatch = powerballMatch,
                    PredictionScore = prediction.Score,
                    DaysUntilDraw = (int)(draw.Date - prediction.CreatedAt).TotalDays
                });
            }
        }
        
        // Calculate summary statistics
        var report = new PredictionAccuracyReport
        {
            AnalyzedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            TotalPredictions = predictions.Count,
            TotalComparisons = accuracyMetrics.Count,
            AccuracyMetrics = accuracyMetrics,
            SummaryBySource = accuracyMetrics
                .GroupBy(m => m.PredictionSource)
                .ToDictionary(g => g.Key, g => new PredictionSourceSummary
                {
                    Source = g.Key,
                    TotalPredictions = g.Select(m => m.PredictionId).Distinct().Count(),
                    TotalComparisons = g.Count(),
                    AverageMatchingNumbers = g.Average(m => m.MatchingNumbers),
                    BestMatch = g.Max(m => m.MatchingNumbers),
                    BonusMatches = g.Count(m => m.BonusMatch),
                    PowerballMatches = g.Count(m => m.PowerballMatch),
                    AverageScore = g.Where(m => m.PredictionScore.HasValue).Average(m => m.PredictionScore.Value)
                })
        };
        
        _logger.LogInformation("Analyzed prediction accuracy: {TotalPredictions} predictions, {TotalComparisons} comparisons across {SourceCount} sources",
            report.TotalPredictions, report.TotalComparisons, report.SummaryBySource.Count);
        
        return report;
    }
    
    public async Task<IEnumerable<ExternalServiceCallLog>> GetServiceCallLogsAsync(string? serviceName = null, 
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.ExternalServiceCallLogs.AsQueryable();
        
        if (!string.IsNullOrEmpty(serviceName))
            query = query.Where(l => l.ServiceName == serviceName);
            
        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
            
        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        
        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }
}