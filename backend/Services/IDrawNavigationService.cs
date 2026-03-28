using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IDrawNavigationService
{
    Task<LottoDrawDto?> GetDrawByNumberAsync(int drawNumber);
    
    Task<LottoDrawDto?> GetDrawByDateAsync(DateTime date);
    
    Task<LottoDrawDto?> GetPreviousDrawAsync(int currentDrawNumber);
    
    Task<LottoDrawDto?> GetNextDrawAsync(int currentDrawNumber);
    
    Task<NavigationContext> GetNavigationContextAsync(int drawNumber);
    
    Task<IEnumerable<LottoDrawDto>> GetDrawsInRangeAsync(DateTime startDate, DateTime endDate);
    
    Task<LottoDrawDto?> JumpToDrawAsync(JumpToRequest request);
}