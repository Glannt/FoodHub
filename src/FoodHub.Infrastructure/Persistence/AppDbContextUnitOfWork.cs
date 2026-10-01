using FoodHub.Domain.Persistence;

namespace FoodHub.Infrastructure.Persistence;

/// <summary>
/// Hiện thực <see cref="IUnitOfWork"/> bằng cách bọc <see cref="AppDbContext"/>.
/// <para>
/// Tại sao cần lớp này: tầng Application được phép biết tới interface <c>IUnitOfWork</c>
/// (thuộc Domain) nhưng không được biết EF Core. Nếu để <c>AppDbContext</c> trực tiếp hiện thực
/// <c>IUnitOfWork</c> thì Application vẫn phải khai báo kiểu cụ thể là AppDbContext khi đăng ký DI.
/// Lớp bọc này giữ đúng ranh giới: Domain/Application chỉ biết interface, chỉ Infrastructure biết DbContext.
/// </para>
/// <para>
/// QUAN TRỌNG — phải dùng CHUNG một AppDbContext với <c>IApplicationDbContext</c>:
/// EF Core chỉ theo dõi thay đổi (ChangeTracker) trên đúng instance đã Add/Modify.
/// Nếu handler gọi <c>dbContext.Restaurants.Add(...)</c> trên instance A rồi lại
/// <c>unitOfWork.SaveChangesAsync()</c> trên instance B, thì B không thấy thay đổi của A và
/// sẽ ghi 0 bản ghi — lỗi "im lặng", không ném exception nào cả.
/// Vì vậy trong DependencyInjection, cả hai đều resolve từ GetRequiredService&lt;AppDbContext&gt;().
/// </para>
/// </summary>
public sealed class AppDbContextUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    /// <summary>
    /// Trả về số bản ghi đã ghi. Số này được dùng để kiểm chứng kết quả
    /// (ví dụ sau khi Add thì số bản ghi phải lớn hơn 0), nên không nuốt giá trị trả về.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
