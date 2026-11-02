using MediatR;

namespace FoodHub.Application.MenuItems.Commands.UpdateMenuItem;

/// <summary>
/// Cập nhật thông tin bán của một món đã có trong thực đơn.
/// Không nhận <c>ProductId</c>/<c>MenuCategoryId</c>: đổi sản phẩm hoặc đổi nhóm món là
/// thao tác khác (xoá rồi thêm), không phải "sửa thông tin món".
/// <para>
/// Vẫn nhận <c>RestaurantId</c> từ route để handler xác nhận món thuộc đúng nhà hàng;
/// nếu bỏ qua, client có thể sửa món của nhà hàng khác chỉ bằng cách đoán MenuItemId.
/// </para>
/// </summary>
public sealed record UpdateMenuItemCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    decimal Price,
    string? DisplayName,
    bool IsAvailable,
    bool IsFeatured,
    int SortOrder,
    int? MaxQuantityPerOrder) : IRequest<UpdateMenuItemResult>;

public sealed record UpdateMenuItemResult(Guid MenuItemId);
