using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FoodHub.Application.Abstractions;

namespace FoodHub.Api.Middleware;

// Exception handler cho lỗi xung đột nghiệp vụ (409 Conflict).
// Ví dụ: tạo nhà hàng trùng đúng tên + địa chỉ + toạ độ với một nhà hàng đã có.
// Handler này chỉ "đóng vai" phần HTTP; quy tắc "trùng là 409" nằm ở tầng Application.
public sealed class ConflictExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ConflictException conflictException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Dữ liệu xung đột",
                Detail = conflictException.Message
            }
        });
    }
}