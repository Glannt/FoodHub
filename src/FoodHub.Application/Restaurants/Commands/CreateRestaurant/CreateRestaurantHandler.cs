using FoodHub.Application.Abstractions;
using FoodHub.Application.Common;
using FoodHub.Domain.Enums;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.Commands.CreateRestaurant;

internal sealed class CreateRestaurantHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateRestaurantCommand, CreateRestaurantResult>
{
    public async Task<CreateRestaurantResult> Handle(
        CreateRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Chặn trùng lặp tên và địa chỉ ở tầng Application để trả 409 Conflict tường minh
        var duplicateAddress = await dbContext.Restaurants
            .AnyAsync(r => r.Name == request.Name && r.Address == request.Address, cancellationToken);

        if (duplicateAddress)
        {
            throw new ConflictException(
                $"Nhà hàng '{request.Name}' tại địa chỉ '{request.Address}' đã tồn tại.");
        }

        // 2. Chuẩn hoá slug (tự sinh từ Tên nếu client không truyền lên)
        var slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(request.Slug)
            : SlugHelper.GenerateSlug(request.Name);

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Guid.NewGuid().ToString("N")[..8];
        }

        // 3. Kiểm tra tính duy nhất của slug URL
        var duplicateSlug = await dbContext.Restaurants
            .AnyAsync(r => r.Slug == slug, cancellationToken);

        if (duplicateSlug)
        {
            throw new ConflictException(
                $"Đường dẫn (slug) '{slug}' đã được sử dụng bởi nhà hàng khác.");
        }

        // 4. Khởi tạo Aggregate Root Restaurant (nhà hàng mới mặc định trạng thái Pending và chưa nhận đơn)
        var restaurant = new Restaurant(
            name: request.Name,
            slug: slug,
            description: request.Description,
            phoneNumber: request.PhoneNumber,
            email: request.Email,
            address: request.Address,
            latitude: request.Latitude,
            longtitude: request.Longtitude,
            logoUrl: request.LogoUrl,
            coverImageUrl: request.CoverImageUrl,
            openingTime: request.OpeningTime,
            closingTime: request.ClosingTime,
            minOrderAmount: request.MinOrderAmount,
            status: RestaurantStatus.Pending,
            isAcceptingOrders: false,
            ownerId: request.OwnerId);

        dbContext.Restaurants.Add(restaurant);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateRestaurantResult(restaurant.Id, restaurant.Slug);
    }
}
