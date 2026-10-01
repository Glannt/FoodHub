using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using FluentValidation;
using FoodHub.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Application;

public static class DependencyInjection
{
    //IServiceCollection là một interface trong .NET dùng để đăng ký các dịch vụ (services) mà ứng dụng sẽ sử dụng.
    //Nó cung cấp các phương thức để thêm, xóa và quản lý các dịch vụ trong container của ứng dụng.
    //Hàm AddApplication là một phương thức mở rộng (extension method) cho IServiceCollection,
    //cho phép bạn thêm các dịch vụ của ứng dụng vào container DI.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Excuting assembly là assembly hiện tại đang chạy, trong trường hợp này là FoodHub.Application
        var assembly = Assembly.GetExecutingAssembly();
        //MediatR là một thư viện trong .NET dùng để triển khai pattern Mediator,
        //giúp tách rời các thành phần trong ứng dụng và giảm sự phụ thuộc giữa chúng.
        //Cần sử dụng khi muốn gửi các yêu cầu (requests) và
        //nhận các phản hồi (responses) mà không cần biết ai sẽ xử lý chúng.
        services.AddMediatR(cfg =>
        {
            // RegisterServicesFromAssembly là một phương thức của MediatR
            // dùng để đăng ký tất cả các dịch vụ (services) trong assembly hiện tại.
            cfg.RegisterServicesFromAssembly(assembly);
            // AddOpenBehavior là một phương thức của MediatR
            // dùng để thêm các behavior (hành vi) mở (open behavior) vào pipeline.
            //behavior này đang được extent từ IPipelineBehavior<,>
            //và có thể được sử dụng để thực hiện các hành vi như logging,
            //validation, caching, v.v. check ở
            //src/FoodHub.Application/Behaviors/LoggingBehavior.cs
            //và src/FoodHub.Application/Behaviors/ValidationBehavior.cs
            //
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        }
            );
        services.AddValidatorsFromAssembly(assembly);
        return services;

    }
}
