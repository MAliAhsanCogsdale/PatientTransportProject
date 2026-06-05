namespace TransportProject.Helper_Classes
{
    public static class IQueryableExtensions
    {
        public static PagedResult<T> ToPagedResult<T>(
            this IQueryable<T> query,
            int page,
            int pageSize,
            string? search = null)
        {
            var totalRecords = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                Search = search
            };
        }
    }
}
