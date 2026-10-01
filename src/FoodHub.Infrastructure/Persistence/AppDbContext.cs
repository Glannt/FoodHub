using Microsoft.EntityFrameworkCore;

using FoodHub.Application.Abstractions;
using FoodHub.Domain.Restaurants;

namespace FoodHub.Infrastructure.Persistence;

/// <summary>
/// DbContext duy nhất của hệ thống — hiện thực <see cref="IApplicationDbContext"/>,
/// lớp trừu tượng mà tầng Application dùng để truy vấn dữ liệu.
/// <para>
/// Tách lớp trừu tượng <c>IApplicationDbContext</c> ra khỏi <c>DbContext</c> cụ thể là để
/// tầng Application không bao giờ phụ thuộc vào EF Core: sau này đổi sang provider khác
/// (hoặc thêm module có database riêng) thì chỉ tầng Infrastructure phải sửa.
/// </para>
/// <para>
/// Đây là lớp "sealed": DbContext là chi tiết triển khai, không ai trong hệ thống được
/// kế thừa lại để mở rộng. Muốn mở rộng hành vi thì dùng interceptor / IEntityTypeConfiguration.
/// </para>
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    /// <summary>
    /// Dùng biểu thức <c>Set&lt;T&gt;()</c> thay vì <c>new DbSet&lt;T&gt;()</c>:
    /// EF Core tự hiện thực DbSet và có thể thay thế bằng bản proxy (lazy loading) nếu cần,
    /// nên không bao giờ tự khởi tạo DbSet bằng tay.
    /// </summary>
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    /// <summary>
    /// Áp dụng toàn bộ <c>IEntityTypeConfiguration</c> nằm trong assembly này.
    /// <para>
    /// Nhờ cách khai báo theo convention này, thêm bảng mới không cần sửa AppDbContext —
    /// chỉ cần thêm một file Configuration trong thư mục Persistence/Configurations.
    /// Ngược lại, nếu hardcode từng bảng ở đây thì file này phình to và mỗi bảng mới đều phải sửa cả 2 nơi.
    /// </para>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Gọi base ở cuối: base.OnModelCreating áp dụng các quy ước mặc định của DbContext
        // sau khi các Configuration cụ thể đã chạy, để cấu hình chi tiết được ưu tiên.
        base.OnModelCreating(modelBuilder);
    }
}
