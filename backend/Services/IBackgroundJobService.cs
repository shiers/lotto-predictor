using PredictLottoNZ.Data;
using PredictLottoNZ.Models;

namespace PredictLottoNZ.Services;

public interface IBackgroundJobService
{
    Task QueueAccuracyAnalysisAsync(int drawId);
    Task QueueModelRetrainingAsync(string providerName, int trainingRunId);
    Task<BackgroundJobStatus> GetJobStatusAsync(string jobId);
    Task<IEnumerable<BackgroundJobStatus>> GetActiveJobsAsync();
}

public class BackgroundJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public double? ProgressPercentage { get; set; }
}

public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, BackgroundJobStatus> _activeJobs;
    private readonly object _lock = new object();
    
    public BackgroundJobService(
        ILogger<BackgroundJobService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _activeJobs = new Dictionary<string, BackgroundJobStatus>();
    }
    
    public Task QueueAccuracyAnalysisAsync(int drawId)
    {
        var jobId = $"accuracy-analysis-{drawId}-{Guid.NewGuid():N}";
        
        var jobStatus = new BackgroundJobStatus
        {
            JobId = jobId,
            JobType = "AccuracyAnalysis",
            Status = "Queued",
            StartedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> { ["DrawId"] = drawId }
        };
        
        lock (_lock)
        {
            _activeJobs[jobId] = jobStatus;
        }
        
        _logger.LogInformation("Queued accuracy analysis job {JobId} for draw {DrawId}", jobId, drawId);
        
        // Execute in background
        _ = Task.Run(async () => await ExecuteAccuracyAnalysisJobAsync(jobId, drawId));
        
        return Task.CompletedTask;
    }
    
    public Task QueueModelRetrainingAsync(string providerName, int trainingRunId)
    {
        var jobId = $"model-retraining-{providerName}-{trainingRunId}-{Guid.NewGuid():N}";
        
        var jobStatus = new BackgroundJobStatus
        {
            JobId = jobId,
            JobType = "ModelRetraining",
            Status = "Queued",
            StartedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> 
            { 
                ["ProviderName"] = providerName,
                ["TrainingRunId"] = trainingRunId
            }
        };
        
        lock (_lock)
        {
            _activeJobs[jobId] = jobStatus;
        }
        
        _logger.LogInformation("Queued model retraining job {JobId} for provider {ProviderName}, training run {TrainingRunId}", 
            jobId, providerName, trainingRunId);
        
        // Execute in background
        _ = Task.Run(async () => await ExecuteModelRetrainingJobAsync(jobId, providerName, trainingRunId));
        
        return Task.CompletedTask;
    }
    
    public Task<BackgroundJobStatus> GetJobStatusAsync(string jobId)
    {
        lock (_lock)
        {
            var result = _activeJobs.GetValueOrDefault(jobId, new BackgroundJobStatus 
            { 
                JobId = jobId, 
                Status = "NotFound" 
            });
            return Task.FromResult(result);
        }
    }
    
    public Task<IEnumerable<BackgroundJobStatus>> GetActiveJobsAsync()
    {
        lock (_lock)
        {
            var result = _activeJobs.Values.Where(j => j.Status != "Completed" && j.Status != "Failed").ToList();
            return Task.FromResult<IEnumerable<BackgroundJobStatus>>(result);
        }
    }
    
    private async Task ExecuteAccuracyAnalysisJobAsync(string jobId, int drawId)
    {
        try
        {
            UpdateJobStatus(jobId, "Running", progressPercentage: 0);
            
            using var scope = _serviceProvider.CreateScope();
            var accuracyService = scope.ServiceProvider.GetRequiredService<IAccuracyAnalysisService>();
            var context = scope.ServiceProvider.GetRequiredService<LottoDbContext>();
            
            // Get the draw
            var draw = await context.LottoDraws.FindAsync(drawId);
            if (draw == null)
            {
                throw new InvalidOperationException($"Draw {drawId} not found");
            }
            
            UpdateJobStatus(jobId, "Running", progressPercentage: 25);
            
            // Analyze accuracy
            var metrics = await accuracyService.AnalyzePredictionAccuracyAsync(draw);
            
            UpdateJobStatus(jobId, "Running", progressPercentage: 75);
            
            // Update job metadata with results
            lock (_lock)
            {
                if (_activeJobs.TryGetValue(jobId, out var job))
                {
                    job.Metadata["TotalPredictions"] = metrics.TotalPredictions;
                    job.Metadata["ExactMatches"] = metrics.ExactMatches;
                    job.Metadata["AverageProximityScore"] = metrics.AverageProximityScore;
                }
            }
            
            UpdateJobStatus(jobId, "Completed", progressPercentage: 100);
            
            _logger.LogInformation("Completed accuracy analysis job {JobId} for draw {DrawId}", jobId, drawId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed accuracy analysis job {JobId} for draw {DrawId}", jobId, drawId);
            UpdateJobStatus(jobId, "Failed", errorMessage: ex.Message);
        }
        finally
        {
            // Clean up completed jobs after 1 hour
            _ = Task.Delay(TimeSpan.FromHours(1)).ContinueWith(_ => RemoveJob(jobId));
        }
    }
    
    private async Task ExecuteModelRetrainingJobAsync(string jobId, string providerName, int trainingRunId)
    {
        try
        {
            UpdateJobStatus(jobId, "Running", progressPercentage: 0);
            
            using var scope = _serviceProvider.CreateScope();
            var retrainingService = scope.ServiceProvider.GetRequiredService<IModelRetrainingService>();
            
            UpdateJobStatus(jobId, "Running", progressPercentage: 25);
            
            // Execute retraining (this is a placeholder - actual implementation would depend on the provider)
            await Task.Delay(5000); // Simulate retraining time
            
            UpdateJobStatus(jobId, "Running", progressPercentage: 75);
            
            // Simulate completion
            await Task.Delay(2000);
            
            UpdateJobStatus(jobId, "Completed", progressPercentage: 100);
            
            _logger.LogInformation("Completed model retraining job {JobId} for provider {ProviderName}", jobId, providerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed model retraining job {JobId} for provider {ProviderName}", jobId, providerName);
            UpdateJobStatus(jobId, "Failed", errorMessage: ex.Message);
        }
        finally
        {
            // Clean up completed jobs after 1 hour
            _ = Task.Delay(TimeSpan.FromHours(1)).ContinueWith(_ => RemoveJob(jobId));
        }
    }
    
    private void UpdateJobStatus(string jobId, string status, double? progressPercentage = null, string? errorMessage = null)
    {
        lock (_lock)
        {
            if (_activeJobs.TryGetValue(jobId, out var job))
            {
                job.Status = status;
                if (progressPercentage.HasValue)
                    job.ProgressPercentage = progressPercentage.Value;
                if (!string.IsNullOrEmpty(errorMessage))
                    job.ErrorMessage = errorMessage;
                if (status == "Completed" || status == "Failed")
                    job.CompletedAt = DateTime.UtcNow;
            }
        }
    }
    
    private void RemoveJob(string jobId)
    {
        lock (_lock)
        {
            _activeJobs.Remove(jobId);
        }
        _logger.LogDebug("Removed completed job {JobId} from active jobs", jobId);
    }
}