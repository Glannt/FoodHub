using FluentValidation;

namespace FoodHub.Application.MenuItems.Commands.CreateMenuItem;

public sealed class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");

        RuleFor(x => x.MenuCategoryId)
            .NotEmpty().WithMessage("MenuCategoryId là bắt buộc.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId là bắt buộc.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá không hợp lý.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200).WithMessage("Tên hiển thị tối đa 200 ký tự.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được âm.");

        RuleFor(x => x.MaxQuantityPerOrder)
            .GreaterThan(0).WithMessage("Số lượng tối đa mỗi đơn phải lớn hơn 0.");
    }
}
