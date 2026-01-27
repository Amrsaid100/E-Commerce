using System;
using System.Collections.Generic;

namespace E_Commerce.Dtos.Helpers
{
    public class Pagination<T> where T : class
    {
        public Pagination(int pageNumber, int pageSize, int totalCount, IReadOnlyList<T> data)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            Data = data;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public IReadOnlyList<T> Data { get; set; }
    }
}