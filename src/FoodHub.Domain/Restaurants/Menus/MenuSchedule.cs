using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Menus;

public sealed class MenuSchedule : Entity<Guid>
{
    private MenuSchedule()
    {
    }

    public MenuSchedule(Guid menuId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly? validFrom, DateOnly? validTo, int priority)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(priority);

        MenuId = menuId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Priority = priority;

        Id = Guid.NewGuid();
    }

    public Guid MenuId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public int Priority { get; private set; }

    /// <summary>
    /// Có khớp với thời điểm cho trước không.
    /// Lưu ý: EndTime &lt; StartTime được phép để biểu diễn khung giờ qua đêm
    /// (ví dụ 22:00 → 02:00). Khi đó điều kiện được hiểu là "sau StartTime HOẶC trước EndTime".
    /// </summary>
    public bool Matches(DateTime localDateTime)
    {
        var date = DateOnly.FromDateTime(localDateTime);

        if (ValidFrom.HasValue && date < ValidFrom.Value) return false;
        if (ValidTo.HasValue && date > ValidTo.Value) return false;

        if (DayOfWeek != localDateTime.DayOfWeek) return false;

        var time = TimeOnly.FromDateTime(localDateTime);
        return StartTime <= EndTime
            ? time >= StartTime && time <= EndTime
            : time >= StartTime || time <= EndTime;
    }

    public void Update(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly? validFrom, DateOnly? validTo, int priority)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(priority);

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Priority = priority;
        Touch();
    }
}