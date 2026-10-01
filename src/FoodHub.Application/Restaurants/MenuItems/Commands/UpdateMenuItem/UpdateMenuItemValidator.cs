using FluentValidation;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.UpdateMenuItem;

// Cùng bộ quy tắc với Create: hai thao tác dùng chung các guard của Domain
// (tên/mô tả không rỗng, giá > 0), tách validator riêng để message báo lỗi
// có thể tinh chỉnh theo từng use case mà không ảnh hưởng lẫn nhau.
public sealed class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId là bắt buộc.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên món là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên món tối đa 200 ký tự.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả là bắt buộc.")
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá không hợp lệ.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("URL ảnh tối đa 500 ký tự.");
    }
}