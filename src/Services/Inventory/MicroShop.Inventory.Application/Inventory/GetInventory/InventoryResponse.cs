namespace MicroShop.Inventory.Application.Inventory.GetInventory;

public sealed record InventoryResponse(
    Guid Id,
    Guid ProductId,
    int Quantity,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
