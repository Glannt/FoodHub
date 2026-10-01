using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants;

namespace FoodHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ánh xạ entity <see cref="MenuCategory"/> vào bảng <c>menu_categories</c>.
/// </summary>
public sealed class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("menu_categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Có navigation property thì nên khai báo quan hệ bằng HasOne để EF tự sinh
        // ForeignKey + Index tương ứng, thay vì chỉ gán Property(c => c.RestaurantId).
        // Quan hệ này không khai báo navigation trên domain (Restaurant không giữ danh sách
        // MenuCategory) nên dùng dạng HasOne<Restaurant>().WithMany() — chỉ cần phía khoá ngoại.
        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(c => c.RestaurantId)
            // Cascade: xoá nhà hàng thì xoá luôn toàn bộ nhóm món của nhà hàng đó.
            // Đúng với bản chất dữ liệu — nhóm món mà không còn nhà hàng thì vô nghĩa,
            // và để lại sẽ làm rò rỉ dữ liệu mồ côi.
            .OnDelete(DeleteBehavior.Cascade);

        // Index UNIQUE trên (RestaurantId, Name) chặn 2 nhóm món trùng tên trong cùng 1 nhà hàng.
        // Đặt ràng buộc ở database chứ không chỉ kiểm tra trong code Application, vì code có thể bị
        // bỏ qua qua nhiều đường (script tay, migration, service khác) — unique constraint thì luôn đúng.
        builder.HasIndex(c => new { c.RestaurantId, c.Name }).IsUnique();
    }
}
