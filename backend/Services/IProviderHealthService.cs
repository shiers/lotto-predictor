namespace PredictLottoNZ.Services;

public interface IProviderHealthService
{
    Task<bool> IsProviderHealthyAsync(string providerName);
    Task<ProviderHealthStatus> GetProviderHealthStatusAsync(string providerName);
    Task<Dictionary<string, ProviderHealthStatus>> GetAllProviderHealthStatusAsync();
    Task RecordProviderSuccessAsync(string providerName, TimeSpan responseTime);
    Task RecordProviderFailureAsync(string providerName, string errorMessage, TimeSpan responseTime);
    Task<bool> ShouldUseProviderAsync(string providerName, double confidenceThreshold = 0.0);
}

public class ProviderHealthStatus
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public DateTime? LastSuccess { get; set; }
    public DateTime? LastFailure { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0.0;
    public TimeSpan AverageResponseTime { get; set; }
    public bool IsCircuitBreakerOpen { get; set; }
    public DateTime? CircuitBreakerOpenedAt { get; set; }
    public string? LastErrorMessage { get; set; }
}

public class ProviderHealthService : IProviderHealthService
{
    private readonly ILogger<ProviderHealthService> _logger;
    private readonly Dictionary<string, ProviderHealthStatus> _healthStatuses;
    private readonly Dictionary<string, CircuitBreakerConfig> _circuitBreakerConfigs;
    private readonly object _lock = new object();
    
    // Circuit breaker configuration
    private class CircuitBreakerConfig
    {
        public int FailureThreshold { get; set; } = 5;
        public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    }
    
    public ProviderHealthService(ILogger<ProviderHealthService> logger)
    {
        _logger = logger;
        _healthStatuses = new Dictionary<string, ProviderHealthStatus>();
        _circuitBreakerConfigs = new Dictionary<string, CircuitBreakerConfig>
        {
            ["Bedrock AgentCore"] = new CircuitBreakerConfig 
            { 
                FailureThreshold = 3, 
                OpenTimeout = TimeSpan.FromMinutes(10),
                HealthCheckInterval = TimeSpan.FromMinutes(2)
            },
            ["Local LLM"] = new CircuitBreakerConfig 
            { 
                FailureThreshold = 3, 
                OpenTimeout = TimeSpan.FromMinutes(5),
                HealthCheckInterval = TimeSpan.FromMinutes(1)
            },
            ["FastAPI"] = new CircuitBreakerConfig 
            { 
                FailureThreshold = 5, 
                OpenTimeout = TimeSpan.FromMinutes(3),
                HealthCheckInterval = TimeSpan.FromSeconds(30)
            },
            ["Frequency"] = new CircuitBreakerConfig 
            { 
                FailureThreshold = 10, 
                OpenTimeout = TimeSpan.FromMinutes(1),
                HealthCheckInterval = TimeSpan.FromSeconds(15)
            }
        };
    }
    
    public async Task<bool> IsProviderHealthyAsync(string providerName)
    {
        var status = await GetProviderHealthStatusAsync(providerName);
        return status.IsHealthy && !status.IsCircuitBreakerOpen;
    }
    
    public async Task<ProviderHealthStatus> GetProviderHealthStatusAsync(string providerName)
    {
        lock (_lock)
        {
            if (!_healthStatuses.TryGetValue(providerName, out var status))
            {
                status = new ProviderHealthStatus
                {
                    ProviderName = providerName,
                    IsHealthy = true, // Assume healthy until proven otherwise
                    LastChecked = DateTime.UtcNow
                };
                _healthStatuses[providerName] = status;
            }
            
            // Check if circuit breaker should be closed
            if (status.IsCircuitBreakerOpen && status.CircuitBreakerOpenedAt.HasValue)
            {
                var config = _circuitBreakerConfigs.GetValueOrDefault(providerName, new CircuitBreakerConfig());
                if (DateTime.UtcNow - status.CircuitBreakerOpenedAt.Value > config.OpenTimeout)
                {
                    _logger.LogInformation("Circuit breaker timeout elapsed for provider {ProviderName}, attempting to close", providerName);
                    status.IsCircuitBreakerOpen = false;
                    status.CircuitBreakerOpenedAt = null;
                    status.ConsecutiveFailures = 0;
                }
            }
            
            return status;
        }
    }
    
    public async Task<Dictionary<string, ProviderHealthStatus>> GetAllProviderHealthStatusAsync()
    {
        var result = new Dictionary<string, ProviderHealthStatus>();
        
        foreach (var providerName in _circuitBreakerConfigs.Keys)
        {
            result[providerName] = await GetProviderHealthStatusAsync(providerName);
        }
        
        return result;
    }
    
    public async Task RecordProviderSuccessAsync(string providerName, TimeSpan responseTime)
    {
        lock (_lock)
        {
            var status = GetOrCreateStatus(providerName);
            
            status.LastSuccess = DateTime.UtcNow;
            status.LastChecked = DateTime.UtcNow;
            status.TotalRequests++;
            status.SuccessfulRequests++;
            status.ConsecutiveFailures = 0;
            status.IsHealthy = true;
            
            // Update average response time
            var totalTime = status.AverageResponseTime.TotalMilliseconds * (status.TotalRequests - 1) + responseTime.TotalMilliseconds;
            status.AverageResponseTime = TimeSpan.FromMilliseconds(totalTime / status.TotalRequests);
            
            // Close circuit breaker on success
            if (status.IsCircuitBreakerOpen)
            {
                _logger.LogInformation("Closing circuit breaker for provider {ProviderName} after successful request", providerName);
                status.IsCircuitBreakerOpen = false;
                status.CircuitBreakerOpenedAt = null;
            }
        }
        
        _logger.LogDebug("Recorded success for provider {ProviderName}, response time: {ResponseTime}ms", 
            providerName, responseTime.TotalMilliseconds);
    }
    
    public async Task RecordProviderFailureAsync(string providerName, string errorMessage, TimeSpan responseTime)
    {
        lock (_lock)
        {
            var status = GetOrCreateStatus(providerName);
            var config = _circuitBreakerConfigs.GetValueOrDefault(providerName, new CircuitBreakerConfig());
            
            status.LastFailure = DateTime.UtcNow;
            status.LastChecked = DateTime.UtcNow;
            status.TotalRequests++;
            status.ConsecutiveFailures++;
            status.LastErrorMessage = errorMessage;
            
            // Update average response time
            var totalTime = status.AverageResponseTime.TotalMilliseconds * (status.TotalRequests - 1) + responseTime.TotalMilliseconds;
            status.AverageResponseTime = TimeSpan.FromMilliseconds(totalTime / status.TotalRequests);
            
            // Check if circuit breaker should be opened
            if (!status.IsCircuitBreakerOpen && status.ConsecutiveFailures >= config.FailureThreshold)
            {
                _logger.LogWarning("Opening circuit breaker for provider {ProviderName} after {ConsecutiveFailures} consecutive failures", 
                    providerName, status.ConsecutiveFailures);
                status.IsCircuitBreakerOpen = true;
                status.CircuitBreakerOpenedAt = DateTime.UtcNow;
                status.IsHealthy = false;
            }
        }
        
        _logger.LogWarning("Recorded failure for provider {ProviderName}: {ErrorMessage}, response time: {ResponseTime}ms", 
            providerName, errorMessage, responseTime.TotalMilliseconds);
    }
    
    public async Task<bool> ShouldUseProviderAsync(string providerName, double confidenceThreshold = 0.0)
    {
        var status = await GetProviderHealthStatusAsync(providerName);
        
        // Don't use if circuit breaker is open
        if (status.IsCircuitBreakerOpen)
        {
            return false;
        }
        
        // Don't use if not healthy
        if (!status.IsHealthy)
        {
            return false;
        }
        
        // If confidence threshold is specified, check success rate
        if (confidenceThreshold > 0.0 && status.SuccessRate < confidenceThreshold)
        {
            _logger.LogDebug("Provider {ProviderName} success rate {SuccessRate:P2} is below threshold {Threshold:P2}", 
                providerName, status.SuccessRate, confidenceThreshold);
            return false;
        }
        
        return true;
    }
    
    private ProviderHealthStatus GetOrCreateStatus(string providerName)
    {
        if (!_healthStatuses.TryGetValue(providerName, out var status))
        {
            status = new ProviderHealthStatus
            {
                ProviderName = providerName,
                IsHealthy = true,
                LastChecked = DateTime.UtcNow
            };
            _healthStatuses[providerName] = status;
        }
        
        return status;
    }
}