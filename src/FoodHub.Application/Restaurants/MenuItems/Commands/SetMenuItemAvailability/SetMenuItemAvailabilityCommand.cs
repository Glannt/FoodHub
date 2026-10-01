using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;

public sealed record SetMenuItemAvailabilityCommand(
    Guid MenuItemId,
    bool IsAvailable) : IRequest<Unit>;

/// <summary>
/// Kiểu kết quả rỗng cho các command không có dữ liệu để trả về.
/// Dùng chung một kiểu thay vì mỗi use case tự định nghĩa EmptyResult riêng —
/// tránh sinh ra nhiều type rỗng mà ý nghĩa hoàn toàn giống nhau.
/// </summary>
public sealed record Unit;