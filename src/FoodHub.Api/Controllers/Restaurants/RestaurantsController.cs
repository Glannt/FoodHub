using System.ComponentModel.DataAnnotations;

using MediatR;
using Microsoft.AspNetCore.Mvc;

using FoodHub.Application.Restaurants.Commands.ApproveRestaurant;
using FoodHub.Application.Restaurants.Commands.CreateRestaurant;
using FoodHub.Application.Restaurants.Commands.RejectRestaurant;
using FoodHub.Application.Restaurants.Commands.UpdateRestaurant;
using FoodHub.Application.Restaurants.Queries.GetRestaurantById;
using FoodHub.Application.Restaurants.Queries.GetRestaurants;

namespace FoodHub.Api.Controllers.Restaurants;

// Controller chỉ làm 3 việc: nhận request -> ghép thành Command -> trả về status code đúng.
// Không chứa business logic, không gọi DbContext trực tiếp. Nhờ vậy đổi quy tắc nghiệp vụ
// không phải đụng vào tầng Api, và Api có thể test được chỉ bằng cách kiểm tra status code.
[ApiController]
[Route("api/restaurants")]
public sealed class RestaurantsController(ISender sender) : ControllerBase
{
    // Ràng buộc :guid trên path giúp id sai định dạng trả 404 gọn gàng
    // thay vì lỗi model binding 400 — cùng pattern với MenuItemsController.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetRestaurantByIdResult>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetRestaurantByIdQuery(id);
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    // Đặt GetAll trước GetById để "api/restaurants" không bị hiểu nhầm là có tham số.
    [HttpGet]
    public async Task<ActionResult<GetRestaurantsResult>> GetAll(
        [FromQuery] string restaurantQuery = "",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRestaurantsQuery(restaurantQuery, pageNumber, pageSize);
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateRestaurantResult>> Create(
        CreateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        // RestaurantId KHÔNG có trong body: Id do server sinh ở AggregateRoot.
        var command = new CreateRestaurantCommand(
            Name: request.Name,
            Slug: request.Slug,
            Description: request.Description,
            PhoneNumber: request.PhoneNumber,
            Email: request.Email,
            Address: request.Address,
            Latitude: request.Latitude.GetValueOrDefault(),
            Longtitude: request.Longtitude.GetValueOrDefault(),
            LogoUrl: request.LogoUrl,
            CoverImageUrl: request.CoverImageUrl,
            OpeningTime: request.OpeningTime.GetValueOrDefault(new TimeOnly(8, 0)),
            ClosingTime: request.ClosingTime.GetValueOrDefault(new TimeOnly(22, 0)),
            MinOrderAmount: request.MinOrderAmount.GetValueOrDefault(0m),
            OwnerId: request.OwnerId.GetValueOrDefault(Guid.NewGuid()));

        var result = await sender.Send(command, cancellationToken);

        // 201 + Location trỏ về chính endpoint đọc lại vừa tạo.
        return CreatedAtAction(nameof(GetById), new { id = result.RestaurantId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateRestaurantResult>> Update(
        Guid id,
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        // Id lấy từ ROUTE, không lấy từ body: tránh client gửi nhầm id khác route
        var command = new UpdateRestaurantCommand(
            RestaurantId: id,
            Name: request.Name,
            Slug: request.Slug,
            Description: request.Description,
            PhoneNumber: request.PhoneNumber,
            Email: request.Email,
            Address: request.Address,
            Latitude: request.Latitude.GetValueOrDefault(),
            Longtitude: request.Longtitude.GetValueOrDefault(),
            LogoUrl: request.LogoUrl,
            CoverImageUrl: request.CoverImageUrl,
            OpeningTime: request.OpeningTime.GetValueOrDefault(new TimeOnly(8, 0)),
            ClosingTime: request.ClosingTime.GetValueOrDefault(new TimeOnly(22, 0)),
            MinOrderAmount: request.MinOrderAmount.GetValueOrDefault(0m),
            IsAcceptingOrders: request.IsAcceptingOrders);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    // Approve và Reject là 2 endpoint riêng, KHÔNG gộp thành
    // PATCH ".../approval" + body { approved: true }.
    // Ở tầng Application chúng đã là 2 command tách biệt để audit log sau này đọc dễ;
    // gộp lại thành 1 endpoint ở Api sẽ phản bội đúng lý do đó.
    // TODO Giai đoạn 2: thêm [Authorize(Roles = "Admin")] cho 2 endpoint này.
    [HttpPatch("{id:guid}/approval")]
    public async Task<IActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveRestaurantCommand(id), cancellationToken);

        // 204 vì command này là IRequest<Unit> — không có dữ liệu để trả về.
        return NoContent();
    }

    [HttpPatch("{id:guid}/rejection")]
    public async Task<IActionResult> Reject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RejectRestaurantCommand(id), cancellationToken);
        return NoContent();
    }
}

// DTO request khai báo ở Api chứ không dùng luôn Command của tầng Application:
// Command là hợp đồng nội bộ, đổi nó thì cả contract HTTP đổi theo và client vỡ.
//
// Toạ độ khai `decimal?` + [Required] thay vì `decimal` thuần — đây là điểm then chốt:
// với `decimal` (value type không nullable), client BỎ QUA field sẽ khiến System.Text.Json
// gán giá trị mặc định 0, và validator InclusiveBetween(-90, 90) / (-180, 180) VẪN THẢ QUA
// vì 0 nằm trong khoảng hợp lệ. Kết quả: client quên gửi toạ độ vẫn tạo/sửa được nhà hàng,
// và nhà hàng bị ghi đè tọa độ 0.000000 — im lặng, không lỗi, không cảnh báo.
// `decimal?` biến "thiếu" thành null, [Required] chặn lại, client nhận 400 với thông điệp rõ ràng.
//
// LƯU ý: ghi `[Required]` trần, KHÔNG ghi `[property: Required]`. Với positional record,
// attribute mặc định nhắm vào tham số của primary constructor. Nếu ghi `[property: Required]`
// thì MVC ném InvalidOperationException lúc binding:
// "Record type '...' has validation metadata defined on property '...' that will be ignored."
// — và mọi request tới endpoint đó đều thành 500, kể cả request hợp lệ.
public sealed record CreateRestaurantRequest(
    [Required] string Name,
    string? Slug,
    string? Description,
    [Required] string PhoneNumber,
    [Required] string Email,
    [Required] string Address,
    [Required] double? Latitude,
    [Required] double? Longtitude,
    string? LogoUrl,
    string? CoverImageUrl,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    decimal? MinOrderAmount,
    Guid? OwnerId);

public sealed record UpdateRestaurantRequest(
    [Required] string Name,
    string? Slug,
    string? Description,
    [Required] string PhoneNumber,
    [Required] string Email,
    [Required] string Address,
    [Required] double? Latitude,
    [Required] double? Longtitude,
    string? LogoUrl,
    string? CoverImageUrl,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    decimal? MinOrderAmount,
    bool? IsAcceptingOrders);
