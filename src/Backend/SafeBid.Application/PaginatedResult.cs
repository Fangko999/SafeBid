using System.Text.Json.Serialization;

namespace SafeBid.Application;

public class PaginatedResult<T>
{
    public int TotalCount { get; }
    public int TotalPages { get; }
    public int CurrentPage { get; }
    public bool HasNext { get; }
    public IReadOnlyList<T> Items { get; }

    public PaginatedResult(IReadOnlyList<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        CurrentPage = pageNumber;
        HasNext = CurrentPage < TotalPages;
        Items = items;
    }

    [JsonConstructor]
    public PaginatedResult(int totalCount, int totalPages, int currentPage, bool hasNext, IReadOnlyList<T> items)
    {
        TotalCount = totalCount;
        TotalPages = totalPages;
        CurrentPage = currentPage;
        HasNext = hasNext;
        Items = items;
    }
}
