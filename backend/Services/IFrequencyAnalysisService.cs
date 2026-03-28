using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IFrequencyAnalysisService
{
    Task<IEnumerable<NumberFrequencyDto>> GetNumberFrequenciesAsync();
    
    Task<IEnumerable<RangeFrequency>> GetRangeFrequenciesAsync(IEnumerable<NumberRange> ranges);
    
    Task<IEnumerable<NumberFrequencyDto>> CompareFrequenciesAsync(FrequencyAnalysisRequest request);
    
    Task<IEnumerable<HotColdNumber>> GetHotColdAnalysisAsync(int periodDays = 365);
    
    Task RefreshFrequencyDataAsync();
}