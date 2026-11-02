using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.MenuItems.Commands.SetMenuItemAvailability;

internal sealed class SetMenuItemAvailabilityHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<SetMenuItemAvailabilityCommand>
{
    public async Task Handle(
        SetMenuItemAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        // Join lên tận Menu để xác nhận món thuộc đúng nhà hàng trên route.
        var menuItem = await (
                from i in dbContext.MenuItems
                join c in dbContext.MenuCategories on i.MenuCategoryId equals c.Id
                join v in dbContext.MenuVersions on c.MenuVersionId equals v.Id
                join m in dbContext.Menus on v.MenuId equals m.Id
                where i.Id == request.MenuItemId && m.RestaurantId == request.RestaurantId
                select i)
            .FirstOrDefaultAsync(cancellationToken);

        if (menuItem is null)
            throw new NotFoundException(
                $"món ăn {request.MenuItemId} của nhà hàng {request.RestaurantId}.");

        // SetAvailable() là method của Domain nên UpdatedAt được chạm đúng;
        // set trực tiếp IsAvailable sẽ bỏ qua Touch() và mất dấu vết thời gian.
        menuItem.SetAvailable(request.IsAvailable);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
