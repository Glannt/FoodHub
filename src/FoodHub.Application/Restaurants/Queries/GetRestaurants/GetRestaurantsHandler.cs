using System;
using System.Collections.Generic;
using System.Text;
using FoodHub.Application.Abstractions;
using FoodHub.Application.Pagination;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.Queries.GetRestaurants;

internal sealed class GetRestaurantsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetRestaurantsQuery, GetRestaurantsResult>
{
    public async Task<GetRestaurantsResult> Handle(
        GetRestaurantsQuery query,
        CancellationToken cancellationToken)
    {
        var restaurants = await dbContext.Restaurants
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        if(!string.IsNullOrEmpty(query.RestaurantQuery))
        {
            restaurants = restaurants
                .Where(r => r.Name.Contains(query.RestaurantQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        var totalCount = await dbContext.Restaurants.CountAsync(cancellationToken);
        //check restaurant có null không
        if (restaurants is null)
        {
            throw new Exception("Không tìm thấy quán ăn, nhà hàng nào");
        }
        return new GetRestaurantsResult(
            new PageResult<RestaurantDto>(
                query.PageNumber,
                query.PageSize,
                totalCount,
                restaurants.Select(r => new RestaurantDto(
                    r.Id,
                    r.Name,
                    r.Address,
                    r.Longtitude.ToString(),
                    r.Latitude.ToString(),
                    r.IsApproved
                )).ToList()
            )
        );
    }
}
