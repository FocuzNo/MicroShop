using FluentValidation;
using MediatR;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Application;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(
                context,
                cancellationToken)));

        var failures = validationResults
            .SelectMany(validationResult => validationResult.Errors)
            .Where(failure => failure is not null)
            .Select(failure => failure.ErrorMessage)
            .Distinct()
            .ToArray();

        if (failures.Length == 0)
        {
            return await next(cancellationToken);
        }

        var error = Error.Validation(
            "Validation.Failed",
            string.Join(
                "; ",
                failures));

        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = responseType.GetGenericArguments()[0];
        var failure = typeof(Result)
            .GetMethod(
                nameof(Result.Failure),
                1,
                [typeof(Error)])!
            .MakeGenericMethod(valueType)
            .Invoke(
                null,
                [error])!;

        return (TResponse)failure;
    }
}
