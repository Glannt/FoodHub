using FluentValidation;

namespace FoodHub.Application.Restaurants.MenuItems.Commands.SetMenuItemAvailability;

// Không có rule cho IsAvailable: bool chỉ có 2 giá trị, mọi giá trị đều hợp lệ về nghiệp vụ.
// Chỉ cần chặn Id rỗng để tránh truy vấn vô nghĩa xuống DB.
public sealed class SetMenuItemAvailabilityValidator : AbstractValidator<SetMenuItemAvailabilityCommand>
{
    public SetMenuItemAvailabilityValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId là bắt buộc.");
    }
}