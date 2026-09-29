using MediatR;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Catalog.Application.Products.CreateProduct;

namespace MicroShop.Catalog.Api.Endpoints;

internal sealed class CreateProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "api/products",
            async (
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateProductCommand(
                    request.Name,
                    request.Price);

                var result = await sender.Send(
                    command,
                    cancellationToken);

                return result.IsSuccess
                    ? Results.Created(
                        $"/api/products/{result.Value}",
                        new { result.Value })
                    : ApiResults.Problem(result);
            });
    }

    internal sealed record Request(
        string Name,
        decimal Price);
}
