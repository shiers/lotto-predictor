namespace PredictLottoNZ.Models.DTOs;

public class ProviderSpecificRequest
{
    public string ProviderName { get; set; } = string.Empty;
    public int Count { get; set; } = 5;
}

public class PredictionExplanationRequest
{
    public IEnumerable<int> PredictionIds { get; set; } = Enumerable.Empty<int>();
}

public class PredictionExplanation
{
    public int PredictionId { get; set; }
    public int[] Numbers { get; set; } = Array.Empty<int>();
    public string Source { get; set; } = string.Empty;
    public double? ConfidenceScore { get; set; }
    public string? ReasoningExplanation { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RawRequestPayload { get; set; }
    public string? RawResponsePayload { get; set; }
}