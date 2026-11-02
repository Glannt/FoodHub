using FluentValidation;
using FoodHub.Application.Restaurants.Commands.DeleteRestaurant;

// Validator chạy trong pipeline (ValidationBehavior) trước khi vào handler, nên handler
// luôn nhận dữ liệu đã qua kiểm tra và không phải if-defensive ở đầu mỗi hàm.
//
// PHẢI khai báo `public`: services.AddValidatorsFromAssembly() mặc định KHÔNG quét
// internal type. Một validator internal sẽ bị bỏ qua im lặng — ValidationBehavior vẫn
// chạy, vẫn thấy danh sách rỗng, và mọi dữ liệu sai đều lọt xuống handler.
public sealed class DeleteRestaurantValidator : AbstractValidator<DeleteRestaurantCommand>
{
    public DeleteRestaurantValidator()
    {
    }
}
