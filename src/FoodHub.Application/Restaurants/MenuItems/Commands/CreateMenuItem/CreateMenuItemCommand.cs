using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;

// Command + Result nằm chung file: người đọc thấy "gõ vào" và "nhận ra" ngay cạnh nhau,
// không phải nhảy qua nhảy lại giữa 2 file để hiểu một use case.
public sealed record CreateMenuItemCommand(
    Guid RestaurantId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl) : IRequest<CreateMenuItemResult>;

// Chỉ trả về Id chứ không trả cả entity: giữ cho API contract ổn định,
// không vô tình lộ field nội bộ của Domain ra ngoài.
public sealed record CreateMenuItemResult(Guid MenuItemId);