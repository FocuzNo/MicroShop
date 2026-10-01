using MediatR;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Inventory.Application.Inventory.GetInventory;

namespace MicroShop.Inventory.Api.Endpoints;

internal sealed class GetInventory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "api/inventory/{productId:guid}",
            async (
                Guid productId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetInventoryQuery(productId),
                    cancellationToken);

                return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
            })
            .WithName("GetInventory")
            .WithTags("Inventory")
            .WithSummary("Get product inventory")
            .WithDescription("Returns the inventory state associated with a ProductId.")
            .Produces<InventoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
