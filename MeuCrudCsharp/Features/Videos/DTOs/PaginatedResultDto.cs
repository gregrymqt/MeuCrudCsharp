using System;
using System.Collections.Generic;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    /// <summary>
    /// Represents a generic result set for paginated queries.
    /// </summary>
    /// <typeparam name="T">The type of the items in the result set.</typeparam>
    public class PaginatedResultDto<T>
    {
        /// <summary>
        /// The collection of items for the current page.
        /// </summary>
        public List<T> Items { get; }

        /// <summary>
        /// The current page number.
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        public PaginatedResultDto(List<T> items, int count, int page, int pageSize)
        {
            Items = items;
            TotalCount = count;
            Page = page;
            PageSize = pageSize;
        }
    }
}
