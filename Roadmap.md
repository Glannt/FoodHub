Project: FoodHub, nền tảng đặt và giao đồ ăn realtime

Đây là một "GrabFood mini" gồm khách hàng, nhà hàng, shipper và admin. Mình chọn nó vì các tính năng như theo dõi đơn realtime, thanh toán, email, cache, tác vụ nền và app mobile cho shipper đều là nhu cầu thật, không phải ghép cho đủ.

1. Bản đồ phủ tiêu chí
Mục cần có	Nằm ở đâu trong FoodHub
Clean Architecture + CQRS + MediatR	Toàn bộ backend, chia Domain/Application/Infrastructure/Api
SignalR	Theo dõi trạng thái đơn, vị trí shipper, thông báo cho nhà hàng, chat khách-shipper
Redis	Cache menu, SignalR backplane, khóa phân tán, lưu vị trí shipper
Hangfire / BackgroundService	Gửi email, tự hủy đơn quá hạn thanh toán, báo cáo cuối ngày
VNPay/Momo	Thanh toán online (sandbox), xử lý IPN callback
Gửi email	Xác nhận đơn, hóa đơn, quên mật khẩu (MailKit + template)
OpenTelemetry, Health Checks, Seq	Tracing, metrics, log tập trung, endpoint /health
CI/CD, Docker, cloud	GitHub Actions, build image, deploy tự động
Blazor hoặc React	Web cho khách hàng và trang quản trị/nhà hàng
.NET MAUI	App cho shipper (nhận đơn, GPS, cập nhật trạng thái)
Microservices	Giai đoạn cuối: tách từ modular monolith

Vì vậy nên bắt đầu bằng modular monolith rồi mới tách service. Tách sớm khi chưa hiểu nghiệp vụ là lỗi rất phổ biến.

2. Công nghệ

Backend

.NET 10 (LTS) hoặc .NET 8, ASP.NET Core Web API
EF Core + PostgreSQL (hoặc SQL Server), Dapper cho vài query báo cáo nặng
MediatR + FluentValidation + Mapster/AutoMapper
ASP.NET Core Identity + JWT + Refresh Token
SignalR (Redis backplane)
Hangfire (storage PostgreSQL/Redis), MailKit, Polly (retry/circuit breaker)
Redis (IDistributedCache, StackExchange.Redis)

Frontend và mobile

Web khách hàng và admin: Blazor Web App (hoặc React + TypeScript, nếu bạn muốn học thêm stack JS)
Shipper: .NET MAUI (CommunityToolkit.Mvvm, Maps, Geolocation)

Quan sát và vận hành

OpenTelemetry (traces, metrics) → Jaeger hoặc Grafana Tempo/Prometheus
Serilog → Seq
HealthChecks (AspNetCore.HealthChecks.NpgSql, .Redis, Hangfire)
Docker, docker-compose, GitHub Actions
Cloud: Azure (App Service/Container Apps + Azure Database for PostgreSQL + Azure Cache for Redis), hoặc rẻ hơn là VPS + Docker + Caddy/Nginx

Test: xUnit, Moq/NSubstitute, FluentAssertions, Testcontainers, WebApplicationFactory, NetArchTest (kiểm tra quy tắc phụ thuộc giữa các lớp)

Lưu ý: MediatR từ bản 13 trở đi có giấy phép thương mại (miễn phí cho cá nhân/công ty nhỏ theo điều kiện của họ). Với dự án học tập thì dùng bình thường, hoặc thay bằng Mediator (source generator) / Wolverine nếu muốn tránh.

3. Yêu cầu chức năng

Khách hàng

Đăng ký/đăng nhập, xác thực email, quên mật khẩu, sổ địa chỉ
Tìm nhà hàng theo tên, món, khoảng cách, đánh giá; xem menu
Giỏ hàng (mỗi giỏ thuộc một nhà hàng), mã giảm giá
Đặt hàng, thanh toán COD / VNPay / Momo
Theo dõi đơn realtime: trạng thái đổi ngay, xem shipper di chuyển trên bản đồ
Chat với shipper, đánh giá nhà hàng và shipper sau khi giao
Lịch sử đơn, đặt lại đơn cũ

Nhà hàng

Quản lý menu, nhóm món, tùy chọn (size, topping), bật/tắt món hết hàng
Nhận đơn mới qua SignalR kèm âm thanh, xác nhận/từ chối, báo "đã nấu xong"
Đặt giờ mở cửa, xem doanh thu

Shipper (MAUI)

Bật/tắt trạng thái nhận đơn, nhận đơn được gán hoặc đơn ở gần
Gửi vị trí GPS định kỳ, cập nhật "đã lấy hàng", "đã giao"
Xem thu nhập theo ngày

Admin

Duyệt nhà hàng và shipper, quản lý người dùng, mã giảm giá
Dashboard: doanh thu, số đơn, tỷ lệ hủy, top nhà hàng
Xem trạng thái hệ thống (health), đối soát thanh toán

Luồng trạng thái đơn
PendingPayment → Placed → Accepted → Preparing → ReadyForPickup → PickedUp → Delivered (nhánh Cancelled, Refunded). Dùng state machine trong Domain để chặn chuyển trạng thái sai.

Nghiệp vụ nâng cao (điểm học được nhiều nhất)

Đơn PendingPayment quá 15 phút không thanh toán thì Hangfire tự hủy
Thanh toán idempotent: VNPay/Momo có thể gọi IPN nhiều lần, chỉ xử lý một lần
Gán shipper: tìm shipper gần nhất bằng Redis GEO (GEOSEARCH), gửi đề nghị, hết 30 giây không nhận thì chuyển người khác
Outbox pattern: ghi sự kiện vào bảng Outbox cùng transaction với đơn hàng, một worker gửi email/SignalR/message sau, tránh mất sự kiện
4. Thiết kế dữ liệu (rút gọn)
Bảng	Cột chính
Users, Roles	Identity (Customer, Restaurant, Shipper, Admin)
Restaurants	Id, OwnerId, Name, Address, Lat, Lng, OpenTime, CloseTime, IsApproved
MenuCategories, MenuItems	Id, RestaurantId, Name, Price, IsAvailable, ImageUrl
Carts, CartItems	UserId, RestaurantId, MenuItemId, Quantity, Options
Orders	Id, CustomerId, RestaurantId, ShipperId, Status, Subtotal, DeliveryFee, Discount, Total, PaymentMethod, PaymentStatus, RowVersion
OrderItems	OrderId, MenuItemId, NameSnapshot, UnitPrice, Quantity
Payments	Id, OrderId, Provider, TxnRef, Amount, Status, RawResponse, PaidAt
ShipperProfiles	UserId, VehiclePlate, IsOnline
Reviews	OrderId, Rating, Comment
Coupons	Code, Type, Value, MinOrder, ExpiresAt, UsageLimit
OutboxMessages	Id, Type, Payload, ProcessedAt
ChatMessages	OrderId, SenderId, Content, SentAt

Vị trí shipper (thay đổi liên tục) không lưu vào SQL từng lần, chỉ đẩy vào Redis, và lưu mẫu thưa vào DB nếu cần lịch sử.

5. Cấu trúc thư mục
FoodHub/
├── FoodHub.sln
├── docker-compose.yml               # api, postgres, redis, seq, jaeger, web
├── .github/workflows/
│   ├── ci.yml                       # build + test + kiểm tra format
│   └── cd.yml                       # build image, push, deploy
├── deploy/                          # bicep/terraform hoặc script VPS
│
├── src/
│   ├── BuildingBlocks/              # dùng chung
│   │   └── FoodHub.SharedKernel/    # Entity, AggregateRoot, IDomainEvent, Result<T>
│   │
│   ├── FoodHub.Domain/
│   │   ├── Orders/                  # Order (aggregate), OrderItem, OrderStatus, events
│   │   ├── Restaurants/
│   │   ├── Payments/
│   │   └── Shipping/
│   │
│   ├── FoodHub.Application/         # CQRS
│   │   ├── Abstractions/            # IEmailSender, IPaymentGateway, ICacheService, IUnitOfWork
│   │   ├── Behaviors/               # ValidationBehavior, LoggingBehavior, TransactionBehavior, CachingBehavior
│   │   ├── Orders/
│   │   │   ├── Commands/PlaceOrder/         # Command, Handler, Validator
│   │   │   ├── Commands/UpdateOrderStatus/
│   │   │   ├── Queries/GetOrderById/
│   │   │   ├── Queries/GetMyOrders/
│   │   │   └── EventHandlers/OrderPlacedHandler.cs
│   │   ├── Restaurants/
│   │   ├── Payments/
│   │   ├── Shipping/
│   │   └── Identity/
│   │
│   ├── FoodHub.Infrastructure/
│   │   ├── Persistence/             # AppDbContext, Configurations, Migrations, Interceptors, Outbox
│   │   ├── Identity/                # JwtTokenService
│   │   ├── Payments/                # VnPayGateway, MomoGateway
│   │   ├── Email/                   # MailKitSender, Templates/
│   │   ├── Caching/                 # RedisCacheService
│   │   ├── Jobs/                    # Hangfire jobs: ExpireUnpaidOrders, ProcessOutbox, DailyReport
│   │   ├── Realtime/                # SignalR notifier implementation
│   │   ├── Observability/           # OpenTelemetry setup
│   │   └── DependencyInjection.cs
│   │
│   ├── FoodHub.Api/
│   │   ├── Controllers/ (hoặc Endpoints/ nếu dùng Minimal API)
│   │   ├── Hubs/                    # OrderTrackingHub, RestaurantHub, ChatHub
│   │   ├── Middleware/              # GlobalExceptionHandler, CorrelationId
│   │   ├── HealthChecks/
│   │   └── Program.cs
│   │
│   ├── FoodHub.Web/                 # Blazor Web App (khách + admin + nhà hàng)
│   │   ├── Components/Pages/
│   │   ├── Services/                # ApiClient, SignalRClient
│   │   └── Auth/
│   │
│   └── FoodHub.Shipper.Maui/        # App shipper
│       ├── Views/ ViewModels/ Services/ (LocationService, HubService)
│       └── Platforms/
│
└── tests/
    ├── FoodHub.Domain.UnitTests/
    ├── FoodHub.Application.UnitTests/
    ├── FoodHub.Api.IntegrationTests/    # Testcontainers: Postgres + Redis thật
    └── FoodHub.ArchitectureTests/       # Domain không được tham chiếu Infrastructure

Quy tắc phụ thuộc: Api, Infrastructure → Application → Domain. Domain không biết EF Core, SignalR hay MediatR (ngoài interface event).

6. Các điểm kỹ thuật chính

CQRS + MediatR. Mỗi use case là một Command/Query riêng. Pipeline Behavior xử lý những thứ lặp lại: validation → logging → transaction → (query) cache. Query đọc thẳng bằng projection (hoặc Dapper), không đi qua Aggregate.

SignalR.

OrderTrackingHub: khách join group order-{id}; server đẩy OrderStatusChanged, ShipperLocationUpdated
RestaurantHub: group restaurant-{id} nhận NewOrder
Xác thực hub bằng JWT (token qua query string cho WebSocket), kiểm tra quyền khi join group (không cho xem đơn của người khác)
Nhiều instance API thì bắt buộc bật Redis backplane

Thanh toán.

Tạo Payment(Pending) + URL thanh toán có chữ ký (VNPay: HMAC-SHA512; Momo: HMAC-SHA256)
Người dùng thanh toán, provider gọi IPN về server → verify chữ ký, kiểm tra số tiền, cập nhật idempotent bằng TxnRef duy nhất
Trang return chỉ để hiển thị, không tin tham số return để đánh dấu đã trả tiền
Hangfire đối soát các payment treo

Redis dùng vào 4 việc: cache menu (invalidate khi nhà hàng sửa), SignalR backplane, GEO vị trí shipper, khóa phân tán khi gán shipper.

Observability.

Traces: ASP.NET Core, HttpClient, EF Core, Redis, và ActivitySource tự viết cho luồng đặt đơn
Metrics: số đơn tạo, thời gian xử lý, số kết nối SignalR (custom Meter)
Log: Serilog gắn TraceId → Seq, nhờ vậy nhảy từ log sang trace
Health: /health/live (process sống), /health/ready (DB, Redis, SMTP, Hangfire)
7. CI/CD và cloud

ci.yml (mỗi PR): restore → build → dotnet test (Testcontainers cần Docker, runner GitHub có sẵn) → dotnet format --verify-no-changes → quét lỗ hổng gói (dotnet list package --vulnerable).

cd.yml (merge vào main): build Docker image (multi-stage) → push GHCR hoặc Azure Container Registry → deploy → chạy migration → gọi /health/ready kiểm tra → tự rollback nếu fail.

Môi trường: dev (docker-compose local) và prod (cloud). Secret để trong GitHub Secrets / Azure Key Vault, dùng OIDC thay vì lưu credential dài hạn.

Hai phương án triển khai

Azure: Container Apps (hoặc App Service) + Azure Database for PostgreSQL + Azure Cache for Redis + Application Insights/Grafana. Sinh viên có thể dùng credit miễn phí.
Tiết kiệm: một VPS, docker-compose + Caddy (HTTPS tự động), Seq và Jaeger chạy cùng.

Với MAUI, CI có thể build APK (Android); iOS cần macOS runner nên có thể bỏ qua.

8. Lộ trình (khoảng 14 tuần)
Giai đoạn	Tuần	Nội dung	Kiến thức học được
1. Nền tảng	1-2	Solution Clean Architecture, Domain, EF Core, migration, CRUD nhà hàng/menu bằng CQRS	DDD cơ bản, MediatR, Behaviors
2. Auth + Đơn hàng	3-4	Identity + JWT, giỏ hàng, PlaceOrder, state machine, outbox	Transaction, concurrency, domain events
3. Test + CI	5	Unit/integration/architecture test, GitHub Actions CI	Testcontainers, pipeline
4. Realtime + Cache	6-7	SignalR hubs, Redis cache + backplane + GEO, gán shipper	WebSocket, phân tán
5. Nền + Email + Thanh toán	8-9	Hangfire, MailKit, VNPay/Momo sandbox, idempotent IPN	Job nền, tích hợp bên thứ ba
6. Quan sát	10	OpenTelemetry, Serilog + Seq, Health Checks	Observability
7. Frontend + Mobile	11-12	Blazor (khách, nhà hàng, admin), MAUI cho shipper	Realtime UI, GPS
8. Deploy	13	Docker, CD, lên cloud, HTTPS, domain	DevOps
9. Microservices (tùy chọn)	14+	Tách Payment và Notification thành service riêng, RabbitMQ/MassTransit, YARP gateway	Giao tiếp bất đồng bộ, eventual consistency

Phần microservices để cuối vì nó chỉ có ý nghĩa khi bạn đã cảm nhận được đau đớn của monolith (deploy chung, scale không đều). Khi đó nhớ dùng Outbox đã làm sẵn để publish sự kiện.

9. Các lỗi dễ mắc
Tin tham số return của VNPay/Momo để xác nhận thanh toán → bị giả mạo. Chỉ tin IPN đã verify chữ ký.
Xử lý IPN không idempotent → cộng tiền/đổi trạng thái hai lần.
Gửi SignalR/email ngay trong transaction → transaction rollback nhưng thông báo đã đi. Dùng Outbox.
Không giới hạn quyền trong Hub → người này nghe được đơn của người khác.
Ghi vị trí shipper vào SQL mỗi 3 giây → DB quá tải. Dùng Redis.
Quên RowVersion → hai người cùng cập nhật một đơn thì bản ghi sau đè bản trước.
Domain phụ thuộc Infrastructure → phá Clean Architecture; test kiến trúc sẽ bắt lỗi này.
Cache menu không invalidate → nhà hàng tắt món mà khách vẫn đặt được.
10. Tiêu chí hoàn thành
docker compose up chạy đủ: API, Postgres, Redis, Seq, Jaeger, Web
Đặt một đơn end-to-end: thanh toán VNPay sandbox → nhà hàng nhận thông báo realtime → shipper nhận đơn trên MAUI → khách thấy shipper di chuyển
Trong Seq, tìm được một TraceId và mở trace tương ứng thấy đủ span (API → DB → Redis → email)
/health/ready báo đúng khi tắt thử Redis hoặc DB
Merge vào main thì tự deploy lên URL công khai
README có sơ đồ kiến trúc, sơ đồ luồng đặt đơn, ảnh chụp màn hình và hướng dẫn chạy