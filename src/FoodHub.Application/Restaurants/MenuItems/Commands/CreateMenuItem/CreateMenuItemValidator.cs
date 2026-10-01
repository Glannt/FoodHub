using FluentValidation;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.CreateMenuItem;

// Validator chạy trong pipeline (ValidationBehavior) trước khi vào handler,
// nên handler luôn nhận dữ liệu đã qua kiểm tra và không phải if-defensive ở đầu mỗi hàm.
public sealed class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId là bắt buộc.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên món là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên món tối đa 200 ký tự.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Mô tả là bắt buộc.")
            .MaximumLength(1000).WithMessage("Mô tả tối đa 1000 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Giá không hợp lý.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("URL ảnh tối đa 500 ký tự.");
    }
}