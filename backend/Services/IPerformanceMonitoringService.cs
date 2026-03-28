using System.Diagnostics;

namespace PredictLottoNZ.Services;

public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Start monitoring a search operation
    /// </summary>
    /// <param name="operationType">Type of operation (lookup, combination, frequency, etc.)</param>
    /// <param name="parameters">Operation parameters for logging</param>
    /// <returns>Performance tracking context</returns>
    IPerformanceContext StartOperation(string operationType, object? parameters = null);
    
    /// <summary>
    /// Log a completed operation
    /// </summary>
    /// <param name="context">Performance context from StartOperation</param>
    /// <param name="resultCount">Number of results returned</param>
    /// <param name="success">Whether the operation succeeded</param>
    void CompleteOperation(IPerformanceContext context, int resultCount, bool success = true);
    
    /// <summary>
    /// Get performance statistics for a time period
    /// </summary>
    /// <param name="startTime">Start of time period</param>
    /// <param name="endTime">End of time period</param>
    /// <returns>Performance statistics</returns>
    Task<PerformanceStatistics> GetStatisticsAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get real-time performance metrics
    /// </summary>
    /// <returns>Current performance metrics</returns>
    Task<PerformanceMetrics> GetCurrentMetricsAsync();
    
    /// <summary>
    /// Log a slow query for analysis
    /// </summary>
    /// <param name="operationType">Type of operation</param>
    /// <param name="duration">Query duration</param>
    /// <param name="parameters">Query parameters</param>
    Task LogSlowQueryAsync(string operationType, TimeSpan duration, object? parameters = null);
}

public interface IPerformanceContext : IDisposable
{
    string OperationId { get; }
    string OperationType { get; }
    DateTime StartTime { get; }
    Stopwatch Stopwatch { get; }
    object? Parameters { get; }
    
    /// <summary>
    /// Add additional context information
    /// </summary>
    void AddContext(string key, object value);
    
    /// <summary>
    /// Mark an intermediate checkpoint
    /// </summary>
    void Checkpoint(string name);
}

public class PerformanceStatistics
{
    public string OperationType { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;
    
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    
    public double AverageResultCount { get; set; }
    public int MaxResultCount { get; set; }
    public int MinResultCount { get; set; }
    
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    public List<SlowQueryInfo> SlowQueries { get; set; } = new();
}

public class PerformanceMetrics
{
    public int ActiveOperations { get; set; }
    public double OperationsPerSecond { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double CacheHitRate { get; set; }
    public long MemoryUsage { get; set; }
    public Dictionary<string, int> OperationCounts { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SlowQueryInfo
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public object? Parameters { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public List<CheckpointInfo> Checkpoints { get; set; } = new();
}

public class CheckpointInfo
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; }
    public DateTime Timestamp { get; set; }
}