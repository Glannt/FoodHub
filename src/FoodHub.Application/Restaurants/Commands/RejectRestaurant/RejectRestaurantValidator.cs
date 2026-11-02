using FluentValidation;

namespace FoodHub.Application.Restaurants.Commands.RejectRestaurant;

public sealed class RejectRestaurantValidator : AbstractValidator<RejectRestaurantCommand>
{
    public RejectRestaurantValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");
    }
}