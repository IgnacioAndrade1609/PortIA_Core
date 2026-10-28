namespace PortIA.Core.Services.Common.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages
)
{
    public PagedResult() : this([], 1, 10, 0, 1) { }
}