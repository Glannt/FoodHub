using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

/// <summary>
/// Món ăn. Là aggregate root riêng (tách khỏi Restaurant) vì món ăn có vòng đời
/// độc lập và thay đổi rất thường xuyên — nếu gộp vào aggregate Restaurant,
/// mỗi lần đổi trạng thái món sẽ kéo theo việc nạp và kiểm soát toàn bộ menu của nhà hàng.
/// </summary>
public sealed class MenuItem : AggregateRoot<Guid>
{
    private MenuItem()
    {
    }

    public MenuItem(
        Guid restaurantId,
        Guid categoryId,
        string name,
        string description,
        decimal price,
        string? imageUrl)
    {
        // Guard — bất biến nghiệp vụ được kiểm tra TẠI LÚC TẠO MỚI.
        // Đây là điểm khác biệt cốt lõi so với DTO: DTO có thể nhận giá trị rác,
        // Aggregate thì không bao giờ tồn tại ở trạng thái không hợp lệ.
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        RestaurantId = restaurantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        IsAvailable = true;

        // Bắn sự kiện — chỉ ghi vào List trong RAM, chưa gửi đi đâu cả.
        Raise(new MenuItemAddedEvent(Guid.NewGuid(), restaurantId, name));
    }

    public Guid RestaurantId { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public bool IsAvailable { get; private set; }

    public string? ImageUrl { get; private set; }

    /// <summary>Cập nhật thông tin món. Mọi thay đổi phải qua method của Domain, không set trực tiếp.</summary>
    public void UpdateDetails(string name, string description, decimal price, string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        Name = name;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;

        Touch();
    }

    /// <summary>Nhà hàng bật/tắt món khi hết hàng.</summary>
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        Touch();
    }
}
