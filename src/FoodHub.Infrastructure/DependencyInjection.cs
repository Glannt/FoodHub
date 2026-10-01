using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Infrastructure.Persistence;

namespace FoodHub.Infrastructure;

/// <summary>
/// Điểm vào duy nhất để đăng ký mọi thứ của tầng Infrastructure vào DI container.
/// <para>
/// Tách ra extension method <c>AddInfrastructure</c> để tầng Api chỉ cần gọi
/// <c>builder.Services.AddInfrastructure(builder.Configuration)</c> mà không phải biết
/// DbContext, provider Postgres hay connection string nào được dùng.
/// </para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Chuỗi kết nối lấy từ appsettings.json (mục ConnectionStrings:Default),
        // KHÔNG hardcode trong code: mỗi môi trường (dev/staging/prod) một chuỗi khác nhau.
        //
        // Chỉ chặn trường hợp THIẾU HẲN node cấu hình (null), báo lỗi ngay lúc khởi động —
        // vì lỗi "không tìm thấy ConnectionStrings:Default" dễ hiểu hơn nhiều so với lỗi
        // mơ hồ "no database provider configured" xảy ra giữa lúc chạy.
        //
        // Cố tình KHÔNG chặn chuỗi rỗng (""): ở giai đoạn dev, khi chưa dựng Postgres,
        // appsettings để "Default": "" để app vẫn khởi động được (xem Program.cs — health check
        // DB chỉ được đăng ký khi chuỗi không rỗng). Chặn rỗng ở đây sẽ làm app crash ngay.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình \"ConnectionStrings:Default\" trong appsettings.json.");

        // AddDbContext đăng ký chính AppDbContext với Lifetime mặc định là Scoped.
        // Scoped = 1 instance duy nhất cho mỗi HTTP request, nên mọi thao tác đọc/ghi
        // trong cùng 1 request đều dùng chung một ChangeTracker và có thể ghi trong 1 transaction.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // GHI CHÚ: 2 dòng dưới đây CỐ TÌNH dùng GetRequiredService<AppDbContext>() thay vì
        // new AppDbContext(...) hay AddDbContext lần nữa — để cả IApplicationDbContext và IUnitOfWork
        // trỏ TỚI CÙNG MỘT instance AppDbContext đã được AddDbContext đăng ký.
        //
        // TẠI SAO phải chung instance? EF Core chỉ theo dõi thay đổi trên instance đã Add/Modify.
        // Nếu handler Add vào dbContext_A (IApplicationDbContext) rồi lại SaveChangesAsync trên
        // unitOfWork_B (IUnitOfWork gắn với dbContext_B), thì B không thấy thay đổi của A và sẽ
        // ghi ra 0 bản ghi — lỗi "im lặng", không exception nào được ném ra.
        // Dùng chung 1 instance là cách duy nhất bảo đảm SaveChangesAsync luôn thấy đúng thay đổi cần lưu.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork>(sp => new AppDbContextUnitOfWork(sp.GetRequiredService<AppDbContext>()));

        return services;
    }
}
