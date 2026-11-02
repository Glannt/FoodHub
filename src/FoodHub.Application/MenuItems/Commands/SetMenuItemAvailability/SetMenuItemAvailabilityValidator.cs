using FluentValidation;

namespace FoodHub.Application.MenuItems.Commands.SetMenuItemAvailability;

// Không có rule cho IsAvailable: bool chỉ có 2 giá trị, cả hai đều hợp lệ về nghiệp vụ.
public sealed class SetMenuItemAvailabilityValidator : AbstractValidator<SetMenuItemAvailabilityCommand>
{
    public SetMenuItemAvailabilityValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");

        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId là bắt buộc.");
    }
}
