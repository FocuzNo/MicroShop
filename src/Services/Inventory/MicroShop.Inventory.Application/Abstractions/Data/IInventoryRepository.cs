using MicroShop.Inventory.Domain.Inventory;

namespace MicroShop.Inventory.Application.Abstractions.Data;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken);

    void Add(InventoryItem inventoryItem);
}
