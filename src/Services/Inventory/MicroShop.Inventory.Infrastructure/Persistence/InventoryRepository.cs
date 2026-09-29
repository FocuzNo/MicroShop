using Microsoft.EntityFrameworkCore;
using MicroShop.Inventory.Application.Abstractions.Data;
using MicroShop.Inventory.Domain.Inventory;

namespace MicroShop.Inventory.Infrastructure.Persistence;

internal sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    public Task<InventoryItem?> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return dbContext.InventoryItems.SingleOrDefaultAsync(
            inventoryItem => inventoryItem.ProductId == productId,
            cancellationToken);
    }

    public void Add(InventoryItem inventoryItem)
    {
        dbContext.InventoryItems.Add(inventoryItem);
    }
}
