namespace JAS_MINE_IT15.Models
{
    /// <summary>
    /// Generic server-side pagination result.
    /// Use this in all controllers: var paged = await query.ToPagedResultAsync(page, pageSize);
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Returns a range of page numbers for rendering pagination controls.
        /// E.g., for page 5 of 20 with maxVisible=5: [3, 4, 5, 6, 7]
        /// </summary>
        public IEnumerable<int> GetPageRange(int maxVisible = 5)
        {
            var half = maxVisible / 2;
            var start = Math.Max(1, Page - half);
            var end = Math.Min(TotalPages, start + maxVisible - 1);
            start = Math.Max(1, end - maxVisible + 1);
            for (int i = start; i <= end; i++)
                yield return i;
        }
    }
}
