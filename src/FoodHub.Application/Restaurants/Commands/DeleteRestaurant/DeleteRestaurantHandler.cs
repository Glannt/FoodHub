using FoodHub.Application.Abstractions;
using FoodHub.Application.Restaurants.Commands.DeleteRestaurant;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;


// internal: chi tiết trong file, không ai gọi trực tiếp, chỉ MediatR dùng.
internal sealed class DeleteRestaurantHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteRestaurantCommand, DeleteRestaurantResult>
{
    public async Task<DeleteRestaurantResult> Handle(
        DeleteRestaurantCommand request,
        CancellationToken cancellationToken)
    {

        var existing = await dbContext.Restaurants
            .FirstOrDefaultAsync(r =>
               r.Id == request.RestaurantId,
                cancellationToken);

        if (existing is null)
        {
            throw new NotFoundException(
                $"Không tìm thấy Nhà hàng!");
        }

        // new Restaurant() là nơi duy nhất được phép set thuộc tính — guard của Domain chạy ở đây
        // (IsApproved = false: nhà hàng mới luôn phải chờ quản trị viên duyệt).
        dbContext.Restaurants.Remove(existing);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteRestaurantResult(true);
    }
}
