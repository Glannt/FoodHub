using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Pagination;

internal static class QueryablePaginationExtensions
{
    public static Task<PageResult<T>> ToPageResultAsync<T>(
        this IQueryable<T> query,
        int PageNumber,
        int PageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = query.CountAsync(cancellationToken);
        var items = query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        return Task.FromResult(new PageResult<T>(PageNumber, PageSize, totalCount.Result, items.Result));
    }
}
