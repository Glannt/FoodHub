# FoodHub — Class diagram: từng lớp làm gì và ai gọi ai

Tài liệu này vẽ **cấu trúc lớp** của hệ thống: lớp nền tảng, Domain, pipeline CQRS,
luồng chạy thật của một request, state machine của đơn hàng, và sơ đồ quan hệ toàn bộ bảng.

Đọc theo thứ tự từ trên xuống — mỗi phần dựa vào phần trước.

> **Bản HTML có diễn giải chi tiết từng diagram**: `docs/CLASS-DIAGRAMS.html`
> **Kế hoạch triển khai kèm theo**: `docs/PLAN-SHOPEEFOOD.html`
> **Mã nguồn Mermaid**: `docs/diagrams/2*.mmd` · copy nguyên khối vào <https://mermaid.live> để xem trực tiếp.

---

## Mục lục

| Phần                                                   | Nội dung                                     | Diagram |
| ------------------------------------------------------ | -------------------------------------------- | ------- |
| [1](#phần-1--sharedkernel--4-lớp-nền)                  | SharedKernel — 4 lớp nền                     | `20`    |
| [2](#phần-2--domain-hiện-tại--restaurant-và-thực-đơn)  | Domain hiện tại — Restaurant / Menu          | `21`    |
| [3](#phần-3--order-aggregate--phần-sắp-viết)           | **Order aggregate — phần sắp viết**          | `22`    |
| [4](#phần-4--cqrs--command-handler-validator-behavior) | CQRS — Command, Handler, Validator, Behavior | `23`    |
| [5](#phần-5--luồng-chạy-thật-của-một-request)          | Luồng chạy thật của một request              | `24`    |
| [6](#phần-6--state-machine-của-đơn-hàng)               | State machine của đơn hàng                   | `25`    |
| [7](#phần-7--quan-hệ-18-bảng)                          | Quan hệ 18 bảng                              | `26`    |

---

# PHẦN 1 — SharedKernel: 4 lớp nền

Không thuộc domain nào cụ thể, dùng chung cho toàn hệ thống. Nếu không có tầng này,
mỗi module sẽ phải tự viết lại `Entity` và `IUnitOfWork` của riêng mình.

```mermaid
classDiagram
    direction TB

    class Entity {
        <<abstract>>
        +TId Id
        +DateTimeOffset CreatedAt
        +DateTimeOffset? UpdatedAt
        #Touch()
        +Equals(obj) bool
        +GetHashCode() int
    }

    class AggregateRoot {
        <<abstract>>
        -List _domainEvents
        +IReadOnlyCollection DomainEvents
        #Raise(event)
        +ClearDomainEvents()
    }

    class IDomainEvent {
        <<interface>>
        +Guid EventId
        +DateTimeOffset OccurredOn
    }

    class Result {
        +bool IsSuccess
        +bool IsFailure
        +T Value
        +string Error
        +Success(value)
        +Failure(error)
    }

    class IUnitOfWork {
        <<interface>>
        +SaveChangesAsync(ct) Task
    }

    Entity <|-- AggregateRoot : kế thừa
    AggregateRoot o-- IDomainEvent : danh sách sự kiện
```

### Ba điều dễ nhầm ở tầng này

**1. `Equals` so sánh bằng `Id`, không so sánh thuộc tính.**
Hai `Restaurant` khác `Id` là hai đối tượng khác nhau kể cả khi tên và địa chỉ giống hệt —
đó là lý do `CreateRestaurantHandler` phải hỏi database thay vì so sánh trong RAM.

**2. `UpdatedAt` là `DateTimeOffset?` và chỉ có giá trị sau khi gọi `Touch()`.**
Nó được set trong ctor của `Restaurant` — vì vậy bảng cho phép `updated_at` NULL.
NULL nghĩa là _"chưa sửa lần nào"_, không phải lỗi. Sửa nó thành NOT NULL là mất thông tin.

**3. `ClearDomainEvents()` phải gọi sau khi `SaveChangesAsync` thành công.**
Gọi sớm thì mất sự kiện khi transaction rollback.

---

# PHẦN 2 — Domain hiện tại: Restaurant và thực đơn

```mermaid
classDiagram
    direction TB

    class Restaurant {
        <<AggregateRoot>>
        +string Name
        +string Address
        +decimal Latitude
        +decimal Longtitude
        +bool IsApproved
        +Restaurant(name, addr, lat, lng)
        +Approve()
        +Reject()
        +UpdateDetails(name, addr, lat, lng)
    }

    class MenuCategory {
        <<Entity>>
        +Guid RestaurantId
        +string Name
        +MenuCategory(restaurantId, name)
    }

    class MenuItem {
        <<AggregateRoot>>
        +Guid RestaurantId
        +Guid CategoryId
        +string Name
        +string Description
        +decimal Price
        +bool IsAvailable
        +string ImageUrl
        +MenuItem(restaurantId, categoryId, name, desc, price, img)
        +UpdateDetails(name, desc, price, img)
        +SetAvailability(bool)
    }

    class MenuItemAddedEvent {
        <<record>>
        +Guid MenuItemId
        +Guid RestaurantId
        +string Name
        +DateTimeOffset OccurredOn
    }

    Restaurant "1" --> "*" MenuCategory : chứa
    Restaurant "1" --> "*" MenuItem : sở hữu
    MenuCategory "1" --> "*" MenuItem : nhóm
    MenuItem ..> MenuItemAddedEvent : Raise khi tạo
    MenuItemAddedEvent ..|> IDomainEvent : implements
```

### Vì sao MenuItem là AggregateRoot chứ không phải Entity

Vì nó cần `Raise()` để bắn `MenuItemAddedEvent`. Về mặt DDD cũng đúng: món ăn có vòng đời
độc lập và đổi rất thường xuyên — nếu gộp vào aggregate `Restaurant`, mỗi lần bật/tắt món
sẽ kéo theo việc nạp và kiểm soát toàn bộ thực đơn của nhà hàng.

### Còn thiếu trong diagram này

Không có mảnh nào chỉ ra **ai sở hữu nhà hàng**. Không có `owner_user_id`,
không có `cuisine_id`, không có giờ mở cửa. Đó chính là danh sách bổ sung ở
`PLAN-SHOPEEFOOD.html` §4.

---

# PHẦN 3 — Order aggregate: phần sắp viết

Đây là lớp khó nhất của cả app: 8 trạng thái, **3 vai trò khác nhau**, mỗi vai trò được
phép chuyển những trạng thái riêng. Hiện `TODO.md` mới chỉ ghi nó trong một dòng bullet.

```mermaid
classDiagram
    direction TB

    class Order {
        <<AggregateRoot>>
        +string OrderNo
        +Guid CustomerId
        +Guid RestaurantId
        +OrderStatus Status
        +string RecipientName
        +string PhoneNumber
        +string Address
        +decimal Latitude
        +decimal Longtitude
        +decimal Subtotal
        +decimal DeliveryFee
        +decimal DiscountAmount
        +decimal TotalAmount
        +string Note
        +DateTimeOffset? PlacedAt
        +DateTimeOffset? AcceptedAt
        +DateTimeOffset? CancelledAt
        +Place()
        +Accept()
        +Reject(reason)
        +StartPreparing()
        +MarkReady()
        +MarkPickedUp()
        +Complete()
        +Cancel(reason)
    }

    class OrderItem {
        <<Entity>>
        +Guid OrderId
        +Guid MenuItemId
        +string ItemName
        +decimal UnitPrice
        +int Quantity
        +decimal Subtotal
        +string Note
        +OrderItem(orderId, menuItemId, name, price, qty, note)
    }

    class OrderStatusHistoryEntry {
        <<Entity>>
        +Guid OrderId
        +OrderStatus FromStatus
        +OrderStatus ToStatus
        +Guid ChangedByUserId
        +string Reason
        +DateTimeOffset ChangedAt
    }

    class OrderStatus {
        <<enum>>
        PendingPayment
        Placed
        Accepted
        Preparing
        ReadyForPickup
        PickedUp
        Delivered
        Cancelled
    }

    class OrderPlacedEvent {
        <<record>>
        +Guid OrderId
        +Guid RestaurantId
    }

    class OrderCancelledEvent {
        <<record>>
        +Guid OrderId
        +string Reason
    }

    class IDomainEvent {
        <<interface>>
    }

    Order "1" *-- "*" OrderItem : snapshot giá lúc đặt
    Order "1" --> "*" OrderStatusHistoryEntry : mỗi lần chuyển một dòng
    Order --> OrderStatus : trạng thái hiện tại
    Order ..> OrderPlacedEvent : Raise khi Place
    Order ..> OrderCancelledEvent : Raise khi Cancel
    OrderStatusHistoryEntry --> OrderStatus : from và to
    OrderPlacedEvent ..|> IDomainEvent : implements
    OrderCancelledEvent ..|> IDomainEvent : implements
```

### Ba cột không được bỏ: ItemName, UnitPrice, Subtotal

Chúng là **snapshot**. Nếu nhà hàng tăng giá món từ 30k lên 50k, đơn cũ **không được**
đổi theo — nếu không, hệ thống sẽ in ra tổng tiền sai cho một đơn đã giao xong
mà khách vẫn còn giữ.

Vì vậy `menu_item_id` được để **NULL** khi món bị xoá khỏi thực đơn
(`ON DELETE SET NULL`), chứ không phải xoá luôn dòng đơn.

### Guard trạng thái đặt trong Domain, không đặt trong Handler

Mỗi method chuyển trạng thái tự kiểm tra trạng thái hiện tại. Nếu không hợp lệ thì ném
`BusinessRuleException` → 422. Handler chỉ gọi `order.Cancel(reason)`, không tự kiểm tra
_"đơn có đang nấu không"_.

Lý do: có 8 endpoint chuyển trạng thái và một SignalR hub tự chuyển theo timeout.
Nếu rải điều kiện ra handler, sẽ có 10 chỗ phải giữ cho đúng. Để trong Domain thì
**một chỗ duy nhất, không thể bỏ sót**.

---

# PHẦN 4 — CQRS: Command, Handler, Validator, Behavior

```mermaid
classDiagram
    direction TB

    class ApiController {
        <<RestaurantsController>>
        +Create(request, ct) ActionResult
        +Update(id, request, ct) ActionResult
        +Approve(id, ct) IActionResult
        +Reject(id, ct) IActionResult
    }

    class CreateRestaurantCommand {
        <<record>>
        +string Name
        +string Address
        +decimal Latitude
        +decimal Longtitude
    }

    class CreateRestaurantResult {
        <<record>>
        +Guid RestaurantId
    }

    class CreateRestaurantHandler {
        <<internal sealed>>
        -IApplicationDbContext dbContext
        -IUnitOfWork unitOfWork
        +Handle(request, ct) Task
    }

    class CreateRestaurantValidator {
        <<public sealed>>
        +Name
        +Address
        +Latitude
        +Longtitude
    }

    class IApplicationDbContext {
        <<interface>>
        +DbSet Restaurants
        +DbSet MenuCategories
        +DbSet MenuItems
    }

    class ValidationBehavior {
        +Handle(request, next, ct) Task
    }

    class LoggingBehavior {
        +Handle(request, next, ct) Task
    }

    class TransactionBehavior {
        +Handle(request, next, ct) Task
    }

    class NotFoundException {
        <<exception>>
    }

    class ConflictException {
        <<exception>>
    }

    class BusinessRuleException {
        <<exception mới>>
    }

    class ForbiddenException {
        <<exception mới>>
    }

    class ExceptionHandler {
        <<middleware>>
        +TryHandleAsync(context, exception)
    }

    ApiController ..> CreateRestaurantCommand : ghép từ route và body
    ApiController ..> CreateRestaurantHandler : MediatR Send
    CreateRestaurantHandler --> IApplicationDbContext : truy vấn
    CreateRestaurantHandler ..> ConflictException : ném khi trùng
    CreateRestaurantValidator ..> ValidationBehavior : được tìm và gọi
    ValidationBehavior ..> CreateRestaurantCommand : kiểm tra
    TransactionBehavior --> IApplicationDbContext : mở transaction
    ExceptionHandler ..> NotFoundException : bắt thành 404
    ExceptionHandler ..> ConflictException : bắt thành 409
    ExceptionHandler ..> BusinessRuleException : bắt thành 422
    ExceptionHandler ..> ForbiddenException : bắt thành 403
```

### Validator phải public, Handler thì internal

`AddValidatorsFromAssembly()` mặc định `includeInternalTypes: false`.
Validator `internal` bị bỏ qua **im lặng** — vì `ValidationBehavior` làm
`if (validators.Any())`: không tìm thấy validator nào thì coi như hợp lệ.
Không có lỗi, không cảnh báo, chỉ có dữ liệu rác lọt vào DB.

Handler thì **được** `internal` vì MediatR tạo qua reflection.

---

# PHẦN 5 — Luồng chạy thật của một request

```mermaid
flowchart TD
    %% ═══ LUỒNG 1 REQUEST ═══
    subgraph API["TẦNG API"]
        AC["Controller<br/>đọc route + body<br/>ghép thành Command"]
        EH1["ExceptionHandler<br/>404 · 409 · 422 · 403"]
        AC -->|"ISender.Send(command, ct)"| MB
    end

    subgraph APP["TẦNG APPLICATION - MediatR pipeline"]
        MB["MediatR<br/>dựng pipeline theo đúng thứ tự đăng ký"]
        VB["① ValidationBehavior<br/>→ thấy lỗi thì dừng luôn"]
        LB["② LoggingBehavior<br/>→ Stopwatch, ghi log trước/sau"]
        TB["③ TransactionBehavior<br/>→ mới thêm<br/>BEGIN + COMMIT, chỉ Command"]
        CB["④ CachingBehavior<br/>→ GĐ4"]
        MB --> VB --> LB --> TB --> CB
    end

    subgraph HAND["HANDLER - Application"]
        H["Handle(request, ct)"]
        H -->|"AnyAsync chặn trùng"| CHK{"Trùng?"}
        CHK -->|"có"| CE["throw ConflictException"]
        CHK -->|"không"| DOM
    end

    subgraph DOMAIN["TẦNG DOMAIN - luật nghiệp vụ"]
        DOM["new Restaurant(...)<br/>Guard chạy ở đây:<br/>tên/địa chỉ không rỗng"]
        EVT["Raise(MenuItemAddedEvent)<br/>ghi vào _domainEvents trong RAM"]
        DOM --> EVT
    end

    subgraph INFRA["TẦNG INFRASTRUCTURE"]
        CTX["AppDbContext<br/>ChangeTracker giữ thay đổi trong RAM"]
        UOW["AppDbContextUnitOfWork<br/>bọc AppDbContext"]
        PG[("PostgreSQL")]
        H --> CTX
        CTX --> UOW
        UOW --> PG
    end

    %% ═══ ĐƯỜNG GHI ═══
    DOM -.->|"Add(entity)"| CTX
    TB -.->|"bao SaveChangesAsync"| H

    %% ═══ KẾT QUẢ ═══
    UOW -->|"int rows affected"| H
    H -->|"CreateRestaurantResult"| CTX2["kết quả đi ngược lại<br/>Transaction → Cache → Logging → Validation"]
    CTX2 --> AC
    AC -->|"201 Created + Location"| CLI["Client"]

    %% ═══ ĐƯỜNG ĐỌC ═══
    CLI2["Client"] -->|"GET"| QC["Query Handler"]
    QC -->|"AsNoTracking + Skip/Take<br/>+ Select projection"| PG
    PG -->|"chỉ đọc, không BEGIN"| QC
    QC -->|"200 OK"| CLI2

    %% ═══ LỖI ═══
    CE -.-> EH1
    DOM -.->|"Guard fail → ArgumentException"| EH1
    EH1 -->|"400 · 404 · 409 · 422 · 403"| CLI

    classDef api fill:#eaf1ff,stroke:#4aa8ff,color:#0f1115
    classDef app fill:#e8f7ed,stroke:#3ecf8e,color:#0f1115
    classDef dom fill:#fff8e1,stroke:#f5b544,color:#0f1115
    classDef inf fill:#fcebf5,stroke:#d47ab0,color:#0f1115
    classDef err fill:#ffe8e9,stroke:#f2555a,color:#0f1115
    class AC,EH1,CLI,CLI2 api
    class MB,VB,LB,TB,CB,H,CHK,CTX2 app
    class DOM,EVT dom
    class CTX,UOW,PG,QC inf
    class CE err
```

### Đường ghi và đường đọc khác nhau ở chỗ nào

**Command** đi qua `TransactionBehavior` mở `BEGIN`, rồi `SaveChangesAsync` đẩy xuống
PostgreSQL. **Query** không mở transaction — chỉ `SELECT` với
`AsNoTracking + Skip/Take + Select`. Đó là lý do `TransactionBehavior` **phải kiểm tra
Command hay Query** trước khi mở transaction.

### Cửa sổ tranh chấp nằm ở đâu

`AnyAsync` chặn trùng và `INSERT` là **hai câu lệnh riêng biệt**. Hai request song song
cùng chạy `AnyAsync` → cùng thấy _"chưa có"_ → cùng `INSERT`.
Chỉ unique index ở database mới đóng được lỗ hổng này.

---

# PHẦN 6 — State machine của đơn hàng

```mermaid
stateDiagram-v2
    direction TB

    [*] --> PendingPayment : khách bấm Đặt món

    PendingPayment --> Placed : thanh toán thành công - KHÁCH
    PendingPayment --> Cancelled : không trả được - KHÁCH

    Placed --> Accepted : quán bấm Nhận đơn - NHÀ HÀNG
    Placed --> Cancelled : quán từ chối - NHÀ HÀNG

    Accepted --> Preparing : bắt đầu nấu - NHÀ HÀNG
    Preparing --> ReadyForPickup : nấu xong - NHÀ HÀNG
    ReadyForPickup --> PickedUp : shipper lấy hàng - SHIPPER
    PickedUp --> Delivered : giao tới nơi - SHIPPER

    Delivered --> [*]
    Cancelled --> [*]

    Accepted --> Cancelled : khách đổi ý - KHÁCH
    ReadyForPickup --> Cancelled : khách huỷ - KHÁCH

    note right of Preparing
        KHÔNG huỷ được.
        Đã vào bếp thì món ăn
        không còn hủy được nữa.
        Guard trong Domain phải chặn.
    end note

    note right of PickedUp
        Chỉ còn đánh dấu Delivered.
        Shipper đã cầm hàng trên tay.
    end note

    note right of Cancelled
        Mỗi lần chuyển ghi một dòng
        vào order_status_history:
        from_status, to_status,
        changed_by_user_id, changed_at.
        from_status rỗng ở lần đầu.
    end note
```

### Ai được chuyển trạng thái nào

| Vai trò      | Được chuyển sang                                       | Endpoint dự kiến                                                                   |
| ------------ | ------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| **Khách**    | `Placed`, `Cancelled`                                  | `POST /api/orders` · `PATCH /api/orders/{id}/cancellation`                         |
| **Nhà hàng** | `Accepted`, `Cancelled`, `Preparing`, `ReadyForPickup` | `PATCH /api/orders/{id}/acceptance` · `/rejection` · `/preparation` · `/readiness` |
| **Shipper**  | `PickedUp`, `Delivered`                                | `PATCH /api/orders/{id}/pickup` · `/delivery`                                      |
| **Hệ thống** | `Cancelled`                                            | Job quét đơn quá hạn không ai nhận                                                 |

**Mỗi hàng trong bảng trên là một authorization requirement.** Khách gọi
`.../acceptance` phải bị từ chối 403. Nhà hàng gọi `.../delivery` cũng vậy. Đây là chỗ
`[Authorize]` + `owner_user_id` ở `restaurants` mới đủ sức phân biệt —
mà cả hai đều chưa có trong hệ thống.

---

# PHẦN 7 — Quan hệ 18 bảng

Ba bảng gốc hiện có (`restaurants`, `menu_categories`, `menu_items`) nằm ở phía trên;
15 bảng mới là phần còn lại.

```mermaid
erDiagram
    APP_USERS ||--o{ CUSTOMER_ADDRESSES : "có sổ địa chỉ"
    APP_USERS ||--o{ RESTAURANTS : "sở hữu"
    APP_USERS ||--o{ CARTS : "có giỏ hàng"
    APP_USERS ||--o{ ORDERS : "đặt"
    APP_USERS ||--o{ REVIEWS : "viết"
    APP_USERS ||--o{ ORDER_DELIVERIES : "giao"

    RESTAURANTS ||--o{ MENU_CATEGORIES : "chia danh mục"
    RESTAURANTS ||--o{ MENU_ITEMS : "có món"
    RESTAURANTS ||--o{ CARTS : "có giỏ"
    RESTAURANTS ||--o{ ORDERS : "nhận đơn"
    RESTAURANTS ||--o{ RESTAURANT_HOURS : "mở cửa"
    RESTAURANTS ||--o{ RESTAURANT_CLOSURES : "nghỉ lễ"
    RESTAURANTS ||--o{ REVIEWS : "được đánh giá"
    RESTAURANTS }o--o| CUISINES : "phục vụ"

    MENU_CATEGORIES ||--o{ MENU_ITEMS : "gồm"

    CARTS ||--o{ CART_ITEMS : "chứa"
    MENU_ITEMS ||--o{ CART_ITEMS : "được thêm"

    ORDERS ||--|{ ORDER_ITEMS : "chốt"
    ORDERS ||--o{ ORDER_STATUS_HISTORY : "ghi lại từng bước"
    ORDERS ||--o| PAYMENTS : "thanh toán"
    ORDERS ||--o| ORDER_DELIVERIES : "giao hàng"
    ORDERS ||--o{ REVIEWS : "được đánh giá"
    ORDERS ||--o{ ORDER_VOUCHERS : "dùng voucher"
    VOUCHERS ||--o{ ORDER_VOUCHERS : "được dùng"

    MENU_ITEMS ||--o{ ORDER_ITEMS : "nguồn - FK SET NULL"
    MENU_ITEMS ||--o{ REVIEWS : "được đánh giá"

    APP_USERS {
        uuid id PK
        string user_name
        string email
        string phone_number
        string password_hash
        string full_name "cột thêm"
    }

    CUSTOMER_ADDRESSES {
        uuid id PK
        uuid user_id FK
        string recipient_name
        string phone_number
        string address_text
        decimal latitude
        decimal longtitude
        bool is_default
    }

    CUISINES {
        uuid id PK
        string name UK
        string slug UK
        int sort_order
    }

    RESTAURANTS {
        uuid id PK
        uuid owner_user_id FK "MỚI"
        uuid cuisine_id FK "MỚI"
        string name
        string address_text
        decimal latitude
        decimal longtitude
        bool is_approved
        bool is_active "MỚI"
        string phone_number "MỚI"
        string image_url "MỚI"
        decimal delivery_fee "MỚI"
        decimal min_order_value "MỚI"
        int preparation_minutes "MỚI"
        decimal rating_avg "MỚI"
        int rating_count "MỚI"
    }

    RESTAURANT_HOURS {
        uuid id PK
        uuid restaurant_id FK
        int day_of_week "0 đến 6"
        time opens_at
        time closes_at
        bool is_closed
    }

    RESTAURANT_CLOSURES {
        uuid id PK
        uuid restaurant_id FK
        date from_date
        date to_date
        string reason
    }

    MENU_CATEGORIES {
        uuid id PK
        uuid restaurant_id FK
        string name
    }

    MENU_ITEMS {
        uuid id PK
        uuid restaurant_id FK
        uuid category_id FK
        string name
        decimal price
        bool is_available
    }

    CARTS {
        uuid id PK
        uuid user_id FK
        uuid restaurant_id FK
    }

    CART_ITEMS {
        uuid id PK
        uuid cart_id FK
        uuid menu_item_id FK
        int quantity
        string note
    }

    ORDERS {
        uuid id PK
        string order_no UK
        uuid user_id FK
        uuid restaurant_id FK
        int status
        string recipient_name "snapshot"
        string phone_number "snapshot"
        string address_text "snapshot"
        decimal latitude "snapshot"
        decimal longtitude "snapshot"
        decimal subtotal
        decimal delivery_fee
        decimal discount_amount
        decimal total_amount
        string idempotency_key UK
        timestamptz placed_at
        timestamptz accepted_at
        timestamptz delivered_at
        timestamptz cancelled_at
    }

    ORDER_ITEMS {
        uuid id PK
        uuid order_id FK
        uuid menu_item_id FK "SET NULL"
        string item_name "SNAPSHOT"
        decimal unit_price "SNAPSHOT"
        int quantity
        decimal subtotal "SNAPSHOT"
        string note
    }

    ORDER_STATUS_HISTORY {
        uuid id PK
        uuid order_id FK
        int from_status "có thể rỗng"
        int to_status
        uuid changed_by_user_id FK
        string reason
        timestamptz changed_at
    }

    PAYMENTS {
        uuid id PK
        uuid order_id FK
        string provider
        int status
        decimal amount
        string provider_txn_id UK
        jsonb raw_payload
    }

    REVIEWS {
        uuid id PK
        uuid order_id FK "UNIQUE"
        uuid restaurant_id FK
        uuid user_id FK
        uuid menu_item_id FK
        int rating "1 đến 5"
        string comment
    }

    SHIPPER_PROFILES {
        uuid user_id PK "FK"
        bool is_available
        string vehicle_type
        decimal latitude
        decimal longtitude
    }

    ORDER_DELIVERIES {
        uuid id PK
        uuid order_id FK "UNIQUE"
        uuid shipper_user_id FK
        timestamptz assigned_at
        timestamptz delivered_at
        string proof_image_url
    }

    VOUCHERS {
        uuid id PK
        string code UK
        int discount_type
        decimal discount_value
        decimal max_discount
        int usage_limit
        int used_count
        bool is_active
    }

    ORDER_VOUCHERS {
        uuid order_id PK "FK"
        uuid voucher_id PK "FK"
        decimal discount_amount
    }
```

---

## Ghi chú đọc diagram

| Ký hiệu             | Nghĩa                                |
| ------------------- | ------------------------------------ |
| `<<AggregateRoot>>` | Có `Raise()`, là ranh giới giao dịch |
| `<<Entity>>`        | Chỉ có `Id`, không bắn sự kiện       |
| `<<enum>>`          | Kiểu liệt kê                         |
| `+` / `-`           | public / private                     |
| `#`                 | protected                            |
| `-->`               | có quan hệ, hướng đọc                |
| `..>`               | phụ thuộc, không giữ tham chiếu      |
| `*--`               | thành phần, xoá chung                |
| `<\|--`             | kế thừa                              |

### Hai điều dễ hiểu sai trong sơ đồ ER

**1. Dòng `MENU_ITEMS ||--o{ ORDER_ITEMS` không giữ tính toàn vẹn.**
Cột `menu_item_id` trong `order_items` được khai `NULL` và FK là `SET NULL`.
Khi nhà hàng xoá món khỏi thực đơn, dòng đơn hàng vẫn còn nguyên, chỉ mất liên kết.
Đó là hệ quả trực tiếp của việc snapshot giá.

**2. `orders.user_id` và `orders.restaurant_id` là RESTRICT, không phải CASCADE.**
Xoá nhà hàng khi đã có đơn là mất lịch sử giao hàng — khách khiếu nại
thì không còn dữ liệu đối chiếu.
