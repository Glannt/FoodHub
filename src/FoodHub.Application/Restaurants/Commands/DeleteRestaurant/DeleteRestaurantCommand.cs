using System;
using MediatR;

namespace FoodHub.Application.Restaurants.Commands.DeleteRestaurant;

public sealed record DeleteRestaurantCommand(
    Guid RestaurantId
) : IRequest<DeleteRestaurantResult>;

public sealed record DeleteRestaurantResult(bool IsDeleted);
