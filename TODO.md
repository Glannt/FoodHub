# FoodHub — Tiến độ

Cập nhật: **sau commit `24417e2`** · Build sạch `0 Warning / 0 Error`

## Vị trí hiện tại

> **Giai đoạn 1 / Tuần 1–2 — code đã xong, còn 2 việc để đóng giai đoạn.**

Migration đã sinh được. Còn **1 việc chờ bạn dựng Postgres**, và **1 việc là viết code**.

```text
Giai đoạn 1 ██████████████░░░░░░░  70%   ← đây
         ↑ đã xong                    ↑ còn 2 việc

Giai đoạn 2 ░░░░░░░░░░░░░░░░░░░░░   0%
Giai đoạn 3-9 ░░░░░░░░░░░░░░░░░░░░░  0%
```

---

## Giai đoạn 1 — Nền tảng (Tuần 1–2)

Mục tiêu roadmap: *"Solution Clean Architecture, Domain, EF Core, migration, CRUD nhà hàng/menu bằng CQRS"*

### ✅ 1.1 Khung solution

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| 5 project + `FoodHub.slnx` | ✅ | `.slnx` thay `.sln` |
| Chiều tham chiếu | ✅ | `Api → Infrastructure → Application → Domain → SharedKernel` |
| `global.json` (SDK 10.0.401) | ✅ | |
| `Directory.Build.props` | ✅ | `Nullable` + `TreatWarningsAsErrors` |
| `Directory.Packages.props` (CPM) | ✅ | version nằm 1 chỗ duy nhất |
| `.editorconfig` | ✅ | |

### ✅ 1.2 SharedKernel

`IDomainEvent` · `Entity<TId>` · `AggregateRoot<TId>` · `Result<T>` — hết.

### ✅ 1.3 Domain

| Entity / interface | Trạng thái | Ghi chú |
|---|---|---|
| `Restaurant` | ✅ | `Approve()` / `Reject()`, có Guard |
| `MenuCategory` | ✅ | `Entity<Guid>` — con của aggregate Restaurant |
| `MenuItem` | ✅ | `AggregateRoot<Guid>`, `Raise(MenuItemAddedEvent)` |
| `MenuItemAddedEvent` | ✅ | `sealed record` + `OccurredOn` |
| `IUnitOfWork` | ✅ | chỉ có `SaveChangesAsync` |
| `Order` (aggregate) | ❌ | thuộc **Giai đoạn 2** |

### ✅ 1.4 Application — CQRS cho MenuItem

| Use case | Loại | Handler | Validator |
|---|---|---|---|
| `CreateMenuItem` | Command | ✅ | ✅ 6 rule |
| `UpdateMenuItem` | Command | ✅ | ✅ |
| `SetMenuItemAvailability` | Command | ✅ | ✅ |
| `GetMenuByRestaurant` | Query | ✅ 3 truy vấn `AsNoTracking` + projection | không cần |

Hạ tầng: `IApplicationDbContext` · `NotFoundException` · `ValidationBehavior` · `LoggingBehavior` · `AddApplication()`

### ✅ 1.5 Infrastructure — EF Core

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| `AppDbContext` (3 `DbSet`) | ✅ | |
| `RestaurantConfiguration` | ✅ | `HasColumnName` tường minh snake_case |
| `MenuCategoryConfiguration` | ✅ | |
| `MenuItemConfiguration` | ✅ | |
| `AppDbContextUnitOfWork` | ✅ | bọc `AppDbContext`, chung `ChangeTracker` |
| `IDesignTimeDbContextFactory` | ✅ | chuỗi placeholder |
| `AddInfrastructure(configuration)` | ✅ | |
| `Microsoft.EntityFrameworkCore.Design` trong **Api** | ✅ | bắt buộc cho startup project, xem ⑤ |
| **`InitialCreate` migration** | ✅ | **đã sinh code** — 3 bảng, 4 index, 3 FK Cascade |
| Áp migration lên DB (`database update`) | ❌ | **chờ Postgres** |

> **⑤ `Microsoft.EntityFrameworkCore.Design` phải nằm ở `FoodHub.Api`, không chỉ ở Infrastructure.**
> Đã mắc lỗi thật: *"Your startup project 'FoodHub.Api' doesn't reference Microsoft.EntityFrameworkCore.Design"*.
> Nguyên nhân: Infrastructure khai báo package này với `PrivateAssets="all"` → **không truyền sang** `FoodHub.Api`.
> Mà `dotnet ef` phải load `FoodHub.Api.dll` để dựng host rồi gọi design-time service lấy `DbContext`.
> Chi tiết: `SETUP-BUOC-4-10.md` §10.7.

### ✅ 1.6 Api

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| `MenuItemsController` (4 endpoint) | ✅ | |
| `NotFoundExceptionHandler` → 404 | ✅ | đã test thật |
| `ValidationExceptionHandler` → 400 | ✅ | đã test thật, trả đúng `errors` |
| `/health/live` | ✅ | **không** chạm DB |
| `/health/ready` | ✅ | tự bật khi có connection string |
| OpenAPI built-in (`/openapi/v1.json`) | ✅ | không dùng Swashbuckle |

### ⬜ 1.7 Việc còn lại

**A. Áp migration lên Postgres — chờ bạn dựng database**

Migration `InitialCreate` **đã sinh xong** (không cần DB sống). Chỉ còn áp nó:

```powershell
# 1. Điền ConnectionStrings:Default trong src\FoodHub.Api\appsettings.json
# 2. Sửa chuỗi trong src\FoodHub.Infrastructure\Persistence\IDesignTimeDbContextFactory.cs
dotnet ef database update -p src\FoodHub.Infrastructure -s src\FoodHub.Api
```

Sau đó kiểm tra `/health/ready` phải báo `Healthy` (tự bật DB check vì connection string không còn rỗng).

**B. CRUD nhà hàng bằng CQRS — viết code**

Hiện `Restaurant` mới chỉ có entity, **chưa có endpoint nào**. Cần:

| Use case | Loại | Ghi chú |
|---|---|---|
| `CreateRestaurant` | Command | Validator: name, addressText không rỗng; lat/lng trong khoảng |
| `UpdateRestaurant` | Command | load thật → `Approve`/`Reject` tách riêng |
| `ApproveRestaurant` | Command | chỉ Admin — **chưa có auth, để Giai đoạn 2** |
| `GetRestaurants` | Query | `AsNoTracking` + projection, có phân trang |
| `GetRestaurantById` | Query | `NotFoundException` nếu không có |

Cần thêm `RestaurantController` ở `src\FoodHub.Api\Controllers\Restaurants\`.

> **Thứ tự nên làm:** làm **B trước, A sau**. Vì khi Postgres còn chưa có, viết code CQRS vẫn build được;
> còn migration thì không làm được gì cho tới khi bạn dựng xong database.

---

## Tài liệu đã có

| File | Nội dung |
|---|---|
| `docs\ARCHITECTURE.md` | compile-time, assembly, **container vs middleware vs filter vs behavior** |
| `docs\SEQ-DIAGRAMS.md` | `IUnitOfWork`, `Raise`, đường ghi vs đường đọc |
| `docs\diagrams\*.mmd` | 9 diagram — flow trước, sequence sau |
| `docs\sync-mermaid.ps1` | đồng bộ `.mmd` → `.md`, chạy sau mỗi lần sửa diagram |

---

## Giai đoạn 2 — Auth + Đơn hàng (Tuần 3–4)

Chưa bắt đầu. Kiến thức đích: Transaction, concurrency, domain events.

- [ ] ASP.NET Core Identity + `ApplicationUser : IdentityUser<Guid>`
- [ ] JWT + Refresh Token (token trong query string khi dùng SignalR)
- [ ] `/auth/register`, `/auth/login`, `/auth/refresh`
- [ ] Aggregate `Order` + state machine (`PendingPayment → Placed → Accepted → Preparing → ReadyForPickup → PickedUp → Delivered`, nhánh `Cancelled`/`Refunded`)
- [ ] `RowVersion` (optimistic concurrency) — **dễ quên, roadmap đã cảnh báo**
- [ ] Giỏ hàng (`Cart`, `CartItems`, mỗi giỏ thuộc đúng 1 nhà hàng)
- [ ] `PlaceOrder`
- [ ] **`TransactionBehavior`** — behavior thứ 3 trong `AddOpenBehavior`
- [ ] **`CachingBehavior`** — thứ tự: validation → logging → transaction → cache
- [ ] **Outbox pattern** — bảng `OutboxMessages`, ghi event cùng transaction với đơn
- [ ] `RestaurantController` thêm `[Authorize]`

## Giai đoạn 3 — Test + CI (Tuần 5)

Chưa bắt đầu.

- [ ] `tests\FoodHub.Domain.UnitTests`
- [ ] `tests\FoodHub.Application.UnitTests`
- [ ] `tests\FoodHub.Api.IntegrationTests` (Testcontainers: Postgres + Redis thật)
- [ ] `tests\FoodHub.ArchitectureTests` (**NetArchTest** — Domain không được tham chiếu Infrastructure)
- [ ] `.github\workflows\ci.yml`

## Giai đoạn 4–9

| Giai đoạn | Tuần | Nội dung |
|---|---|---|
| 4. Realtime + Cache | 6–7 | SignalR hubs, Redis cache + backplane + GEO, gán shipper |
| 5. Nền + Email + Thanh toán | 8–9 | Hangfire, MailKit, VNPay/Momo sandbox, idempotent IPN |
| 6. Quan sát | 10 | OpenTelemetry, Serilog + Seq, Health Checks |
| 7. Frontend + Mobile | 11–12 | Blazor (khách, nhà hàng, admin), MAUI cho shipper |
| 8. Deploy | 13 | Docker, CD, cloud, HTTPS, domain |
| 9. Microservices (tùy chọn) | 14+ | Tách Payment + Notification, RabbitMQ/MassTransit, YARP |

> **Cố ý bỏ qua:** Docker Compose (bạn tự cấu hình Postgres + Redis), .NET Aspire.
> Hai thứ này quay lại ở Giai đoạn 4 và 8.

---

## Ghi chú kỹ thuật đã chốt trong quá trình làm

| Quyết định | Vì sao |
|---|---|
| Application **chấp nhận** tham chiếu `Microsoft.EntityFrameworkCore` | Query dùng `DbSet<T>` + `AsNoTracking()`. Domain vẫn sạch tuyệt đối. |
| `MenuItem : AggregateRoot<Guid>` | Cần `Raise()`. Về DDD cũng đúng — món ăn có vòng đời độc lập. |
| `IUnitOfWork` **bọc** `AppDbContext` | `AppDbContext` không implement nó → nếu implement thì `Application` phải nhắc tới kiểu của Infrastructure. |
| Bỏ `UseSnakeCaseNamingConvention()` | Package `EFCore.NamingConventions` không có trong CPM. Dùng `HasColumnName` tường minh. |
| `HttpValidationProblemDetails` cho lỗi 400 | `ProblemDetails` không có `Errors`; `ValidationProblemDetails` sẽ **mất sạch** `errors` khi serialize. |
| `ValidationBehavior` đăng ký **trước** `LoggingBehavior` | Đăng ký đầu = bọc ngoài = validate trước. Hệ quả: request lỗi 400 **không có log thời gian xử lý**. Đảo nếu muốn log cả. |
| `internal sealed` cho Handler | MediatR tạo qua reflection, không cần public. |
| OpenAPI built-in, không Swashbuckle | Template .NET 10 đã có sẵn. |
