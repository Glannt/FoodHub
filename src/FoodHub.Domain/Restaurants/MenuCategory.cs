using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

/// <summary>
/// Entity (không phải aggregate root) — nhóm món, ví dụ "Món chính", "Nước".
/// Thuộc về aggregate Restaurant nhưng bản thân không có vòng đời độc lập:
/// không tồn tại ý nghĩa nếu Restaurant bị xoá.
/// </summary>
public sealed class MenuCategory : Entity<Guid>
{
    private MenuCategory()
    {
    }

    public MenuCategory(Guid restaurantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RestaurantId = restaurantId;
        Name = name;
    }

    public Guid RestaurantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Touch();
    }
}
