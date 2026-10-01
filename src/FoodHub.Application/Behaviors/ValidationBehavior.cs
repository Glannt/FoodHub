using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using MediatR;
using ValidationException = FluentValidation.ValidationException;
namespace FoodHub.Application.Behaviors;

/// <summary>
/// Class này được sử dụng để validate các request trước khi được xử lý bởi handler.
/// Nó sẽ kiểm tra tất cả các validator được đăng ký cho request
/// và nếu có bất kỳ lỗi nào, nó sẽ ném ra một ValidationException.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <param name="validators"></param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
: IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if(validators.Any())
        {
            //1. tạo validation context mới từ request
            var validationContext = new ValidationContext<TRequest>(request);
            //2. thực hiện validate tất cả các validator
            var result = await Task.WhenAll(validators.Select(v => v.ValidateAsync(validationContext, cancellationToken)));
            //3. kiểm tra kết quả validate
            var failures = result
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToArray();
            if (failures.Length > 0)
            {
                throw new ValidationException(failures);
            }
        }
        return await next();
    }
}
