using MediatR;

namespace FoodHub.Application.Restaurants.Commands.CreateRestaurant;

/// <summary>
/// Command tạo mới một nhà hàng trong hệ thống.
/// Chứa đầy đủ thông tin chi tiết của nhà hàng theo Domain Model.
/// Id của nhà hàng do server cấp phát để đảm bảo an toàn.
/// </summary>
public sealed record CreateRestaurantCommand(
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
    Guid OwnerId) : IRequest<CreateRestaurantResult>;

public sealed record CreateRestaurantResult(
    Guid RestaurantId,
    string Slug);
