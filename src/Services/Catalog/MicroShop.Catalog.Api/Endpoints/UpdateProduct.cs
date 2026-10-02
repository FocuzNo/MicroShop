using MediatR;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Catalog.Application.Products.UpdateProduct;

namespace MicroShop.Catalog.Api.Endpoints;

internal sealed class UpdateProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
                "api/products/{id:guid}",
                async (
                    Guid id,
                    UpdateProductRequest request,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new UpdateProductCommand(
                        id,
                        request.Name,
                        request.Price);

                    var result = await sender.Send(
                        command,
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.NoContent()
                        : ApiResults.Problem(result);
                })
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithSummary("Update product")
            .WithDescription("Updates the name and price of an existing Catalog product.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateProductRequest(
        string Name,
        decimal Price);
}
