using MediatR;
using Microsoft.AspNetCore.Mvc;

using FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;
using FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Restaurants.MenuItems.Queries.GetMenuByRestaurant;

namespace FoodHub.Api.Controllers.MenuItems;

// Controller chỉ làm 3 việc: nhận request -> ghép thành Command -> trả về status code đúng.
// Không chứa business logic, không gọi DbContext trực tiếp. Nhờ vậy đổi quy tắc nghiệp vụ
// không phải đụng vào tầng Api, và Api có thể test được chỉ bằng cách kiểm tra status code.
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
        // RestaurantId lấy từ ROUTE, các field còn lại lấy từ BODY.
        // Không bind thẳng Command từ body: client sẽ tự ý gửi RestaurantId khác và ghi đè route.
        var command = new CreateMenuItemCommand(
            restaurantId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.ImageUrl);

        var result = await sender.Send(command, cancellationToken);

        // 201 + Location trỏ về URL đọc lại chính món vừa tạo (chuẩn REST).
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

    // PATCH dùng cho việc bật/tắt món: thay đổi nhỏ, không cần gửi cả object.
    [HttpPatch("items/{menuItemId:guid}/availability")]
    public async Task<IActionResult> SetAvailability(
        Guid restaurantId,
        Guid menuItemId,
        SetMenuItemAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetMenuItemAvailabilityCommand(menuItemId, request.IsAvailable);
        await sender.Send(command, cancellationToken);

        // 204 vì command này không có dữ liệu để trả về.
        return NoContent();
    }
}

// DTO request khai báo ở Api chứ không dùng luôn Command của tầng Application:
// Command là hợp đồng nội bộ, đổi nó thì cả contract HTTP đổi theo và client vỡ.
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