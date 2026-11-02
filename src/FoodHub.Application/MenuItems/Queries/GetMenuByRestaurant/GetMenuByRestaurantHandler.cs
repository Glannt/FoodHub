using FoodHub.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.MenuItems.Queries.GetMenuByRestaurant;

internal sealed class GetMenuByRestaurantHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMenuByRestaurantQuery, GetMenuByRestaurantResult>
{
    public async Task<GetMenuByRestaurantResult> Handle(
        GetMenuByRestaurantQuery query,
        CancellationToken cancellationToken)
    {
        // Phải kiểm tra nhà hàng tồn tại: nếu không, kết quả menu rỗng sẽ khiến client
        // tưởng nhà hàng chưa cấu hình thực đơn thay vì hiểu rằng nhà hàng không tồn tại.
        var restaurant = await dbContext.Restaurants
            .AsNoTracking()
            .Where(r => r.Id == query.RestaurantId)
            .Select(r => new { r.Id, r.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
            throw new NotFoundException($"nhà hàng {query.RestaurantId}.");

        // Mỗi nhà hàng có thể có nhiều Menu (sáng/trưa/tối). Endpoint này trả menu
        // mặc định đang hoạt động; nếu chưa có menu nào thì trả thực đơn rỗng.
        var menu = await dbContext.Menus
            .AsNoTracking()
            .Where(m => m.RestaurantId == query.RestaurantId && m.IsActive)
            .OrderByDescending(m => m.IsDefault)
            .ThenBy(m => m.Name)
            .Include(m => m.Versions)
                .ThenInclude(v => v.MenuCategories)
                    .ThenInclude(c => c.MenuItems)
            .FirstOrDefaultAsync(cancellationToken);

        if (menu is null)
        {
            return new GetMenuByRestaurantResult(
                restaurant.Id, restaurant.Name, null, null, null, null, []);
        }

        // Domain tự quyết định version nào đang hiệu lực (CurrentVersionId, fallback Published).
        var version = menu.GetCurrentVersion();

        if (version is null)
        {
            return new GetMenuByRestaurantResult(
                restaurant.Id, restaurant.Name, menu.Id, menu.Name, null, null, []);
        }

        var categories = version.MenuCategories
            .Where(c => c.IsVisible)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();

        // Lấy tên/mô tả/ảnh từ Product (sản phẩm gốc) thay vì lặp lại ở MenuItem.
        // Một truy vấn cho tất cả sản phẩm cần dùng, tránh N+1 theo từng món.
        var productIds = categories
            .SelectMany(c => c.MenuItems)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Description, p.ImageUrl })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var categoryDtos = categories
            .Select(category => new MenuCategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.SortOrder,
                category.MenuItems
                    .OrderBy(i => i.SortOrder)
                    .ThenBy(i => i.DisplayName)
                    .Select(item =>
                    {
                        products.TryGetValue(item.ProductId, out var product);
                        return new MenuItemDto(
                            item.Id,
                            item.ProductId,
                            product?.Name ?? string.Empty,
                            item.DisplayName,
                            product?.Description,
                            product?.ImageUrl,
                            item.Price,
                            item.IsAvailable,
                            item.IsFeatured,
                            item.SortOrder,
                            item.MaxQuantityPerOrder);
                    })
                    .ToList()))
            .ToList();

        return new GetMenuByRestaurantResult(
            restaurant.Id,
            restaurant.Name,
            menu.Id,
            menu.Name,
            version.Id,
            version.VersionNumber,
            categoryDtos);
    }
}
