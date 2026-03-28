namespace PredictLottoNZ.Models.DTOs;

public class BedrockPredictionResult
{
    public int[] Numbers { get; set; } = Array.Empty<int>();
    public double ConfidenceScore { get; set; }
    public string ReasoningChain { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ModelId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
}