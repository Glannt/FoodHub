using FluentValidation;

namespace FoodHub.Application.MenuItems.Commands.UpdateMenuItem;

public sealed class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");

        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId là bắt buộc.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá không hợp lệ.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200).WithMessage("Tên hiển thị tối đa 200 ký tự.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị không được âm.");

        RuleFor(x => x.MaxQuantityPerOrder)
            .GreaterThan(0).WithMessage("Số lượng tối đa mỗi đơn phải lớn hơn 0.");
    }
}
