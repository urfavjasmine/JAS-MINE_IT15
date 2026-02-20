using System;
using System.Linq;
using System.Linq.Expressions;

namespace JAS_MINE_IT15.Services
{
    /// <summary>
    /// Extension methods for multi-tenant barangay filtering.
    /// Use these to filter any IQueryable by BarangayId based on current user's tenant.
    /// </summary>
    public static class TenantQueryExtensions
    {
        /// <summary>
        /// Filters an IQueryable by BarangayId based on current user's tenant.
        /// super_admin sees all records; other roles see only their barangay's records.
        /// 
        /// USAGE:
        /// var query = _context.Documents
        ///     .Where(d => d.IsActive)
        ///     .FilterByTenant(_tenantService, d => d.BarangayId);
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="query">The IQueryable to filter</param>
        /// <param name="tenantService">Injected tenant service</param>
        /// <param name="barangayIdSelector">Expression to select the BarangayId property</param>
        /// <returns>Filtered IQueryable</returns>
        public static IQueryable<T> FilterByTenant<T>(
            this IQueryable<T> query,
            ITenantService tenantService,
            Expression<Func<T, int?>> barangayIdSelector)
        {
            // super_admin sees all records
            if (tenantService.IsSuperAdmin())
                return query;

            var barangayId = tenantService.GetCurrentBarangayId();
            
            // Non-super users without a barangay see nothing
            if (barangayId == null)
                return query.Where(_ => false);

            // Build expression: entity => entity.BarangayId == barangayId
            var parameter = barangayIdSelector.Parameters[0];
            var body = Expression.Equal(
                barangayIdSelector.Body,
                Expression.Constant(barangayId, typeof(int?))
            );
            var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);

            return query.Where(predicate);
        }

        /// <summary>
        /// Simpler filter for non-nullable BarangayId columns.
        /// </summary>
        public static IQueryable<T> FilterByTenant<T>(
            this IQueryable<T> query,
            ITenantService tenantService,
            Expression<Func<T, int>> barangayIdSelector)
        {
            if (tenantService.IsSuperAdmin())
                return query;

            var barangayId = tenantService.GetCurrentBarangayId();
            
            if (barangayId == null)
                return query.Where(_ => false);

            var parameter = barangayIdSelector.Parameters[0];
            var body = Expression.Equal(
                barangayIdSelector.Body,
                Expression.Constant(barangayId.Value, typeof(int))
            );
            var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);

            return query.Where(predicate);
        }
    }
}
