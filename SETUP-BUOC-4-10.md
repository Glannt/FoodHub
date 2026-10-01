# FoodHub — Hướng dẫn Bước 4 đến Bước 10

Trạng thái hiện tại: Bước 0–3 **đã xong** (`dotnet build` sạch 0 warning / 0 error).

| Đã xong | Nội dung |
|---|---|
| Bước 0 | `git init`, `.gitignore` |
| Bước 1 | 5 project + `FoodHub.slnx` + project references |
| Bước 2 | `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` |
| Bước 3 | Package references (không version, CPM lo version) |

File này làm tiếp: **Bước 4 → 10**.

> Bạn tự cấu hình Postgres, nên **Bước 10 chỉ viết code, chưa chạy migration**.
> Mọi thứ ở Bước 4–9 đều chạy được **không cần database**.

### 4 lỗi trong bản gốc đã được sửa (đã kiểm chứng bằng build thật)

| # | Vị trí | Lỗi | Sửa thành |
|---|---|---|---|
| ① | §7.5 | `UseSnakeCaseNamingConvention()` thuộc package `EFCore.NamingConventions` không có trong `Directory.Packages.props` → `CS1061` | Bỏ hẳn, dùng `HasColumnName(...)` tường minh trong từng `Configuration` |
| ② | §7.5 | `AppDbContext` không implement `IUnitOfWork` → `sp.GetRequiredService<AppDbContext>()` không ép được → `CS0266` | Bọc: `new AppDbContextUnitOfWork(sp.GetRequiredService<AppDbContext>())` |
| ③ | §8.3 | `ProblemDetails` không có thuộc tính `Errors` → `CS0117`; dùng `ValidationProblemDetails` thì `errors` bị mất khi serialize | `HttpValidationProblemDetails(errors)` |
| ④ | §10.2 | `MenuItem : Entity<Guid>` nhưng constructor gọi `Raise(...)` → `CS0103` | `MenuItem : AggregateRoot<Guid>` |

Mỗi lỗi đều được ghi chú ngay tại chỗ trong tài liệu, kèm giải thích vì sao.

> **Bài học rút ra:** 4 lỗi trên đều là lỗi *"viết trông đúng nhưng không chạy được"*.
> Chỉ có `dotnet build` mới bắt được — không có tài liệu nào bắt được.

---

## Quy ước bắt buộc trong project

```text
FoodHub/
├── src/
│   ├── BuildingBlocks/FoodHub.SharedKernel/
│   ├── FoodHub.Domain/                    <- KHÔNG được using EFCore, MediatR, SignalR
│   ├── FoodHub.Application/               <- KHÔNG được using Infrastructure, Api
│   ├── FoodHub.Infrastructure/            <- hiện thực interface của Domain/Application
│   └── FoodHub.Api/                       <- Controllers, DI wiring, Middleware
```

Chiều tham chiếu: `Api → Infrastructure → Application → Domain → SharedKernel`

- `TreatWarningsAsErrors=true` trong `Directory.Build.props` → **mọi warning là lỗi**, phải sửa hết.
- Mọi file C# dùng **file-scoped namespace**.
- `using` xếp: System trước, rồi Microsoft, rồi của project (`.editorconfig` đã ép `dotnet_sort_system_directives_first`).

---

# BƯỚC 4 — SharedKernel

## 4.1 Xoá file mẫu

```powershell
Remove-Item src\BuildingBlocks\FoodHub.SharedKernel\Class1.cs
```

## 4.2 Tạo 4 file trong `src\BuildingBlocks\FoodHub.SharedKernel\`

Tạo file trong Visual Studio: click chuột phải vào **project** `FoodHub.SharedKernel` (không phải solution) → Add → New Item → Class → đặt tên. Hoặc tạo bằng PowerShell:

```powershell
cd D:\WorkSpace\project\FoodHub\src\BuildingBlocks\FoodHub.SharedKernel
```

### `IDomainEvent.cs`
```csharp
namespace FoodHub.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
```

### `Entity.cs`
```csharp
namespace FoodHub.SharedKernel;

public abstract class Entity<TId> where TId : IEquatable<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
```

### `AggregateRoot.cs`
```csharp
namespace FoodHub.SharedKernel;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### `Result.cs`
```csharp
namespace FoodHub.SharedKernel;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Result thành công không được mang Error.");

        if (!isSuccess && error is null)
            throw new InvalidOperationException("Result thất bại phải có Error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error? error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không được đọc Value của Result thất bại.");
}

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string what) => new("NotFound", $"Không tìm thấy {what}.");

    public static Error Validation(string message) => new("Validation", message);
}
```

---

# BƯỚC 5 — Domain

## 5.1 Xoá file mẫu

```powershell
Remove-Item src\FoodHub.Domain\Class1.cs
```

## 5.2 Tạo `src\FoodHub.Domain\Persistence\IUnitOfWork.cs`

```csharp
namespace FoodHub.Domain.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

> **Điểm quan trọng:** file này **không** có `using Microsoft.EntityFrameworkCore`.
> Domain chỉ biết "có thứ gì đó lưu thay đổi", không biết EF tồn tại.
> Đây là ranh giới sạch nhất của Clean Architecture trong project bạn.

```powershell
New-Item -ItemType Directory -Force src\FoodHub.Domain\Persistence
```

---

# BƯỚC 6 — Application

## 6.1 Xoá file mẫu

```powershell
Remove-Item src\FoodHub.Application\Class1.cs
New-Item -ItemType Directory -Force src\FoodHub.Application\Abstractions
New-Item -ItemType Directory -Force src\FoodHub.Application\Behaviors
```

## 6.2 `Abstractions/NotFoundException.cs`

```csharp
namespace FoodHub.Application.Abstractions;

public sealed class NotFoundException(string message) : Exception(message);
```

## 6.3 `Abstractions/IApplicationDbContext.cs`

```csharp
using FoodHub.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Restaurant> Restaurants { get; }

    DbSet<MenuCategory> MenuCategories { get; }

    DbSet<MenuItem> MenuItems { get; }
}
```

> **Thỏa hiệp có chủ đích:** Application tham chiếu `Microsoft.EntityFrameworkCore` để dùng `DbSet<T>` và các extension như `AsNoTracking()`, `AnyAsync()`.
> Bản "sạch tuyệt đối" là Application chỉ khai báo `IQueryable<T>`, nhưng khi đó vẫn phải tham chiếu EFCore để gọi extension method — nên mất đi lợi ích, mất luôn type safety.
> Với dự án học tập, chấp nhận thỏa hiệp này. Test kiến trúc ở Bước 3 roadmap (tuần 5) sẽ canh: **Domain không được tham chiếu EFCore**.

## 6.4 `Behaviors/ValidationBehavior.cs`

```csharp
using FluentValidation;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace FoodHub.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var validationContext = new ValidationContext<TRequest>(request);

            var results = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(validationContext, cancellationToken)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToArray();

            if (failures.Length > 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

## 6.5 `Behaviors/LoggingBehavior.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Bắt đầu xử lý {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        logger.LogInformation(
            "Xử lý {RequestName} xong trong {ElapsedMilliseconds} ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

> `Stopwatch` nằm trong `System.Diagnostics`. File-scoped namespace **không** tự import `using`, nên phải thêm dòng `using System.Diagnostics;` lên đầu file.

## 6.6 `DependencyInjection.cs` (ghi đè file đã có)

```csharp
using System.Reflection;
using FluentValidation;
using FoodHub.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

---

# BƯỚC 7 — Infrastructure

## 7.1 Xoá file mẫu + tạo thư mục

```powershell
Remove-Item src\FoodHub.Infrastructure\Class1.cs
New-Item -ItemType Directory -Force src\FoodHub.Infrastructure\Persistence
```

## 7.2 `Persistence/AppDbContext.cs`

```csharp
using FoodHub.Application.Abstractions;
using FoodHub.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## 7.3 `Persistence/IDesignTimeDbContextFactory.cs`

Dùng cho `dotnet ef` khi không cần chạy app:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodHub.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=foodhub;Username=postgres;Password=postgres")
            .Options;

        return new AppDbContext(options);
    }
}
```

> Chuỗi kết nối ở đây **chỉ dùng lúc ghi migration**. Lúc chạy thật, `Program.cs` đọc từ `appsettings.json`.
> **Sửa thành Postgres của bạn** khi tới lúc chạy `dotnet ef`.

## 7.4 `Persistence/AppDbContextUnitOfWork.cs`

```csharp
using FoodHub.Domain.Persistence;

namespace FoodHub.Infrastructure.Persistence;

public sealed class AppDbContextUnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
```

## 7.5 `DependencyInjection.cs` (ghi đè file đã có)

```csharp
using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình \"ConnectionStrings:Default\" trong appsettings.json.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUnitOfWork>(sp =>
            new AppDbContextUnitOfWork(sp.GetRequiredService<AppDbContext>()));

        return services;
    }
}
```

> **Sửa ① — không dùng `UseSnakeCaseNamingConvention()`.**
> Extension này đến từ package `EFCore.NamingConventions`, **không có** trong `Directory.Packages.props`.
> Gọi nó sẽ ra `CS1061`. Thay vào đó, các class `Configuration` ở Bước 10 chỉ định tường minh
> `HasColumnName("menu_item_id")` cho **mọi** property — tường minh hơn convention, và đọc file
> database thì ai cũng hiểu cột tên gì mà không cần đoán.
>
> **Sửa ② — `IUnitOfWork` phải bọc, không phải trả thẳng.**
> `AppDbContext` **không** implement `IUnitOfWork`, nên `sp.GetRequiredService<AppDbContext>()`
> không ép được sang `IUnitOfWork` → `CS0266`. Phải bọc qua `AppDbContextUnitOfWork`.
> Lý do không cho `AppDbContext` implement thẳng: nếu implement, tầng `Application` khi đăng ký DI
> sẽ phải nhắc tới kiểu `AppDbContext` (thuộc Infrastructure) → phá vỡ ranh giới tầng.
>
> **Điểm cần nhớ:** cả hai dòng `AddScoped` đều gọi `GetRequiredService<AppDbContext>()`,
> nên **cùng trỏ tới một** `AppDbContext` → chung `ChangeTracker`.
> Nếu để 2 interface trỏ tới 2 `DbContext` khác nhau thì `Add()` xong `SaveChangesAsync()`
> sẽ INSERT 0 dòng, **không có lỗi, không có cảnh báo**.

---

# BƯỚC 8 — Api

## 8.1 Xoá file mẫu của template

```powershell
Remove-Item src\FoodHub.Api\WeatherForecast.cs
Remove-Item src\FoodHub.Api\Controllers\WeatherForecastController.cs
Remove-Item src\FoodHub.Api\FoodHub.Api.http
New-Item -ItemType Directory -Force src\FoodHub.Api\Middleware
```

## 8.2 `Middleware/NotFoundExceptionHandler.cs`

```csharp
using FoodHub.Application.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Middleware;

public sealed class NotFoundExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy dữ liệu",
                Detail = notFoundException.Message
            }
        });
    }
}
```

## 8.3 `Middleware/ValidationExceptionHandler.cs`

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Middleware;

public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ"
            }
        });
    }
}
```

> **Sửa ③ — bắt buộc dùng `HttpValidationProblemDetails`, không dùng `ProblemDetails`.**
> `ProblemDetails` **không có** thuộc tính `Errors` → `CS0117`.
> Không chỉ vậy: nếu dùng `ValidationProblemDetails`, `IProblemDetailsService` khi serialize với
> kiểu khai báo `ProblemDetails` sẽ **mất sạch** `errors`. Chỉ `HttpValidationProblemDetails` có
> writer riêng nên giữ được `{"errors":{"Name":["..."]}}`.
> Đã kiểm chứng bằng cách chạy thật, response trả về đúng 400 kèm `errors`.

## 8.4 Ghi đè `src\FoodHub.Api\Program.cs`

> Template .NET 10 dùng **OpenAPI built-in** (`AddOpenApi` / `MapOpenApi`), **không** phải Swashbuckle. Giữ nguyên cái đó.

```csharp
using FoodHub.Api.Middleware;
using FoodHub.Application;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("Default");

var healthChecks = builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

// Chỉ đăng ký check DB khi đã có connection string.
// Khi nào bạn điền xong Postgres vào appsettings.json thì /health/ready mới thật sự kiểm tra DB.
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

## 8.5 Cập nhật `src\FoodHub.Api\appsettings.json`

Thêm vào (giữ nguyên phần `Logging` đang có):

```json
"ConnectionStrings": {
  "Default": ""
}
```

> **Để trống** trong lúc này. Bước 9 vẫn chạy được vì `/health/live` không chạm DB.
> Bạn điền chuỗi thật sau khi cấu hình Postgres.

---

# BƯỚC 9 — Build và kiểm chứng

## 9.1 Build

```powershell
cd D:\WorkSpace\project\FoodHub
dotnet build
```

Bắt buộc: `Build succeeded. 0 Warning(s) 0 Error(s)`.

## 9.2 Chạy app

```powershell
dotnet run --project src\FoodHub.Api
```

Console sẽ in URL, thường là `https://localhost:7xxx`. Mở bằng trình duyệt:

| URL | Kỳ vọng |
|---|---|
| `https://localhost:7xxx/health/live` | `Healthy` |
| `https://localhost:7xxx/health/ready` | `Healthy` (chưa check DB vì connection string rỗng) |
| `https://localhost:7xxx/openapi/v1.json` | JSON mô tả API |

> Phải dùng **https**. `UseHttpsRedirection()` sẽ đổi hướng mọi request `http://` sang `https://`.
> Nếu trình duyệt báo cảnh báo chứng thư → chọn "Advanced" → "Proceed".

## 9.3 Kiểm tra đường còn dẫn tới đúng hướng

```powershell
dotnet list FoodHub.Api reference
dotnet list FoodHub.Domain package
```

Lệnh thứ hai **phải trả về rỗng**. Nếu ra package nào nghĩa là Domain đã bị vi phạm ranh giới kiến trúc — sửa ngay.

## 9.4 Commit

```powershell
git add .
git commit -m "feat: khoi tao solution Clean Architecture, SharedKernel, CQRS pipeline va health check"
```

---

# BƯỚC 10 — Vertical slice `MenuItems`

Đây là phần chính của Giai đoạn 1 roadmap: **DDD cơ bản + MediatR + Behaviors**.

## 10.1 Vì sao tách thành "vertical slice"?

Một use case = **xuyên suốt từ HTTP → Application → Domain → Infrastructure → DB**, đủ 4 tầng.
Làm hết 12 bảng rồi mới nối API là cách chậm nhất để phát hiện mình chọn sai kiến trúc.
Làm **1 use case thật** trước, copy pattern đó cho các use case còn lại.

## 10.2 Domain — Aggregate và entity

```powershell
New-Item -ItemType Directory -Force src\FoodHub.Domain\Restaurants
```

### `src\FoodHub.Domain\Restaurants\Restaurant.cs`
```csharp
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

public sealed class Restaurant : AggregateRoot<Guid>
{
    private Restaurant()
    {
    }

    public Restaurant(string name, string addressText, decimal latitude, decimal longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressText);

        Name = name;
        AddressText = addressText;
        Latitude = latitude;
        Longitude = longitude;
        IsApproved = false;
    }

    public string Name { get; private set; } = string.Empty;

    public string AddressText { get; private set; } = string.Empty;

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    public bool IsApproved { get; private set; }

    public void Approve() => IsApproved = true;

    public void Reject() => IsApproved = false;
}
```

> Ctor `private` rỗng là bắt buộc cho EF Core. Kế từ đây **mọi object này chỉ được tạo qua method/factory** — không có `new Restaurant()` rải rác trong code.

### `src\FoodHub.Domain\Restaurants\MenuCategory.cs`
```csharp
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

public sealed class MenuCategory : Entity<Guid>
{
    private MenuCategory()
    {
    }

    public MenuCategory(Guid restaurantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RestaurantId = restaurantId;
        Name = name;
    }

    public Guid RestaurantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
```

### `src\FoodHub.Domain\Restaurants\MenuItem.cs`
```csharp
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

public sealed class MenuItem : AggregateRoot<Guid>
{
    private MenuItem()
    {
    }

    public MenuItem(
        Guid restaurantId,
        Guid categoryId,
        string name,
        string description,
        decimal price,
        string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        RestaurantId = restaurantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        IsAvailable = true;

        Raise(new MenuItemAddedEvent(Guid.NewGuid(), restaurantId, name));
    }

    public Guid RestaurantId { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public bool IsAvailable { get; private set; }

    public string? ImageUrl { get; private set; }

    public void UpdateDetails(string name, string description, decimal price, string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        Name = name;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;

        Touch();
    }

    public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;
}
```

> **Sửa ④ — `MenuItem` kế thừa `AggregateRoot<Guid>`, không phải `Entity<Guid>`.**
> Bản gốc của tài liệu viết `Entity<Guid>` nhưng lại gọi `Raise(...)` trong constructor →
> `CS0103: The name 'Raise' does not exist in the current context`.
> `Raise` chỉ tồn tại trên `AggregateRoot<TId>`.
>
> Về mặt DDD, `MenuItem` **đúng là** nên là aggregate root riêng: nó có vòng đời độc lập và
> thay đổi rất thường xuyên (bật/tắt món khi hết hàng, đổi giá). Nếu gộp vào aggregate
> `Restaurant`, mỗi lần đổi trạng thái món sẽ kéo theo việc nạp và kiểm soát toàn bộ menu.
>
> `MenuCategory` thì giữ nguyên `Entity<Guid>` — nhóm món không tồn tại ý nghĩa nếu nhà hàng bị xoá.

### `src\FoodHub.Domain\Restaurants\MenuItemAddedEvent.cs`
```csharp
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

public sealed record MenuItemAddedEvent(Guid MenuItemId, Guid RestaurantId, string Name) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
```

> **Đây là nơi chuẩn bị cho Outbox pattern** (roadmap mục 3, Giai đoạn 2).
> Hiện tại event được raise lên nhưng chưa ai xử lý. Ở Giai đoạn 2, bảng `OutboxMessages` sẽ đọc `AggregateRoot.DomainEvents` rồi worker gửi email / SignalR.
> Bấy giờ bạn đã có sẵn chỗ cắm.

## 10.3 Application — CQRS cho 4 use case

```powershell
$root = "src\FoodHub.Application\Restaurants\MenuItems"
New-Item -ItemType Directory -Force "$root\Commands\CreateMenuItem"
New-Item -ItemType Directory -Force "$root\Commands\UpdateMenuItem"
New-Item -ItemType Directory -Force "$root\Commands\SetMenuItemAvailability"
New-Item -ItemType Directory -Force "$root\Queries\GetMenuByRestaurant"
```

> Quy ước: **1 thư mục = 1 use case**, mỗi thư mục có `Command.cs` + `Handler.cs` + `Validator.cs`.
> Thư mục cấu trúc **chính là tài liệu** — đọc cây thư mục là hiểu hệ thống.

### `Commands\CreateMenuItem\CreateMenuItemCommand.cs`
```csharp
using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;

public sealed record CreateMenuItemCommand(
    Guid RestaurantId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl) : IRequest<CreateMenuItemResult>;

public sealed record CreateMenuItemResult(Guid MenuItemId);
```

### `Commands\CreateMenuItem\CreateMenuItemValidator.cs`
```csharp
using FluentValidation;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;

public sealed class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId là bắt buộc.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên món là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên món tối đa 200 ký tự.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả là bắt buộc.")
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá không hợp lý.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("URL ảnh tối đa 500 ký tự.");
    }
}
```

### `Commands\CreateMenuItem\CreateMenuItemHandler.cs`
```csharp
using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;

// internal: chi tiết trong file, không ai gọi trực tiếp, chỉ MediatR dùng
internal sealed class CreateMenuItemHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateMenuItemCommand, CreateMenuItemResult>
{
    public async Task<CreateMenuItemResult> Handle(
        CreateMenuItemCommand request,
        CancellationToken cancellationToken)
    {
        var restaurantExists = await dbContext.Restaurants
            .AnyAsync(r => r.Id == request.RestaurantId, cancellationToken);

        if (!restaurantExists)
            throw new NotFoundException($"nhà hàng {request.RestaurantId}.");

        var categoryBelongsToRestaurant = await dbContext.MenuCategories
            .AnyAsync(c => c.Id == request.CategoryId && c.RestaurantId == request.RestaurantId, cancellationToken);

        if (!categoryBelongsToRestaurant)
            throw new NotFoundException($"nhóm món {request.CategoryId} của nhà hàng {request.RestaurantId}.");

        var menuItem = new MenuItem(
            request.RestaurantId,
            request.CategoryId,
            request.Name,
            request.Description ?? string.Empty,
            request.Price,
            request.ImageUrl);

        dbContext.MenuItems.Add(menuItem);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateMenuItemResult(menuItem.Id);
    }
}
```

> Handler `internal` là an toàn: DI của .NET dùng reflection nên vẫn tạo được, nhưng code ngoài không thể `new` trực tiếp. Đây là cách giữ API public nhỏ gọn.

### `Commands\UpdateMenuItem\UpdateMenuItemCommand.cs`
```csharp
using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;

public sealed record UpdateMenuItemCommand(
    Guid MenuItemId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl) : IRequest<UpdateMenuItemResult>;

public sealed record UpdateMenuItemResult(Guid MenuItemId);
```

### `Commands\UpdateMenuItem\UpdateMenuItemValidator.cs`
```csharp
using FluentValidation;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;

public sealed class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId là bắt buộc.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên món là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên món tối đa 200 ký tự.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả là bắt buộc.")
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá không hợp lệ.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("URL ảnh tối đa 500 ký tự.");
    }
}
```

### `Commands\UpdateMenuItem\UpdateMenuItemHandler.cs`
```csharp
using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;

internal sealed class UpdateMenuItemHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateMenuItemCommand, UpdateMenuItemResult>
{
    public async Task<UpdateMenuItemResult> Handle(
        UpdateMenuItemCommand request,
        CancellationToken cancellationToken)
    {
        var menuItem = await dbContext.MenuItems
            .FirstOrDefaultAsync(i => i.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
            throw new NotFoundException($"món ăn {request.MenuItemId}.");

        menuItem.UpdateDetails(
            request.Name,
            request.Description ?? string.Empty,
            request.Price,
            request.ImageUrl);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateMenuItemResult(menuItem.Id);
    }
}
```

### `Commands\SetMenuItemAvailability\SetMenuItemAvailabilityCommand.cs`
```csharp
using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;

public sealed record SetMenuItemAvailabilityCommand(
    Guid MenuItemId,
    bool IsAvailable) : IRequest<Unit>;

public sealed record Unit;
```

> Tái sử dụng `System.Unit` cho các command **không trả dữ liệu về**. Tránh phải bịa `EmptyResult` riêng.

### `Commands\SetMenuItemAvailability\SetMenuItemAvailabilityValidator.cs`
```csharp
using FluentValidation;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;

public sealed class SetMenuItemAvailabilityValidator : AbstractValidator<SetMenuItemAvailabilityCommand>
{
    public SetMenuItemAvailabilityValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId là bắt buộc.");
    }
}
```

### `Commands\SetMenuItemAvailability\SetMenuItemAvailabilityHandler.cs`
```csharp
using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;

internal sealed class SetMenuItemAvailabilityHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<SetMenuItemAvailabilityCommand, Unit>
{
    public async Task<Unit> Handle(
        SetMenuItemAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var menuItem = await dbContext.MenuItems
            .FirstOrDefaultAsync(i => i.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
            throw new NotFoundException($"món ăn {request.MenuItemId}.");

        menuItem.SetAvailability(request.IsAvailable);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Unit();
    }
}
```

### `Queries\GetMenuByRestaurant\GetMenuByRestaurantQuery.cs`
```csharp
using MediatR;

namespace FoodHub.Application.Restaurants.MenuItems.Queries.GetMenuByRestaurant;

public sealed record GetMenuByRestaurantQuery(Guid RestaurantId)
    : IRequest<GetMenuByRestaurantResult>;

public sealed record GetMenuByRestaurantResult(
    Guid RestaurantId,
    string RestaurantName,
    IReadOnlyList<MenuCategoryDto> Categories);

public sealed record MenuCategoryDto(
    Guid Id,
    string Name,
    IReadOnlyList<MenuItemDto> Items);

public sealed record MenuItemDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsAvailable,
    string? ImageUrl);
```

### `Queries\GetMenuByRestaurant\GetMenuByRestaurantHandler.cs`
```csharp
using FoodHub.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Queries.GetMenuByRestaurant;

internal sealed class GetMenuByRestaurantHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMenuByRestaurantQuery, GetMenuByRestaurantResult>
{
    public async Task<GetMenuByRestaurantResult> Handle(
        GetMenuByRestaurantQuery query,
        CancellationToken cancellationToken)
    {
        var restaurant = await dbContext.Restaurants
            .AsNoTracking()
            .Where(r => r.Id == query.RestaurantId)
            .Select(r => new { r.Id, r.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurant is null)
            throw new NotFoundException($"nhà hàng {query.RestaurantId}.");

        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .Where(c => c.RestaurantId == query.RestaurantId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(i => i.RestaurantId == query.RestaurantId)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.CategoryId,
                i.Name,
                i.Description,
                i.Price,
                i.IsAvailable,
                i.ImageUrl
            })
            .ToListAsync(cancellationToken);

        var result = categories
            .Select(category => new MenuCategoryDto(
                category.Id,
                category.Name,
                items
                    .Where(i => i.CategoryId == category.Id)
                    .Select(i => new MenuItemDto(
                        i.Id, i.Name, i.Description, i.Price, i.IsAvailable, i.ImageUrl))
                    .ToList()))
            .ToList();

        return new GetMenuByRestaurantResult(restaurant.Id, restaurant.Name, result);
    }
}
```

> **Điểm quan trọng của Query:** `.AsNoTracking()` + `.Select(...)` chỉ lấy đúng cột cần, **không nạp cả entity lên rồi map tay**.
> Query đi vòng qua Aggregate — đây là ranh giới CQRS: write vào qua Domain, đọc ra thẳng từ DB.
> Cùng một endpoint trả về 50 món mà chỉ có 2 truy vấn, không phải N+1.

## 10.4 Api — Controller

```powershell
New-Item -ItemType Directory -Force src\FoodHub.Api\Controllers\MenuItems
```

### `src\FoodHub.Api\Controllers\MenuItems\MenuItemsController.cs`
```csharp
using FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;
using FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Restaurants.MenuItems.Queries.GetMenuByRestaurant;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers.MenuItems;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/menu")]
public sealed class MenuItemsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetMenuByRestaurantResult>> Get(
        Guid restaurantId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMenuByRestaurantQuery(restaurantId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CreateMenuItemResult>> Create(
        Guid restaurantId,
        CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateMenuItemCommand(
            restaurantId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.ImageUrl);

        var result = await sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { restaurantId }, result);
    }

    [HttpPut("items/{menuItemId:guid}")]
    public async Task<ActionResult<UpdateMenuItemResult>> Update(
        Guid restaurantId,
        Guid menuItemId,
        UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMenuItemCommand(
            menuItemId,
            request.Name,
            request.Description,
            request.Price,
            request.ImageUrl);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("items/{menuItemId:guid}/availability")]
    public async Task<IActionResult> SetAvailability(
        Guid restaurantId,
        Guid menuItemId,
        SetMenuItemAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetMenuItemAvailabilityCommand(menuItemId, request.IsAvailable);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}

public sealed record CreateMenuItemRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl);

public sealed record UpdateMenuItemRequest(
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl);

public sealed record SetMenuItemAvailabilityRequest(bool IsAvailable);
```

> `restaurantId` lấy từ **route**, `CategoryId`/`MenuItemId` lấy từ **body**. Controller ghép lại thành Command.
> Đừng bind thẳng Command từ body — client sẽ tự ý gửi `RestaurantId` khác và ghi đè route.
>
> `PATCH` dùng cho việc bật/tắt món: thay đổi nhỏ, không cần gửi cả object.

## 10.5 Infrastructure — EF Core Configuration

```powershell
New-Item -ItemType Directory -Force src\FoodHub.Infrastructure\Persistence\Configurations
```

### `RestaurantConfiguration.cs`
```csharp
using FoodHub.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("restaurants");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(r => r.AddressText).HasColumnName("address_text").HasMaxLength(500).IsRequired();
        builder.Property(r => r.Latitude).HasColumnName("latitude").HasColumnType("numeric(9,6)");
        builder.Property(r => r.Longitude).HasColumnName("longitude").HasColumnType("numeric(9,6)");
        builder.Property(r => r.IsApproved).HasColumnName("is_approved").HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.Name);
    }
}
```

### `MenuCategoryConfiguration.cs`
```csharp
using FoodHub.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

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

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.RestaurantId, c.Name }).IsUnique();
    }
}
```

### `MenuItemConfiguration.cs`
```csharp
using FoodHub.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

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
        builder.Property(i => i.Price).HasColumnName("price").HasColumnType("numeric(18,2)");
        builder.Property(i => i.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
        builder.Property(i => i.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(i => i.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.RestaurantId, i.CategoryId });
    }
}
```

> **Tiền dùng `numeric(18,2)`** chứ không phải `float`/`double`. Kiểu double làm lệch tiền 0.01 — đủ để mỗi đơn hỏng 1đ x 10.000 đơn là mất tiền.
> `CreatedAt` / `UpdatedAt` chưa tự động sinh: sẽ thêm `SaveChangesInterceptor` ở Giai đoạn 1 nốt, hoặc để nguyên.

## 10.6 Build lại

```powershell
cd D:\WorkSpace\project\FoodHub
dotnet build
```

Kỳ vọng: `0 Warning(s) 0 Error(s)`.

Lỗi thường gặp:

| Lỗi | Nguyên nhân |
|---|---|
| `CS0246: The type or namespace name 'ISender' could not be found` | Thiếu `using MediatR;` trong Controller |
| `CS0122: ... is inaccessible due to its protection level` | Handler `internal` nhưng bạn gọi nó từ chỗ khác — chỉ MediatR được gọi |
| `NU1008: Projects that use central package version management should not define the version` | Còn sót `Version="..."` trong `.csproj` |
| `CS8618: Non-nullable property must contain a non-null value` | Dùng `= string.Empty;` cho property chuỗi trong entity |

## 10.7 Cấu hình Postgres (bạn tự làm)

> **Bắt buộc trước khi chạy `dotnet ef`: startup project phải có `Microsoft.EntityFrameworkCore.Design`.**
>
> Nhiều người tưởng chỉ cần package này ở project chứa `DbContext` (`-p`), nhưng **không đủ**.
> `dotnet ef` phải **load `FoodHub.Api.dll`** vào bộ nhớ để dựng host rồi gọi design-time service
> lấy `DbContext`. Package `Design` nằm trong Infrastructure mà có `PrivateAssets="all"`
> thì **không truyền (flow) sang** `FoodHub.Api` → tool dừng với lỗi:
>
> ```text
> Your startup project 'FoodHub.Api' doesn't reference Microsoft.EntityFrameworkCore.Design.
> ```
>
> Thêm vào `src\FoodHub.Api\FoodHub.Api.csproj` (đã có sẵn trong repo):
>
> ```xml
> <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
>   <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
>   <PrivateAssets>all</PrivateAssets>
> </PackageReference>
> ```
>
> `PrivateAssets="all"` ở đây là đúng — package này chỉ dùng lúc build, **không** được đóng gói
> lên production. Không khai báo `Version` vì CPM đã lo.
>
> **Điểm hay:** `dotnet ef migrations add` **không cần database sống**. Nó chỉ dựng model rồi
> sinh file code. Nên bạn có thể tạo migration ngay cả khi Postgres chưa bật (nhờ
> `IDesignTimeDbContextFactory` cung cấp chuỗi placeholder). Chỉ `dotnet ef database update`
> mới thật sự kết nối.

Sau khi Postgres của bạn chạy được, làm 3 việc:

**a)** Điền `ConnectionStrings:Default` trong `src\FoodHub.Api\appsettings.json` (và `appsettings.Development.json` nếu muốn tách môi trường).

**b)** Sửa chuỗi kết nối trong `src\FoodHub.Infrastructure\Persistence\IDesignTimeDbContextFactory.cs`.

**c)** Tạo migration và áp dụng:
```powershell
dotnet ef migrations add InitialCreate -p src\FoodHub.Infrastructure -s src\FoodHub.Api
dotnet ef database update -p src\FoodHub.Infrastructure -s src\FoodHub.Api
```

Sau đó `/health/ready` sẽ tự động bắt đầu kiểm tra DB (vì `connectionString` không còn rỗng ở Bước 8).

### Kiểm tra thử API

```powershell
$base = "https://localhost:7xxx"
$rest = (New-Guid).Guid
```

Tạo nhà hàng + nhóm món + món ăn, rồi đọc menu:
```powershell
# (nếu đã seed sẵn nhà hàng) — xem danh sách nhà hàng
Invoke-RestMethod "$base/api/restaurants"

# đọc menu
Invoke-RestMethod "$base/api/restaurants/$rest/menu"

# thêm món
Invoke-RestMethod "$base/api/restaurants/$rest/menu/items" -Method Post -ContentType "application/json" -Body (@{
  categoryId = "<guid-nhom-mon>"
  name = "Phở Bò"
  description = "Phở bò truyền thống, thịt bò nhiều"
  price = 35000
  imageUrl = "https://example.com/pho-bo.jpg"
} | ConvertTo-Json)

# tắt món hết hàng
Invoke-RestMethod "$base/api/restaurants/$rest/menu/items/<guid-mon>/availability" -Method Patch -ContentType "application/json" -Body '{"isAvailable":false}'
```

Kiểm tra validation: gửi `price = 0` → phải trả **400** kèm danh sách lỗi tiếng Việt.
Kiểm tra not found: dùng `restaurantId` không tồn tại → phải trả **404** `application/problem+json`.

## 10.8 Commit

```powershell
git add .
git commit -m "feat: vertical slice menu - domain events, CQRS commands/queries, EF configuration"
```

---

# Checklist trước khi kết thúc Giai đoạn 1

- [ ] `dotnet build` → 0 warning, 0 error
- [ ] `dotnet list FoodHub.Domain package` → rỗng (Domain sạch)
- [ ] `/health/live` trả `Healthy`
- [ ] `/health/ready` trả `Healthy` (khi đã cấu hình Postgres)
- [ ] `POST` món ăn không hợp lệ → 400 kèm message
- [ ] `GET` menu nhà hàng không tồn tại → 404 problem+json
- [ ] `PATCH availability` bật/tắt món hoạt động
- [ ] `/openapi/v1.json` liệt kê đủ 4 endpoint

# Lỗi dễ mắc — nhớ từ roadmap mục 9

| Sai lầm | Hậu quả | Cách phòng |
|---|---|---|
| Thêm `using Microsoft.EntityFrameworkCore` vào Domain | Phá Clean Architecture | Test kiến trúc ở tuần 5 bắt lỗi này |
| Gửi SignalR/email ngay trong transaction | Rollback nhưng thông báo đã đi | Đã chuẩn bị `IDomainEvent` + sẽ làm Outbox ở Giai đoạn 2 |
| Query nạp cả entity rồi map tay | Chậm, dễ N+1 | `.AsNoTracking()` + `.Select()` projection |
| Dùng `double` cho tiền | Lệch 0.01 mỗi đơn | `numeric(18,2)` |
| Bind thẳng Command từ body | Client ghi đè `RestaurantId` từ route | Tách `Request` record ở Api |
| Quên `CancellationToken` | Hủy request không dừng query | Truyền xuyên suốt handler |

# Giai đoạn tiếp theo

Roadmap **Giai đoạn 2 (tuần 3–4)**: Identity + JWT, giỏ hàng, `PlaceOrder`, state machine đơn hàng, Outbox pattern.
Bạn đã có sẵn nền: CQRS pipeline, DI, `Result<T>`, `IDomainEvent`, health check, EF configuration.
