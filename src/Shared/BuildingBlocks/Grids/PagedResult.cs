namespace BuildingBlocks.Grids;

/// <summary>Contains one page of query results and paging metadata.</summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Items">Items on the current page.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Effective page size.</param>
/// <param name="TotalCount">Total rows before paging.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    /// <summary>Gets total page count.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
