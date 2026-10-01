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
                CreateProductRequest request,
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
                        new CreateProductResponse(result.Value))
                    : ApiResults.Problem(result);
            })
            .WithName("CreateProduct")
            .WithTags("Products")
            .WithSummary("Create product")
            .WithDescription("Creates a Catalog product and persists its integration event through the transactional Outbox for asynchronous publication to Kafka.")
            .Produces<CreateProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateProductRequest(
        string Name,
        decimal Price);
}
