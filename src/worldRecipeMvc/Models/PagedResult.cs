namespace worldRecipeMvc.Models
{
    /// <summary>Page of items plus paging metadata; shared by MVC views and the API.</summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public int TotalCount { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}
