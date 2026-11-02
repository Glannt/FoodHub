using MediatR;

namespace FoodHub.Application.MenuItems.Commands.CreateMenuItem;

/// <summary>
/// Thêm một món vào thực đơn của nhà hàng.
/// <para>
/// <c>MenuCategoryId</c> trỏ tới nhóm món trong một <c>MenuVersion</c>, còn <c>ProductId</c>
/// trỏ tới sản phẩm gốc trong danh mục sản phẩm của nhà hàng. Tách hai khái niệm này cho phép
/// cùng một sản phẩm xuất hiện ở nhiều thực đơn/phiên bản với giá khác nhau.
/// </para>
/// </summary>
public sealed record CreateMenuItemCommand(
    Guid RestaurantId,
    Guid MenuCategoryId,
    Guid ProductId,
    decimal Price,
    string? DisplayName,
    bool IsAvailable = true,
    bool IsFeatured = false,
    int SortOrder = 0,
    int? MaxQuantityPerOrder = null) : IRequest<CreateMenuItemResult>;

/// <summary>Chỉ trả về Id để API contract không phụ thuộc vào cấu trúc entity bên trong.</summary>
public sealed record CreateMenuItemResult(Guid MenuItemId);
