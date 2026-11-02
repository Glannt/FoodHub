using System;
using System.Collections.Generic;
using System.Text;
using FoodHub.Application.Pagination;
using MediatR;

namespace FoodHub.Application.Restaurants.Queries.GetRestaurants;

public sealed record GetRestaurantsQuery(string RestaurantQuery, int PageNumber, int PageSize)
    : IRequest<GetRestaurantsResult>;

public sealed record GetRestaurantsResult(
   PageResult<RestaurantDto> Paginations);

public sealed record RestaurantDto(
    Guid RestaurantId,
    string RestaurantName,
    string Address,
    string Longtitude,
    string Latitude,
    bool IsApproved);

