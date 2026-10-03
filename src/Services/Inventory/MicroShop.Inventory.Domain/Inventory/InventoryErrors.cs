using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.Inventory.Domain.Inventory;

public static class InventoryErrors
{
    public static readonly Error ProductIdRequired = Error.Validation(
        "Inventory.ProductIdRequired",
        "The product identifier is required.");

    public static readonly Error QuantityCannotBeNegative = Error.Validation(
        "Inventory.QuantityCannotBeNegative",
        "Inventory quantity cannot be negative.");

    public static readonly Error QuantityMustBePositive = Error.Validation(
        "Inventory.QuantityMustBePositive",
        "Inventory quantity increase must be greater than zero.");

    public static Error NotFound(Guid productId) => Error.NotFound(
        "Inventory.NotFound",
        $"Inventory for product '{productId}' was not found.");

    public static Error AlreadyExists(Guid productId) => Error.Conflict(
        "Inventory.AlreadyExists",
        $"Inventory for product '{productId}' already exists.");

    public static readonly Error InsufficientQuantity = Error.Conflict(
        "Inventory.InsufficientQuantity",
        "There is not enough inventory to decrease the requested quantity.");
}

