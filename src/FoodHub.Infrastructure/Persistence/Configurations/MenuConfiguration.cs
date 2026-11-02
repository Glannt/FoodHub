using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants;
using FoodHub.Domain.Restaurants.Menus;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(m => m.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(m => m.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(m => m.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(m => m.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(m => m.CurrentVersionId).HasColumnName("current_version_id");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Versions)
            .WithOne()
            .HasForeignKey(v => v.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Versions)
            .HasField("_versions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(m => m.Schedules)
            .WithOne()
            .HasForeignKey(s => s.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Schedules)
            .HasField("_schedules")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // CurrentVersionId trỏ tới một version của chính menu. Dùng SetNull để khi xoá
        // version không làm mất menu; quan hệ với Versions ở trên vẫn cascade bình thường.
        builder.HasOne<MenuVersion>()
            .WithMany()
            .HasForeignKey(m => m.CurrentVersionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.RestaurantId, m.IsActive });
    }
}
