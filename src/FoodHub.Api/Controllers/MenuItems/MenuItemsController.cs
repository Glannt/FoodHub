using MediatR;
using Microsoft.AspNetCore.Mvc;

using FoodHub.Application.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.MenuItems.Commands.SetMenuItemAvailability;
using FoodHub.Application.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.MenuItems.Queries.GetMenuByRestaurant;

namespace FoodHub.Api.Controllers.MenuItems;

// Controller chỉ làm 3 việc: nhận request -> ghép thành Command -> trả về status code đúng.
// Không chứa business logic, không gọi DbContext trực tiếp.
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
        // RestaurantId lấy từ ROUTE, các field còn lại lấy từ BODY: client không thể
        // ghi đè nhà hàng bằng cách gửi RestaurantId khác trong body.
        var command = new CreateMenuItemCommand(
            restaurantId,
            request.MenuCategoryId,
            request.ProductId,
            request.Price,
            request.DisplayName,
            request.IsAvailable,
            request.IsFeatured,
            request.SortOrder,
            request.MaxQuantityPerOrder);

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
            restaurantId,
            menuItemId,
            request.Price,
            request.DisplayName,
            request.IsAvailable,
            request.IsFeatured,
            request.SortOrder,
            request.MaxQuantityPerOrder);

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
        var command = new SetMenuItemAvailabilityCommand(restaurantId, menuItemId, request.IsAvailable);
        await sender.Send(command, cancellationToken);

        return NoContent();
    }
}

// DTO request khai báo ở Api chứ không dùng luôn Command của tầng Application:
// Command là hợp đồng nội bộ, đổi nó thì cả contract HTTP đổi theo và client vỡ.
public sealed record CreateMenuItemRequest(
    Guid MenuCategoryId,
    Guid ProductId,
    decimal Price,
    string? DisplayName,
    bool IsAvailable = true,
    bool IsFeatured = false,
    int SortOrder = 0,
    int? MaxQuantityPerOrder = null);

public sealed record UpdateMenuItemRequest(
    decimal Price,
    string? DisplayName,
    bool IsAvailable,
    bool IsFeatured,
    int SortOrder,
    int? MaxQuantityPerOrder);

public sealed record SetMenuItemAvailabilityRequest(bool IsAvailable);
