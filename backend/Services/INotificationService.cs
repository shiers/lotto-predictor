using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface INotificationService
{
    Task NotifyAccuracyAnalysisCompletedAsync(int drawId, AccuracyMetrics metrics);
    Task NotifyModelRetrainingCompletedAsync(string providerName, RetrainingResult result);
    Task NotifyProviderHealthStatusChangedAsync(string providerName, ProviderHealthStatus status);
    Task<IEnumerable<NotificationMessage>> GetRecentNotificationsAsync(int count = 10);
}

public class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Success
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsRead { get; set; } = false;
}

public class RetrainingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double? NewAccuracy { get; set; }
    public double? PreviousAccuracy { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ModelVersion { get; set; }
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly List<NotificationMessage> _notifications;
    private readonly object _lock = new object();
    private const int MaxNotifications = 100;
    
    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        _notifications = new List<NotificationMessage>();
    }
    
    public Task NotifyAccuracyAnalysisCompletedAsync(int drawId, AccuracyMetrics metrics)
    {
        var notification = new NotificationMessage
        {
            Type = "AccuracyAnalysis",
            Title = $"Accuracy Analysis Completed - Draw {drawId}",
            Message = $"Analyzed {metrics.TotalPredictions} predictions. " +
                     $"Exact matches: {metrics.ExactMatches}, " +
                     $"Average proximity: {metrics.AverageProximityScore:F3}",
            Severity = metrics.ExactMatches > 0 ? "Success" : "Info",
            Metadata = new Dictionary<string, object>
            {
                ["DrawId"] = drawId,
                ["TotalPredictions"] = metrics.TotalPredictions,
                ["ExactMatches"] = metrics.ExactMatches,
                ["AverageProximityScore"] = metrics.AverageProximityScore,
                ["ProviderAccuracy"] = metrics.ProviderAccuracy
            }
        };
        
        AddNotification(notification);
        
        _logger.LogInformation("Accuracy analysis notification created for draw {DrawId}", drawId);
        
        return Task.CompletedTask;
    }
    
    public Task NotifyModelRetrainingCompletedAsync(string providerName, RetrainingResult result)
    {
        var notification = new NotificationMessage
        {
            Type = "ModelRetraining",
            Title = $"Model Retraining Completed - {providerName}",
            Message = result.Success 
                ? $"Retraining successful. New accuracy: {result.NewAccuracy:P2}, Duration: {result.Duration.TotalMinutes:F1} minutes"
                : $"Retraining failed: {result.ErrorMessage}",
            Severity = result.Success ? "Success" : "Error",
            Metadata = new Dictionary<string, object>
            {
                ["ProviderName"] = providerName,
                ["Success"] = result.Success,
                ["NewAccuracy"] = result.NewAccuracy ?? 0,
                ["PreviousAccuracy"] = result.PreviousAccuracy ?? 0,
                ["Duration"] = result.Duration.TotalMinutes,
                ["ModelVersion"] = result.ModelVersion ?? "Unknown"
            }
        };
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            notification.Metadata["ErrorMessage"] = result.ErrorMessage;
        }
        
        AddNotification(notification);
        
        _logger.LogInformation("Model retraining notification created for provider {ProviderName}, success: {Success}", 
            providerName, result.Success);
            
        return Task.CompletedTask;
    }
    
    public Task NotifyProviderHealthStatusChangedAsync(string providerName, ProviderHealthStatus status)
    {
        var notification = new NotificationMessage
        {
            Type = "ProviderHealth",
            Title = $"Provider Health Status Changed - {providerName}",
            Message = status.IsHealthy 
                ? $"Provider is now healthy. Success rate: {status.SuccessRate:P2}"
                : $"Provider is unhealthy. {status.ConsecutiveFailures} consecutive failures. " +
                  (status.IsCircuitBreakerOpen ? "Circuit breaker is open." : ""),
            Severity = status.IsHealthy ? "Success" : "Warning",
            Metadata = new Dictionary<string, object>
            {
                ["ProviderName"] = providerName,
                ["IsHealthy"] = status.IsHealthy,
                ["SuccessRate"] = status.SuccessRate,
                ["ConsecutiveFailures"] = status.ConsecutiveFailures,
                ["IsCircuitBreakerOpen"] = status.IsCircuitBreakerOpen,
                ["AverageResponseTime"] = status.AverageResponseTime.TotalMilliseconds
            }
        };
        
        if (!string.IsNullOrEmpty(status.LastErrorMessage))
        {
            notification.Metadata["LastErrorMessage"] = status.LastErrorMessage;
        }
        
        AddNotification(notification);
        
        _logger.LogInformation("Provider health notification created for {ProviderName}, healthy: {IsHealthy}", 
            providerName, status.IsHealthy);
            
        return Task.CompletedTask;
    }
    
    public async Task<IEnumerable<NotificationMessage>> GetRecentNotificationsAsync(int count = 10)
    {
        lock (_lock)
        {
            return _notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(Math.Min(count, MaxNotifications))
                .ToList();
        }
    }
    
    private void AddNotification(NotificationMessage notification)
    {
        lock (_lock)
        {
            _notifications.Insert(0, notification);
            
            // Keep only the most recent notifications
            if (_notifications.Count > MaxNotifications)
            {
                _notifications.RemoveRange(MaxNotifications, _notifications.Count - MaxNotifications);
            }
        }
        
        _logger.LogDebug("Added notification: {Type} - {Title}", notification.Type, notification.Title);
    }
}