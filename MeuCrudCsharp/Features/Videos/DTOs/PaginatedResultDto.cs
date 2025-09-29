using System;
using System.Collections.Generic;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public record PaginatedResultDto<T>(
        List<T> Items,
        int TotalCount,
        int Page,
        int PageSize
    );
}
