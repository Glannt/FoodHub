using FluentValidation;

namespace FoodHub.Application.Restaurants.Commands.UpdateRestaurant;

/// <summary>
/// Validator cho UpdateRestaurantCommand.
/// </summary>
public sealed class UpdateRestaurantValidator : AbstractValidator<UpdateRestaurantCommand>
{
    public UpdateRestaurantValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId là bắt buộc.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên nhà hàng là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên nhà hàng tối đa 200 ký tự.");

        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
        {
            RuleFor(x => x.Slug)
                .MaximumLength(200).WithMessage("Đường dẫn (slug) tối đa 200 ký tự.")
                .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Đường dẫn (slug) chỉ được chứa chữ thường không dấu, số và dấu gạch ngang.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả nhà hàng tối đa 1000 ký tự.");
        });

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
            .MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.")
            .Matches(@"^(\+?[0-9]{9,15})$")
            .WithMessage("Số điện thoại không đúng định dạng (9 đến 15 chữ số).");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .MaximumLength(255).WithMessage("Email tối đa 255 ký tự.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Địa chỉ là bắt buộc.")
            .MaximumLength(500).WithMessage("Địa chỉ tối đa 500 ký tự.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Vĩ độ (Latitude) phải nằm trong khoảng -90 đến 90.");

        RuleFor(x => x.Longtitude)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Kinh độ (Longtitude) phải nằm trong khoảng -180 đến 180.");

        When(x => !string.IsNullOrWhiteSpace(x.LogoUrl), () =>
        {
            RuleFor(x => x.LogoUrl)
                .MaximumLength(500).WithMessage("Logo URL tối đa 500 ký tự.")
                .Must(BeValidUrl).WithMessage("Logo URL không đúng định dạng đường dẫn web (http/https).");
        });

        When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl), () =>
        {
            RuleFor(x => x.CoverImageUrl)
                .MaximumLength(500).WithMessage("Cover image URL tối đa 500 ký tự.")
                .Must(BeValidUrl).WithMessage("Cover image URL không đúng định dạng đường dẫn web (http/https).");
        });

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Giá trị đơn hàng tối thiểu không được âm.");
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
