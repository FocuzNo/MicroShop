using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.Inventory.Domain.Inventory;

public sealed class InventoryItem : Entity
{
    private InventoryItem()
    {
    }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<InventoryItem> Create(
        Guid productId,
        DateTime createdAtUtc)
    {
        if (productId == Guid.Empty)
        {
            return InventoryErrors.ProductIdRequired;
        }

        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = 0,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };

        return inventoryItem;
    }

    public Result ChangeQuantity(
        int quantity,
        DateTime updatedAtUtc)
    {
        if (quantity < 0)
        {
            return Result.Failure(InventoryErrors.QuantityCannotBeNegative);
        }

        Quantity = quantity;
        UpdatedAtUtc = updatedAtUtc;
        return Result.Success();
    }

    public Result Increase(
        int quantity,
        DateTime updatedAtUtc)
    {
        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.QuantityMustBePositive);
        }

        Quantity += quantity;
        UpdatedAtUtc = updatedAtUtc;

        return Result.Success();
    }

    public Result Decrease(
        int quantity,
        DateTime updatedAtUtc)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                InventoryErrors.QuantityMustBePositive);
        }

        if (quantity > Quantity)
        {
            return Result.Failure(
                InventoryErrors.InsufficientQuantity);
        }

        Quantity -= quantity;
        UpdatedAtUtc = updatedAtUtc;

        return Result.Success();
    }
}
