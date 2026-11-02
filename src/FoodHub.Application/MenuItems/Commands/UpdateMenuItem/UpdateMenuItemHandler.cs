using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.MenuItems.Commands.UpdateMenuItem;

internal sealed class UpdateMenuItemHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateMenuItemCommand, UpdateMenuItemResult>
{
    public async Task<UpdateMenuItemResult> Handle(
        UpdateMenuItemCommand request,
        CancellationToken cancellationToken)
    {
        // MenuItem không có RestaurantId trực tiếp: menu_item -> menu_category -> menu_version -> menu.
        // Join ngay trong truy vấn vừa xác nhận quyền sở hữu vừa trả về entity đang được tracked,
        // nên không cần thêm một round-trip kiểm tra riêng.
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

        // Update() là nơi duy nhất được phép đổi thông tin bán: nhờ vậy guard của Domain
        // (giá > 0, sortOrder >= 0, maxQuantityPerOrder > 0) và UpdatedAt luôn được áp dụng.
        menuItem.Update(
            request.Price,
            request.DisplayName,
            request.IsAvailable,
            request.IsFeatured,
            request.SortOrder,
            request.MaxQuantityPerOrder);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateMenuItemResult(menuItem.Id);
    }
}
