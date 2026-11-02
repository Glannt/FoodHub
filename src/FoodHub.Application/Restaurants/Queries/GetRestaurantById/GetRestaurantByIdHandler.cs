using System;
using System.Collections.Generic;
using System.Text;
using FoodHub.Application.Abstractions;
using FoodHub.Application.Restaurants.Queries.GetRestaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.Queries.GetRestaurantById;

internal sealed class GetRestaurantByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetRestaurantByIdQuery, GetRestaurantByIdResult>
{
    public async Task<GetRestaurantByIdResult> Handle(
        GetRestaurantByIdQuery query,
        CancellationToken cancellationToken)
    {
        var restaurant = await dbContext.Restaurants
            .AsNoTracking()
            .Where(r => r.Id == query.RestaurantId)
            .FirstOrDefaultAsync(cancellationToken);
        // Không tìm thấy là 404, KHÔNG phải 500.
        // throw new Exception(...) không thuộc handler nào đã đăng ký trong Program.cs
        // nên rơi xuống handler mặc định và client nhận 500 với thông điệp vô nghĩa.
        if (restaurant is null)
        {
            throw new NotFoundException($"Không tìm thấy nhà hàng với id {query.RestaurantId}.");
        }
        return new GetRestaurantByIdResult(
            restaurant.Id,
            restaurant.Name,
            restaurant.Address,
            restaurant.Longtitude.ToString(),
            restaurant.Latitude.ToString(),
            restaurant.IsApproved);
    }
}
