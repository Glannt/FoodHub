using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.Commands.ApproveRestaurant;

internal sealed class ApproveRestaurantHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveRestaurantCommand, Unit>
{
    public async Task<Unit> Handle(
        ApproveRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        // Load thật (không AsNoTracking): cần entity để gọi Approve() và để EF phát hiện thay đổi.
        var restaurant = await dbContext.Restaurants
            .FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken);

        if (restaurant is null)
            throw new NotFoundException($"nhà hàng {request.RestaurantId}.");

        // Cố tình KHÔNG chặn "đã duyệt rồi": Approve() của Domain là idempotent — gọi lần
        // hai không làm hỏng gì. Đây cũng là lý do return Unit (204 No Content): thao tác
        // này chỉ đổi 1 cờ, không có dữ liệu mới để trả về.
        restaurant.Approve();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Unit();
    }
}
