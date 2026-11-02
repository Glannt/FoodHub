# Kiến thức FoodHub — bản chốt

> Tài liệu **tra cứu nhanh**: mỗi mục là một khái niệm đã được kiểm chứng bằng code thật trong
> repo này (đường dẫn file ghi kèm để bạn tự mở ra đọc).
>
> Tài liệu này **không** giải thích chi tiết — phần đó ở `docs/ARCHITECTURE.md` và
> `docs/SEQ-DIAGRAMS.md`. Ở đây chỉ chốt lại **đúng / sai**, tránh ghi nhớ nhầm.

---

## 0. Bốn tài liệu trong dự án

| File | Dùng để làm gì |
|---|---|
| `Roadmap.md` | Nguồn yêu cầu: 14 tuần / 9 giai đoạn |
| `TODO.md` | Tiến độ hiện tại + 10 quyết định kỹ thuật đã chốt |
| `SETUP-BUOC-4-10.md` | Hướng dẫn dựng dự án từ số 0 (đã sửa 4 lỗi compile) |
| `docs/ARCHITECTURE.md` | Tài liệu kiến trúc chính — **đọc trước khi giải thích bất cứ thứ gì** |
| `docs/SEQ-DIAGRAMS.md` | Giải thích sâu `IUnitOfWork`, `Raise`, đường ghi vs đường đọc |
| `docs/KIEN-THUC.md` | File này — chốt khái niệm đúng/sai |

---

## 1. Solution và Project

| Khẳng định | Đúng? |
|---|---|
| "Dù DDD hay layer architecture đều cần tạo solution trước rồi tạo project bên trong" | ✅ Đúng |
| *(ngầm hiểu: cần solution **vì** DDD)* | ❌ **Sai** — lý do là **multi-project**, không liên quan gì đến DDD |

```text
1 project đơn lẻ       → KHÔNG cần solution. dotnet run trong thư mục là chạy.
Nhiều project          → CẦN solution để gom 1 chỗ: build / test / chạy.

DDD · Layered · Vertical Slice · Feature-based
        └─ đều là CÁCH CHIA PROJECT, độc lập hoàn toàn với việc có solution hay không
```

- Thứ tự tạo **không quan trọng**: tạo solution trước rồi add project, hay tạo project trước
  rồi add sau — đều được.
- Dự án này dùng **`.slnx`** (định dạng XML mới của .NET 10) thay cho `.sln`.

---

## 2. `Program.cs` — hai giai đoạn, không phải `index.js`

**Analog `index.js` của Node là KHÔNG chính xác.** Express chỉ có một kiểu `app.use()`.
.NET tách làm **hai giai đoạn rõ rệt**:

```csharp
builder.Services.AddXxx()   // ĐĂNG KÝ vào DI container — chỉ ghi danh sách, chưa chạy gì
app.UseXxx()               // THÊM middleware vào pipeline — chạy mỗi request
app.MapXxx()               // ÁN XẠ endpoint — URL → code
```

### 3 chỗ hay nhầm

| # | Hay hiểu sai | Thực tế |
|---|---|---|
| ❌ | "URL khai báo trong `Program.cs` qua builder" | URL **không** nằm ở `Program.cs`. Nó nằm ở `src/FoodHub.Api/Properties/launchSettings.json` (`applicationUrl`) khi `dotnet run`, và `ASPNETCORE_URLS` khi deploy. Hai cổng `7170` + `5282` khi chạy là do file đó quyết định |
| ❌ | "Controller khai báo trong `Program.cs`" | **Controller KHÔNG khai báo ở `Program.cs`.** `AddControllers()` chỉ bật cơ chế, framework **quét assembly** tự tìm class kế thừa `ControllerBase`. Route viết bằng **attribute** trong chính controller: `[Route("api/…")]`, `[HttpPost]` |
| ⚠️ | "SignalR khai báo qua builder" | Đúng một nửa: `builder.Services.AddSignalR()` ✅. Nhưng endpoint phải map thêm `app.MapHub<MyHub>("/hubs/…")` ✅. Đây là **route kiểu khác**, không phải controller route |

### Extension method của tầng dưới

`AddApplication()` và `AddInfrastructure()` là **extension method do chính tầng dưới viết** —
y hệt `AddControllers()`. Tầng Api gọi **1 dòng**, không cần biết chi tiết bên trong.

```csharp
// FoodHub.Api/Program.cs — tầng Api KHÔNG cần biết Postgres / MediatR / FluentValidation
builder.Services.AddApplication();                          // tầng Application tự lo
builder.Services.AddInfrastructure(builder.Configuration);  // tầng Infrastructure tự lo
```

Đó là lý do mỗi tầng đều có file `DependencyInjection.cs`.

### `launchSettings.json` chỉ có tác dụng khi `dotnet run`

```text
dotnet run                                        → đọc launchSettings.json → 7170 + 5282
dotnet FoodHub.Api.dll                            → KHÔNG đọc → dùng 5000 mặc định
dotnet FoodHub.Api.dll --urls "http://x:8080"     → 8080
ASP.NETCORE_URLS=http://x:8080 dotnet FoodHub.Api.dll → 8080
```

Khi deploy thật (Giai đoạn 8), cổng lấy từ **`ASPNETCORE_URLS`** — không phải từ file này.

---

## 3. Năm tầng kiến trúc

⚠️ Dễ quên nhất: có **5** project, không phải 4. Thiếu `SharedKernel`.

### Ba lỗi cần tránh trong cách mô tả

| # | Hiểu sai | Đúng |
|---|---|---|
| ❌ | "Domain **lưu** các entity" | Domain **định nghĩa** (chứa) entity. Domain **không lưu gì cả** — không có một dòng code nào ghi vào database. Lưu trữ là việc của Infrastructure |
| ❌ | "Application **lưu** các DTO" | Application chứa **use case**. DTO là **đầu vào / đầu ra** của use case, không phải thứ Application "giữ" |
| ❌ | "Api làm controller **implement** middleware" | Controller trả về `ActionResult<T>` — làm việc với HTTP. Api **đăng ký** middleware trong `Program.cs`, và viết class `IExceptionHandler` trong thư mục `Middleware/` |

> **Middleware ≠ Exception handler.** Middleware = nhân viên bảo vệ (lọc request).
> Exception handler = nhân viên y tế (xử lý khi có việc xảy ra).

### Bảng chốt

| Tầng | Chứa gì | File thật trong repo | **KHÔNG** được chứa |
|---|---|---|---|
| **SharedKernel**<br>`src/BuildingBlocks/FoodHub.SharedKernel` | Khái niệm dùng chung nhiều bounded context | `Entity.cs`, `AggregateRoot.cs`, `IDomainEvent.cs`, `Result.cs` | — |
| **Domain**<br>`src/FoodHub.Domain` | Entity + luật nghiệp vụ + **interface** | `Restaurant.cs`, `MenuItem.cs`, `MenuCategory.cs`, `MenuItemAddedEvent.cs`, `Persistence/IUnitOfWork.cs` | EF Core, MediatR, ASP.NET |
| **Application**<br>`src/FoodHub.Application` | **Use case** (CQRS) + abstraction | `Commands/`, `Queries/`, `Abstractions/IApplicationDbContext.cs`, `Behaviors/` | Postgres, Redis, Npgsql |
| **Infrastructure**<br>`src/FoodHub.Infrastructure` | **Code thực thi** abstraction | `Persistence/AppDbContext.cs`, `Persistence/Configurations/`, `Migrations/`, `Persistence/AppDbContextUnitOfWork.cs` | Business logic |
| **Api**<br>`src/FoodHub.Api` | Endpoint + middleware + wiring | `Program.cs`, `MenuItemsController.cs`, `Middleware/*ExceptionHandler.cs` | **Business logic** |

**Từ khóa:** tầng trên **định nghĩa** (interface), tầng dưới **hiện thực hoá**.

```text
IUnitOfWork             ở Domain/Persistence          ← định nghĩa
AppDbContextUnitOfWork  ở Infrastructure/Persistence   ← hiện thực

→ Vì thế Domain không cần biết EF Core tồn tại.
```

### Cấu trúc 1 use case

```text
Application/Restaurants/MenuItems/Commands/CreateMenuItem/
    ├── CreateMenuItemCommand.cs    ← DTO vào  (record + validation attributes)
    ├── CreateMenuItemHandler.cs    ← logic dùng IApplicationDbContext + IUnitOfWork
    └── CreateMenuItemValidator.cs  ← FluentValidation, chạy qua ValidationBehavior
```

---

## 4. Quy tắc phụ thuộc hướng vào trong

```text
                 ┌──────────┐
                 │    Api   │  biết tất cả
                 └────┬─────┘
          ┌───────────┴───────────┐
   ┌──────▼───────┐        ┌──────▼───────┐
   │Infrastructure│───────►│ Application  │  Infrastructure hiện thực
   └──────┬───────┘        └──────┬───────┘  interface của Application
          │                       │
          └───────────┬───────────┘
               ┌──────▼───────┐
               │    Domain    │  không biết ai cả
               └──────────────┘

 Mũi tên chỉ đi vào trong. Không bao giờ đi ra.
```

**Lợi ích:** đổi Postgres sang SQL Server **chỉ sửa Infrastructure** — Domain/Application
không đụng đến dòng nào.

---

## 5. Cặp lệnh `dotnet ef`

Phân biệt rõ hai giai đoạn:

| | `migrations add <Tên>` | `database update` |
|---|---|---|
| Cần DB sống? | ❌ **không** | ✅ **có** |
| Đọc gì? | code C# của bạn | file `.cs` trong `Migrations\` |
| So với cái gì? | `AppDbContextModelSnapshot.cs` | bảng `__EFMigrationsHistory` |
| Viết ra gì? | **file code `.cs`** | **SQL chạy trên DB** |
| Khi nào chạy? | lúc code, lúc model đổi | lúc triển khai, lúc đổi môi trường |

```text
Bạn sửa entity
      ↓  dotnet ef migrations add InitialCreate   → sinh 2026xxxx_InitialCreate.cs
Bạn có DB sống
      ↓  dotnet ef database update                → chạy SQL, tạo __EFMigrationsHistory
App đọc/ghi được dữ liệu
```

**Ý nghĩa từng cờ:**

- `-p src\FoodHub.Infrastructure` (`--project`) — project **chứa class `AppDbContext`** và là
  **nơi ghi file migration ra**. Không phải "init vào assembly khi compile".
- `-s src\FoodHub.Api` (`--startup-project`) — project **EF sẽ chạy `Program.cs`** để lấy
  `AppDbContext` từ DI container. Nhờ vậy nó đọc được `appsettings.json` + User Secrets.
  Không phải "đọc `.csproj`".

> `-s` chính là lý do `FoodHub.Api` phải có package `Microsoft.EntityFrameworkCore.Design`.
> Package này cung cấp cơ chế `HostFactoryResolver` để EF dựng được host từ minimal hosting.

**Ba file** trong `src/FoodHub.Infrastructure/Migrations/`:

| File | Vai trò |
|---|---|
| `2026xxxx_InitialCreate.cs` | Code SQL thật — `Up()` tạo bảng, `Down()` xoá bảng |
| `2026xxxx_InitialCreate.Designer.cs` | Ảnh chụp model **tại thời điểm migration này** |
| `AppDbContextModelSnapshot.cs` | Ảnh chụp model **hiện tại** — lần `migrations add` sau sẽ so với file này |

`2026xxxx` là timestamp lúc sinh file, dùng để **sắp xếp đúng thứ tự** khi áp dụng.

> Chạy `database update` lần thứ 2 sẽ **không làm gì** — nhờ bảng `__EFMigrationsHistory`.
> Đây là lý do migration an toàn khi chạy lại, quan trọng cho CI/CD ở Giai đoạn 8.

---

## 6. Secret — chỉ một nguồn sự thật

### Ba chỗ EF có thể lấy `AppDbContext`, thử theo thứ tự

```text
① IDesignTimeDbContextFactory<T> trong target project (-p)
      ↓ không có
② Dựng host từ STARTUP project (-s), lấy AppDbContext trong DI   ← ĐANG DÙNG
      ↓ không có
③ Tìm AppDbContext có OnConfiguring() tự dựng được
```

Ta **cố ý bỏ cách ①**. Lý do: cách ① buộc phải **hardcode chuỗi kết nối vào source code**
(vì nó không chạy app, không có DI, không đọc được config) → mật khẩu nằm trong git.

Dùng cách ② thì **1 nguồn sự thật duy nhất**: `AddInfrastructure(builder.Configuration)`
đọc `ConnectionStrings:Default` từ `appsettings.json` + User Secrets.

### Cách làm

```powershell
dotnet user-secrets init -p src\FoodHub.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>;Port=5432;Database=FoodHub;Username=<user>;Password=<pass>" -p src\FoodHub.Api
```

Trong `appsettings.json` chỉ để node **rỗng**, để file này an toàn khi commit:

```json
"ConnectionStrings": { "Default": "" }
```

| | User Secrets ✅ | `appsettings.Local.json` |
|---|---|---|
| Nạp tự động ở Development | ✅ có sẵn | ❌ phải viết `AddJsonFile` trong `Program.cs` |
| File nằm trong repo | ❌ **ngoài repo** | ⚠️ trong repo (phải gitignore) |
| Ghi đè được bởi biến môi trường | ✅ có sẵn | ❌ không |
| Cần sửa code | ❌ không | ✅ có |

> Hai cách này **loại trừ nhau**, không phải hai lớp dự phòng.
> Dòng `appsettings.Local.json` trong `.gitignore` nên giữ lại làm tấm chắn.

### Ba điều thực tế

1. **Secret chỉ được load ở môi trường `Development`.** `launchSettings.json` đã đặt sẵn
   `ASPNETCORE_ENVIRONMENT=Development` cho cả hai profile → `dotnet run` và `dotnet ef` đều
   đọc được.
2. **Kiểm chứng không cần DB sống:**
   ```powershell
   dotnet ef dbcontext info -p src\FoodHub.Infrastructure -s src\FoodHub.Api
   ```
   Phải in ra `Database name: FoodHub` — nếu rỗng thì User Secrets **không** được load.
3. **`--connection` thắng `appsettings.json`:**
   ```powershell
   dotnet ef database update -p src\FoodHub.Infrastructure -s src\FoodHub.Api `
     --connection "Host=<host>;Database=FoodHub;Username=<user>;Password=<pass>"
   ```

---

## 7. Những bẫy đã gặp thật trong dự án này

Mỗi mục đều là lỗi **đã xảy ra và đã sửa**, không phải giả định.

| # | Triệu chứng | Nguyên nhân thật |
|---|---|---|
| 1 | `Add()` xong `SaveChangesAsync()` **INSERT 0 dòng**, **không lỗi, không cảnh báo** | `IApplicationDbContext` và `IUnitOfWork` trỏ tới **2 `AppDbContext` khác nhau** → 2 `ChangeTracker` khác nhau. Sửa: bọc `GetRequiredService<AppDbContext>()` |
| 2 | *Your startup project 'FoodHub.Api' doesn't reference Microsoft.EntityFrameworkCore.Design* | Package `Design` nằm ở Infrastructure với `PrivateAssets="all"` → **không truyền sang** `FoodHub.Api`. Mà `dotnet ef` phải **load `FoodHub.Api.dll`** để dựng host |
| 3 | `CS1061` khi gọi `UseSnakeCaseNamingConvention()` | Package `EFCore.NamingConventions` không có trong CPM. Sửa: dùng `HasColumnName(...)` tường minh trong từng `Configuration` |
| 4 | `CS0266` khi `IUnitOfWork` trả thẳng `AppDbContext` | `AppDbContext` không implement nó. Sửa: lớp bọc `AppDbContextUnitOfWork` |
| 5 | `CS0117` khi gán `ProblemDetails.Errors` | Phải dùng `HttpValidationProblemDetails`. Dùng `ValidationProblemDetails` sẽ **mất sạch** `errors` khi serialize |
| 6 | `CS0103` khi `MenuItem : Entity<Guid>` gọi `Raise()` | Chỉ `AggregateRoot<TId>` mới có `Raise()`. Sửa: đổi base class |
| 7 | `NU1008` | Còn sót `Version="…"` trong `.csproj` khi đã bật CPM |
| 8 | Trùng `PackageVersion` | Khai báo trùng trong `Directory.Packages.props` |
| 9 | `CS0122` handler không truy cập được | Handler `internal sealed` mà bị gọi từ nơi khác — chỉ MediatR được gọi |

### Hai mối bẫy đặc biệt của dự án này

**a) `Directory.Build.props` bật `TreatWarningsAsErrors=true` + `Nullable=enable`** →
**mọi warning là lỗi build.** Đừng tưởng build được là chỉ cần xem `Error(s)`.

**b) Thứ tự đăng ký Behavior quyết định hành vi:**

```text
ValidationBehavior đăng ký TRƯỚC LoggingBehavior
   → Validation bọc ngoài cùng: vào trước, ra sau
   → Hệ quả: request lỗi 400 KHÔNG có log thời gian xử lý
```

Đã xác nhận bằng stack trace thật. Chi tiết: `Application/DependencyInjection.cs`.

---

## 8. Thuật ngữ tra nhanh

| Thuật ngữ | Nghĩa |
|---|---|
| **Assembly** | File `.dll` mà `dotnet build` sinh ra từ project |
| **Project** | Đơn vị build (`*.csproj`) → sinh 1 assembly |
| **Solution** (`*.slnx`) | Container chứa nhiều project, gom 1 chỗ build/test |
| **CPM** (Central Package Management) | Khai báo version package 1 lần ở `Directory.Packages.props`; mọi `.csproj` **không** được ghi `Version` |
| **`PrivateAssets="all"`** | Package **không** được truyền (flow) sang project khác |
| **DI Container** | Nơi lưu các service đã đăng ký, cấp instance theo **scope** |
| **Scoped** | 1 instance cho mỗi HTTP request (mặc định) |
| **Aggregate Root** | Entity gốc của một aggregate — mọi thay đổi phải đi qua nó để phát `DomainEvent` |
| **Domain Event** | Sự kiện trong nghiệp vụ đã xảy ra (`MenuItemAddedEvent`) |
| **`Result<T>`** | Kiểu kết quả thay vì ném exception cho lỗi nghiệp vụ dự kiến |
| **`Raise()`** | Đẩy `DomainEvent` vào danh sách chờ, **không** ghi DB ngay |
| **Unit of Work** | Gom nhiều thay đổi thành **một** lần `SaveChangesAsync()` trong **một** transaction |
| **Migration** | File `.cs` mô tả thay đổi schema, sinh từ model |
| **`__EFMigrationsHistory`** | Bảng EF tự tạo, lưu "migration nào đã áp" |
| **Snapshot** | Ảnh chụp model hiện tại để lần `migrations add` sau so sánh |
| **CQRS** | Tách **Command** (ghi) và **Query** (đọc) thành hai nhóm riêng |
| **Vertical Slice** | Mỗi use case 1 thư mục đầy đủ (Command + Handler + Validator) |
| **Behavior** (MediatR) | Code chạy **quanh** mỗi request qua pipeline |
| **OpenAPI** | Đặc tả API (JSON) — ở đây dùng OpenAPI built-in của .NET 10, **không** Swashbuckle |

---

## 9. Mười quyết định kỹ thuật đã chốt

Xem chi tiết ở `TODO.md`. Tóm tắt:

1. **Modular monolith** trước, microservices để cuối (tuần 14+)
2. `.slnx` thay `.sln`; .NET SDK 10.0.401, runtime 10.0.12
3. Template `webapi` .NET 10 dùng **OpenAPI built-in**, **không** Swashbuckle
4. Application **chấp nhận** tham chiếu `Microsoft.EntityFrameworkCore` (Query dùng `DbSet<T>` + `AsNoTracking()`); **Domain sạch tuyệt đối**
5. Handler `internal sealed`; lỗi dùng Exception (`NotFoundException`, `ValidationException`) + `IExceptionHandler` → ProblemDetails
6. `ValidationBehavior` đăng ký **trước** `LoggingBehavior`
7. `/health/live` không chạm DB; `/health/ready` chỉ đăng ký `AddDbContextCheck` khi `ConnectionStrings:Default` khác rỗng
8. Bỏ `UseSnakeCaseNamingConvention()` — dùng `HasColumnName(...)` tường minh
9. `AppDbContext` **không** implement `IUnitOfWork` → bọc bằng `AppDbContextUnitOfWork`
10. Bỏ `IDesignTimeDbContextFactory` → EF lấy `DbContext` từ DI; secret trong **User Secrets**

### Hệ quả của quyết định 7

```text
ConnectionStrings:Default = ""   → AddInfrastructure KHÔNG đăng ký AddDbContextCheck
                                  → /health/ready = Healthy  (chưa hề chạm DB — "Healthy" giả)

Có giá trị (từ User Secrets)     → AddInfrastructure ĐĂNG KÝ AddDbContextCheck
                                  → /health/ready = Unhealthy nếu Postgres tắt
```

`/health/live` luôn `Healthy` — nó không chạm DB.