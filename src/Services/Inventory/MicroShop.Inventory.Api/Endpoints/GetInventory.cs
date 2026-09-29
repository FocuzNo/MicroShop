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
            });
    }
}
