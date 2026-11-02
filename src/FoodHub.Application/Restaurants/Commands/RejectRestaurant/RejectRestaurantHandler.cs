using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.Commands.RejectRestaurant;

internal sealed class RejectRestaurantHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<RejectRestaurantCommand, Unit>
{
    public async Task<Unit> Handle(
        RejectRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        var restaurant = await dbContext.Restaurants
            .FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken);

        if (restaurant is null)
            throw new NotFoundException($"nhà hàng {request.RestaurantId}.");

        // Reject() vừa dùng để từ chối nhà hàng mới, vừa dùng để gỡ duyệt nhà hàng đang chạy.
        // Idempotent giống Approve(): gọi lại nhiều lần không sao.
        restaurant.Reject();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Unit();
    }
}