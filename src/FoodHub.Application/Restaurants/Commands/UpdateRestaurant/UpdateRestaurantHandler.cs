using FoodHub.Application.Abstractions;
using FoodHub.Application.Common;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.Commands.UpdateRestaurant;

internal sealed class UpdateRestaurantHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateRestaurantCommand, UpdateRestaurantResult>
{
    public async Task<UpdateRestaurantResult> Handle(
        UpdateRestaurantCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Tìm nhà hàng cần sửa (load thật có tracking để EF Core theo dõi và cập nhật)
        var restaurant = await dbContext.Restaurants
            .FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken);

        if (restaurant is null)
        {
            throw new NotFoundException($"nhà hàng với mã {request.RestaurantId}.");
        }

        // 2. Chặn trùng tên và địa chỉ với nhà hàng KHÁC
        var duplicateAddress = await dbContext.Restaurants
            .AnyAsync(r => r.Id != request.RestaurantId &&
                           r.Name == request.Name &&
                           r.Address == request.Address, cancellationToken);

        if (duplicateAddress)
        {
            throw new ConflictException(
                $"Đã có nhà hàng khác tên '{request.Name}' tại địa chỉ '{request.Address}'.");
        }

        // 3. Chuẩn hoá slug (giữ slug cũ nếu không gửi mới, hoặc sinh mới từ tên nếu slug cũ chưa có)
        var slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(request.Slug)
            : (!string.IsNullOrWhiteSpace(restaurant.Slug) ? restaurant.Slug : SlugHelper.GenerateSlug(request.Name));

        var duplicateSlug = await dbContext.Restaurants
            .AnyAsync(r => r.Id != request.RestaurantId && r.Slug == slug, cancellationToken);

        if (duplicateSlug)
        {
            throw new ConflictException(
                $"Đường dẫn (slug) '{slug}' đã được sử dụng bởi nhà hàng khác.");
        }

        // 4. Gọi phương thức nghiệp vụ của Domain Model để cập nhật thông tin
        restaurant.UpdateDetails(
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
            minOrderAmount: request.MinOrderAmount);

        // 5. Cập nhật trạng thái nhận đơn nếu có chỉ định
        if (request.IsAcceptingOrders.HasValue)
        {
            restaurant.SetAcceptingOrders(request.IsAcceptingOrders.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateRestaurantResult(restaurant.Id);
    }
}
