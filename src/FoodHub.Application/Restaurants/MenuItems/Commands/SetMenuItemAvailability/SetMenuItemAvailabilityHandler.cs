using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using FoodHub.Domain.Restaurants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;

internal sealed class SetMenuItemAvailabilityHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<SetMenuItemAvailabilityCommand, Unit>
{
    public async Task<Unit> Handle(
        SetMenuItemAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var menuItem = await dbContext.MenuItems
            .FirstOrDefaultAsync(i => i.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
            throw new NotFoundException($"món ăn {request.MenuItemId}.");

        // SetAvailability() là method của Domain nên UpdatedAt được chạm đúng;
        // nếu set IsAvailable trực tiếp, mọi thay đổi trạng thái sẽ mất dấu vết thời gian.
        menuItem.SetAvailability(request.IsAvailable);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Unit();
    }
}