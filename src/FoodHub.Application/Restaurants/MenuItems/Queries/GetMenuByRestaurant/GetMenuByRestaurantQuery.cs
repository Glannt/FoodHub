using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Queries.GetMenuByRestaurant;

public sealed record GetMenuByRestaurantQuery(Guid RestaurantId)
    : IRequest<GetMenuByRestaurantResult>;

// DTO đọc được tách riêng khỏi entity Domain: entity có setter private và bất biến,
// không thể khởi tạo trực tiếp trong projection của LINQ.
public sealed record GetMenuByRestaurantResult(
    Guid RestaurantId,
    string RestaurantName,
    IReadOnlyList<MenuCategoryDto> Categories);

public sealed record MenuCategoryDto(
    Guid Id,
    string Name,
    IReadOnlyList<MenuItemDto> Items);

public sealed record MenuItemDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsAvailable,
    string? ImageUrl);