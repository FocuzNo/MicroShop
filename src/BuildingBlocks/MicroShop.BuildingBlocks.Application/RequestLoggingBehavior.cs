using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Application;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Handling {RequestType}",
            requestType);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();

            if (response is Result { IsFailure: true } result)
            {
                logger.LogWarning(
                    "Handled {RequestType} with failure {ErrorCode} in {ElapsedMilliseconds} ms",
                    requestType,
                    result.Error.Code,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }

            logger.LogInformation(
                "Handled {RequestType} in {ElapsedMilliseconds} ms",
                requestType,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogError(
                exception,
                "Unhandled exception while handling {RequestType} after {ElapsedMilliseconds} ms",
                requestType,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
