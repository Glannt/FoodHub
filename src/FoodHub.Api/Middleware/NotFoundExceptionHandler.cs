using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FoodHub.Application.Abstractions;

namespace FoodHub.Api.Middleware;

// Exception handler chuyên biệt cho lỗi "không tìm thấy dữ liệu".
// Tại sao không bắt Exception chung? Vì mỗi loại lỗi trả về một HTTP status khác nhau
// (404 / 400 / 500). Tách từng handler theo loại lỗi giúp trả đúng mã lỗi
// mà không phải nhét if/else trong một handler nặng nề.
// IExceptionHandler trả về false = "không xử lý, để handler tiếp theo thử".
// Nhờ vậy các handler đăng ký sau vẫn có cơ hội xử lý nếu loại lỗi khác.
public sealed class NotFoundExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        // IProblemDetailsService do AddProblemDetails() đăng ký.
        // Nó ghi ra đúng định dạng RFC 9457 (application/problem+json) mà client nào cũng hiểu.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy dữ liệu",
                Detail = notFoundException.Message
            }
        });
    }
}