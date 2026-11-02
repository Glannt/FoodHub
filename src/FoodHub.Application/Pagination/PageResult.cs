using System;
using System.Collections.Generic;
using System.Text;

namespace FoodHub.Application.Pagination;

public sealed record PageResult<T>(
    int PageNumber,
    int PageSize,
    int TotalCount,  
    IReadOnlyList<T> Items
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
