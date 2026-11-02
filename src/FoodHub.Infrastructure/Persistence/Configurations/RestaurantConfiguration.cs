using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants;

namespace FoodHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ánh xạ entity <see cref="Restaurant"/> vào bảng <c>restaurants</c>.
/// <para>
/// Mỗi bảng có 1 file Configuration riêng, không gộp tất cả vào OnModelCreating, để khi
/// schema phình to thì từng thay đổi vẫn nằm gọn trong 1 file và dễ review/diff.
/// </para>
/// </summary>
public sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        // Tên bảng và tên cột viết snake_case khớp với quy ước Postgres.
        // Chỉ định TƯỜNG MINH (không dựa vào naming convention) để model không đổi hành
        // khi ai đó đổi cấu hình naming: đã có trong database thì phải giữ nguyên tên cột.
        builder.ToTable("restaurants");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.Address).HasColumnName("address_text").HasMaxLength(500).IsRequired();

        // Toạ độ dùng numeric chứ không dùng float/double: Postgres không có kiểu float thật sự,
        // và float làm sai số ở mức km — không chấp nhận được với tọa độ địa lý.
        // numeric(9,6): tối đa 999.999999 — đủ cho mọi vĩ độ/tọa độ trên thế giới,
        // đồng thời giữ được 6 chữ số thập phân (~11 cm).
        builder.Property(r => r.Latitude).HasColumnName("latitude").HasColumnType("numeric(9,6)");
        builder.Property(r => r.Longtitude).HasColumnName("longtitude").HasColumnType("numeric(9,6)");

        // Default ở tầng database: nhà hàng mới luôn ở trạng thái chờ duyệt (false),
        // kể cả khi có câu lệnh INSERT thiếu cột này thì cũng không tự ý thành "đã duyệt".
        builder.Property(r => r.IsApproved).HasColumnName("is_approved").HasDefaultValue(false);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        // Index theo tên: đây là truy vấn tìm nhà hàng theo tên, index giúp Postgres
        // không phải quét tuần tự (seq scan) toàn bảng mỗi lần tìm kiếm.
        builder.HasIndex(r => r.Name);
    }
}
