using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

public class LookupExportRequest
{
    [Required]
    public NumberLookupRequest SearchCriteria { get; set; } = new NumberLookupRequest();
    
    public ExportFormat Format { get; set; }
    
    public bool IncludeMetadata { get; set; } = true;
    
    [StringLength(100)]
    public string FileName { get; set; } = string.Empty;
}

public class FrequencyExportRequest
{
    [Required]
    public FrequencyAnalysisRequest SearchCriteria { get; set; } = new FrequencyAnalysisRequest();
    
    public ExportFormat Format { get; set; }
    
    public bool IncludeCharts { get; set; } = false;
    
    [StringLength(100)]
    public string FileName { get; set; } = string.Empty;
}

public class NavigationExportRequest
{
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public ExportFormat Format { get; set; }
    
    public bool IncludeMetadata { get; set; } = true;
    
    [StringLength(100)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
}

public enum ExportFormat
{
    CSV,
    JSON,
    PDF,
    Excel
}

public class ExportResult
{
    [Required]
    public string ExportId { get; set; } = string.Empty;
    
    [Required]
    public string DownloadUrl { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    
    public long FileSizeBytes { get; set; }
}

public class ExportStatus
{
    [Required]
    public string ExportId { get; set; } = string.Empty;
    
    [Required]
    public string Status { get; set; } = string.Empty;
    
    public int ProgressPercentage { get; set; }
    
    // Alias for compatibility
    public int Progress 
    { 
        get => ProgressPercentage; 
        set => ProgressPercentage = value; 
    }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
}