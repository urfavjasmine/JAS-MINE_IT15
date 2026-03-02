using Microsoft.EntityFrameworkCore;

namespace JAS_MINE_IT15.Models
{
    /// <summary>
    /// Extension method for IQueryable → PagedResult conversion.
    /// </summary>
    public static class PaginationExtensions
    {
        /// <summary>
        /// Converts an IQueryable to a PagedResult with server-side pagination.
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query, int page = 1, int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
