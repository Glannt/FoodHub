using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;

// Command không nhận RestaurantId: món đã tồn tại nên Id là định danh duy nhất cần dùng,
// việc bắt client gửi thêm RestaurantId chỉ tăng khả năng gửi sai dữ liệu không khớp.
public sealed record UpdateMenuItemCommand(
    Guid MenuItemId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl) : IRequest<UpdateMenuItemResult>;

public sealed record UpdateMenuItemResult(Guid MenuItemId);