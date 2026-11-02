using FluentValidation;

namespace FoodHub.Application.Restaurants.Commands.ApproveRestaurant;

public sealed class ApproveRestaurantValidator : AbstractValidator<ApproveRestaurantCommand>
{
    public ApproveRestaurantValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");
    }
}