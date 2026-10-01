using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Behaviors;

internal class LoggingBehavior<TRequest, TResponse>
    (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest: notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var reqquestName = typeof(TRequest).Name;

        logger.LogInformation("Bắt đầu xử lý {RequestName}", reqquestName);
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();
        logger.LogInformation(
            "Đã xử lý xong {RequestName} với response:" +
            " {@Response} trong {ElapsedMilliseconds} ms",
            reqquestName,
            response,
            stopwatch.ElapsedMilliseconds);
        return response;
    }
}
