using MicroShop.BuildingBlocks.Application;

namespace MicroShop.Inventory.Application.Inventory.GetInventory;

public sealed record GetInventoryQuery(Guid ProductId) : IQuery<InventoryResponse>;
