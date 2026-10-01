using FoodHub.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Queries.GetMenuByRestaurant;

internal sealed class GetMenuByRestaurantHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMenuByRestaurantQuery, GetMenuByRestaurantResult>
{
    public async Task<GetMenuByRestaurantResult> Handle(
        GetMenuByRestaurantQuery query,
        CancellationToken cancellationToken)
    {
        // Vẫn phải kiểm tra nhà hàng tồn tại: nếu không, kết quả sẽ là menu rỗng và client
        // tưởng nhà hàng chưa có món nào thay vì hiểu rằng nhà hàng không tồn tại.
        var restaurant = await dbContext.Restaurants
            .AsNoTracking()
            .Where(r => r.Id == query.RestaurantId)
            .Select(r => new { r.Id, r.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
            throw new NotFoundException($"nhà hàng {query.RestaurantId}.");

        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .Where(c => c.RestaurantId == query.RestaurantId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(i => i.RestaurantId == query.RestaurantId)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.CategoryId,
                i.Name,
                i.Description,
                i.Price,
                i.IsAvailable,
                i.ImageUrl
            })
            .ToListAsync(cancellationToken);

        // Ghép nhóm bằng LINQ to Objects sau khi đã lấy đủ dữ liệu về RAM:
        // 2 truy vấn lấy hết menu thay vì 1 truy vấn lấy món cho từng nhóm (N+1).
        var result = categories
            .Select(category => new MenuCategoryDto(
                category.Id,
                category.Name,
                items
                    .Where(i => i.CategoryId == category.Id)
                    .Select(i => new MenuItemDto(
                        i.Id, i.Name, i.Description, i.Price, i.IsAvailable, i.ImageUrl))
                    .ToList()))
            .ToList();

        return new GetMenuByRestaurantResult(restaurant.Id, restaurant.Name, result);
    }
}