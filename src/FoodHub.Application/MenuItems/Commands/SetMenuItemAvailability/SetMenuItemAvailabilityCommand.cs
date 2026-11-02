using MediatR;

namespace FoodHub.Application.MenuItems.Commands.SetMenuItemAvailability;

/// <summary>
/// Bật/tắt trạng thái còn bán của một món.
/// Dùng <see cref="IRequest"/> không có kiểu trả về (thay vì tự định nghĩa một record rỗng)
/// vì thao tác này không có dữ liệu để trả về ngoài HTTP 204.
/// </summary>
public sealed record SetMenuItemAvailabilityCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    bool IsAvailable) : IRequest;
