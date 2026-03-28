using System.ComponentModel.DataAnnotations;

namespace PredictLottoNZ.Models.DTOs;

/// <summary>
/// Generic pagination request parameters
/// </summary>
public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 50;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Page size must be between 1 and 1000")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 1000);
    }

    /// <summary>
    /// Calculate skip count for database queries
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Take count for database queries
    /// </summary>
    public int Take => PageSize;
}

/// <summary>
/// Generic paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of items in the page</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Number of items in the current page
    /// </summary>
    public int ItemCount => Items.Count();

    /// <summary>
    /// Index of the first item in the current page (1-based)
    /// </summary>
    public long FirstItemIndex => TotalItems > 0 ? ((Page - 1) * PageSize) + 1 : 0;

    /// <summary>
    /// Index of the last item in the current page (1-based)
    /// </summary>
    public long LastItemIndex => Math.Min(FirstItemIndex + ItemCount - 1, TotalItems);

    /// <summary>
    /// Pagination metadata for client-side navigation
    /// </summary>
    public PaginationMetadata Metadata => new()
    {
        Page = Page,
        PageSize = PageSize,
        TotalItems = TotalItems,
        TotalPages = TotalPages,
        HasPreviousPage = HasPreviousPage,
        HasNextPage = HasNextPage,
        FirstItemIndex = FirstItemIndex,
        LastItemIndex = LastItemIndex
    };
}

/// <summary>
/// Pagination metadata for API responses
/// </summary>
public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public long FirstItemIndex { get; set; }
    public long LastItemIndex { get; set; }
}

/// <summary>
/// Pagination request with sorting options
/// </summary>
public class SortedPaginationRequest : PaginationRequest
{
    /// <summary>
    /// Field to sort by
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Whether to sort in descending order
    /// </summary>
    public bool IsDescending => SortDirection?.ToLowerInvariant() == "desc";
}

/// <summary>
/// Pagination request for number lookup operations
/// </summary>
public class NumberLookupPaginationRequest : SortedPaginationRequest
{
    /// <summary>
    /// Numbers to look up
    /// </summary>
    public int[] Numbers { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Start date filter
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date filter
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Include bonus numbers
    /// </summary>
    public bool IncludeBonus { get; set; } = true;

    /// <summary>
    /// Include powerball numbers
    /// </summary>
    public bool IncludePowerball { get; set; } = true;
}

/// <summary>
/// Pagination request for combination search operations
/// </summary>
public class CombinationSearchPaginationRequest : SortedPaginationRequest
{
    /// <summary>
    /// Combination to search for
    /// </summary>
    public int[] Combination { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Include partial matches
    /// </summary>
    public bool IncludePartialMatches { get; set; } = true;

    /// <summary>
    /// Minimum number of matches required
    /// </summary>
    public int MinimumMatches { get; set; } = 2;

    /// <summary>
    /// Start date filter
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date filter
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Loading state information for long-running operations
/// </summary>
public class LoadingState
{
    /// <summary>
    /// Whether the operation is currently loading
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    /// Loading progress (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Current loading message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Operation start time
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Items processed so far
    /// </summary>
    public long ItemsProcessed { get; set; }

    /// <summary>
    /// Total items to process
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Whether the operation can be cancelled
    /// </summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Unique identifier for the operation
    /// </summary>
    public string? OperationId { get; set; }
}

/// <summary>
/// Progress update for long-running operations
/// </summary>
public class ProgressUpdate
{
    public string OperationId { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? Message { get; set; }
    public long ItemsProcessed { get; set; }
    public long TotalItems { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Pagination request for prediction operations
/// </summary>
public class PredictionPaginationRequest : SortedPaginationRequest
{
    /// <summary>
    /// Filter by prediction source
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Start date filter (predictions created after this date)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date filter (predictions created before this date)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by target draw date
    /// </summary>
    public DateTime? TargetDrawDate { get; set; }

    /// <summary>
    /// Minimum confidence score filter
    /// </summary>
    public double? MinConfidenceScore { get; set; }

    /// <summary>
    /// Maximum confidence score filter
    /// </summary>
    public double? MaxConfidenceScore { get; set; }

    /// <summary>
    /// Include predictions with reasoning explanation
    /// </summary>
    public bool? HasReasoningExplanation { get; set; }
}