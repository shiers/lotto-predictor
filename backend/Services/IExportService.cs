using PredictLottoNZ.Models.DTOs;

namespace PredictLottoNZ.Services;

public interface IExportService
{
    Task<ExportResult> ExportLookupResultsAsync(LookupExportRequest request);
    
    Task<ExportResult> ExportFrequencyDataAsync(FrequencyExportRequest request);
    
    Task<ExportResult> ExportNavigationHistoryAsync(NavigationExportRequest request);
    
    Task<ExportStatus> GetExportStatusAsync(string exportId);
    
    Task<byte[]?> GetExportFileAsync(string exportId);
    
    Task<bool> DeleteExportAsync(string exportId);
}