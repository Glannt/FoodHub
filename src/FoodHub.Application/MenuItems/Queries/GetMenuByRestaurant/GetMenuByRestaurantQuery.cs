using MediatR;

namespace FoodHub.Application.MenuItems.Queries.GetMenuByRestaurant;

public sealed record GetMenuByRestaurantQuery(Guid RestaurantId)
    : IRequest<GetMenuByRestaurantResult>;

/// <summary>
/// Thực đơn đang hiển thị của nhà hàng: menu đang hoạt động + phiên bản hiện hành.
/// Trả kèm thông tin menu/version để client biết mình đang xem bản nào.
/// </summary>
public sealed record GetMenuByRestaurantResult(
    Guid RestaurantId,
    string RestaurantName,
    Guid? MenuId,
    string? MenuName,
    Guid? MenuVersionId,
    int? VersionNumber,
    IReadOnlyList<MenuCategoryDto> Categories);

public sealed record MenuCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<MenuItemDto> Items);

public sealed record MenuItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? DisplayName,
    string? Description,
    string? ImageUrl,
    decimal Price,
    bool IsAvailable,
    bool IsFeatured,
    int SortOrder,
    int? MaxQuantityPerOrder)
{
    /// <summary>Tên nên hiển thị: ưu tiên tên riêng của thực đơn, fallback về tên sản phẩm.</summary>
    public string Name => DisplayName ?? ProductName;
}
