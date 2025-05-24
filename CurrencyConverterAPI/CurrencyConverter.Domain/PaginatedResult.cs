namespace CurrencyConverter.Domain
{
    public class PaginatedResult<T>
    {
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }

        public PaginatedResult(int totalItems, int pageSize, List<T> items)
        {
            TotalItems = totalItems;
            PageSize = pageSize;
            Items = items;
        }
    }
}
