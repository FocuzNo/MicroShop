using MicroShop.BuildingBlocks.Application;
using MicroShop.BuildingBlocks.Domain;
using MicroShop.Inventory.Application.Abstractions.Data;
using MicroShop.Inventory.Domain.Inventory;

namespace MicroShop.Inventory.Application.Inventory.UpdateInventory;

internal sealed class UpdateInventoryCommandHandler(
    IInventoryRepository inventoryRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateInventoryCommand>
{
    public async Task<Result> Handle(
        UpdateInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryRepository.GetByProductIdAsync(
            command.ProductId,
            cancellationToken);

        if (inventoryItem is null)
        {
            return Result.Failure(InventoryErrors.NotFound(command.ProductId));
        }

        var result = inventoryItem.ChangeQuantity(
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
