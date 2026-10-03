using MicroShop.BuildingBlocks.Application;

namespace MicroShop.Inventory.Application.Inventory.DecreaseInventory;

public sealed record DecreaseInventoryCommand(
    Guid ProductId,
    int Quantity)
    : ICommand;
