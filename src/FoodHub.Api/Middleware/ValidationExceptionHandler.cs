using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Middleware;

// Exception handler cho lỗi validate của FluentValidation.
// Validation chạy trong ValidationBehavior của MediatR (tầng Application), nên lỗi
// nổi lên ở tầng trên cùng và phải được Api dịch thành 400 Bad Request.
// Handler này chỉ "đóng vai" phần HTTP của response, còn logic validate vẫn nằm ở Application.
public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        // Gom lỗi theo tên property để client biết chính xác field nào sai
        // (thay vì chỉ nhận một câu "dữ liệu không hợp lệ" chung chung).
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            // Dùng HttpValidationProblemDetails chứ không dùng ProblemDetails thuần:
            // ProblemDetailsService có writer riêng cho lớp này, nên mảng lỗi theo từng field
            // mới được ghi ra JSON ({"errors": {"Name": ["..."]}}) đúng chuẩn RFC 9457.
            // Nếu dùng ProblemDetails thuần thì client chỉ nhận "Dữ liệu không hợp lệ" mà mất sạch lỗi field.
            ProblemDetails = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ"
            }
        });
    }
}