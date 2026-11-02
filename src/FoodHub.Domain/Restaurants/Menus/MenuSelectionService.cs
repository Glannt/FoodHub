namespace FoodHub.Domain.Restaurants.Menus;

/// <summary>
/// Domain service chọn menu đang hiệu lực cho một nhà hàng tại một thời điểm.
/// <para>
/// Vì sao là service chứ không phải method trên <see cref="Menu"/>?
/// Việc chọn menu cần so sánh NHIỀU <see cref="Menu"/> với nhau (lịch, độ ưu tiên,
/// fallback IsDefault) — nằm ngoài phạm vi một aggregate. Đặt ở đây giữ cho mỗi
/// Menu vẫn là ranh giới nhất quán độc lập, đồng thời tập trung logic chọn lựa một chỗ.
/// </para>
/// <para>
/// Lưu ý thời gian: tham số <paramref name="now"/> là UTC; service tự quy đổi sang
/// múi giờ của nhà hàng trước khi so với <see cref="MenuSchedule"/>.
/// </para>
/// </summary>
public static class MenuSelectionService
{
    public static Menu? GetActiveMenu(
        IEnumerable<Menu> menus,
        DateTimeOffset now,
        TimeZoneInfo restaurantTimeZone)
    {
        ArgumentNullException.ThrowIfNull(menus);
        ArgumentNullException.ThrowIfNull(restaurantTimeZone);

        var localNow = TimeZoneInfo.ConvertTime(now, restaurantTimeZone).DateTime;

        var active = menus.Where(m => m.IsActive).ToList();

        // 1) Menu có lịch khớp, chọn theo Priority cao nhất (mỗi menu lấy lịch ưu tiên nhất của nó).
        var best = active
            .Select(menu => new
            {
                Menu = menu,
                Schedule = menu.Schedules
                    .Where(s => s.Matches(localNow))
                    .OrderByDescending(s => s.Priority)
                    .FirstOrDefault()
            })
            .Where(x => x.Schedule is not null)
            .OrderByDescending(x => x.Schedule!.Priority)
            .FirstOrDefault();

        if (best is not null)
            return best.Menu;

        // 2) Không có lịch nào khớp → fallback về menu mặc định.
        return active.FirstOrDefault(m => m.IsDefault);
    }
}