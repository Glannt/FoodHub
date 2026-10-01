using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants;

namespace FoodHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ánh xạ entity <see cref="MenuItem"/> vào bảng <c>menu_items</c>.
/// </summary>
public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(i => i.CategoryId).HasColumnName("category_id");
        builder.Property(i => i.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();

        // Tiền dùng numeric(18,2) chứ không dùng float/double.
        // Lý do: double chỉ mất độ chính xác ở tầng 15-16 chữ số quanh 1.0 nhưng với giá trị lớn
        // (ví dụ 1e15) sai số là hàng trăm đơn vị. Kiểu double sẽ làm lệch tiền 0.01 —
        // đủ để mỗi đơn hỏng 1đ, 10.000 đơn là mất tiền thật.
        // numeric là kiểu chính xác tuyệt đối trong Postgres: 18 chữ số (15 trước + 2 sau dấu phẩy)
        // là thoải mái cho mọi mức giá của món ăn.
        builder.Property(i => i.Price).HasColumnName("price").HasColumnType("numeric(18,2)");

        // Default true ở database: món mới tạo luôn hiển thị được.
        builder.Property(i => i.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);

        // ImageUrl là string? nên KHÔNG đặt IsRequired — cột cho phép NULL.
        // Nếu đánh dấu IsRequired thì mọi món không có ảnh đều không lưu được.
        builder.Property(i => i.ImageUrl).HasColumnName("image_url").HasMaxLength(500);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        // Cả 2 khoá ngoại đều Cascade: xoá nhà hàng hoặc xoá nhóm món thì các món thuộc về
        // cũng bị xoá theo — không để lại món "mồ côi" tham chiếu tới bản ghi không còn tồn tại.
        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(i => i.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // MenuItem là aggregate root riêng, truy vấn chính là "lấy menu của nhà hàng"
        // (lọc theo RestaurantId rồi nhóm theo CategoryId), nên index phủ 2 cột đó.
        // Thứ tự cột trong composite index phải khớp thứ tự điều kiện lọc để Postgres dùng được.
        builder.HasIndex(i => new { i.RestaurantId, i.CategoryId });
    }
}
