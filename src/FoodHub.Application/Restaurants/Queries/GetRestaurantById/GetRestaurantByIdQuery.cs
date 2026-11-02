using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace FoodHub.Application.Restaurants.Queries.GetRestaurantById;

public sealed record GetRestaurantByIdQuery(Guid RestaurantId)
    : IRequest<GetRestaurantByIdResult>;

public sealed record GetRestaurantByIdResult(
    Guid RestaurantId,
    string RestaurantName,
    string Address,
    string Longtitude,
    string Latitude,
    bool IsApproved);
