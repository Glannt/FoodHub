using FoodHub.Application.Abstractions;
using FoodHub.Domain.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.MenuItems.Commands.CreateMenuItem;

internal sealed class CreateMenuItemHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateMenuItemCommand, CreateMenuItemResult>
{
    public async Task<CreateMenuItemResult> Handle(
        CreateMenuItemCommand request,
        CancellationToken cancellationToken)
    {
        // MenuCategory không có RestaurantId trực tiếp: phải đi qua
        // MenuCategory -> MenuVersion -> Menu mới biết nhóm món thuộc nhà hàng nào.
        // Gộp luôn bước kiểm tra "nhóm món có tồn tại" và "có đúng nhà hàng không"
        // trong một truy vấn để tránh 2 round-trip xuống DB.
        var category = await (
                from c in dbContext.MenuCategories
                join v in dbContext.MenuVersions on c.MenuVersionId equals v.Id
                join m in dbContext.Menus on v.MenuId equals m.Id
                where c.Id == request.MenuCategoryId && m.RestaurantId == request.RestaurantId
                select c)
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            throw new NotFoundException(
                $"nhóm món {request.MenuCategoryId} của nhà hàng {request.RestaurantId}.");

        // Product là sản phẩm gốc và cũng thuộc một nhà hàng. Kiểm tra cả ProductId lẫn
        // RestaurantId để không thể gắn sản phẩm của nhà hàng khác vào thực đơn này.
        var productExists = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == request.ProductId && p.RestaurantId == request.RestaurantId,
                cancellationToken);

        if (!productExists)
            throw new NotFoundException(
                $"sản phẩm {request.ProductId} của nhà hàng {request.RestaurantId}.");

        // AddItem là method duy nhất được phép tạo MenuItem: mọi guard (giá > 0,
        // sortOrder >= 0, maxQuantityPerOrder > 0) nằm trong Domain, không lặp lại ở đây.
        var menuItem = category.AddItem(
            request.ProductId,
            request.Price,
            request.DisplayName,
            request.IsAvailable,
            request.IsFeatured,
            request.SortOrder,
            request.MaxQuantityPerOrder);

        // Thêm tường minh vào DbSet: MenuCategory được load mà không Include MenuItems,
        // nên EF không tự phát hiện entity mới nằm trong collection navigation.
        dbContext.MenuItems.Add(menuItem);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateMenuItemResult(menuItem.Id);
    }
}
