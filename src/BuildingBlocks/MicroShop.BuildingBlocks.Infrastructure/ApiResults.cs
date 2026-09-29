using Microsoft.AspNetCore.Http;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Infrastructure;

public static class ApiResults
{
    public static IResult Problem(Result result)
    {
        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: result.Error.Code,
            detail: result.Error.Description);
    }
}
