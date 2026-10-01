using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodHub.Infrastructure.Persistence;

/// <summary>
/// Factory tạo DbContext lúc DESIGN TIME (chạy `dotnet ef migrations add ...`).
/// <para>
/// Tại sao cần: `dotnet ef` không chạy ứng dụng thật, nên không có DI container và không lấy được
/// chuỗi kết nối từ appsettings.json. Không có factory này thì lệnh `dotnet ef` sẽ báo lỗi
/// "Unable to create an object of type 'AppDbContext'".
/// </para>
/// <para>
/// LƯU Ý: chuỗi kết nối bên dưới CHỈ dùng lúc sinh migration, KHÔNG dùng lúc chạy app.
/// Lúc chạy thật, chuỗi kết nối đến từ `appsettings.json` qua AddInfrastructure trong Program.cs.
/// Nhờ tách vậy, thông tin kết nối thật không nằm trong mã nguồn Infrastructure.
/// </para>
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // TODO: sửa thành Postgres của bạn trước khi chạy `dotnet ef migrations add`.
    private const string DesignTimeConnectionString =
        "Host=localhost;Database=foodhub;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        // args bị bỏ qua có chủ đích: khi chạy qua `dotnet ef` thì EF chỉ cần một DbContext
        // đúng kiểu để đọc/ghi model, không truyền tham số cấu hình nào cần thiết.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
