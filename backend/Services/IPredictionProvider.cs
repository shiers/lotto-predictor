using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IPredictionProvider
{
    Task<IEnumerable<PredictionResult>> PredictAsync(int count);
    string ProviderName { get; }
    int Priority { get; } // Lower numbers = higher priority
    
    /// <summary>
    /// Quick check whether this provider is configured and likely available.
    /// Providers that are not configured (missing API keys, unimplemented, etc.) should return false
    /// so the prediction service can skip them without waiting for a timeout.
    /// </summary>
    Task<bool> IsAvailableAsync() => Task.FromResult(true);
}