using MicroShop.BuildingBlocks.Application;

namespace MicroShop.Inventory.Application.Inventory.IncreaseInventory;

public sealed record IncreaseInventoryCommand(
    Guid ProductId,
    int Quantity)
    : ICommand;
