using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace PredictLottoNZ.Services;

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, PerformanceContext> _activeOperations = new();
    private readonly ConcurrentQueue<OperationRecord> _completedOperations = new();
    private readonly ConcurrentQueue<SlowQueryInfo> _slowQueries = new();
    
    // Configuration
    private readonly TimeSpan _slowQueryThreshold = TimeSpan.FromMilliseconds(1000); // 1 second
    private readonly int _maxRecordHistory = 10000;
    private readonly int _maxSlowQueryHistory = 1000;
    
    // Metrics tracking
    private long _totalOperations = 0;
    private long _successfulOperations = 0;
    private long _failedOperations = 0;
    private readonly ConcurrentDictionary<string, long> _operationCounts = new();

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
        
        // Start background cleanup task
        _ = Task.Run(CleanupOldRecords);
    }

    public IPerformanceContext StartOperation(string operationType, object? parameters = null)
    {
        var context = new PerformanceContext(operationType, parameters, _logger);
        _activeOperations.TryAdd(context.OperationId, context);
        
        Interlocked.Increment(ref _totalOperations);
        _operationCounts.AddOrUpdate(operationType, 1, (key, value) => value + 1);
        
        _logger.LogDebug("Started operation {OperationId} of type {OperationType}", 
            context.OperationId, operationType);
        
        return context;
    }

    public void CompleteOperation(IPerformanceContext context, int resultCount, bool success = true)
    {
        if (context is not PerformanceContext perfContext)
        {
            _logger.LogWarning("Invalid performance context provided");
            return;
        }

        // Remove from active operations
        _activeOperations.TryRemove(perfContext.OperationId, out _);
        
        // Stop timing
        perfContext.Stopwatch.Stop();
        
        // Update counters
        if (success)
        {
            Interlocked.Increment(ref _successfulOperations);
        }
        else
        {
            Interlocked.Increment(ref _failedOperations);
        }
        
        // Create operation record
        var record = new OperationRecord
        {
            OperationId = perfContext.OperationId,
            OperationType = perfContext.OperationType,
            StartTime = perfContext.StartTime,
            Duration = perfContext.Stopwatch.Elapsed,
            ResultCount = resultCount,
            Success = success,
            Parameters = perfContext.Parameters,
            Context = new Dictionary<string, object>(perfContext.AdditionalContext),
            Checkpoints = new List<CheckpointInfo>(perfContext.Checkpoints)
        };
        
        // Add to completed operations (with size limit)
        _completedOperations.Enqueue(record);
        while (_completedOperations.Count > _maxRecordHistory)
        {
            _completedOperations.TryDequeue(out _);
        }
        
        // Check if this was a slow query
        if (record.Duration > _slowQueryThreshold)
        {
            var slowQuery = new SlowQueryInfo
            {
                OperationId = record.OperationId,
                OperationType = record.OperationType,
                Duration = record.Duration,
                Timestamp = record.StartTime,
                Parameters = record.Parameters,
                Context = record.Context,
                Checkpoints = record.Checkpoints
            };
            
            _slowQueries.Enqueue(slowQuery);
            while (_slowQueries.Count > _maxSlowQueryHistory)
            {
                _slowQueries.TryDequeue(out _);
            }
            
            _ = LogSlowQueryAsync(record.OperationType, record.Duration, record.Parameters);
        }
        
        _logger.LogInformation("Completed operation {OperationId} in {Duration}ms with {ResultCount} results (Success: {Success})",
            perfContext.OperationId, perfContext.Stopwatch.ElapsedMilliseconds, resultCount, success);
    }

    public async Task<PerformanceStatistics> GetStatisticsAsync(DateTime startTime, DateTime endTime)
    {
        var operations = _completedOperations.ToArray()
            .Where(op => op.StartTime >= startTime && op.StartTime <= endTime)
            .ToList();
        
        if (!operations.Any())
        {
            return new PerformanceStatistics
            {
                PeriodStart = startTime,
                PeriodEnd = endTime
            };
        }
        
        var durations = operations.Select(op => op.Duration).OrderBy(d => d).ToList();
        var resultCounts = operations.Select(op => op.ResultCount).ToList();
        
        var statistics = new PerformanceStatistics
        {
            TotalOperations = operations.Count,
            SuccessfulOperations = operations.Count(op => op.Success),
            FailedOperations = operations.Count(op => !op.Success),
            
            AverageResponseTime = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MedianResponseTime = durations[durations.Count / 2],
            P95ResponseTime = durations[(int)(durations.Count * 0.95)],
            P99ResponseTime = durations[(int)(durations.Count * 0.99)],
            MinResponseTime = durations.First(),
            MaxResponseTime = durations.Last(),
            
            AverageResultCount = resultCounts.Average(),
            MaxResultCount = resultCounts.Max(),
            MinResultCount = resultCounts.Min(),
            
            PeriodStart = startTime,
            PeriodEnd = endTime,
            
            SlowQueries = _slowQueries.ToArray()
                .Where(sq => sq.Timestamp >= startTime && sq.Timestamp <= endTime)
                .OrderByDescending(sq => sq.Duration)
                .Take(50)
                .ToList()
        };
        
        return await Task.FromResult(statistics);
    }

    public async Task<PerformanceMetrics> GetCurrentMetricsAsync()
    {
        var now = DateTime.UtcNow;
        var recentOperations = _completedOperations.ToArray()
            .Where(op => op.StartTime >= now.AddMinutes(-5)) // Last 5 minutes
            .ToList();
        
        var operationsPerSecond = recentOperations.Count / 300.0; // 5 minutes = 300 seconds
        var averageResponseTime = recentOperations.Any() 
            ? TimeSpan.FromTicks((long)recentOperations.Average(op => op.Duration.Ticks))
            : TimeSpan.Zero;
        
        // Get memory usage
        var process = Process.GetCurrentProcess();
        var memoryUsage = process.WorkingSet64;
        
        var metrics = new PerformanceMetrics
        {
            ActiveOperations = _activeOperations.Count,
            OperationsPerSecond = operationsPerSecond,
            AverageResponseTime = averageResponseTime,
            MemoryUsage = memoryUsage,
            OperationCounts = new Dictionary<string, int>(_operationCounts.ToDictionary(
                kvp => kvp.Key, 
                kvp => (int)kvp.Value)),
            Timestamp = now
        };
        
        return await Task.FromResult(metrics);
    }

    public async Task LogSlowQueryAsync(string operationType, TimeSpan duration, object? parameters = null)
    {
        try
        {
            var parametersJson = parameters != null ? JsonSerializer.Serialize(parameters) : "null";
            
            _logger.LogWarning("Slow query detected: {OperationType} took {Duration}ms. Parameters: {Parameters}",
                operationType, duration.TotalMilliseconds, parametersJson);
            
            // In a production system, you might want to send this to a monitoring service
            // like Application Insights, DataDog, or custom metrics collection
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging slow query for operation {OperationType}", operationType);
        }
    }
    
    private async Task CleanupOldRecords()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10)); // Cleanup every 10 minutes
                
                // Remove old completed operations (older than 1 hour)
                var cutoffTime = DateTime.UtcNow.AddHours(-1);
                var operationsToRemove = new List<OperationRecord>();
                
                foreach (var operation in _completedOperations.ToArray())
                {
                    if (operation.StartTime < cutoffTime)
                    {
                        operationsToRemove.Add(operation);
                    }
                }
                
                // Note: ConcurrentQueue doesn't support selective removal,
                // so we'll rely on the size limit for now
                
                _logger.LogDebug("Performance monitoring cleanup completed. Active operations: {ActiveCount}, Completed operations: {CompletedCount}",
                    _activeOperations.Count, _completedOperations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance monitoring cleanup");
            }
        }
    }
}

internal class PerformanceContext : IPerformanceContext
{
    private readonly ILogger _logger;
    private bool _disposed = false;

    public string OperationId { get; }
    public string OperationType { get; }
    public DateTime StartTime { get; }
    public Stopwatch Stopwatch { get; }
    public object? Parameters { get; }
    
    public Dictionary<string, object> AdditionalContext { get; } = new();
    public List<CheckpointInfo> Checkpoints { get; } = new();

    public PerformanceContext(string operationType, object? parameters, ILogger logger)
    {
        OperationId = Guid.NewGuid().ToString("N")[..8]; // Short ID for logging
        OperationType = operationType;
        Parameters = parameters;
        StartTime = DateTime.UtcNow;
        Stopwatch = Stopwatch.StartNew();
        _logger = logger;
    }

    public void AddContext(string key, object value)
    {
        AdditionalContext[key] = value;
    }

    public void Checkpoint(string name)
    {
        var checkpoint = new CheckpointInfo
        {
            Name = name,
            ElapsedTime = Stopwatch.Elapsed,
            Timestamp = DateTime.UtcNow
        };
        
        Checkpoints.Add(checkpoint);
        
        _logger.LogTrace("Checkpoint '{CheckpointName}' reached at {ElapsedTime}ms for operation {OperationId}",
            name, Stopwatch.ElapsedMilliseconds, OperationId);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stopwatch.Stop();
            _disposed = true;
        }
    }
}

internal class OperationRecord
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int ResultCount { get; set; }
    public bool Success { get; set; }
    public object? Parameters { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public List<CheckpointInfo> Checkpoints { get; set; } = new();
}