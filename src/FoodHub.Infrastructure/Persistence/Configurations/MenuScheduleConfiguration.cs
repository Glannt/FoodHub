using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants.Menus;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class MenuScheduleConfiguration : IEntityTypeConfiguration<MenuSchedule>
{
    public void Configure(EntityTypeBuilder<MenuSchedule> builder)
    {
        builder.ToTable("menu_schedules");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.MenuId).HasColumnName("menu_id");
        builder.Property(s => s.DayOfWeek).HasColumnName("day_of_week").HasConversion<int>();
        builder.Property(s => s.StartTime).HasColumnName("start_time");
        builder.Property(s => s.EndTime).HasColumnName("end_time");
        builder.Property(s => s.ValidFrom).HasColumnName("valid_from");
        builder.Property(s => s.ValidTo).HasColumnName("valid_to");
        builder.Property(s => s.Priority).HasColumnName("priority");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(s => new { s.MenuId, s.DayOfWeek, s.Priority });
    }
}
