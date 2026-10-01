using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using FoodHub.Api.Middleware;
using FoodHub.Application;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// OpenAPI built-in của .NET 10 (dùng Microsoft.AspNetCore.OpenApi, không phải Swashbuckle).
// Cài sẵn từ template nên không cần thêm package.
builder.Services.AddOpenApi();

// Tầng Application: đăng ký MediatR + FluentValidation + các Behavior (validate, logging).
// Chỉ cần 1 dòng, tầng Api không cần biết chi tiết bên trong.
builder.Services.AddApplication();

// Tầng Infrastructure: DbContext (Npgsql), UnitOfWork, cache, ...
// Chỉ cần truyền configuration vì chuỗi kết nối đọc từ appsettings.json ở chính tầng đó.
builder.Services.AddInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("Default");

var healthChecks = builder.Services
    .AddHealthChecks()
    // Check "self" không chạm tài nguyên nào nên luôn Healthy:
    // orchestrator dùng nó để biết container còn sống mà không bị kéo theo DB.
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

// Chỉ đăng ký check DB khi đã có connection string.
// Khi nào bạn điền xong Postgres vào appsettings.json thì /health/ready mới thật sự kiểm tra DB.
// Nếu đăng ký luôn thì app sẽ crash ngay khi ConnectionStrings:Default còn rỗng.
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);
}

// AddProblemDetails là service mà các exception handler ở trên dùng để ghi body lỗi.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Chỉ mở tài liệu OpenAPI ở Development: production không nên lộ bản đồ API ra ngoài.
    app.MapOpenApi();
}

// UseExceptionHandler phải đặt TRƯỚC mọi middleware khác để bắt được exception
// phát sinh từ các middleware phía sau nó.
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