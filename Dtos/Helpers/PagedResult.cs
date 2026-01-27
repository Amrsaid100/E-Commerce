namespace E_Commerce.Dtos.Helpers
{
    public class PagedResult<T> where T : class
    {
        public PagedResult(int pageNumber, int pageSize, int totalCount, List<T> data)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            Data = data;
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<T> Data { get; set; }
    }
}
