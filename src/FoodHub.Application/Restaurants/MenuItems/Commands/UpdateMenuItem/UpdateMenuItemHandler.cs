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
        // Load thật (không AsNoTracking) vì ta cần thay đổi entity để EF theo dõi và UPDATE.
        // Query thì ngược lại, chỉ đọc nên phải AsNoTracking.
        var menuItem = await dbContext.MenuItems
            .FirstOrDefaultAsync(i => i.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
            throw new NotFoundException($"món ăn {request.MenuItemId}.");

        // Không set thuộc tính trực tiếp: UpdateDetails() mới là nơi duy nhất được phép
        // đổi thông tin món, nên UpdatedAt và bất biến nghiệp vụ không thể bị bỏ qua.
        menuItem.UpdateDetails(
            request.Name,
            request.Description ?? string.Empty,
            request.Price,
            request.ImageUrl);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateMenuItemResult(menuItem.Id);
    }
}