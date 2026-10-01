using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

/// <summary>
/// Aggregate root của module Nhà hàng.
/// Là "ranh giới giao dịch" (transaction boundary) của cả module: mọi thay đổi
/// lên MenuCategory / MenuItem đều phải đi qua Restaurant nếu muốn bảo toàn bất biến xuyên suốt.
/// </summary>
public sealed class Restaurant : AggregateRoot<Guid>
{
    // Ctor rỗng, private: bắt buộc cho EF Core.
    // EF cần gọi được 1 ctor không tham số để nạp entity từ database.
    // Private để chặn code khởi tạo tùy tiện — chỉ được tạo qua ctor public có Guard.
    private Restaurant()
    {
    }

    public Restaurant(string name, string addressText, decimal latitude, decimal longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressText);

        Name = name;
        AddressText = addressText;
        Latitude = latitude;
        Longitude = longitude;
        IsApproved = false;
    }

    public string Name { get; private set; } = string.Empty;

    public string AddressText { get; private set; } = string.Empty;

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    public bool IsApproved { get; private set; }

    /// <summary>Quản trị viên duyệt nhà hàng.</summary>
    public void Approve()
    {
        IsApproved = true;
        Touch();
    }

    /// <summary>Quản trị viên từ chối / gỡ duyệt nhà hàng.</summary>
    public void Reject()
    {
        IsApproved = false;
        Touch();
    }
}
