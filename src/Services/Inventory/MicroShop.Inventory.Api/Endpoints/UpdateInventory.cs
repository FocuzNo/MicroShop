using MediatR;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Inventory.Application.Inventory.UpdateInventory;

namespace MicroShop.Inventory.Api.Endpoints;

internal sealed class UpdateInventory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "api/inventory/{productId:guid}",
            async (
                Guid productId,
                Request request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateInventoryCommand(
                    productId,
                    request.Quantity);

                var result = await sender.Send(
                    command,
                    cancellationToken);

                return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
            });
    }

    internal sealed record Request(int Quantity);
}
