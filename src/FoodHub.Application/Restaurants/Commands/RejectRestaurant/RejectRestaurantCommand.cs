using MediatR;

namespace FoodHub.Application.Restaurants.Commands.RejectRestaurant;

/// <summary>
/// Từ chối / gỡ duyệt nhà hàng. Tách riêng khỏi <c>ApproveRestaurant</c> để client không phải
/// gửi một cờ boolean "được duyệt hay không" — mỗi hành động một command, audit log sau này
/// đọc dễ hơn nhiều so với một command có tham số mang nghĩa ngược nhau.
/// </summary>
public sealed record RejectRestaurantCommand(Guid RestaurantId) : IRequest<Unit>;