using MediatR;

namespace FoodHub.Application.Restaurants.Commands.UpdateRestaurant;

/// <summary>
/// Command cập nhật thông tin chi tiết nhà hàng.
/// Chú ý: Trạng thái duyệt (Approve/Reject) được tách thành command riêng của Admin.
/// </summary>
public sealed record UpdateRestaurantCommand(
    Guid RestaurantId,
    string Name,
    string? Slug,
    string? Description,
    string PhoneNumber,
    string Email,
    string Address,
    double Latitude,
    double Longtitude,
    string? LogoUrl,
    string? CoverImageUrl,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    decimal MinOrderAmount,
    bool? IsAcceptingOrders) : IRequest<UpdateRestaurantResult>;

public sealed record UpdateRestaurantResult(Guid RestaurantId);
