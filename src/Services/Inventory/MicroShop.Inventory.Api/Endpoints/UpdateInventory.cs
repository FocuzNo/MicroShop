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
                UpdateInventoryRequest request,
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
            })
            .WithName("UpdateInventory")
            .WithTags("Inventory")
            .WithSummary("Update inventory quantity")
            .WithDescription("Sets the quantity of an existing inventory item to a non-negative value.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateInventoryRequest(int Quantity);
}
