using MediatR;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Inventory.Application.Inventory.IncreaseInventory;

namespace MicroShop.Inventory.Api.Endpoints;

internal sealed class IncreaseInventory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/inventory/{productId:guid}/increase",
                async (
                    Guid productId,
                    IncreaseInventoryRequest request,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new IncreaseInventoryCommand(
                        productId,
                        request.Quantity);

                    var result = await sender.Send(
                        command,
                        cancellationToken);

                    return result.IsSuccess
                        ? Results.NoContent()
                        : ApiResults.Problem(result);
                })
            .WithName("IncreaseInventory")
            .WithTags("Inventory")
            .WithSummary("Increase inventory quantity")
            .WithDescription("Increases the quantity of an existing inventory item.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    internal sealed record IncreaseInventoryRequest(int Quantity);
}

