using MediatR;

namespace FoodHub.Application.Restaurants.Commands.ApproveRestaurant;

/// <summary>
/// Duyệt nhà hàng. Tách khỏi <c>UpdateRestaurant</c> vì đây là hành động của quản trị viên,
/// không phải của chủ nhà hàng.
/// <para>
/// TODO Giai đoạn 2: endpoint này sẽ yêu cầu <c>[Authorize(Roles = "Admin")]</c> ở tầng Api.
/// Hiện chưa có auth nên ai gọi cũng duyệt được — chấp nhận tạm cho tới khi có Identity/JWT.
/// </para>
/// </summary>
public sealed record ApproveRestaurantCommand(Guid RestaurantId) : IRequest<Unit>;