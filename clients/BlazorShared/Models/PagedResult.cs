namespace FSH.BlazorShared.Models;

public sealed record PagedResult<T>(
    List<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);

public sealed record SearchRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null,
    Dictionary<string, string?>? Filters = null);
