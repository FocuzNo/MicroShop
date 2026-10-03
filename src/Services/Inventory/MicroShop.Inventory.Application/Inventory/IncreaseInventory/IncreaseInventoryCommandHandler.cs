using MicroShop.BuildingBlocks.Application;
using MicroShop.BuildingBlocks.Domain;
using MicroShop.Inventory.Application.Abstractions.Data;
using MicroShop.Inventory.Domain.Inventory;

namespace MicroShop.Inventory.Application.Inventory.IncreaseInventory;

internal sealed class IncreaseInventoryCommandHandler(
    IInventoryRepository inventoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<IncreaseInventoryCommand>
{
    public async Task<Result> Handle(
        IncreaseInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryRepository.GetByProductIdAsync(
            command.ProductId,
            cancellationToken);

        if (inventoryItem is null)
        {
            return Result.Failure(InventoryErrors.NotFound(command.ProductId));
        }

        var result = inventoryItem.Increase(
            command.Quantity,
            dateTimeProvider.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
