using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IBookmarkService
{
    Task<BookmarkDto> CreateBookmarkAsync(int drawNumber, string label, string userId);
    
    Task<IEnumerable<BookmarkDto>> GetUserBookmarksAsync(string userId);
    
    Task<bool> DeleteBookmarkAsync(int bookmarkId, string userId);
    
    Task<BookmarkDto?> UpdateBookmarkAsync(int bookmarkId, string newLabel, string userId);
    
    Task<bool> BookmarkExistsAsync(string userId, int drawNumber);
}