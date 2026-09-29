using MicroShop.BuildingBlocks.Application;
using MicroShop.BuildingBlocks.Domain;
using MicroShop.Inventory.Application.Abstractions.Data;
using MicroShop.Inventory.Domain.Inventory;

namespace MicroShop.Inventory.Application.Inventory.GetInventory;

internal sealed class GetInventoryQueryHandler(IInventoryRepository inventoryRepository)
    : IQueryHandler<GetInventoryQuery, InventoryResponse>
{
    public async Task<Result<InventoryResponse>> Handle(
        GetInventoryQuery query,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryRepository.GetByProductIdAsync(
            query.ProductId,
            cancellationToken);

        if (inventoryItem is null)
        {
            return InventoryErrors.NotFound(query.ProductId);
        }

        return new InventoryResponse(
            inventoryItem.Id,
            inventoryItem.ProductId,
            inventoryItem.Quantity,
            inventoryItem.CreatedAtUtc,
            inventoryItem.UpdatedAtUtc);
    }
}
