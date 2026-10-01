using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

/// <summary>
/// Domain event: "vừa có một món ăn mới được thêm vào nhà hàng X".
///
/// Vì sao là <c>record</c>?
///   - Sự kiện là DỆT THÔNG TIN (immutable snapshot tại thời điểm xảy ra).
///   - <c>record</c> tự sinh Equals/GetHashCode theo giá trị, nên test có thể so sánh
///     hai sự kiện bằng == thay vì so từng property.
///   - <c>sealed</c>: sự kiện không bao giờ được kế thừa/đa hình — không cần thiết và gây rối.
///
/// Vì sao <c>public DateTimeOffset OccurredOn { get; init; }</c>?
///   - Để biết sự kiện xảy ra lúc nào, phục vụ truy vết (tracing) và xử lý outbox.
///   - <c>init</c> = chỉ gán được trong constructor hoặc object initializer, không gán lại sau đó.
/// </summary>
public sealed record MenuItemAddedEvent(
    Guid MenuItemId,
    Guid RestaurantId,
    string Name) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
