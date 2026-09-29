using MicroShop.BuildingBlocks.Application;

namespace MicroShop.Inventory.Application.Inventory.UpdateInventory;

public sealed record UpdateInventoryCommand(
    Guid ProductId,
    int Quantity)
    : ICommand;
