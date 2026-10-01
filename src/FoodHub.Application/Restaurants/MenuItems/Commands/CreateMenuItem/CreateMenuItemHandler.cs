using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;

// internal: chi tiết trong file, không ai gọi trực tiếp, chỉ MediatR dùng
internal sealed class CreateMenuItemHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateMenuItemCommand, CreateMenuItemResult>
{
    public async Task<CreateMenuItemResult> Handle(
        CreateMenuItemCommand request,
        CancellationToken cancellationToken)
    {
        // Kiểm tra ở tầng Application thay vì để khóa ngoại của DB ném lỗi:
        // lỗi nghiệp vụ trả về 404 có nghĩa rõ ràng, còn lỗi FK là 500 và đọc nhầm nguyên nhân.
        var restaurantExists = await dbContext.Restaurants
            .AnyAsync(r => r.Id == request.RestaurantId, cancellationToken);

        if (!restaurantExists)
            throw new NotFoundException($"nhà hàng {request.RestaurantId}.");

        // Phải kiểm tra cả RestaurantId: nếu chỉ kiểm tra CategoryId thì client có thể
        // gắn món của nhà hàng A vào nhóm món của nhà hàng B — lọt qua mọi guard của Domain.
        var categoryBelongsToRestaurant = await dbContext.MenuCategories
            .AnyAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken);

        if (!categoryBelongsToRestaurant)
            throw new NotFoundException($"nhóm món {request.CategoryId} của nhà hàng {request.RestaurantId}.");

        // new MenuItem() là nơi duy nhất được phép set thuộc tính — guard của Domain chạy ở đây,
        // đảm bảo aggregate không bao giờ tồn tại ở trạng thái không hợp lệ.
        var menuItem = new MenuItem(
            request.RestaurantId,
            request.CategoryId,
            request.Name,
            request.Description ?? string.Empty,
            request.Price,
            request.ImageUrl);

        dbContext.MenuItems.Add(menuItem);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateMenuItemResult(menuItem.Id);
    }
}