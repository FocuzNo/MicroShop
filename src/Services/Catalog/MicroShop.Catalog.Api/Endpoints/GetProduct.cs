using MediatR;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Catalog.Application.Products.GetProduct;

namespace MicroShop.Catalog.Api.Endpoints;

internal sealed class GetProduct : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "api/products/{id:guid}",
            async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetProductQuery(id),
                    cancellationToken);

                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .WithName("GetProduct")
            .WithTags("Products")
            .WithSummary("Get product")
            .WithDescription("Returns a Catalog product by identifier.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
